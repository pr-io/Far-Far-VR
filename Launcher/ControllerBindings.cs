using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace FarFarWestVR;

internal static class ControllerBindings
{
    private static readonly (string Action, string Original, string Corrected)[] Changes =
    [
        ("InputGrenade", "Gamepad_LeftTrigger", "Gamepad_RightStick_Down"),
        ("InputMap", "Gamepad_RightThumbstick", "Gamepad_Special_Left"),
        ("InputPing", "FFW_Gamepad_Unbound", "Gamepad_RightThumbstick"),
        ("InputAim", "Gamepad_RightStick_Down", "Gamepad_LeftTrigger")
    ];

    internal static string? Apply()
    {
        Process[] processes = Process.GetProcessesByName("FarFarWest-Win64-Shipping");
        bool gameRunning = processes.Length > 0;
        foreach (Process process in processes) process.Dispose();
        if (gameRunning) throw new IOException("Close Far Far West before changing controller mappings.");

        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FarFarWest", "Saved", "SaveGames", "EnhancedInputUserSettings.sav");
        if (!File.Exists(path))
            throw new IOException("The game's input save does not exist yet. Start Far Far West once, close it, then apply the corrected controls.");

        byte[] original = File.ReadAllBytes(path);
        byte[] updated = Patch(original);
        if (updated.SequenceEqual(original))
            return null;

        string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ", System.Globalization.CultureInfo.InvariantCulture);
        string backup = path + ".before-vr-input-" + stamp;
        string pending = path + ".farfarvr-pending";
        File.Copy(path, backup);
        if (!File.ReadAllBytes(path).SequenceEqual(original))
            throw new IOException("The input save changed during setup. No mapping changes were written; close the game and retry.");

        try
        {
            File.WriteAllBytes(pending, updated);
            if (!File.ReadAllBytes(path).SequenceEqual(original))
                throw new IOException("The input save changed during setup. No mapping changes were written; close the game and retry.");
            File.Move(pending, path, true);
        }
        finally
        {
            if (File.Exists(pending)) File.Delete(pending);
        }

        if (!File.ReadAllBytes(path).SequenceEqual(updated))
            throw new IOException("The updated input save did not verify. Restore the backup and retry.");
        return backup;
    }

    internal static byte[] Patch(byte[] data)
    {
        if (!data.AsSpan().StartsWith("GVAS"u8) ||
            !Contains(data, Encoding.ASCII.GetBytes("/Script/FarFarWest.FFWEnhancedInputUserSettings\0")))
            throw new InvalidDataException("This is not a recognized Far Far West input save. No changes were made.");

        byte[] result = data;
        var applied = new List<(byte[] Before, byte[] After)>();
        foreach (var (action, oldValue, newValue) in Changes)
        {
            byte[] before = Pair(action, oldValue);
            byte[] after = Pair(action, newValue);
            int beforeCount = Count(result, before);
            int afterCount = Count(result, after);
            if (beforeCount == 0 && afterCount == 1) continue;
            if (beforeCount != 1 || afterCount != 0)
                throw new InvalidDataException($"The '{action}' binding is different than expected. No changes were made.");
            result = ReplaceUnique(result, before, after);
            applied.Add((before, after));
        }

        byte[] reverse = result;
        foreach (var (before, after) in applied.AsEnumerable().Reverse())
            reverse = ReplaceUnique(reverse, after, before);
        if (!reverse.SequenceEqual(data))
            throw new InvalidDataException("Unrelated save data would change. No mappings were written.");
        return result;
    }

    private static byte[] Pair(string action, string key) => [.. FString(action), .. FString(key)];

    private static byte[] FString(string value)
    {
        byte[] text = Encoding.ASCII.GetBytes(value + '\0');
        byte[] result = new byte[sizeof(int) + text.Length];
        BinaryPrimitives.WriteInt32LittleEndian(result, text.Length);
        text.CopyTo(result, sizeof(int));
        return result;
    }

    private static bool Contains(byte[] data, byte[] value) => Count(data, value) > 0;

    private static int Count(byte[] data, byte[] value)
    {
        int count = 0;
        for (int i = 0; i <= data.Length - value.Length; i++)
            if (data.AsSpan(i, value.Length).SequenceEqual(value)) count++;
        return count;
    }

    private static byte[] ReplaceUnique(byte[] data, byte[] oldValue, byte[] newValue)
    {
        int found = -1;
        for (int i = 0; i <= data.Length - oldValue.Length; i++)
        {
            if (!data.AsSpan(i, oldValue.Length).SequenceEqual(oldValue)) continue;
            if (found >= 0) throw new InvalidDataException("An input mapping is ambiguous. No changes were made.");
            found = i;
        }
        if (found < 0) throw new InvalidDataException("An expected input mapping could not be found. No changes were made.");
        return
        [
            .. data.AsSpan(0, found),
            .. newValue,
            .. data.AsSpan(found + oldValue.Length)
        ];
    }
}
