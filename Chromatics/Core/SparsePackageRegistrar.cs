using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace Chromatics.Core
{
    // Registers Chromatics' sparse signed AppX package on startup so the Win32
    // install gains package identity. The AmbientLightingServer that fronts
    // Windows Dynamic Lighting only accepts background lighting controllers
    // from packaged processes declaring the com.microsoft.windows.lighting
    // AppExtension — without this registration Chromatics can only paint DL
    // devices while it has foreground focus, which is useless during gameplay.
    //
    // Idempotent, per-user, no admin needed. Silently no-ops on:
    //   - Windows builds older than 22621 (TargetDeviceFamily MinVersion in the manifest)
    //   - Builds without a bundled Chromatics.appx (dev/debug runs, unsigned local builds)
    //   - Registration errors — logged and swallowed so foreground DL still works
    // 19041 = Win10 2004 — required for AddPackageOptions.ExternalLocationUri,
    // the sparse-package mechanism we use. Callers must guard with
    // OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) or carry the same
    // [SupportedOSPlatform] attribute or the CA1416 analyzer will flag them.
    [SupportedOSPlatform("windows10.0.19041.0")]
    public static class SparsePackageRegistrar
    {
        // Must match Identity.Name + Publisher in Resources/SparsePackage/AppxManifest.xml.
        // Windows looks the registered package up by these two fields below.
        private const string PackageName      = "com.logicallysynced.Chromatics";
        private const string PackagePublisher = "CN=Danielle Thompson";
        private const string AppxFileName     = "Chromatics.appx";

        // Synchronous wrapper safe to call from STAThread Main. Wraps the async
        // call in Task.Run to dispatch to the thread pool and avoid the
        // sync-over-async deadlock that bites WinRT calls on the STA.
        public static void EnsureRegistered()
        {
            try
            {
                Task.Run(EnsureRegisteredAsync).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] EnsureRegistered failed at top level: {ex.GetType().Name} — {ex.Message}");
                Debug.WriteLine(ex);
            }
        }

        public static async Task EnsureRegisteredAsync()
        {
            try
            {
                var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
                if (string.IsNullOrEmpty(exeDir))
                {
                    Logger.WriteVerbose("[SparsePackage] Skipped: Environment.ProcessPath is empty");
                    return;
                }

                var appxPath = Path.Combine(exeDir, AppxFileName);
                if (!File.Exists(appxPath))
                {
                    Logger.WriteVerbose($"[SparsePackage] Skipped: {AppxFileName} not bundled with this install (debug/unsigned build)");
                    return;
                }

                var currentAppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                                        ?? new Version(0, 0, 0, 0);

                PackageManager pm;
                try
                {
                    pm = new PackageManager();
                }
                catch (PlatformNotSupportedException ex)
                {
                    Logger.WriteVerbose($"[SparsePackage] Skipped: PackageManager unsupported on this OS — {ex.Message}");
                    return;
                }

                // Already registered at the same version? No-op.
                // FindPackagesForUser("") = current user. Filtering by name + publisher
                // sidesteps having to compute the publisher hash for the family name.
                var existing = pm.FindPackagesForUser(string.Empty, PackageName, PackagePublisher);
                foreach (var pkg in existing)
                {
                    var v = pkg.Id.Version;
                    var installed = new Version(v.Major, v.Minor, v.Build, v.Revision);
                    if (installed == currentAppVersion)
                    {
                        Logger.WriteVerbose($"[SparsePackage] Already registered at {installed}; no action needed");
                        return;
                    }
                }

                Logger.WriteVerbose($"[SparsePackage] Registering {currentAppVersion} from {appxPath} (external location {exeDir})");

                var options = new AddPackageOptions
                {
                    // file:/// URI of the directory containing Chromatics.exe — Windows
                    // resolves the unpackaged binaries referenced by the manifest from here.
                    ExternalLocationUri = new Uri(exeDir),
                    // Allows registering 4.2.14 over a previously-installed 4.2.13 without
                    // the caller having to compare versions first.
                    ForceUpdateFromAnyVersion = true,
                };

                var op = pm.AddPackageByUriAsync(new Uri(appxPath), options);
                var result = await op.AsTask().ConfigureAwait(false);
                if (result.ExtendedErrorCode != null)
                {
                    Logger.WriteVerbose(
                        $"[SparsePackage] Registration failed: {result.ErrorText} " +
                        $"(HRESULT 0x{result.ExtendedErrorCode.HResult:X8})");
                    return;
                }
                Logger.WriteVerbose("[SparsePackage] Registered successfully");
            }
            catch (COMException ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Registration failed: COM HRESULT 0x{ex.HResult:X8} — {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Registration failed: {ex.GetType().Name} — {ex.Message}");
                Debug.WriteLine(ex);
            }
        }

        // Called from Velopack's OnBeforeUninstallFastCallback — must run sync
        // and exit fast (no UI, no network) because Velopack kills the process
        // shortly after the callback returns. Removes the sparse package entry
        // so Chromatics stops appearing in Settings → Personalization →
        // Dynamic Lighting → Background light control after uninstall.
        public static void Deregister()
        {
            try
            {
                Task.Run(DeregisterAsync).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Deregister failed at top level: {ex.GetType().Name} — {ex.Message}");
                Debug.WriteLine(ex);
            }
        }

        public static async Task DeregisterAsync()
        {
            try
            {
                PackageManager pm;
                try
                {
                    pm = new PackageManager();
                }
                catch (PlatformNotSupportedException)
                {
                    return;
                }

                var existing = pm.FindPackagesForUser(string.Empty, PackageName, PackagePublisher);
                foreach (var pkg in existing)
                {
                    Logger.WriteVerbose($"[SparsePackage] Removing {pkg.Id.FullName} for current user");
                    var op = pm.RemovePackageAsync(pkg.Id.FullName, RemovalOptions.None);
                    var result = await op.AsTask().ConfigureAwait(false);
                    if (result.ExtendedErrorCode != null)
                    {
                        Logger.WriteVerbose(
                            $"[SparsePackage] Remove failed: {result.ErrorText} " +
                            $"(HRESULT 0x{result.ExtendedErrorCode.HResult:X8})");
                    }
                }
            }
            catch (COMException ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Deregister failed: COM HRESULT 0x{ex.HResult:X8} — {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Deregister failed: {ex.GetType().Name} — {ex.Message}");
                Debug.WriteLine(ex);
            }
        }
    }
}
