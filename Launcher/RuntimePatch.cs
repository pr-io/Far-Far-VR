using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace FarFarWestVR;

// Runs only during setup/update. No callback, monitor or gameplay overhead.
internal static class RuntimePatch
{
    internal const string PatchFile = "backend.ffvrdelta";
    internal const string PatchHash = "6A4E0FF8FC42E94B0FD5F8C1FBC3C41FFDA846C5B621F3318C89AD65FC41C43D";
    internal const int TargetSize = 15811072;
    private static readonly Dictionary<string, string> Required = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UEVRBackend.dll"] = Setup.CurrentBackendHash,
        ["LuaVR.dll"] = Setup.CompatibleLuaVrHash,
        ["openxr_loader.dll"] = Setup.CompatibleOpenXrLoaderHash,
        ["openvr_api.dll"] = Setup.CompatibleOpenVrApiHash
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct DeltaInput { public IntPtr Start; public nuint Size; public int Editable; }
    [StructLayout(LayoutKind.Sequential)]
    private struct DeltaOutput { public IntPtr Start; public nuint Size; }
    [DllImport("msdelta.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ApplyDeltaB(long flags, DeltaInput source, DeltaInput delta, out DeltaOutput output);
    [DllImport("msdelta.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeltaFree(IntPtr memory);

    private static void Verify(byte[] data, string expected, string name)
    {
        if (!Convert.ToHexString(SHA256.HashData(data)).Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new IOException(name + " failed its integrity check. No installed runtime files were changed.");
    }

    internal static byte[] Reconstruct(byte[] source, byte[] delta)
    {
        Verify(source, Setup.BaseBackendHash, "Upstream engine");
        Verify(delta, PatchHash, "Far Far VR runtime patch");
        var sourcePin = GCHandle.Alloc(source, GCHandleType.Pinned);
        var deltaPin = GCHandle.Alloc(delta, GCHandleType.Pinned);
        DeltaOutput output = default;
        try
        {
            if (!ApplyDeltaB(0,
                new DeltaInput { Start = sourcePin.AddrOfPinnedObject(), Size = (nuint)source.Length },
                new DeltaInput { Start = deltaPin.AddrOfPinnedObject(), Size = (nuint)delta.Length }, out output))
                throw new IOException("Windows could not apply the Far Far VR runtime patch.", new Win32Exception(Marshal.GetLastWin32Error()));
            if (output.Size != TargetSize) throw new IOException("The reconstructed runtime has an unexpected size.");
            byte[] result = new byte[TargetSize];
            Marshal.Copy(output.Start, result, 0, result.Length);
            Verify(result, Setup.CurrentBackendHash, "Customized engine");
            return result;
        }
        finally
        {
            if (output.Start != IntPtr.Zero) DeltaFree(output.Start);
            deltaPin.Free(); sourcePin.Free();
        }
    }

    internal static void InstallArchive(string archivePath, string patchPath, string runtimeFolder, CancellationToken cancellation = default)
    {
        // Pin the complete upstream input as well as each final component.
        using (var archiveFile = File.OpenRead(archivePath))
            if (Convert.ToHexString(SHA256.HashData(archiveFile)) != Setup.UevrRuntimeArchiveHash)
                throw new IOException("The upstream runtime archive failed its integrity check.");
        string runtime = Path.GetFullPath(runtimeFolder).TrimEnd(Path.DirectorySeparatorChar);
        string parent = Path.GetDirectoryName(runtime) ?? throw new IOException("Invalid runtime folder.");
        Directory.CreateDirectory(parent);
        string staging = Path.Combine(parent, ".farfarvr-runtime-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        var promoted = new List<(string Target, string? Backup)>();
        bool completed = false, rolledBack = false;
        try
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var file in Required)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var matches = archive.Entries.Where(entry => entry.Name.Equals(file.Key, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matches.Length != 1 || matches[0].Length > 100_000_000)
                        throw new IOException("The upstream archive has an invalid runtime component: " + file.Key);
                    using var content = matches[0].Open();
                    using var memory = new MemoryStream();
                    content.CopyTo(memory);
                    byte[] data = memory.ToArray();
                    if (file.Key == "UEVRBackend.dll") data = Reconstruct(data, File.ReadAllBytes(patchPath));
                    Verify(data, file.Value, file.Key);
                    File.WriteAllBytes(Path.Combine(staging, file.Key), data);
                }
            }
            cancellation.ThrowIfCancellationRequested();
            Directory.CreateDirectory(runtime);
            // All components have passed verification before the first installed file changes.
            foreach (string name in Required.Keys)
            {
                cancellation.ThrowIfCancellationRequested();
                string source = Path.Combine(staging, name), target = Path.Combine(runtime, name);
                string? backup = File.Exists(target) ? Path.Combine(staging, name + ".rollback") : null;
                if (backup is null) File.Move(source, target);
                else File.Replace(source, target, backup);
                promoted.Add((target, backup));
            }
            completed = true;
        }
        catch (Exception failure)
        {
            try
            {
                // Preserve the previous installation if a component is locked or promotion fails.
                for (int i = promoted.Count - 1; i >= 0; --i)
                {
                    var change = promoted[i];
                    if (change.Backup is null) File.Delete(change.Target);
                    else File.Copy(change.Backup, change.Target, overwrite: true);
                }
                rolledBack = true;
            }
            catch (Exception rollbackFailure)
            {
                throw new AggregateException("Runtime setup failed and recovery backups were retained at " + staging, failure, rollbackFailure);
            }
            throw;
        }
        finally
        {
            // On a rollback failure, retain backups instead of deleting the recovery copy.
            if (completed || rolledBack || promoted.Count == 0) Directory.Delete(staging, recursive: true);
        }
    }
}
