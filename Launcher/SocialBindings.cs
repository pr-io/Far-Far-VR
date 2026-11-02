using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace FarFarWestVR;

internal static class SocialBindings
{
    static byte[] FString(string value) {
        byte[] text=Encoding.ASCII.GetBytes(value+"\0"), result=new byte[text.Length+4];
        BinaryPrimitives.WriteInt32LittleEndian(result,text.Length);
        text.CopyTo(result,4); return result;
    }
    static int FindOnce(byte[] data,byte[] pattern) {
        int first=data.AsSpan().IndexOf(pattern);
        if (first>=0 && data.AsSpan(first+1).IndexOf(pattern)>=0) throw new IOException("Ambiguous social binding; input save left unchanged.");
        return first;
    }
    static byte[] Replace(byte[] data,int at,int length,byte[] value) {
        byte[] result=new byte[data.Length-length+value.Length];
        data.AsSpan(0,at).CopyTo(result); value.CopyTo(result,at);
        data.AsSpan(at+length).CopyTo(result.AsSpan(at+value.Length)); return result;
    }
    public static bool IsValidVrInputSave(byte[] data) {
        if (!data.AsSpan().StartsWith("GVAS"u8) || data.AsSpan().IndexOf(
            "/Script/FarFarWest.FFWEnhancedInputUserSettings\0"u8)<0) return false;
        try {
            byte[] marker=FString("/Script/EnhancedInput.EnhancedPlayerMappableKeyProfile");
            int at=FindOnce(data,marker);
            if (at<0) return false;
            int pos=at+marker.Length;
            string ReadString() {
                if (pos+4>data.Length) throw new IOException("Truncated input profile.");
                int length=BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos)); pos+=4;
                if (length<1 || length>512 || pos+length>data.Length || data[pos+length-1]!=0)
                    throw new IOException("Unrecognized input profile string.");
                string value=Encoding.ASCII.GetString(data,pos,length-1); pos+=length;
                return value;
            }
            if (!ReadString().StartsWith("EnhancedPlayerMappableKeyProfile_",StringComparison.Ordinal) ||
                pos+4>data.Length) return false;
            int count=BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos)); pos+=4;
            if (count<1 || count>256) return false;
            bool vrGrenade=false,vrAim=false,desktopBindings=false;
            for (int i=0;i<count;i++) {
                string action=ReadString(),key=ReadString();
                _=ReadString();_=ReadString();
                if (pos+6>data.Length) return false;
                pos+=6;
                if (action=="InputGrenade" && key=="Gamepad_RightStick_Down") vrGrenade=true;
                if (action=="InputAim" && key=="Gamepad_LeftTrigger") vrAim=true;
                if ((action=="InputGrenade" && key=="Gamepad_LeftTrigger") ||
                    (action=="InputAim" && key=="Gamepad_RightStick_Down") ||
                    (action=="InputDash" && key=="Gamepad_FaceButton_Left") ||
                    (action=="InputReload" && key=="Gamepad_FaceButton_Right"))
                    desktopBindings=true;
            }
            return ReadString()=="InputUserSettings.Profiles.Default" &&
                vrGrenade && vrAim && !desktopBindings;
        }
        catch (IOException) {
            return false;
        }
    }
    public static byte[] Patch(byte[] original) {
        if (!original.AsSpan().StartsWith("GVAS"u8) || original.AsSpan().IndexOf(
            "/Script/FarFarWest.FFWEnhancedInputUserSettings\0"u8)<0)
            throw new IOException("Unrecognized game input save; left unchanged.");
        byte[] result=original;
        var changed=new List<(byte[] Before,byte[] After)>();
        foreach (var (action,key) in new[]{("emote","Gamepad_DPad_Left"),("InputBattleCry","Gamepad_DPad_Right")}) {
            byte[] before=[..FString(action),..FString("FFW_Gamepad_Unbound")];
            byte[] after=[..FString(action),..FString(key)];
            int oldAt=FindOnce(result,before), newAt=FindOnce(result,after);
            if (oldAt<0 && newAt>=0) continue;
            if (oldAt<0 || newAt>=0) throw new IOException("Unexpected "+action+" binding; input save left unchanged.");
            result=Replace(result,oldAt,before.Length,after); changed.Add((before,after));
        }
        byte[] reverse=result;
        foreach (var pair in changed.AsEnumerable().Reverse())
            reverse=Replace(reverse,FindOnce(reverse,pair.After),pair.After.Length,pair.Before);
        if (!reverse.AsSpan().SequenceEqual(original)) throw new IOException("Unrelated input data changed; save left unchanged.");
        return AddVoiceBinding(result);
    }
    // UE's custom key-profile archive: count followed by four FStrings and
    // six bytes per key override. Parse the complete table before inserting.
    static byte[] AddVoiceBinding(byte[] data) {
        byte[] marker=FString("/Script/EnhancedInput.EnhancedPlayerMappableKeyProfile");
        int at=FindOnce(data,marker);
        if (at<0) throw new IOException("Missing key profile; input save left unchanged.");
        int pos=at+marker.Length;
        string ReadString() {
            if (pos+4>data.Length) throw new IOException("Truncated input profile.");
            int len=BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos)); pos+=4;
            if (len<1 || len>512 || pos+len>data.Length || data[pos+len-1]!=0) throw new IOException("Unrecognized input profile string.");
            string value=Encoding.ASCII.GetString(data,pos,len-1); pos+=len; return value;
        }
        if (!ReadString().StartsWith("EnhancedPlayerMappableKeyProfile_",StringComparison.Ordinal)) throw new IOException("Unexpected key profile object.");
        int countAt=pos;
        if (pos+4>data.Length) throw new IOException("Truncated input profile count.");
        int count=BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos)); pos+=4;
        if (count<1 || count>256) throw new IOException("Unexpected input profile count.");
        byte[]? template=null; int voiceCount=0; bool grenadeMoved=false;
        for (int i=0;i<count;i++) {
            int begin=pos; string action=ReadString(),key=ReadString();
            string device=ReadString(),hardware=ReadString();
            if (pos+6>data.Length) throw new IOException("Truncated input profile record.");
            var metadata=data.AsSpan(pos,6); pos+=6;
            if (action=="InputGrenade" && key=="Gamepad_RightStick_Down" && metadata.SequenceEqual(new byte[]{0,0,0,0,0,1})) grenadeMoved=true;
            if (action!="InputPushToTalk" && key=="Gamepad_DPad_Up") throw new IOException("Push-to-talk key is already bound; input save left unchanged.");
            if (action=="InputPushToTalk") {
                voiceCount++;
                if (key!="Gamepad_DPad_Up") throw new IOException("Unexpected push-to-talk binding; input save left unchanged.");
            }
            if (action=="InputChat" && key=="FFW_Gamepad_Unbound" && device=="None" && hardware=="None" &&
                metadata.SequenceEqual(new byte[]{0,0,0,0,0,1})) {
                if (template!=null) throw new IOException("Ambiguous input profile template.");
                template=data.AsSpan(begin,pos-begin).ToArray();
            }
        }
        int tableEnd=pos;
        if (ReadString()!="InputUserSettings.Profiles.Default") throw new IOException("Unexpected input profile boundary.");
        // D-pad Up is the default grenade key. Its accepted VR override must
        // already have freed it, otherwise a voice hold could throw a grenade.
        if (!grenadeMoved) throw new IOException("Existing VR grenade binding required before adding voice control.");
        if (voiceCount==1) return data;
        if (voiceCount!=0 || template==null) throw new IOException("Unrecognized voice binding table; input save left unchanged.");
        byte[] oldPair=[..FString("InputChat"),..FString("FFW_Gamepad_Unbound")];
        byte[] newPair=[..FString("InputPushToTalk"),..FString("Gamepad_DPad_Up")];
        byte[] record=Replace(template,0,oldPair.Length,newPair);
        byte[] result=Replace(data,tableEnd,0,record);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(countAt),count+1);
        byte[] reverse=Replace(result,tableEnd,record.Length,Array.Empty<byte>());
        BinaryPrimitives.WriteInt32LittleEndian(reverse.AsSpan(countAt),count);
        if (!reverse.AsSpan().SequenceEqual(data)) throw new IOException("Unrelated input data changed; save left unchanged.");
        return result;
    }
    public static bool Repair(string path) {
        if (!File.Exists(path)) return false;
        byte[] original=File.ReadAllBytes(path), updated=Patch(original);
        if (original.AsSpan().SequenceEqual(updated)) return false;
        var running=Process.GetProcessesByName("FarFarWest-Win64-Shipping");
        bool active=running.Length>0; foreach (var process in running) process.Dispose();
        if (active) throw new IOException("Close Far Far West to repair its social and voice bindings.");
        if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original)) throw new IOException("Input save changed; retry with the game closed.");
        string pending=path+".social-pending";
        File.WriteAllBytes(pending,updated);
        File.Replace(pending,path,path+".before-social-controls-"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ"));
        return true;
    }
}
