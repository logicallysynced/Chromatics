using Chromatics.Enums;
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
        // PackageName must match Identity.Name + Publisher in the bundled
        // AppxManifest.xml (or AppxManifest.Portable.xml for the portable
        // build). publish.py sets -p:PortableBuild=true on the portable's
        // dotnet publish pass, which defines PORTABLE_BUILD and embeds
        // app.fusion.portable.manifest in the exe — so the installer's
        // and portable's SparsePackageRegistrar each register their own
        // distinct package family and the two installs don't trample each
        // other's fusion-identity binding on the same machine.
#if PORTABLE_BUILD
        private const string PackageName      = "com.logicallysynced.Chromatics.Portable";
#else
        private const string PackageName      = "com.logicallysynced.Chromatics";
#endif
        private const string PackagePublisher = "CN=Chromatics Maintainer, O=Chromatics Maintainer, L=Earth, S=Earth, C=AU";
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

        // Reports whether the running process has been granted package
        // identity (i.e. whether Windows.ApplicationModel.Package.Current is
        // available). Bound at CreateProcess time by the OS loader scanning
        // the embedded fusion manifest — never changes during the lifetime
        // of a process. Program.cs uses this on startup to decide whether
        // to relaunch after a fresh sparse-package registration: the process
        // that performs the registration started BEFORE the package existed,
        // so its identity is forever absent; a relaunched copy of the same
        // exe will pick up the binding.
        public static bool HasPackageIdentity()
        {
            try
            {
                _ = Windows.ApplicationModel.Package.Current;
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Logs whether the current process has package identity. The fusion
        // manifest embedded in Chromatics.exe (see app.manifest <msix> element)
        // is what wires the running process to the registered sparse package;
        // if it's missing or malformed, this returns null and background
        // Dynamic Lighting won't work no matter what's registered in Settings.
        // Logged once at startup as a diagnostic — visible in the Console tab
        // so users can verify the identity binding without digging into logs.
        public static void LogPackageIdentity()
        {
            try
            {
                var pkg = Windows.ApplicationModel.Package.Current;
                Logger.WriteVerbose($"[SparsePackage] Process has package identity: {pkg.Id.FullName}");
            }
            catch (InvalidOperationException)
            {
                Logger.WriteVerbose("[SparsePackage] Process has NO package identity — background Dynamic Lighting will not work (foreground only)");
                LogEmbeddedManifestCheck();

                // Only surface a user-visible hint on Console when this install
                // *expected* to be packaged — i.e. when the bundled Chromatics
                // .appx exists alongside the exe. Debug/unsigned builds skip
                // the registrar entirely (no .appx), so there's no value in
                // telling those users that DL background access is missing —
                // it was never intended to work for that build.
                if (HasBundledAppx())
                {
                    Logger.WriteConsole(LoggerTypes.Devices,
                        "[DynamicLighting] Background access is unavailable — Chromatics will only paint Dynamic Lighting devices while it has foreground focus. " +
                        "Try Settings → Advanced → Reset Chromatics to re-register the sparse package, then relaunch.");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Identity check failed: {ex.GetType().Name} — {ex.Message}");
            }
        }

        // True when Chromatics.appx ships alongside Chromatics.exe — i.e.
        // when this is a release / portable / installer build that
        // *expects* to be packaged. Debug builds and IDE F5 runs ship
        // without the .appx, so package identity will never bind for
        // them no matter what; the identity check + relaunch flow in
        // Program.cs gates on this to avoid (a) the noisy first-chance
        // InvalidOperationException from Package.Current, and (b) the
        // automatic relaunch that detaches the VS debugger.
        public static bool HasBundledAppx()
        {
            try
            {
                var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
                return !string.IsNullOrEmpty(exeDir) && File.Exists(Path.Combine(exeDir, AppxFileName));
            }
            catch
            {
                return false;
            }
        }

        // Compares the package's registered external path against the directory
        // the current exe expects to register under. Returns false on mismatch
        // (forces re-registration). Treats unreadable EffectiveExternalPath as
        // a mismatch so old packages registered without an external location
        // get refreshed too. Path comparison is case-insensitive and normalises
        // trailing slashes via Path.GetFullPath.
        private static bool ExternalPathMatches(Windows.ApplicationModel.Package pkg, string expectedDir)
        {
            string registered = SafeExternalPath(pkg);
            if (string.IsNullOrEmpty(registered)) return false;
            try
            {
                var normRegistered = Path.GetFullPath(registered).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var normExpected   = Path.GetFullPath(expectedDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(normRegistered, normExpected, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string SafeExternalPath(Windows.ApplicationModel.Package pkg)
        {
            try { return pkg.EffectiveExternalPath ?? string.Empty; }
            catch { return string.Empty; }
        }

        // Byte-searches the running .exe for the fusion-manifest msix namespace
        // string. Distinguishes "build embedded the manifest correctly but the
        // loader still didn't bind identity" (namespace present) from "build
        // stripped the <msix> element so the loader never had a chance"
        // (namespace absent). Only called when Package.Current already failed.
        private static void LogEmbeddedManifestCheck()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    Logger.WriteVerbose("[SparsePackage] Cannot inspect manifest: ProcessPath empty or missing");
                    return;
                }

                var bytes = File.ReadAllBytes(exePath);
                var marker = System.Text.Encoding.UTF8.GetBytes("urn:schemas-microsoft-com:msix.v1");
                bool nsFound = ContainsBytes(bytes, marker);

                var pkgMarker = System.Text.Encoding.UTF8.GetBytes(PackageName);
                bool pkgNameFound = ContainsBytes(bytes, pkgMarker);

                Logger.WriteVerbose($"[SparsePackage] Embedded manifest check on {exePath}: msix-namespace={nsFound}, packageName={pkgNameFound}");
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[SparsePackage] Manifest content check failed: {ex.GetType().Name} — {ex.Message}");
            }
        }

        private static bool ContainsBytes(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0 || haystack.Length < needle.Length) return false;
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) { match = false; break; }
                }
                if (match) return true;
            }
            return false;
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

                // External location must cover Velopack's full launch chain.
                // The portable / installed layout puts Chromatics.exe
                // (ExecutionStub) at the parent folder and current\Chromatics.exe
                // (the real app) one level down. When the stub launches the
                // inner from OUTSIDE the external location, the OS won't bind
                // identity to the child and Windows escalates to a hard
                // launch failure after a few attempts. Walking up to the
                // parent when we're inside a current\ folder puts both the
                // stub and the inner inside the external location, so
                // identity binding propagates correctly through stub-mediated
                // launches. Dev builds (no current\ folder) use exeDir
                // directly.
                var externalLocationDir = exeDir;
                if (string.Equals(Path.GetFileName(exeDir), "current", StringComparison.OrdinalIgnoreCase))
                {
                    var parent = Path.GetDirectoryName(exeDir);
                    if (!string.IsNullOrEmpty(parent))
                        externalLocationDir = parent;
                }

                // Already registered at the same version AND for this exe's path?
                // No-op. The path check matters when the user has both the
                // installer build and the portable build on the same machine —
                // sparse-package identity is bound to ExternalLocationUri, so
                // a package registered for path A won't grant identity to a
                // process started from path B. Without this, switching between
                // builds leaves background DL silently broken until the user
                // hits Reset Chromatics. FindPackagesForUser("") = current
                // user; filtering by name + publisher sidesteps having to
                // compute the publisher hash for the family name.
                var existing = pm.FindPackagesForUser(string.Empty, PackageName, PackagePublisher);
                foreach (var pkg in existing)
                {
                    var v = pkg.Id.Version;
                    var installed = new Version(v.Major, v.Minor, v.Build, v.Revision);
                    bool versionMatch = installed == currentAppVersion;
                    bool pathMatch = ExternalPathMatches(pkg, externalLocationDir);
                    if (versionMatch && pathMatch)
                    {
                        Logger.WriteVerbose($"[SparsePackage] Already registered at {installed} for {externalLocationDir}; no action needed");
                        return;
                    }
                    if (!versionMatch)
                        Logger.WriteVerbose($"[SparsePackage] Re-registering: installed version {installed} differs from app {currentAppVersion}");
                    if (!pathMatch)
                        Logger.WriteVerbose($"[SparsePackage] Re-registering: registered external path '{SafeExternalPath(pkg)}' differs from current exe location '{externalLocationDir}'");
                    break;
                }

                Logger.WriteVerbose($"[SparsePackage] Registering {currentAppVersion} from {appxPath} (external location {externalLocationDir})");

                var options = new AddPackageOptions
                {
                    ExternalLocationUri = new Uri(externalLocationDir),
                    // Allows registering 4.2.14 over a previously-installed 4.2.13 without
                    // the caller having to compare versions first.
                    ForceUpdateFromAnyVersion = true,
                };

                var op = pm.AddPackageByUriAsync(new Uri(appxPath), options);
                var result = await op.AsTask().ConfigureAwait(false);
                if (result.ExtendedErrorCode != null)
                {
                    Logger.WriteVerbose($"[SparsePackage] Registration failed: {result.ErrorText} (HRESULT 0x{result.ExtendedErrorCode.HResult:X8})");
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
                        Logger.WriteVerbose($"[SparsePackage] Remove failed: {result.ErrorText} (HRESULT 0x{result.ExtendedErrorCode.HResult:X8})");
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
