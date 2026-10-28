using System.Buffers.Binary;
using System.Text;

var original = MakeSave(
    ("InputGrenade", "Gamepad_LeftTrigger"),
    ("InputMap", "Gamepad_RightThumbstick"),
    ("InputPing", "FFW_Gamepad_Unbound"),
    ("InputAim", "Gamepad_RightStick_Down"));
var patched = FarFarWestVR.ControllerBindings.Patch(original);
Assert(!patched.SequenceEqual(original), "The expected mappings should change.");
Assert(Contains(patched, Pair("InputGrenade", "Gamepad_RightStick_Down")), "Utility mapping was not corrected.");
Assert(Contains(patched, Pair("InputMap", "Gamepad_Special_Left")), "Map mapping was not corrected.");
Assert(Contains(patched, Pair("InputPing", "Gamepad_RightThumbstick")), "Ping mapping was not corrected.");
Assert(Contains(patched, Pair("InputAim", "Gamepad_LeftTrigger")), "Aim mapping was not corrected.");
Assert(Contains(patched, "unrelated-save-content"u8.ToArray()), "Unrelated save content was not preserved.");
Assert(FarFarWestVR.ControllerBindings.Patch(patched).SequenceEqual(patched), "Applying the mapping twice should be idempotent.");

AssertThrows<InvalidDataException>(() => FarFarWestVR.ControllerBindings.Patch("not-a-save"u8.ToArray()), "Unknown save should be rejected.");
var unexpected = MakeSave(
    ("InputGrenade", "Gamepad_LeftTrigger"),
    ("InputMap", "Gamepad_RightThumbstick"),
    ("InputPing", "FFW_Gamepad_Unbound"),
    ("InputAim", "Gamepad_RightTrigger"));
AssertThrows<InvalidDataException>(() => FarFarWestVR.ControllerBindings.Patch(unexpected), "Unexpected mapping should be rejected.");
Assert(Contains(unexpected, Pair("InputAim", "Gamepad_RightTrigger")), "Rejected save data should remain unchanged.");
Console.WriteLine("Controller mapping smoke checks passed.");

static byte[] MakeSave(params (string Action, string Value)[] bindings)
{
    using var stream = new MemoryStream();
    stream.Write("GVAS"u8);
    stream.Write(Encoding.ASCII.GetBytes("/Script/FarFarWest.FFWEnhancedInputUserSettings\0"));
    stream.Write("unrelated-save-content"u8);
    foreach (var (action, value) in bindings) stream.Write(Pair(action, value));
    return stream.ToArray();
}

static byte[] Pair(string action, string key) => [.. FString(action), .. FString(key)];

static byte[] FString(string value)
{
    byte[] text = Encoding.ASCII.GetBytes(value + '\0');
    byte[] result = new byte[sizeof(int) + text.Length];
    BinaryPrimitives.WriteInt32LittleEndian(result, text.Length);
    text.CopyTo(result, sizeof(int));
    return result;
}

static bool Contains(byte[] source, byte[] pattern)
{
    for (int i = 0; i <= source.Length - pattern.Length; i++)
        if (source.AsSpan(i, pattern.Length).SequenceEqual(pattern)) return true;
    return false;
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertThrows<TException>(Action action, string message) where TException : Exception
{
    try { action(); }
    catch (TException) { return; }
    throw new InvalidOperationException(message);
}
