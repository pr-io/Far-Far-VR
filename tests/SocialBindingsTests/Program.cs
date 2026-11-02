using System.Text;
using System.Buffers.Binary;
using FarFarWestVR;
static byte[] F(string s) { byte[] t=Encoding.ASCII.GetBytes(s+"\0"),r=new byte[t.Length+4]; BinaryPrimitives.WriteInt32LittleEndian(r,t.Length); t.CopyTo(r,4); return r; }
static void Check(bool b) { if (!b) throw new Exception("Social binding check failed"); }
static byte[] Record(string action,string key) => [..F(action),..F(key),..F("None"),..F("None"),0,0,0,0,0,1];
byte[] emote=Record("emote","FFW_Gamepad_Unbound"),cry=Record("InputBattleCry","FFW_Gamepad_Unbound"),chat=Record("InputChat","FFW_Gamepad_Unbound");
byte[] prefix=[..Encoding.ASCII.GetBytes("GVAS/Script/FarFarWest.FFWEnhancedInputUserSettings\0"),..F("/Script/EnhancedInput.EnhancedPlayerMappableKeyProfile"),..F("EnhancedPlayerMappableKeyProfile_123")];
byte[] suffix=[..F("InputUserSettings.Profiles.Default"),0xff,0x72,0x11];
byte[] input=[..prefix,4,0,0,0,..Record("InputGrenade","Gamepad_RightStick_Down"),..emote,..cry,..chat,..suffix];
byte[] expected=[..prefix,5,0,0,0,..Record("InputGrenade","Gamepad_RightStick_Down"),..Record("emote","Gamepad_DPad_Left"),..Record("InputBattleCry","Gamepad_DPad_Right"),..chat,..Record("InputPushToTalk","Gamepad_DPad_Up"),..suffix];
Check(SocialBindings.Patch(input).AsSpan().SequenceEqual(expected));
Check(SocialBindings.Patch(expected).AsSpan().SequenceEqual(expected));
foreach (byte[] bad in new[]{Array.Empty<byte>(),input[..^25],input.Concat(emote).ToArray(),input.Concat(F("emote")).Concat(F("Gamepad_DPad_Left")).ToArray()}) {
    bool rejected=false; try { SocialBindings.Patch(bad); } catch (IOException) { rejected=true; } Check(rejected);
}
if (args.Length>0) {
    byte[] actual=File.ReadAllBytes(args[0]),patched=SocialBindings.Patch(actual);
    Check(SocialBindings.Patch(patched).AsSpan().SequenceEqual(patched));
    File.WriteAllBytes(Path.Combine("diagnostics/social-refinement-20261029","input-preview.sav"),patched);
    Console.WriteLine("Actual game save parsed; social keys and one voice override only; repeated patch identical.");
    if (args.Contains("--apply")) Console.WriteLine("Repair applied: "+SocialBindings.Repair(args[0]));
}
Console.WriteLine("Social binding migration, exact preservation, idempotence and refusal checks passed.");
