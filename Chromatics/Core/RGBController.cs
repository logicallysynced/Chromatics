using Chromatics.Extensions.RGB.NET.ColorCorrections;
using Chromatics.Extensions.RGB.NET.Devices;
using Chromatics.Extensions.RGB.NET.Devices.Hue;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using Chromatics.Extensions.RGB.NET.Devices.PlayStation;
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
using RGB.NET.Layout;
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

        private static Dictionary<int, ListLedGroup[]> _layergroups = new Dictionary<int, ListLedGroup[]>();

        private static List<Led> _layergroupledcollection = new List<Led>();

        private static PaletteColorModel _colorPalette = new PaletteColorModel();

        private static EffectTypesModel _effects = new EffectTypesModel();

        private static List<ListLedGroup> _runningEffects = new List<ListLedGroup>();

        private static bool _baseLayerEffectRunning;

        private static RGBSurface.ExceptionEventHandler surfaceExceptionEventHandler;

        private static EventHandler<ExceptionEventArgs> deviceExceptionEventHandler;

        private static TimerUpdateTrigger _timerUpdateTrigger;

        private const double IdleUpdateFrequency = 0.05; // 20 Hz

        private static void SetIdleUpdateRate(bool idle)
        {
            if (_timerUpdateTrigger == null) return;
            _timerUpdateTrigger.UpdateFrequency = idle
                ? IdleUpdateFrequency
                : AppSettings.GetSettings().rgbRefreshRate;
        }

        public static void Setup()
        {
            try
            {
                //Bind to console
                Logger.WriteConsole(Enums.LoggerTypes.Devices, @"Looking for RGB Devices..");

                var appSettings = AppSettings.GetSettings();

                //Setup Exception Events
                surfaceExceptionEventHandler = args_ => Logger.WriteConsole(Enums.LoggerTypes.Error, $"Device Error: {args_.Exception.Message}", forwardToSentry: false);
                deviceExceptionEventHandler = (sender, e) => Logger.WriteConsole(Enums.LoggerTypes.Error, $"Device Error: {e.Exception.Message}", forwardToSentry: false);

                surface.Exception += surfaceExceptionEventHandler;

            
                if (appSettings.deviceLogitechEnabled)
                {
                    LoadDeviceProvider(LogitechDeviceProvider.Instance);
                }
                    

                if (appSettings.deviceCorsairEnabled)
                {
                    var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                    var natives = CorsairDeviceProvider.PossibleX64NativePaths;
                    natives.Add($"{enviroment}\\x64\\CUESDK.dll");

                    Debug.WriteLine($"{enviroment}\\x64\\CUESDK.dll");

                    LoadDeviceProvider(CorsairDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceCoolermasterEnabled)
                {
                    LoadDeviceProvider(CoolerMasterDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceNovationEnabled)
                {
                    LoadDeviceProvider(NovationDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceRazerEnabled)
                {
                    if (AppSettings.GetSettings().showEmulatorDevices)
                        RazerDeviceProvider.Instance.LoadEmulatorDevices = RazerEndpointType.All;

                    #if DEBUG
                        RazerDeviceProvider.Instance.LoadEmulatorDevices = RazerEndpointType.All;
                    #endif

                    LoadDeviceProvider(RazerDeviceProvider.Instance); 
                }
            
                if (appSettings.deviceAsusEnabled)
                {
                    LoadDeviceProvider(AsusDeviceProvider.Instance);
                }
                    
                
                if (appSettings.deviceMsiEnabled)
                {
                    LoadDeviceProvider(MsiDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceSteelseriesEnabled)
                {
                    LoadDeviceProvider(SteelSeriesDeviceProvider.Instance);
                }
                    
            
                if (appSettings.deviceWootingEnabled)
                {
                    LoadDeviceProvider(WootingDeviceProvider.Instance);
                }

                if (appSettings.deviceOpenRGBEnabled)
                {
                    var openrgb = new OpenRGBServerDefinition
                    {
                        Port = 6742,
                        Ip = "127.0.0.1",
                        ClientName = "Chromatics"
                    };

                    OpenRGBDeviceProvider.Instance.AddDeviceDefinition(openrgb);
                    LoadDeviceProvider(OpenRGBDeviceProvider.Instance);



                }   

                if (appSettings.deviceHueEnabled)
                {
                    try
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

                                    var migrated = lights.Data.Select(l => new Models.HueAdoptedDevice
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
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteConsole(Enums.LoggerTypes.Error, $"[HueDeviceProvider] LoadDeviceProvider Error: {ex.Message}");
                    }

                }

                if (appSettings.devicePlayStationEnabled)
                {
                    LoadDeviceProvider(PlayStationControllerRGBDeviceProvider.Instance);
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


                if (appSettings.rgbRefreshRate <= 0) appSettings.rgbRefreshRate = 0.05;

                _timerUpdateTrigger = new TimerUpdateTrigger();
                _timerUpdateTrigger.UpdateFrequency = appSettings.rgbRefreshRate;
                surface.RegisterUpdateTrigger(_timerUpdateTrigger);

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


                if (_activeDevices.ContainsKey(device))
                {
                    _activeDevices[device] = false;
                }
                else
                {
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

                if (_activeDevices.ContainsKey(device))
                {
                    _activeDevices[device] = true;
                }
                else
                {
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

                //Handle cases where a device is loaded with 0 LEDs
                if (device.Count() <= 0 && device.DeviceInfo.DeviceType == RGBDeviceType.Keyboard)
                {
                    var path = $"{enviroment}/Layouts/Default/Keyboard/Artemis XL keyboard-ISO.xml";

                    if (File.Exists(path))
                    {
                        var layout = DeviceLayout.Load(path);
                        LayoutExtension.ApplyTo(layout, device, true);

                        #if DEBUG
                            Debug.WriteLine($"Loaded layout for {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}. New Leds: {device.Count()}");
                        #endif
                    }
                }
                else if (device.Count() <= 0 && device.DeviceInfo.DeviceType == RGBDeviceType.Headset)
                {
                    var path = $"{enviroment}/Layouts/Default/Keyboard/Artemis 4 LEDs headset.xml";

                    if (File.Exists(path))
                    {
                        var layout = DeviceLayout.Load(path);
                        LayoutExtension.ApplyTo(layout, device, true);

                        #if DEBUG
                            Debug.WriteLine($"Loaded layout for {device.DeviceInfo.Manufacturer} {device.DeviceInfo.DeviceType}. New Leds: {device.Count()}");
                        #endif
                    }
                }

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

                if (_activeDevices.ContainsKey(device))
                {
                    _activeDevices[device] = !isDisabled;
                }
                else
                {
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

                if (_activeDevices.ContainsKey(device))
                {
                    _activeDevices[device] = false;
                }
                else
                {
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

        public static void Unload()
        {
            if (!_loaded) return;

            try
            {
                var appSettings = AppSettings.GetSettings();

                foreach (var deviceProvider in loadedDeviceProviders)
                {
                    UnloadDeviceProvider(deviceProvider, false);
                }

                loadedDeviceProviders.Clear();

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
        {
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

                    if (showErrors)
                        provider.Exception += deviceExceptionEventHandler;

                    provider.DevicesChanged += DevicesChanged;

                    surface.Load(provider);
                    loadedDeviceProviders.Add(provider);

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
                        if (_activeDevices.ContainsKey(device))
                            _activeDevices[device] = false;
                        else
                            _activeDevices.Add(device, false);
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
                Logger.WriteConsole(Enums.LoggerTypes.Error, $"[{provider.Devices.FirstOrDefault().DeviceInfo.DeviceName}] LoadDeviceProvider Error: {ex.Message}");
                return false;
            }
            
        }

        public static void UnloadDeviceProvider(IRGBDeviceProvider provider, bool removeFromList = true)
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

                        _activeDevices.Remove(device);
                        anyRemoved = true;
                    }

                    provider.Exception -= deviceExceptionEventHandler;
                    provider.DevicesChanged -= DevicesChanged;

                    if (removeFromList)
                        loadedDeviceProviders.Remove(provider);

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
            ledgroup.ZIndex = 1000;
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
            // Drop the surface to the idle tick rate whether or not the startup
            // animation is enabled — nothing game-driven is running either way.
            SetIdleUpdateRate(true);

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

                ledgroup.ZIndex = 1000;
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
                SetIdleUpdateRate(false);
            }

            foreach (var effects in _runningEffects)
            {
                foreach (var decorator in effects.Decorators)
                {
                    decorator.IsEnabled = false;
                }

                effects.RemoveAllDecorators();
                effects.Detach();
            }

            _runningEffects.Clear();

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

            foreach (var g in snapshot)
            {
                g.Detach();
                _runningEffects.Remove(g);
            }
        }

        public static bool IsBaseLayerEffectRunning()
        {
            return _baseLayerEffectRunning;
        }

        public static void SetBaseLayerEffect(bool toggle)
        {
            _baseLayerEffectRunning = toggle;
        }

        public static List<ListLedGroup> GetRunningEffects()
        {
            return _runningEffects;
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
            return _activeDevices;
        }

        public static List<IRGBDeviceProvider> GetDeviceProviders()
        {
            return loadedDeviceProviders;
        }

        public static List<Led> GetLiveLayerGroupCollection()
        {
            return _layergroupledcollection;
        }

        public static Dictionary<int, ListLedGroup[]> GetLiveLayerGroups()
        {
            return _layergroups;
        }

        public static void RemoveLayerGroup(int targetId)
        {
            if (_layergroups.ContainsKey(targetId))
            {
                foreach (var layer in _layergroups[targetId])
                {
                    layer.RemoveAllDecorators();
                    layer.Detach();

                    if (_runningEffects.Contains(layer))
                        _runningEffects.Remove(layer);
                }
                
                _layergroups.Remove(targetId);
                               
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
