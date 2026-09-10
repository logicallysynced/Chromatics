using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using Chromatics.Extensions.RGB.NET.Devices.PlayStation;
using RGB.NET.Devices.PlayStation;
using Chromatics.Extensions.RGB.NET.Devices.Alienware;
using Chromatics.Extensions.RGB.NET.Devices.DynamicLighting;
using Chromatics.Extensions.RGB.NET.Devices.QmkRawHid;
using Chromatics.Extensions.RGB.NET.Devices.Yeelight;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.Models;
using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.CoolerMaster;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.Msi;
using RGB.NET.Devices.Novation;
using RGB.NET.Devices.OpenRGB;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Chromatics.Core
{
    public static class RGBController
    {
        // Fires whenever a device is added or removed. The Avalonia Mapping view
        // subscribes to keep its device list and keycap layouts in sync.
        public static event EventHandler DeviceConnectionChanged;

        private static RGBSurface surface = new RGBSurface();

        private static bool _loaded;

        private static List<IRGBDeviceProvider> loadedDeviceProviders = new List<IRGBDeviceProvider>();

        private static readonly System.Threading.Lock _devicesLock = new();
        private static Dictionary<Guid, IRGBDevice> _devices = new Dictionary<Guid, IRGBDevice>();

        private static readonly System.Threading.Lock _activeDevicesLock = new();
        private static Dictionary<IRGBDevice, bool> _activeDevices = new Dictionary<IRGBDevice, bool>();

        // Per-device brightness corrections, keyed by the device GUID stamped in
        // by DevicesChanged. Lazily created on first device-add and reused if
        // the same device is re-attached. Composes multiplicatively with the
        // global GlobalBrightnessCorrection so the global value remains the
        // master cap (e.g. global=50, perDevice=80 → 40% effective).
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, PerDeviceBrightnessCorrection> _perDeviceBrightness
            = new System.Collections.Concurrent.ConcurrentDictionary<Guid, PerDeviceBrightnessCorrection>();

        // Tagged-effect tracking for animations that need targeted teardown
        // (e.g. when the user toggles "Startup Animation" off mid-cycle, we
        // need to stop ONLY the rainbow groups, not every running effect).
        // Keys are short, hardcoded category names ("startup", "title");
        // values are the ListLedGroups that belong to that animation.
        private static readonly Dictionary<string, List<ListLedGroup>> _taggedEffects = new();
        // Parallel index keyed by source device — lets the EffectLayer per-device
        // toggle attach/detach a single device's tagged group without disturbing
        // the rest of the rig. Populated by the deviceGuid-aware overload of
        // RegisterTaggedEffect; kept in sync by StopTaggedEffects /
        // DetachTaggedEffectForDevice.
        private static readonly Dictionary<string, Dictionary<Guid, ListLedGroup>> _taggedEffectsByDevice = new();
        private static readonly System.Threading.Lock _taggedEffectsLock = new();

        // ConcurrentDictionary because the game-loop thread mutates this while
        // RaidEffectProcessor / GoldSaucerVegas read it from the RGB.NET timer
        // thread via surface.Updating.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, ListLedGroup[]> _layergroups
            = new System.Collections.Concurrent.ConcurrentDictionary<int, ListLedGroup[]>();

        private static List<Led> _layergroupledcollection = new List<Led>();

        private static PaletteColorModel _colorPalette = new PaletteColorModel();

        private static EffectTypesModel _effects = new EffectTypesModel();

        private static readonly System.Threading.Lock _runningEffectsLock = new();
        private static List<ListLedGroup> _runningEffects = new List<ListLedGroup>();

        private static bool _baseLayerEffectRunning;

        private static RGBSurface.ExceptionEventHandler surfaceExceptionEventHandler;

        private static EventHandler<ExceptionEventArgs> deviceExceptionEventHandler;

        // Detects "Failed to initialize Logitech-SDK." (the exact phrase
        // RGB.NET.Devices.Logitech surfaces when LogiLedInit returns false)
        // and appends a follow-up console line pointing the user at G HUB.
        // Called from every place a Logitech load failure can surface:
        // the surface.Exception event, the provider.Exception event, and
        // the synchronous catch inside LoadDeviceProvider. The Logitech SDK
        // failure can route through any of those depending on whether the
        // provider raises sync or async, so this helper covers all three.
        private static void LogLogitechSdkHintIfNeeded(Exception ex)
        {
            if (ex == null) return;
            if (!ex.Message.Contains("Failed to initialize Logitech-SDK", StringComparison.OrdinalIgnoreCase)) return;
            Logger.WriteConsole(Enums.LoggerTypes.Devices,
                "[Logitech] The Logitech LightSync SDK didn't load. The SDK only loads while Logitech G HUB is running. Open G HUB on this machine, then re-enable the Logitech provider. If G HUB is already open, try restarting it.");
        }

        private static TimerUpdateTrigger _timerUpdateTrigger;

        private const double IdleUpdateFrequency = 0.05; // 20 Hz

        // Sets the surface tick rate from live state: the idle rate applies
        // only while the setting is on AND the game is genuinely detached.
        // Derived from GameController.IsGameConnected rather than a flag the
        // callers pass, because RunStartupEffects (which used to pass
        // idle:true) also runs on device hot-plug and re-enable while the
        // game is connected - trusting the flag throttled mid-game. With
        // the setting off, idle is always false, so the configured rate
        // runs in every state.
        public static void ApplyUpdateRate()
        {
            if (_timerUpdateTrigger == null) return;

            var settings = AppSettings.GetSettings();
            bool idle = settings.idleRefreshWhenDisconnected && !GameController.IsGameConnected();
            _timerUpdateTrigger.UpdateFrequency = idle ? IdleUpdateFrequency : settings.rgbRefreshRate;
        }

        // Called when the user flips the idle-refresh setting so the change
        // lands immediately instead of on the next connect or disconnect.
        public static void ReapplyIdleUpdateRate() => ApplyUpdateRate();

        // Runs one provider's load block and turns assembly-load faults into
        // console guidance instead of a startup crash. Windows App Control
        // (Smart App Control / WDAC) blocks the unsigned vendor DLLs on some
        // machines (0x800711C7) - Chromatics signs only its own binaries by
        // policy, so the RGB.NET / HueApi assemblies are fair game for the
        // policy. The provider code sits in a lambda on purpose: lambda
        // bodies compile to their own methods, so a blocked assembly
        // resolves when the lambda RUNS (inside this try) instead of when
        // Setup itself is JIT-compiled - the JIT-time fault happens at
        // Setup's call site, outside every catch, and crashed startup as
        // CHROMATICS-17 / CHROMATICS-18.
        // Thin wrapper over the shared guard so the provider blocks below
        // stay readable. The lambda-isolation rule is documented on
        // Helpers.AssemblyLoadGuard.
        private static void TryLoadProviderIsolated(string label, Action load)
            => Helpers.AssemblyLoadGuard.TryRun(label, load);

        public static void Setup()
        {
            try
            {
                //Bind to console
                Logger.WriteConsole(Enums.LoggerTypes.Devices, @"Looking for RGB Devices..");

                var appSettings = AppSettings.GetSettings();

                //Setup Exception Events
                // RGB.NET surfaces provider initialisation failures through
                // either the surface.Exception event or the provider's
                // Exception event (depends on the provider; Logitech goes
                // through the surface). Both handlers append the same
                // Logitech-SDK hint when the message matches, so the user
                // sees the G HUB tip regardless of which channel fires.
                surfaceExceptionEventHandler = args_ =>
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Device Error: {args_.Exception.Message}", forwardToSentry: false);
                    LogLogitechSdkHintIfNeeded(args_.Exception);
                };
                deviceExceptionEventHandler = (sender, e) =>
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"Device Error: {e.Exception.Message}", forwardToSentry: false);
                    LogLogitechSdkHintIfNeeded(e.Exception);
                };

                surface.Exception += surfaceExceptionEventHandler;

            
                if (appSettings.deviceLogitechEnabled)
                {
                    TryLoadProviderIsolated("Logitech", () => LoadDeviceProvider(LogitechDeviceProvider.Instance));
                }


                if (appSettings.deviceCorsairEnabled)
                {
                    TryLoadProviderIsolated("Corsair", () =>
                    {
                        var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                        var natives = CorsairDeviceProvider.PossibleX64NativePaths;
                        natives.Add($"{enviroment}\\x64\\CUESDK.dll");

                        Debug.WriteLine($"{enviroment}\\x64\\CUESDK.dll");

                        // Ask iCUE for exclusive lighting control. Without this,
                        // iCUE keeps painting its own profile in parallel and our
                        // writes fight the SDK's background animation thread,
                        // which manifests as flicker or partial colour reverts on
                        // some boards. Static on RGB.NET's CorsairDeviceProvider
                        // and read once when the provider is initialised, so set
                        // it before LoadDeviceProvider runs.
                        CorsairDeviceProvider.ExclusiveAccess = true;

                        LoadDeviceProvider(CorsairDeviceProvider.Instance);
                    });
                }


                if (appSettings.deviceCoolermasterEnabled)
                {
                    TryLoadProviderIsolated("CoolerMaster", () => LoadDeviceProvider(CoolerMasterDeviceProvider.Instance));
                }


                if (appSettings.deviceNovationEnabled)
                {
                    TryLoadProviderIsolated("Novation", () => LoadDeviceProvider(NovationDeviceProvider.Instance));
                }


                if (appSettings.deviceRazerEnabled)
                {
                    TryLoadProviderIsolated("Razer", () =>
                    {
                        if (AppSettings.GetSettings().showEmulatorDevices)
                            RazerDeviceProvider.Instance.LoadEmulatorDevices = RazerEndpointType.All;

                        #if DEBUG
                            RazerDeviceProvider.Instance.LoadEmulatorDevices = RazerEndpointType.All;
                        #endif

                        LoadDeviceProvider(RazerDeviceProvider.Instance);
                    });
                }

                if (appSettings.deviceAsusEnabled)
                {
                    TryLoadProviderIsolated("ASUS", () => LoadDeviceProvider(AsusDeviceProvider.Instance));
                }


                if (appSettings.deviceMsiEnabled)
                {
                    TryLoadProviderIsolated("MSI", () => LoadDeviceProvider(MsiDeviceProvider.Instance));
                }


                if (appSettings.deviceSteelseriesEnabled)
                {
                    TryLoadProviderIsolated("SteelSeries", () => LoadDeviceProvider(SteelSeriesDeviceProvider.Instance));
                }


                if (appSettings.deviceWootingEnabled)
                {
                    TryLoadProviderIsolated("Wooting", () => LoadDeviceProvider(WootingDeviceProvider.Instance));
                }

                if (appSettings.deviceOpenRGBEnabled)
                {
                    TryLoadProviderIsolated("OpenRGB", () =>
                    {
                        // IP comes from settings.chromatics4 (hidden field — not
                        // exposed in the UI). Defaults to 127.0.0.1 for the
                        // local-SDK-server case; users on a multi-machine setup
                        // can point Chromatics at a remote server by editing
                        // openRgbServerIp directly.
                        var ip = string.IsNullOrWhiteSpace(appSettings.openRgbServerIp)
                            ? "127.0.0.1"
                            : appSettings.openRgbServerIp.Trim();
                        var openrgb = new OpenRGBServerDefinition
                        {
                            Port = 6742,
                            Ip = ip,
                            ClientName = "Chromatics"
                        };

                        OpenRGBDeviceProvider.Instance.AddDeviceDefinition(openrgb);
                        LoadDeviceProvider(OpenRGBDeviceProvider.Instance);
                    });
                }

                if (appSettings.deviceHueEnabled)
                {
                    TryLoadProviderIsolated("Hue", () =>
                    {
                        if (string.IsNullOrEmpty(appSettings.deviceHueBridgeIP))
                        {
                            Logger.WriteConsole(Enums.LoggerTypes.Error, $"Hue settings are missing. Please re-enable in settings tab to add.");
                        }
                        else
                        {
                            //HueRGBDeviceProvider.Instance.Exception += (sender, e) => Logger.WriteConsole(Enums.LoggerTypes.Error, $"Hue Device Error: {e.Exception.Message}");

                            // Auto-adopt migration. Pre-v4.1.31 builds had no
                            // adoption list — every bulb the bridge exposed was
                            // automatically adopted. After upgrading, an
                            // existing user's deviceHueAdoptedDevices is empty
                            // but they have layers.chromatics4 entries
                            // referencing Hue device GUIDs. Querying the bridge
                            // once and seeding the adoption list keeps those
                            // mappings working without a user prompt; the user
                            // can later open Settings -> Hue to deselect bulbs.
                            //
                            // Falling back silently: if the bridge is offline
                            // or the client key is stale, we leave the adopted
                            // list empty and let HueRGBDeviceProvider's
                            // "empty list = adopt everything" path handle it
                            // for this session. Migration retries next launch.
                            if ((appSettings.deviceHueAdoptedDevices == null || appSettings.deviceHueAdoptedDevices.Count == 0)
                                && !string.IsNullOrEmpty(appSettings.deviceHueBridgeClientKey))
                            {
                                try
                                {
                                    var api = new HueApi.LocalHueApi(appSettings.deviceHueBridgeIP, appSettings.deviceHueBridgeClientKey);
                                    var lights = api.Light.GetAllAsync().GetAwaiter().GetResult();
                                    var devicesResp = api.Device.GetAllAsync().GetAwaiter().GetResult();
                                    var modelByDevice = devicesResp.Data.ToDictionary(d => d.Id, d => d.ProductData?.ModelId ?? "");

                                    var migrated = lights.Data.Select(l => new Chromatics.Extensions.RGB.NET.Devices.Hue.HueAdoptedDevice
                                    {
                                        LightId = l.Id,
                                        Label = l.Metadata?.Name ?? l.Id.ToString(),
                                        ModelId = l.Owner != null && modelByDevice.TryGetValue(l.Owner.Rid, out var m) ? m : (l.Type ?? ""),
                                    }).ToList();

                                    if (migrated.Count > 0)
                                    {
                                        appSettings.deviceHueAdoptedDevices = migrated;
                                        AppSettings.SaveSettings(appSettings);
                                        Logger.WriteConsole(Enums.LoggerTypes.Devices, $"[Hue] Adopted {migrated.Count} bulb(s) from existing bridge pairing. Open Settings -> Hue to deselect any you don't want Chromatics to control.");
                                    }
                                }
                                catch (Exception migEx)
                                {
                                    Logger.WriteConsole(Enums.LoggerTypes.Error, $"[Hue] Auto-adopt migration failed: {migEx.Message}. Bridge may be offline; will retry on next launch.");
                                }
                            }

                            // ClientKey is the entertainment-streaming PSK and is
                            // unused by the CLIP-based HueUpdateQueue. Leave empty
                            // until/unless we add an entertainment streaming path
                            // (which would also require persisting the streaming
                            // key returned by LocalHueApi.RegisterAsync).
                            var hueBridge = new HueClientDefinition(appSettings.deviceHueBridgeIP, "chromatics", "");

                            HueRGBDeviceProvider.Instance.ClientDefinitions.Add(hueBridge);
                            LoadDeviceProvider(HueRGBDeviceProvider.Instance);

                        }
                    });
                }

                if (appSettings.devicePlayStationEnabled)
                {
                    TryLoadProviderIsolated("PlayStation", () =>
                    {
                        PlayStationProviderHooks.EnsureInstalled();
                        LoadDeviceProvider(PlayStationDeviceProvider.Instance);
                        PlayStationProviderHooks.EmitPostLoadHints();
                    });
                }

                if (appSettings.deviceLifxEnabled)
                {
                    try
                    {
                        // Hydrate the provider's adopted-device list from settings
                        // before LoadDeviceProvider triggers discovery / endpoint
                        // resolution. An empty list short-circuits LoadDevices —
                        // the user can re-open the adoption dialog from Settings
                        // to add devices without a re-toggle.
                        LifxRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        foreach (var d in appSettings.deviceLifxAdoptedDevices ?? new List<LifxAdoptedDevice>())
                        {
                            System.Net.IPEndPoint ep = null;
                            if (!string.IsNullOrEmpty(d.LastIp) &&
                                System.Net.IPAddress.TryParse(d.LastIp, out var ip))
                            {
                                ep = new System.Net.IPEndPoint(ip, Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol.LifxDiscovery.LifxPort);
                            }
                            LifxRGBDeviceProvider.Instance.ClientDefinitions.Add(
                                new LifxClientDefinition(d.Mac, d.Label, ep, d.ProductId, d.ZoneCount));
                        }

                        LoadDeviceProvider(LifxRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[LifxDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceNanoleafEnabled)
                {
                    try
                    {
                        // Nanoleaf controllers are pre-paired through the
                        // adoption dialog (Hue pattern), so there's no auto-
                        // adopt sweep here - we just hydrate the provider
                        // from the persisted, tokened controller list and
                        // load. Unreachable controllers are skipped by the
                        // provider and retried next launch.
                        var adopted = appSettings.deviceNanoleafAdoptedDevices ?? new List<Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.NanoleafAdoptedDevice>();
                        Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.NanoleafRGBDeviceProvider.UpdateRateHz = appSettings.nanoleafUpdateRateHz;
                        Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.NanoleafRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        foreach (var d in adopted)
                        {
                            if (string.IsNullOrEmpty(d.AuthToken) || string.IsNullOrEmpty(d.LastIp)) continue;
                            System.Net.IPEndPoint ep = null;
                            if (System.Net.IPAddress.TryParse(d.LastIp, out var ip))
                                ep = new System.Net.IPEndPoint(ip, d.Port > 0 ? d.Port : 16021);
                            Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.NanoleafRGBDeviceProvider.Instance.ClientDefinitions.Add(
                                new Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.NanoleafClientDefinition(
                                    d.Id, d.Label, ep, d.AuthToken, d.Model, d.Firmware, d.PanelCount)
                                {
                                    PanelOrder = d.PanelOrder ?? new List<int>(),
                                });
                        }

                        LoadDeviceProvider(Chromatics.Extensions.RGB.NET.Devices.Nanoleaf.NanoleafRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[NanoleafDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceYeelightEnabled)
                {
                    try
                    {
                        // Auto-adopt on first enable. Mirrors the QMK
                        // provider's pattern: when the persisted adopted-set
                        // is empty (initial launch after the user enabled
                        // the Yeelight toggle), run one SSDP sweep and adopt
                        // every bulb that responds. Subsequent launches
                        // reuse the persisted list and re-resolve only the
                        // IPs that may have changed. Users disable specific
                        // bulbs they don't want Chromatics to drive from the
                        // Mapping tab.
                        //
                        // (A proper Hue/LIFX-style adoption picker dialog
                        // is tracked as a v4.2.x follow-up — it's
                        // significantly more UI work and shipping
                        // auto-adopt first keeps the beta path testable.)
                        var adopted = appSettings.deviceYeelightAdoptedDevices ?? new List<YeelightAdoptedDevice>();
                        if (adopted.Count == 0)
                        {
                            try
                            {
                                var discovered = Chromatics.Extensions.RGB.NET.Devices.Yeelight.Protocol.YeelightDiscovery
                                    .DiscoverAsync(TimeSpan.FromMilliseconds(2500))
                                    .GetAwaiter().GetResult();
                                foreach (var d in discovered)
                                {
                                    if (string.IsNullOrEmpty(d.Id) || d.Endpoint == null) continue;
                                    adopted.Add(new YeelightAdoptedDevice
                                    {
                                        Id = d.Id,
                                        Label = d.DisplayLabel,
                                        LastIp = d.Endpoint.Address.ToString(),
                                        LastPort = d.Endpoint.Port,
                                        Model = d.Model,
                                        FirmwareVersion = d.FirmwareVersion,
                                        Support = d.Support is List<string> list ? list : new List<string>(d.Support ?? Array.Empty<string>()),
                                    });
                                }
                                if (adopted.Count > 0)
                                {
                                    appSettings.deviceYeelightAdoptedDevices = adopted;
                                    AppSettings.SaveSettings(appSettings);
                                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"[Yeelight] Adopted {adopted.Count} bulb(s) discovered on the LAN. Open the Mapping tab to disable any you don't want Chromatics to control.");
                                }
                                else
                                {
                                    Logger.WriteConsole(Enums.LoggerTypes.Devices, "[Yeelight] Discovery found no Yeelight bulbs on the LAN. Make sure each bulb has LAN Control enabled in the Yeelight / Mi Home app (Settings -> LAN Control).", forwardToSentry: false);
                                }
                            }
                            catch (Exception discEx)
                            {
                                Logger.WriteConsole(Enums.LoggerTypes.Error, $"[Yeelight] Initial discovery sweep failed: {discEx.Message}");
                            }
                        }

                        YeelightRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        foreach (var d in adopted)
                        {
                            System.Net.IPEndPoint ep = null;
                            if (!string.IsNullOrEmpty(d.LastIp) &&
                                System.Net.IPAddress.TryParse(d.LastIp, out var ip))
                            {
                                ep = new System.Net.IPEndPoint(ip, d.LastPort > 0 ? d.LastPort : 55443);
                            }
                            YeelightRGBDeviceProvider.Instance.ClientDefinitions.Add(
                                new YeelightClientDefinition(d.Id, d.Label, ep, d.Model, d.FirmwareVersion, d.Support));
                        }

                        LoadDeviceProvider(YeelightRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[YeelightDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceQmkRawHidEnabled)
                {
                    try
                    {
                        // QMK Raw HID provider — auto-adopts every QMK-compatible
                        // keyboard discovered on the USB bus when the user has
                        // an empty persisted adopted-set (first launch after
                        // enabling). The adopted-set is the union of (a) the
                        // boards the user has explicitly seen in the Mapping
                        // tab and not removed via per-device disable, and (b)
                        // any new boards that appear on subsequent launches
                        // — the keymap fetch + handshake is cheap so refreshing
                        // is fine. Persistence stays in
                        // deviceQmkRawHidAdoptedDevices for the next launch's
                        // hot-plug filter.
                        Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance.AdoptedDevices.Clear();
                        var adopted = appSettings.deviceQmkRawHidAdoptedDevices ?? new List<QmkRawHidAdoptedDevice>();
                        if (adopted.Count == 0)
                        {
                            // Discover once and adopt everything that responds.
                            // Subsequent launches will reuse the persisted list
                            // unless the user explicitly clears it.
                            var discovered = Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.Protocol.QmkRawHidDiscovery.Discover();
                            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var c in discovered)
                            {
                                string mfg = "";
                                string prod = "";
                                try { mfg = c.Hid.GetManufacturer() ?? ""; } catch { }
                                try { prod = c.Hid.GetProductName() ?? ""; } catch { }
                                var key = $"{c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}:{mfg}:{prod}";
                                if (!seen.Add(key)) continue;
                                adopted.Add(new QmkRawHidAdoptedDevice
                                {
                                    VendorId = c.Hid.VendorID,
                                    ProductId = c.Hid.ProductID,
                                    Manufacturer = mfg,
                                    Product = prod,
                                    LedCount = c.LedCount,
                                    Protocol = c.Protocol.ToString(),
                                    ViaKeymapKey = string.Empty,
                                });
                            }
                            appSettings.deviceQmkRawHidAdoptedDevices = adopted;
                            if (adopted.Count > 0) AppSettings.SaveSettings(appSettings);
                        }

                        foreach (var d in adopted)
                        {
                            Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance.AdoptedDevices.Add(
                                new Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidAdoptedDeviceFilter(
                                    d.VendorId, d.ProductId, d.Manufacturer, d.Product));
                        }

                        LoadDeviceProvider(Chromatics.Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[QmkRawHidDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceAlienwareEnabled)
                {
                    try
                    {
                        // Auto-adopt on first enable. Mirrors the QMK / Yeelight
                        // pattern: when the persisted adopted-set is empty,
                        // sweep the HID bus once and adopt every Alienware
                        // device that responds. Subsequent launches reuse the
                        // persisted list and re-bind by VID/PID/DevicePath.
                        var adopted = appSettings.deviceAlienwareAdoptedDevices ?? new List<AlienwareAdoptedDevice>();
                        if (adopted.Count == 0)
                        {
                            try
                            {
                                var discovered = Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol.AlienwareDiscovery.Discover();
                                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                foreach (var c in discovered)
                                {
                                    string key = $"{c.Hid.VendorID:X4}:{c.Hid.ProductID:X4}:{c.Hid.DevicePath}";
                                    if (!seen.Add(key)) continue;
                                    adopted.Add(new AlienwareAdoptedDevice
                                    {
                                        VendorId = c.Hid.VendorID,
                                        ProductId = c.Hid.ProductID,
                                        Manufacturer = c.Manufacturer,
                                        Product = c.Product,
                                        DevicePath = c.Hid.DevicePath,
                                        ApiVersion = c.ApiVersion.ToString(),
                                        LightCount = c.LightCount,
                                        ReportLength = c.ReportLength,
                                    });
                                }
                                if (adopted.Count > 0)
                                {
                                    appSettings.deviceAlienwareAdoptedDevices = adopted;
                                    AppSettings.SaveSettings(appSettings);
                                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"[Alienware] Adopted {adopted.Count} AlienFX device(s) discovered on the HID bus.");
                                }
                                else
                                {
                                    Logger.WriteConsole(Enums.LoggerTypes.Devices, "[Alienware] No AlienFX devices detected. Make sure your machine is an Alienware / Dell G-series with AlienFX hardware, and that the Alienware Command Center isn't holding the HID interface exclusively.", forwardToSentry: false);
                                }
                            }
                            catch (Exception discEx)
                            {
                                Logger.WriteConsole(Enums.LoggerTypes.Error, $"[Alienware] Initial discovery sweep failed: {discEx.Message}");
                            }
                        }

                        Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance.ClientDefinitions.Clear();
                        foreach (var d in adopted)
                        {
                            if (!Enum.TryParse<Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol.AlienwareApiVersion>(d.ApiVersion, out var api))
                                api = Chromatics.Extensions.RGB.NET.Devices.Alienware.Protocol.AlienwareApiVersion.Unknown;
                            Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance.ClientDefinitions.Add(
                                new Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareClientDefinition(
                                    d.VendorId, d.ProductId, d.Manufacturer, d.Product,
                                    api, d.LightCount, d.ReportLength, d.DevicePath));
                        }

                        LoadDeviceProvider(Chromatics.Extensions.RGB.NET.Devices.Alienware.AlienwareRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[AlienwareDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceDynamicLightingEnabled)
                {
                    try
                    {
                        // Windows Dynamic Lighting (LampArray). DeviceWatcher
                        // inside the provider handles initial enumeration and
                        // hot-plug, so there's no per-device adoption list to
                        // hydrate before LoadDeviceProvider.
                        LoadDeviceProvider(DynamicLightingRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[DynamicLightingDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceRedragonEnabled)
                {
                    try
                    {
                        // Redragon mice on the OpenRGB protocol family.
                        // RedragonDiscovery inside LoadDevices walks USB and
                        // auto-adopts everything matching the curated VID/PID
                        // table — there's no per-device persisted adoption
                        // list, so users disable individual devices on the
                        // Mapping tab instead.
                        LoadDeviceProvider(Chromatics.Extensions.RGB.NET.Devices.Redragon.RedragonRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[RedragonDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.deviceEVisionEnabled)
                {
                    try
                    {
                        // EVision-family keyboards (Glorious, Redragon,
                        // Womier, Tecware, Mars Gaming, and others — 13
                        // boards, one shared firmware). Auto-adopt model
                        // matches Redragon: discovery returns every match
                        // and the Mappings tab is the per-device disable.
                        LoadDeviceProvider(Chromatics.Extensions.RGB.NET.Devices.EVision.EVisionRGBDeviceProvider.Instance);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[EVisionDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }
                }

                if (appSettings.rgbRefreshRate <= 0) appSettings.rgbRefreshRate = 0.05;

                // TimerUpdateTrigger lives in RGB.NET.Presets.dll, which App
                // Control policies also block (CHROMATICS-17). Same lambda
                // isolation as the providers; without a trigger the surface
                // can't tick, so bail out of setup with lighting disabled
                // rather than crash.
                bool triggerReady = false;
                TryLoadProviderIsolated("RGB.NET.Presets", () =>
                {
                    _timerUpdateTrigger = new TimerUpdateTrigger();
                    _timerUpdateTrigger.UpdateFrequency = appSettings.rgbRefreshRate;
                    surface.RegisterUpdateTrigger(_timerUpdateTrigger);
                    triggerReady = true;
                });
                if (!triggerReady)
                {
                    Logger.WriteConsole(Enums.LoggerTypes.Error, "RGB lighting is disabled for this session because the device update timer failed to load.");

                    // Providers loaded above are already registered. Bailing
                    // with _loaded still false means Unload() no-ops on exit,
                    // so nothing would restore smart lights or release native
                    // SDK handles - tear the providers down here instead.
                    TeardownLoadedProviders();
                    return;
                }

                surface.AlignDevices();
                surface.Updating += Surface_Updating;

                #if DEBUG
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"{surface.Devices.Count} devices loaded.");
                #endif
                
                _loaded = true;
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"RGBController Setup Error: {ex.Message}");

                // Same leak as the trigger bail-out above: any throw after
                // the provider blocks (AlignDevices, event hookup) leaves
                // _loaded false, so Unload() would no-op and the loaded
                // providers would never restore devices or release SDKs.
                if (!_loaded)
                {
                    try { TeardownLoadedProviders(); }
                    catch (Exception teardownEx)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"Provider teardown after Setup failure also failed: {teardownEx.Message}");
                    }
                }
            }
        }

        public static void RemoveDevice(IRGBDevice device)
        {
            // Persist the disable so it survives restarts. Lookup runs
            // BEFORE the detach because surface.Detach removes the device
            // from _devices via the DevicesChanged.Removed handler in some
            // providers, which would race the GUID lookup. Disabled state
            // is keyed on the same GenerateDeviceGuid the rest of the
            // codebase uses for stable per-device identity.
            if (device != null)
            {
                var deviceGuid = GetDeviceGuid(device);
                if (deviceGuid != Guid.Empty)
                    Layers.MappingLayers.SetDeviceDisabled(deviceGuid, true);
            }

            if (surface != null && device != null && surface.Devices.Contains(device))
            {
                // For LIFX / Hue, send the captured pre-Chromatics state
                // (colour + power) before detaching so the bulb returns to
                // whatever the user had before adoption. surface.Detach
                // alone just stops further updates, leaving the bulb on the
                // last colour we sent — which is undesirable when the user
                // explicitly disables a single device. Run on a worker so
                // the UI thread doesn't block on the UDP / HTTP sends.
                //
                // SetPerDeviceDisabled is set BEFORE Task.Run kicks the
                // restore so any decorator data still buffered in the
                // queue's _currentDataSet from a TimerUpdateTrigger tick
                // that raced surface.Detach gets dropped. Without this
                // gate the restore's chunked SetExtendedColorZones
                // packets interleave with the late decorator chunks and
                // leave half a chained Beam strip stuck on the last
                // decorator colour.
                if (device is Extensions.RGB.NET.Devices.LIFX.LifxDevice lifxDev)
                {
                    lifxDev.SetPerDeviceDisabled(true);
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try { await lifxDev.RestoreOriginalStateAsync(); }
                        catch { /* best-effort */ }
                    });
                }
                else if (device is Extensions.RGB.NET.Devices.Hue.HueDevice hueDev)
                {
                    hueDev.SetPerDeviceDisabled(true);
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try { await hueDev.RestoreOriginalStateAsync(); }
                        catch { /* best-effort */ }
                    });
                }
                else if (device is Extensions.RGB.NET.Devices.Nanoleaf.NanoleafDevice nanoDev)
                {
                    nanoDev.SetPerDeviceDisabled(true);
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try { await nanoDev.RestoreOriginalStateAsync(); }
                        catch { /* best-effort */ }
                    });
                }
                else if (device is Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidDevice qmkDev)
                {
                    // QMK boards have no captured pre-Chromatics state to
                    // restore — the firmware's built-in RGB matrix mode
                    // resumes by itself as soon as Update() stops sending
                    // frames. Gate the queue so any buffered frames in
                    // flight don't slip through, then let the firmware
                    // take over.
                    qmkDev.SetPerDeviceDisabled(true);
                }

                surface.Detach(device);

                lock (_activeDevicesLock)
                {
                    if (_activeDevices.ContainsKey(device))
                        _activeDevices[device] = false;
                    else
                        _activeDevices.Add(device, false);
                }

            }
        }

        public static void AddDevice(IRGBDevice device)
        {
            // Clear the persisted disable bit before attaching so a
            // subsequent restart or LoadDeviceProvider doesn't skip the
            // attach. Capture is done by the caller-specific branch below
            // (CaptureOriginalStateAsync for LIFX/Hue) AFTER attach.
            if (device != null)
            {
                var deviceGuid = GetDeviceGuid(device);
                if (deviceGuid != Guid.Empty)
                    Layers.MappingLayers.SetDeviceDisabled(deviceGuid, false);
            }

            if (surface != null && device != null && !surface.Devices.Contains(device))
            {
                surface.Attach(device);
                AttachGlobalBrightness(device);

                // Open the queue back up SYNCHRONOUSLY — order matters because
                // surface.Update(true) below is a single critical section that
                // both renders LedGroups (populating RequestedColor on every
                // painted LED) and dispatches a flushed device.Update for each
                // attached device. If the queue is still gated when that
                // dispatch fires, the resulting SetData would be drained by
                // OnUpdate and ignored by Update, leaving the bulb at its
                // restored "original" colour with no Chromatics paint.
                if (device is Extensions.RGB.NET.Devices.LIFX.LifxDevice lifxDev)
                {
                    lifxDev.ResetCache();
                    lifxDev.SetPerDeviceDisabled(false);
                }
                else if (device is Extensions.RGB.NET.Devices.Hue.HueDevice hueDev)
                {
                    hueDev.SetPerDeviceDisabled(false);
                }
                else if (device is Extensions.RGB.NET.Devices.Nanoleaf.NanoleafDevice nanoDev)
                {
                    nanoDev.ResetCache();
                    nanoDev.SetPerDeviceDisabled(false);
                }
                else if (device is Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidDevice qmkDev)
                {
                    qmkDev.ResetCache();
                    qmkDev.SetPerDeviceDisabled(false);
                }

                // Tagged effects (startup rainbow, title-screen starfield)
                // build their per-device ListLedGroup at the moment the tag
                // is started — devices that were disabled at the time
                // RunStartupEffects ran have no tagged group covering their
                // LEDs, so Render never paints them and led.Color never
                // changes. Without this, re-enabling a device while the
                // startup rainbow is running leaves the bulb dark and the
                // Mapping-tab preview shows no activity. Mirrors the
                // behaviour of DevicesChanged.Added's "running animation,
                // join the rig" path for hot-plug.
                var deviceGuid = GetDeviceGuid(device);
                if (deviceGuid != Guid.Empty)
                    SyncTaggedEffectsForDevice(deviceGuid);

                // Mark all layers tied to this device for re-process so
                // their processors rebuild fresh ListLedGroups and re-paint
                // the LEDs from scratch on the next GameController tick.
                // This dirties the LEDs even when the layer's colour hasn't
                // changed since pre-disable — without it, a Static red
                // layer on a re-enabled bulb sees "RequestedColor==Color"
                // (Render kept painting red during the disabled period and
                // led.Update ran every surface tick to keep _color in
                // sync), IsDirty=false, no SetData call, no UDP / HTTP.
                foreach (var layer in MappingLayers.GetLayers().Values)
                {
                    if (layer.deviceGuid == deviceGuid)
                        layer.requestUpdate = true;
                }

                // Force a full surface render + flushed device update in a
                // single locked pass. Render walks every attached LedGroup
                // and writes RequestedColor for each LED it covers; the
                // following device.Update(true) phase then returns every
                // LED with RequestedColor.A > 0 regardless of IsDirty,
                // so the queue receives the LED state immediately on
                // re-enable instead of waiting for the next decorator
                // tick to dirty something. Caches in LIFX's queue are
                // already cleared above so the per-zone diff doesn't
                // suppress this flush.
                try { surface.Update(flushLeds: true); } catch { }

                // For LIFX, the bulb may have come up powered-off (we
                // honour the persisted disable state at startup with
                // CaptureOriginalStateAsync(turnOnIfOff: false)). The
                // paint frames above pre-load the per-zone HSBK on the
                // bulb but do not switch it on — SetExtendedColorZones
                // doesn't toggle power. Send an explicit SetLightPower
                // AFTER the flush so the bulb wakes up already showing
                // the freshly-painted Chromatics state instead of the
                // last restored frame. EnsurePoweredOn deliberately
                // does NOT recapture _original — we used to re-run
                // CaptureOriginalStateAsync here for its auto-on side
                // effect, but the recapture's GetExtendedColorZones
                // query saw the rainbow paints we'd just sent and
                // poisoned _original.Zones, so the next disable
                // restored to "rainbow" instead of pre-Chromatics.
                // _original from app startup is still the right thing
                // to restore to.
                if (device is Extensions.RGB.NET.Devices.LIFX.LifxDevice lifxDevPower)
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        try { lifxDevPower.EnsurePoweredOn(); }
                        catch { /* best-effort */ }
                    });
                }
                // Hue's Update() includes On=true on every paint frame,
                // so the bridge powers the bulb on automatically as soon
                // as the surface.Update flush above lands. No equivalent
                // power-on call needed.

                // Nanoleaf must re-negotiate extControl streaming over
                // REST after a re-enable: the disable path restored the
                // controller to its scene (which exits streaming), and a
                // controller that started persisted-disabled never entered
                // streaming at all - either way it ignores UDP frames
                // until the handshake runs again.
                if (device is Extensions.RGB.NET.Devices.Nanoleaf.NanoleafDevice nanoDevStream)
                {
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        try { await nanoDevStream.EnsureStreamingAsync(); }
                        catch { /* best-effort */ }
                    });
                }

                lock (_activeDevicesLock)
                {
                    if (_activeDevices.ContainsKey(device))
                        _activeDevices[device] = true;
                    else
                        _activeDevices.Add(device, true);
                }

            }
        }

        private static void AttachGlobalBrightness(IRGBDevice device)
        {
            var corrections = device.ColorCorrections;
            if (corrections == null) return;
            if (!corrections.Contains(GlobalBrightnessCorrection.Instance))
                corrections.Add(GlobalBrightnessCorrection.Instance);
        }

        // Reverse lookup from IRGBDevice to the Chromatics-managed GUID. Used by
        // tagged-effect builders (RunStartupEffects, BuildTitleScreenAnimation)
        // that iterate surface.Devices and need to consult the per-device
        // EffectLayer toggle. Returns Guid.Empty when the device hasn't yet been
        // registered through DevicesChanged.Added — caller treats Empty as "no
        // toggle known, paint as normal".
        public static Guid GetDeviceGuid(IRGBDevice device)
        {
            if (device == null) return Guid.Empty;
            lock (_devicesLock)
            {
                foreach (var kvp in _devices)
                {
                    if (ReferenceEquals(kvp.Value, device))
                        return kvp.Key;
                }
            }
            return Guid.Empty;
        }

        // Per-device brightness needs the device GUID, which is only known
        // once DevicesChanged.Added fires. Called from there after the GUID
        // has been computed; idempotent on re-attach.
        private static void AttachPerDeviceBrightness(IRGBDevice device, Guid deviceGuid)
        {
            if (device == null || deviceGuid == Guid.Empty) return;

            var correction = _perDeviceBrightness.GetOrAdd(deviceGuid, _ => new PerDeviceBrightnessCorrection());
            correction.BrightnessPercent = Layers.MappingLayers.GetDeviceBrightness(deviceGuid);

            var corrections = device.ColorCorrections;
            if (corrections != null && !corrections.Contains(correction))
                corrections.Add(correction);

            // Hue applies brightness via the bridge's separate brightness
            // channel — RGB scaling alone leaves xy chromaticity unchanged.
            if (device is Chromatics.Extensions.RGB.NET.Devices.Hue.HueDevice hueDevice)
                hueDevice.SetPerDeviceBrightness(correction);
        }


        // Pushes a new per-device brightness value to the active correction.
        // Called by MappingLayers.SetDeviceBrightness after persisting; the
        // next render frame picks up the new value via _brightnessPercent.
        public static void SetDeviceBrightness(Guid deviceGuid, int value)
        {
            if (_perDeviceBrightness.TryGetValue(deviceGuid, out var correction))
                correction.BrightnessPercent = value;
        }

        private static void DevicesChanged(object sender, DevicesChangedEventArgs e)
        {
            var device = e.Device;
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

            //Generate GUID for Device
            int counter = 1;
            var guid = Helpers.DeviceHelper.GenerateDeviceGuid(device.DeviceInfo.DeviceName);

            if (e.Action == DevicesChangedEventArgs.DevicesChangedAction.Added)
            {
                //Device Added

                // Some providers hand us a device with zero LEDs (SDK silent
                // on the geometry, or a model we have no layout for). Without
                // at least one Led the device is invisible to every layer,
                // so synthesise a sensible default grid from KeyLocalization
                // — keyboards get the full QWERTY ANSI 104, headsets get a
                // 2x2 left-ear / right-ear quartet.
                Helpers.DefaultLayoutInference.Apply(device);

                // Logitech per-key keyboards arrive from RGB.NET with every
                // LED at Y=0 (LogitechPerKeyRGBDevice.InitializeLayout lays
                // them out in a single horizontal row at pos*19,0). That
                // breaks any decorator that reads Led.Location for spatial
                // computation — most visibly conical gradients, which use
                // atan2(dy, dx) and degenerate to a left-half / right-half
                // fade when every dy is 0. Per-key matrix-grid effects
                // (CircularPulse via DeviceGridHelper) are unaffected
                // because they use the QWERTY row/col grid, not Location.
                //
                // Apply the matching shipped layout XML over the top so
                // Location matches the physical keycap. Mice / headsets
                // get the same treatment for consistency. Falls through
                // silently when the model has no shipped layout.
                Helpers.LogitechLayoutFixup.Apply(device, enviroment);

                lock (_devicesLock)
                {
                    while (_devices.ContainsKey(guid))
                    {
                        var deviceName = device.DeviceInfo.DeviceName + counter;
                        guid = Helpers.DeviceHelper.GenerateDeviceGuid(deviceName);
                        counter++;
                    }
                    _devices.Add(guid, device);
                }

                // Attach to the RGBSurface here for hot-plug ONLY. Startup
                // attachment is owned by SurfaceExtensions.Load, which calls
                // provider.Initialize() (during which DevicesChanged fires
                // for every initial device) and THEN surface.Attach(provider.Devices).
                // Attaching during Initialize would cause Load's later
                // surface.Attach pass to throw "already attached", so we
                // gate on IRGBDeviceProvider.IsInitialized — false during
                // Initialize, true once Load has completed and any
                // subsequent AddDevice is from a provider's runtime
                // hot-plug logic (e.g. PlayStation USB/BT connect).
                //
                // Persisted per-device disable state (schema v5+) wins over
                // hot-plug attach: if the user disabled this device in the
                // Mapping tab, leave it detached. They'll re-enable it via
                // the Mapping tab toggle when they want it back, which
                // triggers AddDevice + (for Hue/LIFX) state capture.
                var senderProvider = sender as IRGBDeviceProvider;
                bool isHotPlug = senderProvider?.IsInitialized == true;
                bool isDisabled = Layers.MappingLayers.IsDeviceDisabled(guid);
                if (isHotPlug && !isDisabled && surface != null && !surface.Devices.Contains(device))
                    surface.Attach(device);

                AttachGlobalBrightness(device);
                AttachPerDeviceBrightness(device, guid);

                #if DEBUG
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Found {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName} (ID: {guid}).");
                #else
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Found {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName}.");
                #endif

                lock (_activeDevicesLock)
                {
                    if (_activeDevices.ContainsKey(device))
                        _activeDevices[device] = !isDisabled;
                    else
                        _activeDevices.Add(device, !isDisabled);
                }

                // Hot-plug into a running startup animation: rebuild the
                // "startup" tagged ledgroups so the new device participates.
                // RunStartupEffects builds a fresh ListLedGroup per device at
                // the moment it's called — devices added later are otherwise
                // never included, which is what manifested as "DS5 hot-plug
                // controller stays on firmware-default blue while the rest
                // of the rig is in the startup rainbow".
                //
                // Gated on isHotPlug + an existing "startup" tag so we don't
                // restart the animation during the initial DevicesChanged
                // burst (Load owns that path), and so we don't accidentally
                // re-fire startup over the user's running game effects.
                if (isHotPlug && HasActiveTaggedEffect("startup"))
                {
                    RunStartupEffects();
                }

                DeviceConnectionChanged?.Invoke(null, EventArgs.Empty);

            }
            else if (e.Action == DevicesChangedEventArgs.DevicesChangedAction.Removed)
            {
                //Device Removed

                #if DEBUG
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Lost {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName} (ID: {guid}).");
                #else
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, $"Lost {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}: {device.DeviceInfo.DeviceName}.");
                #endif

                // Detach from the surface for hot-plug only — same reasoning
                // as the Added branch above. During provider unload
                // (UnloadDeviceProvider) the surface.Detach is already done
                // before Reset() fires DevicesChanged.Removed for each device.
                // Detaching here too would throw "not attached".
                var senderProviderRemoved = sender as IRGBDeviceProvider;
                bool isHotUnplug = senderProviderRemoved?.IsInitialized == true;
                if (isHotUnplug && surface != null && surface.Devices.Contains(device))
                {
                    try { surface.Detach(device); } catch { }
                }

                lock (_devicesLock)
                {
                    if (_devices.ContainsKey(guid))
                        _devices.Remove(guid);
                }

                lock (_activeDevicesLock)
                {
                    if (_activeDevices.ContainsKey(device))
                        _activeDevices[device] = false;
                    else
                        _activeDevices.Add(device, false);
                }

                DeviceConnectionChanged?.Invoke(null, EventArgs.Empty);
            }

            
            surface.AlignDevices();

            /*
            if (_loaded)
            {
                StopEffects();
                ResetLayerGroups();
                    
                if (!GameController.IsGameConnected())
                    RunStartupEffects();
            }
            */
        }

        // Shared provider teardown for every exit path: normal Unload, the
        // Setup trigger bail-out, and Setup's outer catch. Detach and
        // bookkeeping run sequentially on this thread (the surface is not
        // thread-safe), then the stateful smart-light providers (LIFX, Hue,
        // Nanoleaf) dispose in parallel under one 30s ceiling - each blocks
        // in Dispose while restoring its devices, so sequential disposal
        // makes the wait additive across brands. Native-SDK providers keep
        // the sequential dispose; some vendor SDKs are touchy about which
        // thread tears them down.
        private static void TeardownLoadedProviders()
        {
            var deferredRestore = new List<IRGBDeviceProvider>();
            foreach (var deviceProvider in loadedDeviceProviders)
            {
                bool statefulRestore =
                    deviceProvider is Extensions.RGB.NET.Devices.LIFX.LifxRGBDeviceProvider
                    or Extensions.RGB.NET.Devices.Hue.HueRGBDeviceProvider
                    or Extensions.RGB.NET.Devices.Nanoleaf.NanoleafRGBDeviceProvider;

                UnloadDeviceProvider(deviceProvider, removeFromList: false, disposeProvider: !statefulRestore);
                if (statefulRestore) deferredRestore.Add(deviceProvider);
            }

            if (deferredRestore.Count > 0)
            {
                var restoreTasks = deferredRestore
                    .Select(p => System.Threading.Tasks.Task.Run(() =>
                    {
                        try { p.Dispose(); }
                        catch (Exception ex)
                        {
                            Logger.WriteConsole(Enums.LoggerTypes.Error, $"Provider dispose error during shutdown: {ex.Message}");
                        }
                    }))
                    .ToArray();

                if (!System.Threading.Tasks.Task.WaitAll(restoreTasks, TimeSpan.FromSeconds(30)))
                    Logger.WriteConsole(Enums.LoggerTypes.Devices, "Smart-light restore hit the 30s shutdown ceiling; remaining restores were abandoned.");
            }

            loadedDeviceProviders.Clear();
        }

        public static void Unload()
        {
            if (!_loaded) return;

            try
            {
                var appSettings = AppSettings.GetSettings();

                TeardownLoadedProviders();

                // Stop and dispose of the update trigger. Previously this only called
                // Stop(), which left the trigger's internal timer and its handle alive.
                if (_timerUpdateTrigger != null)
                {
                    _timerUpdateTrigger.Stop();
                    _timerUpdateTrigger.Dispose();
                    _timerUpdateTrigger = null;
                }

                surface.Updating -= Surface_Updating;
                surface.Exception -= surfaceExceptionEventHandler;

                surface.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"RGBController Unload Error: {ex.Message}");
            }
            finally
            {
                _loaded = false;
            }
        }

        public static bool LoadDeviceProvider(IRGBDeviceProvider provider)
            => LoadDeviceProvider(provider, out _);

        // Overload that surfaces the caught exception to the caller so toggle
        // / first-run handlers can react to specific failure shapes (e.g.
        // Logitech's "Failed to initialize Logitech-SDK." when G HUB isn't
        // running) without re-parsing the console log. Returns null in
        // loadError when the call succeeds or is a no-op (provider already
        // loaded).
        public static bool LoadDeviceProvider(IRGBDeviceProvider provider, out Exception loadError)
        {
            loadError = null;
            try
            {
                if (provider == null) return false;

                if (!loadedDeviceProviders.Contains(provider))
                {
                    //Attach device provider
                    foreach (var device in provider.Devices)
                    {
                        Console.WriteLine(@"Device: " + device.DeviceInfo.DeviceName);

                        // Skip surface.Attach for devices the user previously
                        // disabled in the Mapping tab (persisted via
                        // layers.chromatics4 schema v5+). The device still
                        // gets registered through DevicesChanged.Added so it
                        // appears in the Mapping tab list — re-enabling it
                        // there calls AddDevice which performs the attach
                        // and (for stateful providers like Hue/LIFX) captures
                        // the bulb's pre-Chromatics state.
                        var guidProbe = Helpers.DeviceHelper.GenerateDeviceGuid(device.DeviceInfo.DeviceName);
                        bool disabled = Layers.MappingLayers.IsDeviceDisabled(guidProbe);

                        if (!disabled)
                            surface.Attach(device);
                        AttachGlobalBrightness(device);
                    }

                    var showErrors = AppSettings.GetSettings().showDeviceErrors;

#if DEBUG
                    showErrors = true;
#endif

                    provider.DevicesChanged += DevicesChanged;

                    var initError = LoadProviderWithDiagnostics(provider);

                    // Subscribed after the load so start-up failures are
                    // reported once, by the probe, with the provider named.
                    // This handler covers runtime errors from here on, and
                    // stays behind the user's preference — a load failure is
                    // actionable and reports either way.
                    if (showErrors)
                        provider.Exception += deviceExceptionEventHandler;

                    loadedDeviceProviders.Add(provider);

                    if (initError != null)
                        loadError ??= initError;

                    // Warn the user when a freshly-loaded provider gives us
                    // devices whose hardware/SDK can't accept per-LED writes
                    // (zone-only or single-colour fallback). Effects that
                    // depend on per-LED spatial position — radial pulses,
                    // ripples, audio-visualizer columns — degrade visually
                    // on those devices, and the user can't tell whether
                    // it's a Chromatics bug or hardware limit without this
                    // hint. Logitech is the only provider with a known set
                    // of zone/per-device fallback classes; the helper is
                    // pattern-matchable and easy to extend.
                    WarnLimitedDevices(provider);

                    // surface.Load attaches every device in provider.Devices
                    // unconditionally (the comment in DevicesChanged about
                    // "startup attachment is owned by SurfaceExtensions.Load"
                    // is the source of truth here). Our pre-Load skip-attach
                    // loop above is moot for async providers (Hue/LIFX),
                    // whose Devices collection is empty until Initialize
                    // runs INSIDE Load. To honour the persisted disable
                    // state we have to detach the disabled devices here,
                    // AFTER Load has populated provider.Devices and put
                    // them on the surface.
                    //
                    // For Hue/LIFX we ALSO set the queue's per-device
                    // disable flag so any decorator data that managed to
                    // get buffered during the brief surface-load window
                    // (Load → render tick → device.Update → SetData) gets
                    // dropped by the queue's next OnUpdate instead of
                    // being sent to the bulb. The flag stays set until
                    // the user re-enables the device via AddDevice, which
                    // captures fresh state and clears the flag.
                    foreach (var device in provider.Devices)
                    {
                        var guidProbe = Helpers.DeviceHelper.GenerateDeviceGuid(device.DeviceInfo.DeviceName);
                        if (!Layers.MappingLayers.IsDeviceDisabled(guidProbe))
                            continue;
                        if (surface.Devices.Contains(device))
                            surface.Detach(device);
                        if (device is Extensions.RGB.NET.Devices.LIFX.LifxDevice lifxDev)
                            lifxDev.SetPerDeviceDisabled(true);
                        else if (device is Extensions.RGB.NET.Devices.Hue.HueDevice hueDev)
                            hueDev.SetPerDeviceDisabled(true);
                        else if (device is Extensions.RGB.NET.Devices.QmkRawHid.QmkRawHidDevice qmkDev)
                            qmkDev.SetPerDeviceDisabled(true);
                        lock (_activeDevicesLock)
                        {
                            if (_activeDevices.ContainsKey(device))
                                _activeDevices[device] = false;
                            else
                                _activeDevices.Add(device, false);
                        }
                    }

                    if (_loaded)
                    {
                        StopEffects();
                        ResetLayerGroups();

                        if (!GameController.IsGameConnected())
                            RunStartupEffects();
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // Asynchronous providers (Hue, LIFX, OpenRGB) fail before
                // populating Devices, so the old `provider.Devices.FirstOrDefault().DeviceInfo.DeviceName`
                // log line NRE'd inside the catch and masked the real error.
                // Provider type name is always available and more useful for
                // diagnosis anyway (which provider failed, not which device).
                var label = provider?.GetType().Name ?? "Unknown";

                // RGB.NET singletons (Corsair in particular) can throw
                // ObjectDisposedException when Load is called after the
                // provider's own teardown path disposed Instance. The user
                // still sees the load failure in console, but Sentry doesn't
                // get spammed — the root cause is upstream and a single
                // failed load is non-fatal (Chromatics carries on with the
                // remaining providers).
                bool benign = ex is ObjectDisposedException;
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"[{label}] LoadDeviceProvider Error: {ex.Message}", forwardToSentry: !benign);

                // Logitech-SDK init failure can route through the catch
                // (synchronous throw) AND/OR through the surface.Exception
                // event handler (async). Dispatch to the shared helper so
                // every channel produces the same G HUB tip.
                LogLogitechSdkHintIfNeeded(ex);

                loadError = ex;
                return false;
            }

        }

        // Stand-in for surface.Load(provider) that reports what went wrong
        // instead of swallowing it, without changing which failures are fatal.
        //
        // RGB.NET's Load calls Initialize(throwExceptions: false), and its
        // Throw() only rethrows when that flag is set — otherwise it raises
        // the Exception event and returns to the caller, which carries on.
        // A provider whose native SDK is missing or refused to start ends up
        // reporting success: the HID scan still lists the hardware, so the
        // devices show up in the Mapping tab and never light. Nothing reaches
        // the console tab and nothing reaches our caller.
        //
        // Initializing with throwExceptions: true lets the probe below decide
        // per exception. Non-critical ones keep today's behaviour exactly
        // (logged, provider keeps going); critical ones abort the provider and
        // are returned to the caller. Either way the exception is caught here,
        // so one bad provider never stops the others from loading.
        private static Exception LoadProviderWithDiagnostics(IRGBDeviceProvider provider)
        {
            Exception captured = null;
            var label = provider.GetType().Name;

            void Probe(object sender, ExceptionEventArgs args)
            {
                // Only a critical exception means the provider failed. RGB.NET
                // also raises this event per device from GetLoadedDevices, with
                // isCritical false, when one device of many fails to add - the
                // load carries on and the rest attach normally. Treating those
                // as the verdict reported a working provider as failed and
                // handed the caller a load error it should not have had.
                if (args.IsCritical)
                    captured ??= args.Exception;
                else
                    Logger.WriteConsole(Enums.LoggerTypes.Devices,
                        $"[{label}] a device was skipped: {args.Exception.Message}", forwardToSentry: false);

                args.Throw = args.IsCritical;
            }

            provider.Exception += Probe;
            try
            {
                if (!provider.IsInitialized)
                    provider.Initialize(RGBDeviceType.All, throwExceptions: true);
            }
            catch (Exception ex)
            {
                captured ??= ex;
            }
            finally
            {
                provider.Exception -= Probe;
            }

            surface?.Attach(provider.Devices);

            if (captured != null)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error,
                    $"[{label}] failed to start: {captured.Message}", forwardToSentry: false);
                LogLogitechSdkHintIfNeeded(captured);
            }
            else if (provider.Devices.Count == 0)
            {
                // Loaded without complaint and handed us nothing. Benign for
                // the smart-light providers when the user owns no bulbs, but
                // for an SDK provider it usually means the vendor software
                // isn't running, so say so rather than leave a dead toggle.
                Logger.WriteConsole(Enums.LoggerTypes.Devices,
                    $"[{label}] loaded but reported no devices.", forwardToSentry: false);
            }

            return captured;
        }

        // Surface a one-time console warning per provider load when devices
        // can't accept per-LED writes — zone-based or single-colour SDK
        // fallbacks. Effects that depend on per-LED spatial position
        // (CircularPulse, BPMRipple, AudioVisualizer) degrade visibly on
        // those devices, and the user otherwise has no way to tell whether
        // it's a hardware limit or a Chromatics bug. Pattern-match on the
        // device's runtime class — RGB.NET names them <Brand><Family>RGBDevice
        // (LogitechZoneRGBDevice, LogitechPerDeviceRGBDevice, etc.).
        // Extend the type-name list as new providers ship limited-mode
        // device classes.
        private static readonly Dictionary<string, string> _limitedDeviceTypeWarnings = new(StringComparer.Ordinal)
        {
            ["LogitechZoneRGBDevice"]      = "is using zone-based lighting (the SDK limits this model to a small number of zones); radial effects (CircularPulse, BPMRipple, AudioVisualizer) will look like horizontal stripes rather than circles",
            ["LogitechPerDeviceRGBDevice"] = "is using single-colour lighting (the SDK exposes one zone for the whole device); radial and per-key effects will paint a single uniform colour",
        };

        private static void WarnLimitedDevices(IRGBDeviceProvider provider)
        {
            if (provider == null) return;
            try
            {
                foreach (var device in provider.Devices)
                {
                    string typeName = device.GetType().Name;
                    if (!_limitedDeviceTypeWarnings.TryGetValue(typeName, out var description)) continue;

                    Logger.WriteConsole(Enums.LoggerTypes.Devices,
                        $"[Devices] '{device.DeviceInfo.DeviceName}' {description}.",
                        forwardToSentry: false);
                }
            }
            catch { /* diagnostic — never fatal */ }
        }

        // disposeProvider false lets Unload() defer the blocking Dispose of
        // the stateful smart-light providers so their restores can run in
        // parallel; the surface detach and bookkeeping still run here, on
        // this thread, because the RGB.NET surface is not thread-safe.
        public static void UnloadDeviceProvider(IRGBDeviceProvider provider, bool removeFromList = true, bool disposeProvider = true)
        {
            bool anyRemoved = false;
            try
            {
                if (loadedDeviceProviders.Contains(provider))
                {
                    foreach (var device in provider.Devices)
                    {
                        // Guard against double-detach. RGB.NET's surface throws
                        // "The device 'X' is not attached to this surface." when
                        // Detach is called on a device that's already been
                        // removed (e.g. via a per-device disable in the Mapping
                        // tab, or a hot-unplug DevicesChanged race during
                        // provider teardown). The same device may also appear
                        // in provider.Devices after we've already detached it
                        // earlier in this loop on certain provider
                        // implementations. Skipping the detach in that case is
                        // safe — the rest of the cleanup (_devices /
                        // _activeDevices removal) still runs.
                        if (surface != null && surface.Devices.Contains(device))
                            surface.Detach(device);

                        // Remove from _devices so the GUID slot is freed. Without this,
                        // re-enabling the same provider constructs fresh device objects with the
                        // same names, hits the GUID-collision loop, and registers them under
                        // counter-suffixed GUIDs — leaving stale entries that show up as
                        // phantom duplicates in the Mapping tab.
                        lock (_devicesLock)
                        {
                            var key = _devices.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, device)).Key;
                            if (key != default)
                                _devices.Remove(key);
                        }

                        lock (_activeDevicesLock)
                            _activeDevices.Remove(device);
                        anyRemoved = true;
                    }

                    provider.Exception -= deviceExceptionEventHandler;
                    provider.DevicesChanged -= DevicesChanged;

                    if (removeFromList)
                        loadedDeviceProviders.Remove(provider);

                    if (disposeProvider)
                        provider.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"[{provider.Devices.FirstOrDefault()?.DeviceInfo.DeviceName}] UnloadDeviceProvider Error: {ex.Message}");
            }

            // Notify listeners that the device set changed. The DevicesChanged
            // handler already fires DeviceConnectionChanged on hot-unplug, but
            // a provider-level disable from Settings unsubscribes that handler
            // before the manual surface.Detach loop runs above, so the event
            // never reaches the Mapping view. Firing once here covers the
            // settings-disable path for every provider in one place.
            if (anyRemoved)
                DeviceConnectionChanged?.Invoke(null, EventArgs.Empty);
        }

        public static bool IsLoaded()
        {
            return _loaded;
        }

        public static PaletteColorModel GetActivePalette()
        {
            return _colorPalette;
        }

        public static bool LoadColorPalette()
        {
            if (FileOperationsHelper.CheckColorMappingsExist())
            {
                _colorPalette = FileOperationsHelper.LoadColorMappings();

                return true;
            }

            return false;
        }

        public static bool SaveColorPalette()
        {
            FileOperationsHelper.SaveColorMappings(_colorPalette);
            
            foreach (var layer in MappingLayers.GetLayers())
            {
                layer.Value.requestUpdate = true;
            }

            return true;
        }

        public static bool ImportColorPalette(string path)
        {
            var colorPalette = FileOperationsHelper.ImportColorMappingsFromPath(path);
            if (colorPalette == null) return false;

            _colorPalette = colorPalette;
            SaveColorPalette();
            return true;
        }

        public static bool ExportColorPalette(string path)
        {
            FileOperationsHelper.ExportColorMappingsToPath(_colorPalette, path);
            return true;
        }

        public static EffectTypesModel GetEffectsSettings()
        {
            return _effects;
        }

        public static bool LoadEffectsSettings()
        {
            if (FileOperationsHelper.CheckEffectSettingsExist())
            {
                _effects = FileOperationsHelper.LoadEffectSettings();

                return true;
            }

            return false;
        }

        public static bool SaveEffectsSettings()
        {
            FileOperationsHelper.SaveEffectSettings(_effects);
            return true;
        }

        // Reconciles a single device's contribution to any currently-active
        // tagged effect (startup animation, title screen) with the device's
        // EffectLayer toggle state. Per-device — does NOT touch other devices'
        // groups, so toggling one device does not disturb the rest of the
        // rig's animation phase.
        //
        //   - Effects toggled OFF for the device: detach + remove its group
        //     from the tagged-effect tracking. Other devices' groups keep
        //     painting at their current phase.
        //   - Effects toggled ON for the device: build a new group for just
        //     this device using the current animation's gradient/decorator
        //     setup, attach it, and register under the existing tag. Joins
        //     mid-animation; the new group inherits the global gradient
        //     phase (MoveGradientDecorator advances over time, doesn't
        //     restart on re-attach).
        public static void SyncTaggedEffectsForDevice(Guid deviceGuid)
        {
            if (deviceGuid == Guid.Empty) return;
            bool effectsEnabled = MappingLayers.IsDeviceEffectsEnabled(deviceGuid);

            // Resolve the IRGBDevice for this guid — needed when (re)building.
            IRGBDevice device;
            lock (_devicesLock) { _devices.TryGetValue(deviceGuid, out device); }
            if (device == null) return;

            SyncTaggedEffectForDevice("startup", deviceGuid, device, effectsEnabled,
                                      buildIfEnabled: () => BuildStartupEffectForDevice(device, deviceGuid));
            SyncTaggedEffectForDevice("title", deviceGuid, device, effectsEnabled,
                                      buildIfEnabled: () => BuildTitleEffectForDevice(device, deviceGuid));
        }

        private static void SyncTaggedEffectForDevice(string tag, Guid deviceGuid, IRGBDevice device,
                                                      bool effectsEnabled, Action buildIfEnabled)
        {
            if (!HasActiveTaggedEffect(tag)) return;

            ListLedGroup existing = null;
            lock (_taggedEffectsLock)
            {
                if (_taggedEffectsByDevice.TryGetValue(tag, out var byDevice))
                    byDevice.TryGetValue(deviceGuid, out existing);
            }

            if (!effectsEnabled && existing != null)
            {
                // Effects just turned OFF for this device — detach its group
                // without disturbing groups on other devices.
                lock (_taggedEffectsLock)
                {
                    if (_taggedEffects.TryGetValue(tag, out var groups))
                        groups.Remove(existing);
                    if (_taggedEffectsByDevice.TryGetValue(tag, out var byDevice))
                        byDevice.Remove(deviceGuid);
                }
                lock (_runningEffectsLock)
                    _runningEffects.Remove(existing);
                try { existing.RemoveAllDecorators(); } catch { }
                try { existing.Detach(); } catch { }
            }
            else if (effectsEnabled && existing == null)
            {
                // Effects just turned ON for this device — build + register a
                // fresh group. Inherits the global animation phase from the
                // shared MoveGradientDecorator (if any).
                buildIfEnabled?.Invoke();
            }
        }

        // Builds the startup-rainbow ledgroup for a single device. Mirrors
        // the per-device branch of RunStartupEffects so a hot-toggle of
        // EffectLayer can attach just this device without rebuilding others.
        private static void BuildStartupEffectForDevice(IRGBDevice device, Guid deviceGuid)
        {
            if (!_effects.effect_startupanimation) return;

            var move = new MoveGradientDecorator(surface)
            {
                IsEnabled = true,
                Speed = 100,
            };
            var gradient = new RainbowGradient();
            var ledgroup = new ListLedGroup(surface);
            ledgroup.ZIndex = EffectZIndex.StartupAnimation;
            foreach (var led in device) ledgroup.AddLed(led);
            gradient.AddDecorator(move);

            if (device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                ledgroup.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), gradient));
            else
                ledgroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), gradient));

            RegisterTaggedEffect("startup", deviceGuid, ledgroup);
        }

        // Builds the title-screen starfield ledgroup for a single device.
        // Defers to GameController for the construction details since the
        // colour palette + decorator config live there.
        private static void BuildTitleEffectForDevice(IRGBDevice device, Guid deviceGuid)
            => GameController.BuildTitleEffectForDeviceInternal(device, deviceGuid);

        public static void RunStartupEffects()
        {
            // Re-evaluate the surface tick rate. ApplyUpdateRate reads the
            // live connection state, so this is a no-op when the game is
            // already attached (device re-enable / hot-plug mid-game) and
            // only idles when genuinely disconnected with the setting on.
            ApplyUpdateRate();

            if (!_effects.effect_startupanimation) return;

            // Idempotent: clear any prior startup groups so a re-call (e.g.
            // user toggles startup off then on while game still
            // disconnected) doesn't stack duplicates.
            StopTaggedEffects("startup");

            var devices = surface.GetDevices(RGBDeviceType.All);

            var move = new MoveGradientDecorator(surface)
            {
                IsEnabled = true,
                Speed = 100,
            };

            foreach (var device in devices)
            {
                // Per-device "all effects off" gate from the EffectLayer
                // checkbox on the Mappings tab. If the user has unticked
                // effects for this device, skip its startup-animation
                // ledgroup so the rainbow doesn't paint there.
                var deviceGuid = GetDeviceGuid(device);
                if (deviceGuid != Guid.Empty
                    && !MappingLayers.IsDeviceEffectsEnabled(deviceGuid))
                    continue;

                var gradient = new RainbowGradient();
                var ledgroup = new ListLedGroup(surface);

                ledgroup.ZIndex = EffectZIndex.StartupAnimation;
                foreach (var led in device)
                {
                    ledgroup.AddLed(led);
                }

                gradient.AddDecorator(move);

                if (device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                {
                    ledgroup.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), gradient));
                }
                else
                {
                    ledgroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), gradient));
                }


                RegisterTaggedEffect("startup", deviceGuid, ledgroup);
            }
        }

        public static void StopEffects(bool gameFirstConnected = false)
        {
            if (gameFirstConnected)
            {
                // Game just attached (gameConnected is already true here),
                // so this restores the configured rate.
                ApplyUpdateRate();
            }

            List<ListLedGroup> snapshot;
            lock (_runningEffectsLock)
            {
                snapshot = new List<ListLedGroup>(_runningEffects);
                _runningEffects.Clear();
            }

            foreach (var effects in snapshot)
            {
                foreach (var decorator in effects.Decorators)
                {
                    decorator.IsEnabled = false;
                }

                effects.RemoveAllDecorators();
                effects.Detach();
            }

            // Clear all tagged-effect lists too. The groups they pointed at
            // are now detached (above), so any future StopTaggedEffects(tag)
            // would no-op anyway — but keeping the dict in sync prevents
            // stale references piling up across StopEffects() calls.
            lock (_taggedEffectsLock)
            {
                foreach (var groups in _taggedEffects.Values) groups.Clear();
                _taggedEffectsByDevice.Clear();
            }
        }

        // Register a ListLedGroup as part of a named animation category, so
        // a later StopTaggedEffects(tag) can detach JUST that animation's
        // groups without touching unrelated running effects (raid overlays,
        // weather, etc.). The group is also added to the global
        // _runningEffects list so existing teardown paths still see it.
        public static void RegisterTaggedEffect(string tag, ListLedGroup group)
            => RegisterTaggedEffect(tag, Guid.Empty, group);

        // Variant that records the source device GUID alongside the group so
        // the per-device EffectLayer toggle can attach/detach a single
        // device's contribution to a tagged animation (startup rainbow, title
        // starfield) without restarting the whole animation across the rig.
        public static void RegisterTaggedEffect(string tag, Guid deviceGuid, ListLedGroup group)
        {
            if (group == null || string.IsNullOrEmpty(tag)) return;
            lock (_taggedEffectsLock)
            {
                if (!_taggedEffects.TryGetValue(tag, out var groups))
                    _taggedEffects[tag] = groups = new List<ListLedGroup>();
                groups.Add(group);

                if (deviceGuid != Guid.Empty)
                {
                    if (!_taggedEffectsByDevice.TryGetValue(tag, out var byDevice))
                        _taggedEffectsByDevice[tag] = byDevice = new Dictionary<Guid, ListLedGroup>();
                    byDevice[deviceGuid] = group;
                }
            }
            lock (_runningEffectsLock)
                _runningEffects.Add(group);
        }

        // Tear down ONLY the groups registered under `tag` — used when the
        // user disables Startup Animation / Title Screen via the Effects
        // tab and we need to stop the effects mid-cycle without
        // killing other running effects on the surface.
        //
        // LEDs are painted BLACK and one surface render is forced before
        // detach so the hardware actually clears, instead of latching at
        // the last-rendered animation frame. Without this, disabling the
        // startup rainbow leaves whatever colours were last on the LEDs
        // (typically a frozen rainbow) until something else paints them.
        // Returns true if at least one ledgroup is currently registered under
        // the given tag. Cheap to call from the DevicesChanged hot path —
        // single dict lookup under the same lock the writers use.
        public static bool HasActiveTaggedEffect(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            lock (_taggedEffectsLock)
            {
                return _taggedEffects.TryGetValue(tag, out var groups) && groups.Count > 0;
            }
        }

        public static void StopTaggedEffects(string tag)
        {
            List<ListLedGroup> snapshot;
            lock (_taggedEffectsLock)
            {
                if (!_taggedEffects.TryGetValue(tag, out var groups) || groups.Count == 0) return;
                snapshot = new List<ListLedGroup>(groups);
                groups.Clear();
                if (_taggedEffectsByDevice.TryGetValue(tag, out var byDevice))
                    byDevice.Clear();
            }

            // Disable decorators + write black DIRECTLY to each LED's Color
            // property. We can't rely on `g.Brush = SolidColorBrush(black)`
            // + surface.Update() because some decorators (e.g.
            // StarfieldDecorator.OnAttached at line 57) call `ledGroup.Detach()`
            // when they attach — they paint LEDs directly via the
            // surface.Updating event and want the group out of the brush
            // render path. Setting brush on a detached group is silent.
            // Writing led.Color directly works regardless of attachment
            // state because device.GetUpdateData reads each LED's current
            // Color at render time.
            var black = new Color((byte)0, (byte)0, (byte)0);
            foreach (var g in snapshot)
            {
                foreach (var d in g.Decorators) d.IsEnabled = false;
                g.RemoveAllDecorators();
                foreach (var led in g)
                {
                    led.Color = black;
                }
            }

            // Force a render so the black hits hardware before we detach.
            try { surface?.Update(); } catch { }

            lock (_runningEffectsLock)
            {
                foreach (var g in snapshot)
                    _runningEffects.Remove(g);
            }

            foreach (var g in snapshot)
                g.Detach();
        }

        public static bool IsBaseLayerEffectRunning()
        {
            return _baseLayerEffectRunning;
        }

        public static void SetBaseLayerEffect(bool toggle)
        {
            _baseLayerEffectRunning = toggle;
        }

        // Registration API for the effect processors (ReactiveWeather,
        // RaidEffect, CutsceneAnimation), which run on the game-loop thread.
        // Every mutation of _runningEffects goes through _runningEffectsLock
        // here - handing out the live list let those processors race the
        // locked Clear() in StopEffects / LoadDeviceProvider on the UI and
        // thread-pool sides, which can corrupt List<T> internals.
        public static void AddRunningEffect(ListLedGroup group)
        {
            if (group == null) return;
            lock (_runningEffectsLock)
            {
                if (!_runningEffects.Contains(group))
                    _runningEffects.Add(group);
            }
        }

        public static void RemoveRunningEffect(ListLedGroup group)
        {
            if (group == null) return;
            lock (_runningEffectsLock)
                _runningEffects.Remove(group);
        }

        public static RGBSurface GetLiveSurfaces()
        {
            return surface;
        }

        public static Dictionary<Guid, IRGBDevice> GetLiveDevices()
        {
            lock (_devicesLock)
                return new Dictionary<Guid, IRGBDevice>(_devices);
        }

        public static Dictionary<IRGBDevice, bool> GetActiveDevices()
        {
            lock (_activeDevicesLock)
                return new Dictionary<IRGBDevice, bool>(_activeDevices);
        }

        public static List<IRGBDeviceProvider> GetDeviceProviders()
        {
            return loadedDeviceProviders;
        }

        public static List<Led> GetLiveLayerGroupCollection()
        {
            return _layergroupledcollection;
        }

        public static System.Collections.Concurrent.ConcurrentDictionary<int, ListLedGroup[]> GetLiveLayerGroups()
        {
            return _layergroups;
        }

        // Appends a group to a layer's live-group registration without
        // displacing groups other processors registered under the same id.
        // The effect-layer processors each own one group per layerID, and
        // the requestUpdate / type-switch cleanup in GameController detaches
        // all of them through this one registry entry.
        public static void RegisterLiveLayerGroup(int layerID, ListLedGroup group)
        {
            if (group == null) return;
            _layergroups.AddOrUpdate(layerID,
                _ => new[] { group },
                (_, existing) =>
                {
                    if (Array.IndexOf(existing, group) >= 0) return existing;
                    var next = new ListLedGroup[existing.Length + 1];
                    existing.CopyTo(next, 0);
                    next[existing.Length] = group;
                    return next;
                });
        }

        public static void RemoveLayerGroup(int targetId)
        {
            // Remove before detaching so the timer-thread readers never see a
            // half-detached group set.
            if (_layergroups.TryRemove(targetId, out var removedGroups))
            {
                foreach (var layer in removedGroups)
                {
                    layer.RemoveAllDecorators();
                    layer.Detach();

                    lock (_runningEffectsLock)
                        _runningEffects.Remove(layer);
                }
            }
        }

        public static void ResetLayerGroups()
        {
            foreach (var layergroup in _layergroups)
            {
                foreach (var layer in layergroup.Value)
                {
                    layer.RemoveAllDecorators();
                    layer.Detach();
                }
            }

            foreach (var mapping in MappingLayers.GetLayers())
            {
                mapping.Value.requestUpdate = true;
            }

            lock (_runningEffectsLock)
                _runningEffects.Clear();
            _layergroups.Clear();
            _layergroupledcollection.Clear();
        }

        // Avalonia MappingViewModel registers here when preview is active.
        // Fired on every RGB surface update tick (background thread) —
        // the callback must marshal to the UI thread itself.
        private static Action _avaloniaPreviewCallback;

        public static void SetAvaloniaPreviewCallback(Action callback)
            => _avaloniaPreviewCallback = callback;

        public static void ClearAvaloniaPreviewCallback()
            => _avaloniaPreviewCallback = null;

        private static void Surface_Updating(UpdatingEventArgs args)
        {
            if (!_loaded) return;

            if (MappingLayers.IsPreview())
            {
                _avaloniaPreviewCallback?.Invoke();
            }
        }
    }
}
