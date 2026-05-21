using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Helpers;
using Chromatics.ViewModels;
using Chromatics.Views.Dialogs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Chromatics.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EnsureTaskbarIconLoaded();
            DataContext = new MainWindowViewModel();
            Opened += OnOpened;
            Closed += OnClosed;
            Closing += OnClosing;
        }

        // Defensive re-load of the window icon. The Icon="avares://..."
        // attribute on the AXAML root is resolved through Avalonia's asset
        // pipeline at parse time; if that resolution races (notably after
        // FirstRunDialog tear-down, or when the styled-element graph is
        // still warming up on cold start) it lands as null and Windows
        // shows the default blank taskbar entry instead of our icon.
        // Re-opening the asset stream and assigning a fresh WindowIcon
        // after InitializeComponent() bypasses whatever the parser saw
        // and lets the window register the real icon before Show() runs.
        private void EnsureTaskbarIconLoaded()
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri("avares://Chromatics/Resources/Chromatics_icon_128x128.png"));
                Icon = new WindowIcon(stream);
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[MainWindow] Could not re-load taskbar icon: {ex.GetType().Name} — {ex.Message}");
            }
        }

        // Three-stage taskbar icon fix.
        //
        // STAGE 1 — PKEY_AppUserModel_ID. Velopack hard-codes the process-level
        // AUMID to "velopack.Chromatics" early in startup. The Win11 taskbar
        // keys its icon off the AUMID, looking for a Start Menu shortcut whose
        // target AUMID matches. Portable extractions have no such shortcut, so
        // the taskbar caches a blank entry and never falls back to the window
        // HICON. Setting PKEY_AppUserModel_ID on this window's property store
        // overrides the process AUMID for THIS window with one the shell has
        // never seen — no cached entry, no broken shortcut binding, so the
        // taskbar falls through to Stage 2 + 3 cleanly.
        //
        // STAGE 2 — PKEY_AppUserModel_RelaunchIconResource. Points the shell at
        // "<exe>,0" (first icon resource in Chromatics.exe) for this window's
        // taskbar entry and jump-list relaunch icon.
        //
        // STAGE 3 — WM_SETICON. Sets the window's own HICON pair from the EXE's
        // embedded application icon. Title bar and Alt-Tab thumbnail read this
        // directly; the Win11 taskbar uses it as the final fallback once the
        // AUMID lookup chain (Stages 1 + 2) lands somewhere with no shortcut.
        //
        // All three stages run from OnOpened so the HWND exists and is
        // registered with the shell before we touch any icon path.
        private void ForceTaskbarIcon()
        {
            var platformHandle = TryGetPlatformHandle();
            if (platformHandle == null || platformHandle.Handle == IntPtr.Zero)
            {
                Logger.WriteVerbose("[MainWindow] ForceTaskbarIcon: no platform handle yet, skipping");
                return;
            }
            var hwnd = platformHandle.Handle;
            var exe = Environment.ProcessPath;

            // Stages 1 + 2: AUMID override + RelaunchIconResource on this
            // window's property store. Both writes share one IPropertyStore.
            try
            {
                var iid = NativeRelaunch.IID_IPropertyStore;
                int hr = NativeRelaunch.SHGetPropertyStoreForWindow(hwnd, ref iid, out var propStore);
                if (hr != 0 || propStore == null)
                {
                    Logger.WriteVerbose($"[MainWindow] SHGetPropertyStoreForWindow returned HRESULT 0x{hr:X8}, skipping AUMID + relaunch-icon stages");
                }
                else
                {
                    try
                    {
                        // Fresh AUMID the shell has no cached binding for. Distinct
                        // from Velopack's "velopack.Chromatics" so we get a brand-new
                        // taskbar identity. Portable + installer split so they don't
                        // collide if both are launched on the same machine.
                        const string aumid =
#if PORTABLE_BUILD
                            "com.logicallysynced.Chromatics.Portable.MainWindow";
#else
                            "com.logicallysynced.Chromatics.MainWindow";
#endif
                        SetStringProperty(propStore, NativeRelaunch.PKEY_AppUserModel_ID, aumid, "PKEY_AppUserModel_ID");

                        if (!string.IsNullOrEmpty(exe))
                        {
                            var iconResource = $"{exe},0";
                            SetStringProperty(propStore, NativeRelaunch.PKEY_AppUserModel_RelaunchIconResource, iconResource, "PKEY_AppUserModel_RelaunchIconResource");
                        }

                        int commitHr = propStore.Commit();
                        Logger.WriteVerbose($"[MainWindow] IPropertyStore.Commit returned 0x{commitHr:X8}");
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(propStore);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[MainWindow] Property-store path failed: {ex.GetType().Name} — {ex.Message}");
            }

            // Stage 3: WM_SETICON from EXE's embedded application icon.
            try
            {
                if (!string.IsNullOrEmpty(exe))
                {
                    var extracted = NativeIcon.ExtractIconEx(exe, 0, out var bigIcon, out var smallIcon, 1);
                    Logger.WriteVerbose($"[MainWindow] ExtractIconEx returned {extracted}, small=0x{smallIcon.ToInt64():X}, big=0x{bigIcon.ToInt64():X}");
                    try
                    {
                        if (smallIcon != IntPtr.Zero)
                            NativeIcon.SendMessage(hwnd, NativeIcon.WM_SETICON, (IntPtr)NativeIcon.ICON_SMALL, smallIcon);
                        if (bigIcon != IntPtr.Zero)
                            NativeIcon.SendMessage(hwnd, NativeIcon.WM_SETICON, (IntPtr)NativeIcon.ICON_BIG, bigIcon);
                    }
                    finally
                    {
                        // WM_SETICON copies the HICON internally, so the originals
                        // can be released right after.
                        if (smallIcon != IntPtr.Zero) NativeIcon.DestroyIcon(smallIcon);
                        if (bigIcon   != IntPtr.Zero) NativeIcon.DestroyIcon(bigIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[MainWindow] WM_SETICON path failed: {ex.GetType().Name} — {ex.Message}");
            }
        }

        private static void SetStringProperty(NativeRelaunch.IPropertyStore propStore, NativeRelaunch.PROPERTYKEY pkey, string value, string label)
        {
            var pwsz = Marshal.StringToCoTaskMemUni(value);
            try
            {
                var pv = new NativeRelaunch.PROPVARIANT { vt = NativeRelaunch.VT_LPWSTR, valuePtr = pwsz };
                var keyLocal = pkey;
                int setHr = propStore.SetValue(ref keyLocal, ref pv);
                Logger.WriteVerbose($"[MainWindow] {label} = '{value}' (SetValue=0x{setHr:X8})");
            }
            finally
            {
                Marshal.FreeCoTaskMem(pwsz);
            }
        }

        private static class NativeIcon
        {
            public const uint WM_SETICON = 0x0080;
            public const int  ICON_SMALL = 0;
            public const int  ICON_BIG   = 1;

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyIcon(IntPtr hIcon);
        }

        private static class NativeRelaunch
        {
            public const ushort VT_LPWSTR = 31;

            public static readonly Guid IID_IPropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");

            // PKEY_AppUserModel_RelaunchIconResource — see
            // https://learn.microsoft.com/windows/win32/properties/props-system-appusermodel-relauniconresource
            public static readonly PROPERTYKEY PKEY_AppUserModel_RelaunchIconResource = new PROPERTYKEY
            {
                fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                pid = 3,
            };

            // PKEY_AppUserModel_ID — see
            // https://learn.microsoft.com/windows/win32/properties/props-system-appusermodel-id
            public static readonly PROPERTYKEY PKEY_AppUserModel_ID = new PROPERTYKEY
            {
                fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                pid = 5,
            };

            [StructLayout(LayoutKind.Sequential)]
            public struct PROPERTYKEY
            {
                public Guid fmtid;
                public uint pid;
            }

            // PROPVARIANT in C is a 24-byte struct on x64 (2-byte vt + 6 bytes
            // of reserved padding + 16-byte union). We only need the VT_LPWSTR
            // case here, which stores a pointer at union offset 0.
            [StructLayout(LayoutKind.Explicit, Size = 24)]
            public struct PROPVARIANT
            {
                [FieldOffset(0)] public ushort vt;
                [FieldOffset(2)] public ushort wReserved1;
                [FieldOffset(4)] public ushort wReserved2;
                [FieldOffset(6)] public ushort wReserved3;
                [FieldOffset(8)] public IntPtr valuePtr;
            }

            [ComImport]
            [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IPropertyStore
            {
                [PreserveSig] int GetCount(out uint cProps);
                [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);
                [PreserveSig] int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
                [PreserveSig] int SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
                [PreserveSig] int Commit();
            }

            [DllImport("shell32.dll")]
            public static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore propStore);
        }

        private async void OnOpened(object sender, EventArgs e)
        {
            ForceTaskbarIcon();

            // Match Fm_MainWindow's bring-up order so backend subsystems initialize
            // exactly as they did under WinForms. Settings are already loaded by
            // Program.Main. Offload BOTH RGBController.Setup (device enumeration)
            // AND KeyController.Setup to a background task — KeyController's
            // SetWindowsHookEx path touches Process.MainModule, which blocks
            // for hundreds of ms to seconds on Windows while the OS resolves
            // the loaded-module list, freezing the UI and tab switching.
            Logger.WriteConsole(LoggerTypes.System, "Chromatics is starting up..");

            await Task.Run(() =>
            {
                Chromatics.Core.SentryService.RunInstrumented(
                    "app.startup",
                    "Chromatics backend init (keyboard hook + RGB provider load)",
                    () =>
                    {
                        // LoadMappings runs first so it's populated BEFORE
                        // RGBController.Setup fires DeviceConnectionChanged.
                        // Off the UI thread because it deserialises the whole
                        // layers.chromatics4 file — the single largest UI-thread
                        // blocker during startup. LoadMappings uses
                        // ConcurrentDictionary internally so concurrent reads
                        // from the UI thread are safe.
                        if (!Chromatics.Layers.MappingLayers.LoadMappings())
                        {
                            Logger.WriteConsole(LoggerTypes.System, "No layer file found. Defaults will be created per device.");
                        }
                        KeyController.Setup();
                        RGBController.Setup();
                    });
            });
            GameController.Setup();

            // Defer the VM population until after the first full layout/render
            // cycle has completed. If we run InitializeAfterRgb inline here, the
            // Mappings tab content (an unrealized TabItem on the first pass) is
            // still mid-measure when we mutate its bound ObservableCollections
            // — the container generator ends up producing blank rows until
            // something forces a re-realization (theme toggle, tray-hide/show).
            // Loaded priority fires after layout settles, so the ItemsControls
            // see a populated collection from their first container pass.
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () => (DataContext as MainWindowViewModel)?.InitializeAfterRgb(),
                Avalonia.Threading.DispatcherPriority.Loaded);

            if (AppSettings.GetSettings().checkupdates)
                _ = CheckForUpdateAsync();
        }

        private async Task CheckForUpdateAsync()
        {
            // Fire-and-forget at the call site (`_ = CheckForUpdateAsync()`), so
            // any exception that escapes here lands in the UnobservedTaskException
            // path and Sentry captures it as an unhandled crash. Wrap the whole
            // body so the worst case is a verbose-log line, not a beta-channel
            // Sentry event.
            try
            {
                var includeBeta = AppSettings.GetSettings().betaChannel;
                var result = await UpdateService.CheckAsync(includeBeta);
                if (result == null) return;

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    // If the user hid the window to the tray between the check
                    // kickoff and the result landing, ShowDialog throws "Cannot
                    // show window with non-visible owner". The next launch will
                    // re-detect the same update, so skip silently this run.
                    if (!IsVisible) return;

                    var dialog = new UpdateDialog(result);
                    await dialog.ShowDialog(this);
                });
            }
            catch (Exception ex)
            {
                Logger.WriteVerbose($"[MainWindow] CheckForUpdateAsync skipped: {ex.GetType().Name} — {ex.Message}");
            }
        }

        private void OnClosing(object sender, WindowClosingEventArgs e)
        {
            // Mirror the old Fm_MainWindow behavior: if minimize-to-tray is enabled
            // and the window is being closed by the user (red X), hide to tray
            // instead. Explicit shutdown (tray → Close menu) uses
            // desktop.Shutdown() and this handler is bypassed.
            var settings = AppSettings.GetSettings();
            if (!settings.minimizetray) return;
            if (e.CloseReason != WindowCloseReason.WindowClosing) return;

            e.Cancel = true;
            Hide();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            GameController.Exit();
            KeyController.Stop();
            RGBController.Unload();

            (DataContext as MainWindowViewModel)?.Dispose();

            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }

            // desktop.Shutdown() ends Avalonia's loop but background threads
            // (Sentry.Profiling EventPipe, RGB.NET update timer that wasn't
            // fully torn down by RGBController.Unload, Sharlayan polling,
            // Hue update trigger) keep the process alive — and Environment.Exit
            // can stall on Sentry's AppDomain.ProcessExit flush handler. Without
            // an explicit Process.Kill the "closed" Chromatics survives as a
            // zombie that holds the single-instance mutex AND Sentry's envelope
            // cache, causing both "Already running" prompts on the next launch
            // AND silent crash-event drops. SentryService.Shutdown does a sync
            // 3-second flush first so any pending events leave the wire before
            // we kill the process.
            //
            // Flush the OLE clipboard first so anything the user copied via
            // Ctrl+C in the Console (or the Copy All button) materialises
            // into the system clipboard and survives Process.Kill. Avalonia
            // uses OLE delayed rendering for its clipboard writes; without
            // this, paste-after-close returns empty.
            try { Chromatics.Helpers.ClipboardHelper.FlushOleClipboard(); } catch { }
            try { Chromatics.Core.SentryService.Shutdown(); } catch { }
            try { Process.GetCurrentProcess().Kill(); } catch { }
        }

        private void OnHelpClick(object sender, RoutedEventArgs e)
        {
            const string url = "https://docs.chromaticsffxiv.com/chromatics-4";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
