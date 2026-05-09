using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Chromatics.Extensions.RGB.NET.Decorators;
using Chromatics.Extensions.RGB.NET.Devices;
using Chromatics.Extensions.RGB.NET.Devices.LIFX;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
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
using RGBColor = RGB.NET.Core.Color;
using SolidColorBrush = RGB.NET.Core.SolidColorBrush;
using GradientStop = RGB.NET.Presets.Textures.Gradients.GradientStop;
using Timer = System.Timers.Timer;

namespace Chromatics.DecoratorHarnessUI;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private const int SurfaceTickMs = 50;

    private const int MaxColorSlots = 16;

    private readonly RGBSurface _surface = new();
    private Timer? _surfaceTimer;
    private IRGBDeviceProvider? _loadedProvider;
    private ListLedGroup? _activeGroup;
    private Action? _detachAction;

    // Optional always-on highlight that paints a static colour on the WASD
    // keys, useful for spotting where the gameplay-movement keys land
    // while iterating an effect's visual. Not part of the code-snippet
    // output (harness-only diagnostic chrome).
    //
    // Implemented via a Surface.Updating subscription rather than a
    // ListLedGroup at higher ZIndex because many decorators
    // (StarfieldDecorator, BPMRipple, BPM* family) write LEDs directly via
    // their own surface.Updating handlers and ignore brush/group renders.
    // Subscribing late (after Start runs and decorators have subscribed)
    // makes our handler the LAST event subscriber, so our writes overwrite
    // the decorator's writes and the highlight always wins.
    private bool _wasdHookSubscribed;

    public MainViewModel()
    {
        ColorSlots.CollectionChanged += OnColorSlotsChanged;
    }

    // ── Provider ─────────────────────────────────────────────────────────

    public record ProviderItem(string Name, Func<IRGBDeviceProvider> Factory);

    public static ProviderItem[] AvailableProviders { get; } =
    [
        new("Razer",        () => RazerDeviceProvider.Instance),
        new("Logitech",     () => LogitechDeviceProvider.Instance),
        new("Corsair",      () => CorsairDeviceProvider.Instance),
        new("CoolerMaster", () => CoolerMasterDeviceProvider.Instance),
        new("Asus",         () => AsusDeviceProvider.Instance),
        new("MSI",          () => MsiDeviceProvider.Instance),
        new("SteelSeries",  () => SteelSeriesDeviceProvider.Instance),
        new("Wooting",      () => WootingDeviceProvider.Instance),
        new("Novation",     () => NovationDeviceProvider.Instance),
        new("OpenRGB",      () => OpenRGBDeviceProvider.Instance),
        new("PlayStation",  () => PlayStationControllerRGBDeviceProvider.Instance),
        new("LIFX",         () => LifxRGBDeviceProvider.Instance),
    ];

    [ObservableProperty] private ProviderItem? _selectedProvider;
    [ObservableProperty] private bool _providerLoaded;
    [ObservableProperty] private string _providerStatus = "Not loaded";

    public ObservableCollection<DeviceItem> Devices { get; } = [];

    public record DeviceItem(string Name, string Type, IRGBDevice Device)
    {
        public bool IsSelected { get; set; } = true;
    }

    [RelayCommand]
    private async Task LoadProvider()
    {
        if (SelectedProvider is null) return;

        ProviderStatus = "Loading...";
        try
        {
            var provider = SelectedProvider.Factory();

            // LIFX is the only provider in the harness that needs an
            // adoption gate — the LAN protocol has no concept of "all
            // devices on the segment", so the user has to pick which
            // bulbs Chromatics drives. Reuses the main app's adoption
            // dialog (we have a project reference to Chromatics) and
            // persists picks to a harness-local file so the user
            // doesn't have to re-discover every launch.
            if (provider is LifxRGBDeviceProvider lifxProvider)
            {
                var saved = LoadHarnessLifxAdoptions();
                var alreadyAdopted = saved.ToDictionary(d => d.Mac, d => d, StringComparer.OrdinalIgnoreCase);

                var owner = GetMainWindow();
                var dlg = new Chromatics.Views.LifxAdoptionDialog(alreadyAdopted);
                if (owner is not null) await dlg.ShowDialog(owner);
                else dlg.Show();

                if (!dlg.Saved)
                {
                    ProviderStatus = "Cancelled";
                    return;
                }

                SaveHarnessLifxAdoptions(dlg.SelectedDevices);

                lifxProvider.ClientDefinitions.Clear();
                foreach (var d in dlg.SelectedDevices)
                {
                    System.Net.IPEndPoint? ep = null;
                    if (!string.IsNullOrEmpty(d.LastIp) && System.Net.IPAddress.TryParse(d.LastIp, out var ip))
                        ep = new System.Net.IPEndPoint(ip, Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol.LifxDiscovery.LifxPort);
                    lifxProvider.ClientDefinitions.Add(
                        new LifxClientDefinition(d.Mac, d.Label, ep, d.ProductId, d.ZoneCount));
                }
            }

            provider.Initialize(throwExceptions: false);
            _surface.Load(provider);
            _loadedProvider = provider;

            Devices.Clear();
            foreach (var d in _surface.Devices)
                Devices.Add(new DeviceItem(d.DeviceInfo.DeviceName, d.DeviceInfo.DeviceType.ToString(), d));

            StartSurfaceTick();
            ProviderLoaded = true;
            ProviderStatus = $"{Devices.Count} device(s) loaded";
            // Now that the surface has devices, evaluate the WASD highlight
            // hook so the toggle works even before the user starts an effect.
            RefreshWasdOverlay();
        }
        catch (Exception ex)
        {
            ProviderStatus = $"Error: {ex.Message}";
        }
    }

    private static Avalonia.Controls.Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    private static string LifxAdoptionsPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Chromatics.DecoratorHarnessUI",
        "lifx-adopted.json");

    private static List<Chromatics.Models.LifxAdoptedDevice> LoadHarnessLifxAdoptions()
    {
        try
        {
            if (!System.IO.File.Exists(LifxAdoptionsPath)) return new();
            var json = System.IO.File.ReadAllText(LifxAdoptionsPath);
            return System.Text.Json.JsonSerializer.Deserialize<List<Chromatics.Models.LifxAdoptedDevice>>(json) ?? new();
        }
        catch { return new(); }
    }

    private static void SaveHarnessLifxAdoptions(IEnumerable<Chromatics.Models.LifxAdoptedDevice> adoptions)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(LifxAdoptionsPath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(adoptions, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(LifxAdoptionsPath, json);
        }
        catch { /* best-effort; harness will just re-prompt next launch */ }
    }

    // ── Effect selection ─────────────────────────────────────────────────

    public record EffectEntry(string Name, string Category, string? BasedOn = null)
    {
        public string DisplayName => BasedOn is not null
            ? $"[Saved] {BasedOn}"
            : Category == "Decorators" ? Name : $"[{Category}]  {Name}";
    }

    public static EffectEntry[] RawEffects { get; } =
    [
        new("StarfieldDecorator",              "Decorators"),
        new("FastStarfieldDecorator",           "Decorators"),
        new("BPMStarfieldDecorator",            "Decorators"),
        new("BPMFastStarfieldDecorator",        "Decorators"),
        new("StrobeDecorator",                  "Decorators"),
        new("PulseDecorator",                   "Decorators"),
        new("BPMPWMDecorator",                  "Decorators"),
        new("BPMSINDecorator",                  "Decorators"),
        new("ArenaLightShowDecorator",          "Decorators"),
        new("BlockFallEffect",                  "Decorators"),
        new("ShotFlashDecorator",               "Decorators"),
        new("MoveBPMGradientDecorator",         "Decorators"),
        new("MoveBPMDiagonalGradientDecorator", "Decorators"),
        new("AudioVisualizerEffect",            "Decorators"),
        new("FireEffect",                       "Decorators"),
        new("LaserEffect",                      "Decorators"),
        new("CircularPulseEffect",              "Decorators"),
        new("BPMRippleDecorator",               "Decorators"),
        new("BPMChaseDecorator",                "Decorators"),
        new("BPMLaserEffect",                   "Decorators"),
        new("BPMLaserEffect2",                  "Decorators"),
        new("BPMCircularPulseEffect",           "Decorators"),
        new("MatrixEffect",                     "Decorators"),
        new("BPMMatrixEffect",                  "Decorators"),
        new("BPMChaseRandom",                   "Decorators"),
        new("BPMThunderstrikeEffect",           "Decorators"),
        new("BPMHeartbeatEffect",               "Decorators"),
        new("BPMEqualizerEffect",               "Decorators"),
        new("BPMSpinnerEffect",                 "Decorators"),
    ];

    public static EffectEntry[] RaidPresets { get; } =
    [
        new("Summit of Everkeep (Zoraal Ja)",       "[RAID] Presets"),
        new("Interphos (Queen Eternal)",             "[RAID] Presets"),
        new("Scratching Ring (Black Cat)",           "[RAID] Presets"),
        new("Lovely Lovering (Honey B. Lovely)",     "[RAID] Presets"),
        new("Blasting Ring (Brute Bomber)",          "[RAID] Presets"),
        new("The Thundering (Wicked Thunder)",       "[RAID] Presets"),
        new("Sphere of Naught (Cloud of Darkness)",  "[RAID] Presets"),
        // Dawntrail M5-M8 single-effect presets
        new("Groovy Ring (Dancing Green)",           "[RAID] Presets"),
        new("Rebel Ring (Sugar Riot)",               "[RAID] Presets"),
        new("Demolition Site (Brute Abombinator)",   "[RAID] Presets"),
        // M8 floor is BGM-switched: normal (20149) vs savage (20150).
        // Each variant is its own harness preset so the user can preview
        // whichever they're building an effect for.
        new("Hunter's Ring — Howling Blade (M8)",    "[RAID] Presets"),
        new("Hunter's Ring — Howling Blade (M8S)",   "[RAID] Presets"),
        // Arcadia (M12/M12S) runs a scripted three-beat choreography in-game.
        // Each beat is its own preset; the harness cannot replay the per-layer
        // state machine, so each one is previewable in isolation.
        new("Arcadia P1 Effect 1 — Intro",           "[RAID] Presets"),
        new("Arcadia P1 Effect 2 — Post-Flash",      "[RAID] Presets"),
        new("Arcadia P2 (M12)",                      "[RAID] Presets"),
    ];

    public static EffectEntry[] WeatherPresets { get; } =
    [
        new("Moon Dust (Mare Lamentorum)", "[WEATHER] Presets"),
        new("Rain",                        "[WEATHER] Presets"),
        new("Showers",                     "[WEATHER] Presets"),
        new("Snow",                        "[WEATHER] Presets"),
        new("Blizzards",                   "[WEATHER] Presets"),
        new("Wind",                        "[WEATHER] Presets"),
        new("Gales",                       "[WEATHER] Presets"),
        new("Sandstorms / Dust Storms",    "[WEATHER] Presets"),
        new("Thunder / Thunderstorms",     "[WEATHER] Presets"),
        new("Umbral Wind",                 "[WEATHER] Presets"),
        new("Astromagnetic Storm",         "[WEATHER] Presets"),
        new("Umbral Static",              "[WEATHER] Presets"),
        new("Everlasting Light",           "[WEATHER] Presets"),
    ];

    public ObservableCollection<EffectEntry> AllEffects { get; } = new(
        RawEffects.Concat(RaidPresets).Concat(WeatherPresets));

    [ObservableProperty] private EffectEntry? _selectedEffect;
    [ObservableProperty] private string _selectedEffectLabel = "Parameters";

    // ── Parameters ───────────────────────────────────────────────────────

    [ObservableProperty] private int _paramBpm = 128;
    [ObservableProperty] private int _paramSpeed = 180;
    [ObservableProperty] private double _paramInterval = 20;
    [ObservableProperty] private double _paramFadeSpeed = 200;
    [ObservableProperty] private double _paramDensity = 1.0;
    [ObservableProperty] private double _paramWaveSpeed = 3.0;
    [ObservableProperty] private double _paramWaveFreq = 1.0;
    [ObservableProperty] private double _paramFadeTime = 0.10;
    [ObservableProperty] private int _paramGroupSize = 4;
    [ObservableProperty] private int _paramBlockSize = 3;
    [ObservableProperty] private int _paramBlocks = 6;
    [ObservableProperty] private int _paramSize = 100;
    [ObservableProperty] private int _paramNumberOfLeds = 10;
    [ObservableProperty] private int _paramStepSpeed = 4;
    [ObservableProperty] private double _paramSustain = 0.40;
    [ObservableProperty] private double _paramRelease = 0.40;
    [ObservableProperty] private int _paramRepetitions = 0;
    [ObservableProperty] private bool _paramDirection = true;
    [ObservableProperty] private bool _paramRandomise = true;
    [ObservableProperty] private string _paramTextureType = "Linear";
    [ObservableProperty] private string _paramDiagonalDir = "Random";
    [ObservableProperty] private string _paramFallDir = "TopToBottom";

    [ObservableProperty] private double _paramIntensity = 1.5;
    [ObservableProperty] private double _paramFlickerSpeed = 3.0;
    [ObservableProperty] private double _paramBeamWidth = 1.5;
    [ObservableProperty] private double _paramPulseRadius = 12.0;
    [ObservableProperty] private double _paramFadeWidth = 2.0;
    [ObservableProperty] private double _paramTailLength = 5.0;
    [ObservableProperty] private double _paramSpawnInterval = 0.3;
    [ObservableProperty] private double _paramRippleSpeed = 8.0;
    [ObservableProperty] private string _paramLaserDir = "Random";
    [ObservableProperty] private string _paramBpmSpeed = "Sync";
    [ObservableProperty] private double _paramBeatsPerCycle = 1.0;

    // Params for the new BPM decorators (BPMLaserEffect2, MatrixEffect family,
    // BPMChaseRandom, Thunderstrike, Heartbeat, Equalizer, Spinner).
    [ObservableProperty] private int _paramSimultaneousBeams = 3;
    [ObservableProperty] private double _paramFlickerOpacity = 0.4;
    [ObservableProperty] private string _paramMatrixDir = "Down";
    [ObservableProperty] private double _paramFadeBetween = 0.0;
    [ObservableProperty] private int _paramAccentEvery = 4;
    [ObservableProperty] private double _paramDecay = 0.5;
    [ObservableProperty] private double _paramWedgeDegrees = 60;

    public static string[] TextureTypes { get; } = ["Linear", "Conical"];
    public static string[] DiagonalDirections { get; } = ["TopLeftToBottomRight", "TopRightToBottomLeft", "BottomLeftToTopRight", "BottomRightToTopLeft", "Random"];
    public static string[] FallDirections { get; } = ["TopToBottom", "BottomToTop", "LeftToRight", "RightToLeft"];
    public static string[] LaserDirections { get; } = ["Horizontal", "Vertical", "DiagonalForward", "DiagonalBackward", "RandomHV", "RandomDiagonal", "RandomAll"];
    public static string[] BpmSpeedModifiers { get; } = ["/8", "/4", "/2", "Sync", "x2", "x4", "x8", "Custom"];

    // Map speed modifier label → beatsPerCycle (how many beats one cycle takes)
    private double ResolveBeatsPerCycle() => ParamBpmSpeed switch
    {
        "/8"     => 8.0,
        "/4"     => 4.0,
        "/2"     => 2.0,
        "Sync"   => 1.0,
        "x2"     => 0.5,
        "x4"     => 0.25,
        "x8"     => 0.125,
        "Custom" => Math.Max(0.05, ParamBeatsPerCycle),
        _        => 1.0,
    };

    [ObservableProperty] private AvColor _colorBase = AvColors.Black;

    // ── ASDW highlight overlay ───────────────────────────────────────────
    // Always-available diagnostic toggle in the harness (not wired into
    // the code-snippet generator). Paints a static colour on Keyboard A,
    // S, D, W keys at a higher ZIndex than the running effect, so the
    // user can see where the WASD movement keys land relative to the
    // effect they're iterating.
    [ObservableProperty] private bool _paramAsdwHighlight;
    [ObservableProperty] private AvColor _asdwHighlightColor = AvColors.Yellow;
    public Avalonia.Media.SolidColorBrush AsdwHighlightColorBrush => new(AsdwHighlightColor);

    partial void OnParamAsdwHighlightChanged(bool value) => RefreshWasdOverlay();
    partial void OnAsdwHighlightColorChanged(AvColor value)
    {
        OnPropertyChanged(nameof(AsdwHighlightColorBrush));
        RefreshWasdOverlay();
    }

    public ObservableCollection<ColorSlot> ColorSlots { get; } = [];

    public bool CanAddColor => ColorSlots.Count < MaxColorSlots;
    public bool CanRemoveColor => ColorSlots.Count > 1;

    [RelayCommand]
    private void AddColor()
    {
        if (!CanAddColor) return;
        ColorSlots.Add(new ColorSlot { Index = ColorSlots.Count + 1, Color = AvColors.White });
    }

    [RelayCommand]
    private void RemoveColor(ColorSlot? slot)
    {
        if (slot is null || !CanRemoveColor) return;
        ColorSlots.Remove(slot);
    }

    private void OnColorSlotsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (ColorSlot s in e.OldItems) s.PropertyChanged -= OnSlotPropertyChanged;
        if (e.NewItems != null)
            foreach (ColorSlot s in e.NewItems) s.PropertyChanged += OnSlotPropertyChanged;

        for (var i = 0; i < ColorSlots.Count; i++) ColorSlots[i].Index = i + 1;

        OnPropertyChanged(nameof(CanAddColor));
        OnPropertyChanged(nameof(CanRemoveColor));
        RegenerateCode();
        LiveRestart();
    }

    private void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ColorSlot.Color))
        {
            RegenerateCode();
            LiveRestart();
        }
    }

    private void SetColors(params AvColor[] colors)
    {
        while (ColorSlots.Count > colors.Length) ColorSlots.RemoveAt(ColorSlots.Count - 1);
        for (var i = 0; i < colors.Length; i++)
        {
            if (i < ColorSlots.Count) ColorSlots[i].Color = colors[i];
            else ColorSlots.Add(new ColorSlot { Index = i + 1, Color = colors[i] });
        }
    }

    private RGBColor[] SlotsRgb() => ColorSlots.Select(s => ToRgb(s.Color)).ToArray();

    private RGBColor SlotRgb(int i)
    {
        if (ColorSlots.Count == 0) return new RGBColor(0, 0, 0);
        var idx = Math.Min(i, ColorSlots.Count - 1);
        return ToRgb(ColorSlots[idx].Color);
    }

    private string SlotCode(int i)
    {
        if (i < ColorSlots.Count) return Rgb(ColorSlots[i].Color);
        return ColorSlots.Count > 0 ? Rgb(ColorSlots[^1].Color) : "new Color(255, 255, 255)";
    }

    private string SlotsArrayCode() => "new Color[] { " + string.Join(", ", ColorSlots.Select(s => Rgb(s.Color))) + " }";

    // ── Parameter visibility ─────────────────────────────────────────────

    [ObservableProperty] private bool _showBpm;
    [ObservableProperty] private bool _showSpeed;
    [ObservableProperty] private bool _showInterval;
    [ObservableProperty] private bool _showFadeSpeed;
    [ObservableProperty] private bool _showDensity;
    [ObservableProperty] private bool _showWaveSpeed;
    [ObservableProperty] private bool _showWaveFreq;
    [ObservableProperty] private bool _showFadeTime;
    [ObservableProperty] private bool _showGroupSize;
    [ObservableProperty] private bool _showSize;
    [ObservableProperty] private bool _showBlocks;
    [ObservableProperty] private bool _showBlockSize;
    [ObservableProperty] private bool _showNumberOfLeds;
    [ObservableProperty] private bool _showStepSpeed;
    [ObservableProperty] private bool _showSustain;
    [ObservableProperty] private bool _showRelease;
    [ObservableProperty] private bool _showRepetitions;
    [ObservableProperty] private bool _showDirection;
    [ObservableProperty] private bool _showRandomise;
    [ObservableProperty] private bool _showTextureType;
    [ObservableProperty] private bool _showDiagonalDir;
    [ObservableProperty] private bool _showFallDir;
    [ObservableProperty] private bool _showIntensity;
    [ObservableProperty] private bool _showFlickerSpeed;
    [ObservableProperty] private bool _showBeamWidth;
    [ObservableProperty] private bool _showPulseRadius;
    [ObservableProperty] private bool _showFadeWidth;
    [ObservableProperty] private bool _showTailLength;
    [ObservableProperty] private bool _showSpawnInterval;
    [ObservableProperty] private bool _showRippleSpeed;
    [ObservableProperty] private bool _showLaserDir;
    [ObservableProperty] private bool _showBpmSpeed;
    [ObservableProperty] private bool _showBeatsPerCycle;
    [ObservableProperty] private bool _showColorBase = true;

    // Visibility flags for the new BPM decorators.
    [ObservableProperty] private bool _showSimultaneousBeams;
    [ObservableProperty] private bool _showFlickerOpacity;
    [ObservableProperty] private bool _showMatrixDir;
    [ObservableProperty] private bool _showFadeBetween;
    [ObservableProperty] private bool _showAccentEvery;
    [ObservableProperty] private bool _showDecay;
    [ObservableProperty] private bool _showWedgeDegrees;

    public static string[] MatrixDirections { get; } = ["Down", "Up", "Left", "Right"];

    // ── Live update ──────────────────────────────────────────────────────

    [ObservableProperty] private bool _liveUpdate = true;

    private void LiveRestart()
    {
        if (LiveUpdate && IsRunning) Restart();
    }

    [RelayCommand] private void BpmDiv8()  => ParamBpm = Math.Max(1, ParamBpm / 8);
    [RelayCommand] private void BpmDiv4()  => ParamBpm = Math.Max(1, ParamBpm / 4);
    [RelayCommand] private void BpmDiv2()  => ParamBpm = Math.Max(1, ParamBpm / 2);
    [RelayCommand] private void BpmMul2()  => ParamBpm = Math.Min(999, ParamBpm * 2);
    [RelayCommand] private void BpmMul4()  => ParamBpm = Math.Min(999, ParamBpm * 4);
    [RelayCommand] private void BpmMul8()  => ParamBpm = Math.Min(999, ParamBpm * 8);

    // ── Color brush properties for swatches ─────────────────────────────

    public Avalonia.Media.SolidColorBrush ColorBaseBrush => new(ColorBase);

    partial void OnColorBaseChanged(AvColor value) { OnPropertyChanged(nameof(ColorBaseBrush)); RegenerateCode(); LiveRestart(); }

    // ── Running state ────────────────────────────────────────────────────

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _runningStatus = "Stopped";

    partial void OnSelectedEffectChanged(EffectEntry? value)
    {
        if (value is null) return;
        SelectedEffectLabel = $"Parameters — {value.Name}";
        UpdateParamVisibility(value.Name);
        ApplyDefaults(value);
        RegenerateCode();
    }

    private void UpdateParamVisibility(string name)
    {
        // BPM-driven effects
        ShowBpm = name is "BPMStarfieldDecorator" or "BPMFastStarfieldDecorator"
            or "Summit of Everkeep (Zoraal Ja)" or "Scratching Ring (Black Cat)"
            or "Blasting Ring (Brute Bomber)" or "The Thundering (Wicked Thunder)"
            or "BPMPWMDecorator" or "BPMSINDecorator"
            or "MoveBPMGradientDecorator" or "MoveBPMDiagonalGradientDecorator"
            or "BPMRippleDecorator" or "BPMChaseDecorator"
            or "BPMLaserEffect" or "BPMLaserEffect2" or "BPMCircularPulseEffect"
            or "BPMMatrixEffect" or "BPMChaseRandom" or "BPMThunderstrikeEffect"
            or "BPMHeartbeatEffect" or "BPMEqualizerEffect" or "BPMSpinnerEffect"
            or "Groovy Ring (Dancing Green)" or "Rebel Ring (Sugar Riot)"
            or "Demolition Site (Brute Abombinator)"
            or "Hunter's Ring — Howling Blade (M8)" or "Hunter's Ring — Howling Blade (M8S)"
            or "Arcadia P1 Effect 1 — Intro" or "Arcadia P1 Effect 2 — Post-Flash"
            or "Arcadia P2 (M12)";

        // Speed-driven gradient effects
        ShowSpeed = name is "Interphos (Queen Eternal)" or "Wind" or "Gales"
            or "Sandstorms / Dust Storms" or "Umbral Wind" or "Astromagnetic Storm"
            or "Umbral Static" or "Everlasting Light" or "MatrixEffect";

        // Interval-based effects
        ShowInterval = name is "StarfieldDecorator" or "FastStarfieldDecorator"
            or "Moon Dust (Mare Lamentorum)" or "Rain" or "Showers" or "Snow" or "Blizzards"
            or "StrobeDecorator" or "Thunder / Thunderstorms" or "PulseDecorator"
            or "ArenaLightShowDecorator" or "Lovely Lovering (Honey B. Lovely)"
            or "Sphere of Naught (Cloud of Darkness)";

        // Fade speed
        ShowFadeSpeed = name is "StarfieldDecorator" or "FastStarfieldDecorator"
            or "BPMStarfieldDecorator" or "BPMFastStarfieldDecorator"
            or "Summit of Everkeep (Zoraal Ja)"
            or "Moon Dust (Mare Lamentorum)" or "Rain" or "Showers" or "Snow" or "Blizzards"
            or "StrobeDecorator" or "Thunder / Thunderstorms" or "PulseDecorator"
            or "ShotFlashDecorator" or "BlockFallEffect"
            or "Hunter's Ring — Howling Blade (M8)" or "Arcadia P1 Effect 1 — Intro";

        ShowDensity = name is "FastStarfieldDecorator" or "BPMStarfieldDecorator"
            or "BPMFastStarfieldDecorator" or "Summit of Everkeep (Zoraal Ja)"
            or "Hunter's Ring — Howling Blade (M8)" or "Arcadia P1 Effect 1 — Intro";

        ShowWaveSpeed = name is "ArenaLightShowDecorator"
            or "Lovely Lovering (Honey B. Lovely)" or "Sphere of Naught (Cloud of Darkness)";

        ShowWaveFreq = name is "ArenaLightShowDecorator"
            or "Lovely Lovering (Honey B. Lovely)" or "Sphere of Naught (Cloud of Darkness)"
            or "BPMPWMDecorator" or "BPMSINDecorator" or "The Thundering (Wicked Thunder)";

        ShowFadeTime = name is "BPMPWMDecorator" or "BPMSINDecorator"
            or "The Thundering (Wicked Thunder)" or "ShotFlashDecorator";

        ShowGroupSize = name is "BPMPWMDecorator" or "BPMSINDecorator"
            or "The Thundering (Wicked Thunder)" or "BPMChaseRandom";

        // BPMRippleDecorator-based raid presets use ParamFadeWidth as the
        // ripple width and ParamBpmSpeed (defaulting to /2 or /4) for the
        // beatsPerCycle divisor.
        // BPMChaseDecorator-based raid presets use ParamTailLength as the
        // tail-length parameter.

        ShowSize = name is "Everlasting Light";
        ShowBlocks = name is "BlockFallEffect";
        ShowBlockSize = name is "BlockFallEffect";

        ShowNumberOfLeds = name is "StarfieldDecorator" or "FastStarfieldDecorator"
            or "BPMStarfieldDecorator" or "BPMFastStarfieldDecorator"
            or "Hunter's Ring — Howling Blade (M8)" or "Arcadia P1 Effect 1 — Intro";

        ShowStepSpeed = name is "PulseDecorator";

        ShowSustain = name is "ShotFlashDecorator";
        ShowRelease = name is "ShotFlashDecorator";
        ShowRepetitions = name is "ShotFlashDecorator";

        ShowDirection = name is "MoveBPMGradientDecorator"
            or "Interphos (Queen Eternal)" or "Scratching Ring (Black Cat)"
            or "Wind" or "Gales" or "Sandstorms / Dust Storms" or "Umbral Wind"
            or "Astromagnetic Storm" or "Umbral Static" or "Everlasting Light";

        ShowRandomise = name is "StrobeDecorator" or "Thunder / Thunderstorms";

        ShowTextureType = name is "MoveBPMGradientDecorator" or "MoveBPMDiagonalGradientDecorator";

        ShowDiagonalDir = name is "MoveBPMDiagonalGradientDecorator"
            or "Blasting Ring (Brute Bomber)";

        ShowFallDir = name is "BlockFallEffect";

        ShowIntensity = name is "FireEffect";
        ShowFlickerSpeed = name is "FireEffect" or "MatrixEffect" or "BPMMatrixEffect";
        ShowBeamWidth = name is "LaserEffect" or "BPMLaserEffect" or "BPMLaserEffect2"
            or "BPMHeartbeatEffect";
        ShowPulseRadius = name is "CircularPulseEffect" or "BPMCircularPulseEffect"
            or "Hunter's Ring — Howling Blade (M8S)" or "Arcadia P2 (M12)";
        ShowFadeWidth = name is "CircularPulseEffect" or "BPMCircularPulseEffect" or "BPMRippleDecorator"
            or "Groovy Ring (Dancing Green)" or "Demolition Site (Brute Abombinator)"
            or "Hunter's Ring — Howling Blade (M8S)" or "Arcadia P2 (M12)";
        ShowTailLength = name is "BPMChaseDecorator" or "Rebel Ring (Sugar Riot)";
        ShowSpawnInterval = name is "LaserEffect" or "CircularPulseEffect";
        ShowRippleSpeed = name is "CircularPulseEffect"; // non-BPM only; BPM* now uses BpmSpeed
        ShowLaserDir = name is "LaserEffect" or "BPMLaserEffect" or "BPMLaserEffect2";
        ShowBpmSpeed = name is "BPMLaserEffect" or "BPMLaserEffect2" or "BPMCircularPulseEffect"
            or "BPMRippleDecorator" or "BPMMatrixEffect" or "BPMSpinnerEffect"
            or "Groovy Ring (Dancing Green)" or "Demolition Site (Brute Abombinator)"
            or "Hunter's Ring — Howling Blade (M8S)" or "Arcadia P2 (M12)";
        ShowBeatsPerCycle = ShowBpmSpeed && ParamBpmSpeed == "Custom";

        // New BPM decorator visibility flags
        ShowSimultaneousBeams = name is "BPMLaserEffect2";
        ShowFlickerOpacity = name is "MatrixEffect" or "BPMMatrixEffect";
        ShowMatrixDir = name is "MatrixEffect" or "BPMMatrixEffect";
        ShowFadeBetween = name is "BPMChaseRandom";
        ShowAccentEvery = name is "BPMThunderstrikeEffect" or "Arcadia P1 Effect 2 — Post-Flash";
        ShowDecay = name is "BPMThunderstrikeEffect" or "BPMEqualizerEffect" or "Arcadia P1 Effect 2 — Post-Flash";
        ShowWedgeDegrees = name is "BPMSpinnerEffect";

        ShowColorBase = name is not ("PulseDecorator" or "ShotFlashDecorator"
            or "MoveBPMGradientDecorator" or "MoveBPMDiagonalGradientDecorator");
    }

    private void ApplyDefaults(EffectEntry effect)
    {
        if (effect.BasedOn is not null && _savedParams.TryGetValue(effect.BasedOn, out var snap))
        {
            ParamBpm = snap.Bpm; ParamSpeed = snap.Speed; ParamInterval = snap.Interval;
            ParamFadeSpeed = snap.FadeSpeed; ParamDensity = snap.Density;
            ParamWaveSpeed = snap.WaveSpeed; ParamWaveFreq = snap.WaveFreq;
            ParamFadeTime = snap.FadeTime; ParamGroupSize = snap.GroupSize;
            ParamBlockSize = snap.BlockSize; ParamBlocks = snap.Blocks; ParamSize = snap.Size;
            ColorBase = snap.Base;
            SetColors(snap.Colors);
            return;
        }

        switch (effect.Name)
        {
            case "Summit of Everkeep (Zoraal Ja)":
                ParamBpm = 198; ParamFadeSpeed = 80; ParamDensity = 2.0;
                ColorBase = ToAv(0, 0, 0);
                SetColors(ToAv(52, 0, 126), ToAv(0, 0, 255), ToAv(128, 0, 128));
                break;
            case "Interphos (Queen Eternal)":
                ParamSpeed = 180;
                ColorBase = ToAv(50, 205, 50);
                SetColors(ToAv(0, 128, 0), ToAv(0, 255, 0), ToAv(255, 215, 0));
                break;
            case "Scratching Ring (Black Cat)":
                ParamBpm = 31;
                ColorBase = ToAv(255, 0, 255);
                SetColors(ToAv(30, 144, 255), ToAv(10, 29, 198), ToAv(255, 105, 180));
                break;
            case "Lovely Lovering (Honey B. Lovely)":
                ParamInterval = 20; ParamWaveSpeed = 3.0; ParamWaveFreq = 1.0;
                ColorBase = ToAv(255, 255, 0);
                SetColors(ToAv(255, 255, 0), ToAv(255, 19, 147));
                break;
            case "Blasting Ring (Brute Bomber)":
                ParamBpm = 27; ParamDiagonalDir = "TopLeftToBottomRight";
                ColorBase = ToAv(255, 0, 0);
                SetColors(ToAv(255, 140, 0), ToAv(255, 0, 0), ToAv(255, 140, 0));
                break;
            case "The Thundering (Wicked Thunder)":
                ParamBpm = 162; ParamWaveFreq = 1.0; ParamFadeTime = 0.10; ParamGroupSize = 4;
                ColorBase = ToAv(0, 0, 255);
                SetColors(ToAv(0, 0, 255), ToAv(255, 0, 255), ToAv(255, 255, 255), ToAv(0, 172, 255));
                break;
            case "Sphere of Naught (Cloud of Darkness)":
                ParamInterval = 20; ParamWaveSpeed = 3.0; ParamWaveFreq = 1.0;
                ColorBase = ToAv(147, 112, 219);
                SetColors(ToAv(255, 0, 255), ToAv(147, 112, 219), ToAv(128, 0, 128));
                break;
            // Dawntrail M5-M7 — defaults mirror RaidEffectProcessor.ApplyRaidEffect
            case "Groovy Ring (Dancing Green)":
                ParamBpm = 60; ParamBpmSpeed = "/2"; ParamFadeWidth = 5.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 255, 128), ToAv(255, 0, 200), ToAv(0, 200, 255));
                break;
            case "Rebel Ring (Sugar Riot)":
                ParamBpm = 160; ParamTailLength = 5.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(255, 80, 0), ToAv(255, 200, 0), ToAv(255, 40, 120), ToAv(180, 40, 255));
                break;
            case "Demolition Site (Brute Abombinator)":
                ParamBpm = 178; ParamBpmSpeed = "/2"; ParamFadeWidth = 2.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(255, 80, 0), ToAv(255, 200, 0), ToAv(255, 40, 40));
                break;
            // M8 Hunter's Ring splits on BGM id (20149 vs 20150 for normal vs savage)
            case "Hunter's Ring — Howling Blade (M8)":
                // Normal mode: BPMStarfield at 272 BPM, density 2. In-game uses layer.Count()/6 LEDs;
                // harness uses ParamNumberOfLeds — leave at default 10 and the user can tune.
                ParamBpm = 272; ParamFadeSpeed = 500; ParamDensity = 2.0; ParamNumberOfLeds = 10;
                ColorBase = AvColors.Black;
                SetColors(ToAv(255, 255, 255), ToAv(180, 220, 255), ToAv(120, 180, 255));
                break;
            case "Hunter's Ring — Howling Blade (M8S)":
                ParamBpm = 164; ParamBpmSpeed = "/4"; ParamPulseRadius = 12.0; ParamFadeWidth = 2.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(255, 255, 255), ToAv(180, 220, 255), ToAv(80, 140, 255));
                break;
            // Arcadia (M12/M12S) — three scripted visuals. Defaults match the
            // real-code hardcoded values (see RaidEffectProcessor.ApplyRaidEffect
            // Arcadia case).
            case "Arcadia P1 Effect 1 — Intro":
                ParamBpm = 360; ParamFadeSpeed = 2000; ParamDensity = 1.0; ParamNumberOfLeds = 6;
                ColorBase = AvColors.Black;
                SetColors(AvColors.White);
                break;
            case "Arcadia P1 Effect 2 — Post-Flash":
                ParamBpm = 360; ParamAccentEvery = 4; ParamDecay = 0.6;
                ColorBase = ToAv(1, 40, 5);
                SetColors(ToAv(255, 220, 180), ToAv(0, 255, 43));
                break;
            case "Arcadia P2 (M12)":
                ParamBpm = 165; ParamBpmSpeed = "/4"; ParamPulseRadius = 12.0; ParamFadeWidth = 1.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(255, 0, 4), ToAv(93, 0, 255), ToAv(255, 117, 0), ToAv(249, 255, 0));
                break;
            case "Moon Dust (Mare Lamentorum)":
                ParamInterval = 20; ParamFadeSpeed = 900;
                ColorBase = ToAv(0, 0, 0);
                SetColors(ToAv(255, 255, 255));
                break;
            case "Rain":
                ParamInterval = 20; ParamFadeSpeed = 200;
                ColorBase = ToAv(0, 0, 205);
                SetColors(ToAv(37, 41, 165));
                break;
            case "Showers":
                ParamInterval = 20; ParamFadeSpeed = 100;
                ColorBase = ToAv(0, 0, 205);
                SetColors(ToAv(0, 255, 127));
                break;
            case "Snow":
                ParamInterval = 40; ParamFadeSpeed = 1500;
                ColorBase = ToAv(135, 206, 235);
                SetColors(ToAv(0, 0, 0));
                break;
            case "Blizzards":
                ParamInterval = 20; ParamFadeSpeed = 200;
                ColorBase = ToAv(119, 136, 153);
                SetColors(ToAv(0, 0, 0));
                break;
            case "Wind":
                ParamSpeed = 180;
                ColorBase = ToAv(65, 105, 225);
                SetColors(ToAv(85, 156, 47));
                break;
            case "Gales":
                ParamSpeed = 220;
                ColorBase = ToAv(65, 105, 225);
                SetColors(ToAv(85, 156, 47));
                break;
            case "Sandstorms / Dust Storms":
                ParamSpeed = 220; ParamDirection = false;
                ColorBase = ToAv(205, 133, 63);
                SetColors(ToAv(255, 165, 0));
                break;
            case "Thunder / Thunderstorms":
                ParamInterval = 10000; ParamFadeSpeed = 100;
                ColorBase = ToAv(138, 43, 226);
                SetColors(ToAv(255, 255, 255));
                break;
            case "Umbral Wind":
                ParamSpeed = 200;
                ColorBase = ToAv(10, 29, 198);
                SetColors(ToAv(0, 255, 127));
                break;
            case "Astromagnetic Storm":
                ParamSpeed = 120;
                ColorBase = ToAv(255, 0, 255);
                SetColors(ToAv(255, 0, 255), ToAv(255, 20, 147), ToAv(0, 191, 255));
                break;
            case "Umbral Static":
                ParamSpeed = 100;
                ColorBase = ToAv(0, 0, 205);
                SetColors(ToAv(0, 255, 255));
                break;
            case "Everlasting Light":
                ParamSpeed = 180; ParamSize = 100;
                ColorBase = ToAv(255, 248, 220);
                SetColors(ToAv(255, 255, 0));
                break;

            // Raw decorators — sensible defaults
            case "StarfieldDecorator":
                ParamInterval = 25; ParamFadeSpeed = 600; ParamNumberOfLeds = 10;
                ColorBase = AvColors.Black;
                SetColors(AvColors.White);
                break;
            case "FastStarfieldDecorator":
                ParamInterval = 10; ParamFadeSpeed = 200; ParamDensity = 1.0; ParamNumberOfLeds = 10;
                ColorBase = AvColors.Black;
                SetColors(AvColors.White);
                break;
            case "BPMStarfieldDecorator":
                ParamBpm = 128; ParamFadeSpeed = 80; ParamDensity = 1.0; ParamNumberOfLeds = 10;
                ColorBase = AvColors.Black;
                SetColors(AvColors.White);
                break;
            case "BPMFastStarfieldDecorator":
                ParamBpm = 198; ParamFadeSpeed = 80; ParamDensity = 2.0; ParamNumberOfLeds = 10;
                ColorBase = AvColors.Black;
                SetColors(AvColors.Blue, AvColors.Purple);
                break;
            case "StrobeDecorator":
                ParamInterval = 120; ParamFadeSpeed = 60; ParamRandomise = true;
                ColorBase = AvColors.Black;
                SetColors(AvColors.White);
                break;
            case "PulseDecorator":
                ParamInterval = 60; ParamFadeSpeed = 400; ParamStepSpeed = 4;
                SetColors(AvColors.White);
                break;
            case "BPMPWMDecorator":
                ParamBpm = 162; ParamWaveFreq = 1.0; ParamFadeTime = 0.10; ParamGroupSize = 4;
                ColorBase = AvColors.Black;
                SetColors(AvColors.Red, AvColors.Blue);
                break;
            case "BPMSINDecorator":
                ParamBpm = 128; ParamWaveFreq = 1.0; ParamFadeTime = 0.25; ParamGroupSize = 4;
                ColorBase = AvColors.Black;
                SetColors(AvColors.Red, AvColors.Blue);
                break;
            case "ArenaLightShowDecorator":
                ParamInterval = 20; ParamWaveSpeed = 3.0; ParamWaveFreq = 1.0;
                ColorBase = AvColors.Black;
                SetColors(AvColors.Cyan, AvColors.Magenta);
                break;
            case "BlockFallEffect":
                ParamBlocks = 6; ParamBlockSize = 3; ParamFadeSpeed = 2.0; ParamFallDir = "TopToBottom";
                ColorBase = AvColors.Black;
                SetColors(AvColors.Red, AvColors.Green, AvColors.Blue);
                break;
            case "MoveBPMGradientDecorator":
                ParamBpm = 32; ParamDirection = true; ParamTextureType = "Linear";
                SetColors(AvColors.Red, AvColors.Green, AvColors.Blue);
                break;
            case "MoveBPMDiagonalGradientDecorator":
                ParamBpm = 28; ParamDiagonalDir = "Random"; ParamTextureType = "Linear";
                SetColors(AvColors.Red, AvColors.Green, AvColors.Blue);
                break;
            case "AudioVisualizerEffect":
                ColorBase = AvColors.Black;
                SetColors(AvColors.Green, AvColors.Yellow, AvColors.Red);
                break;
            case "ShotFlashDecorator":
                ParamFadeTime = 0.05; ParamFadeSpeed = 0.15; ParamSustain = 0.40; ParamRelease = 0.40; ParamRepetitions = 0;
                SetColors(AvColors.White);
                break;
            case "FireEffect":
                ParamIntensity = 1.5; ParamFlickerSpeed = 3.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(20, 0, 0), ToAv(200, 40, 0), ToAv(255, 120, 0), ToAv(255, 220, 50));
                break;
            case "LaserEffect":
                ParamSpeed = 8; ParamBeamWidth = 1.5; ParamSpawnInterval = 0.3; ParamLaserDir = "RandomAll";
                ColorBase = AvColors.Black;
                SetColors(AvColors.Cyan, AvColors.Magenta, AvColors.Lime);
                break;
            case "CircularPulseEffect":
                ParamRippleSpeed = 6.0; ParamPulseRadius = 12.0; ParamSpawnInterval = 0.5; ParamFadeWidth = 2.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 200, 255), ToAv(255, 0, 200), ToAv(0, 255, 100));
                break;
            case "BPMRippleDecorator":
                ParamBpm = 128; ParamBpmSpeed = "Sync"; ParamFadeWidth = 2.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 150, 255), ToAv(255, 50, 200), ToAv(0, 255, 150));
                break;
            case "BPMChaseDecorator":
                ParamBpm = 128; ParamTailLength = 5.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(255, 0, 100), ToAv(0, 100, 255), ToAv(255, 200, 0));
                break;
            case "BPMLaserEffect":
                ParamBpm = 120; ParamBpmSpeed = "Sync"; ParamBeamWidth = 1.5; ParamLaserDir = "RandomAll";
                ColorBase = AvColors.Black;
                SetColors(AvColors.Cyan, AvColors.Magenta, AvColors.Lime);
                break;
            case "BPMCircularPulseEffect":
                ParamBpm = 120; ParamBpmSpeed = "Sync"; ParamPulseRadius = 12.0; ParamFadeWidth = 2.0;
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 200, 255), ToAv(255, 0, 200), ToAv(0, 255, 100));
                break;
            case "BPMLaserEffect2":
                ParamBpm = 130; ParamBpmSpeed = "Sync"; ParamBeamWidth = 1.5;
                ParamSimultaneousBeams = 3; ParamLaserDir = "RandomAll";
                ColorBase = AvColors.Black;
                SetColors(AvColors.Cyan, AvColors.Magenta, AvColors.Lime, AvColors.Yellow);
                break;
            case "MatrixEffect":
                ParamSpeed = 8; ParamFlickerOpacity = 0.5; ParamFlickerSpeed = 8.0;
                ParamMatrixDir = "Down";
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 255, 100), ToAv(0, 200, 60));
                break;
            case "BPMMatrixEffect":
                ParamBpm = 110; ParamBpmSpeed = "Sync";
                ParamFlickerOpacity = 0.5; ParamFlickerSpeed = 8.0; ParamMatrixDir = "Down";
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 255, 100), ToAv(0, 200, 60));
                break;
            case "BPMChaseRandom":
                ParamBpm = 140; ParamFadeBetween = 0.0; ParamGroupSize = 1;
                ColorBase = AvColors.Black;
                SetColors(AvColors.Cyan, AvColors.Magenta, AvColors.Yellow);
                break;
            case "BPMThunderstrikeEffect":
                ParamBpm = 95; ParamAccentEvery = 4; ParamDecay = 0.6;
                ColorBase = ToAv(40, 0, 0);
                SetColors(ToAv(255, 220, 180), ToAv(255, 100, 80));
                break;
            case "BPMHeartbeatEffect":
                ParamBpm = 72; ParamBeamWidth = 2.0;
                ColorBase = ToAv(20, 0, 0);
                SetColors(ToAv(255, 30, 50), ToAv(255, 100, 100));
                break;
            case "BPMEqualizerEffect":
                ParamBpm = 120; ParamDecay = 0.5;
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 255, 0), ToAv(255, 200, 0), ToAv(255, 0, 0));
                break;
            case "BPMSpinnerEffect":
                ParamBpm = 130; ParamBpmSpeed = "Sync"; ParamWedgeDegrees = 60;
                ColorBase = AvColors.Black;
                SetColors(ToAv(0, 200, 255), ToAv(255, 0, 200), ToAv(255, 200, 0));
                break;
        }
    }

    // ── Start / Stop ─────────────────────────────────────────────────────

    [RelayCommand]
    private void Start()
    {
        if (!ProviderLoaded || SelectedEffect is null) return;
        Stop();

        var targetDevices = Devices.Where(d => d.IsSelected).Select(d => d.Device).ToList();
        if (targetDevices.Count == 0) return;

        var leds = targetDevices.SelectMany(d => d).ToArray();
        _activeGroup = new ListLedGroup(_surface, leds) { ZIndex = 100 };
        _activeGroup.Brush = new SolidColorBrush(ToRgb(ColorBase));

        try
        {
            _detachAction = BuildEffect(SelectedEffect, _activeGroup);
            IsRunning = true;
            RunningStatus = $"Running: {SelectedEffect.Name}";
            // Layer the WASD overlay on top of the just-built effect.
            RefreshWasdOverlay();
        }
        catch (Exception ex)
        {
            RunningStatus = $"Error: {ex.Message}";
            try { _activeGroup.Detach(); } catch { }
            _activeGroup = null;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (!IsRunning) return;
        _detachAction?.Invoke();
        _detachAction = null;
        try { _activeGroup?.Detach(); } catch { }
        BlackoutSurface();
        _activeGroup = null;
        IsRunning = false;
        RunningStatus = "Stopped";
        // The WASD overlay survives Stop() — it's a diagnostic chrome,
        // not part of the effect. Re-painted on Start() via Start →
        // RefreshWasdOverlay below (after _activeGroup is set up so
        // the higher-ZIndex overlay is layered above the new effect).
        RefreshWasdOverlay();
    }

    // Re-evaluate the WASD highlight subscription. Called whenever the
    // toggle changes, the colour changes, the provider state changes, or
    // an effect Start/Restart happens (so we always re-subscribe LAST and
    // beat any decorator that just subscribed inside BuildEffect).
    private void RefreshWasdOverlay()
    {
        bool wasSubscribed = _wasdHookSubscribed;
        if (_wasdHookSubscribed)
        {
            try { _surface.Updating -= OnSurfaceUpdating_WriteWasd; } catch { }
            _wasdHookSubscribed = false;
        }

        if (!ParamAsdwHighlight || !ProviderLoaded)
        {
            // Toggle just went OFF (or provider unloaded). When no effect
            // is running, nothing else writes to the W/A/S/D LEDs after
            // we unsubscribe — they'd latch at the last highlight colour
            // until some future effect overwrites them. Explicitly clear
            // them to black and push one render so the toggle visibly
            // turns the highlight off on hardware in the no-effect case.
            // (When an effect IS running its decorators will overwrite
            // the LEDs on the next tick, which is also fine.)
            if (wasSubscribed && ProviderLoaded)
            {
                var black = new Color((byte)0, (byte)0, (byte)0);
                foreach (var dev in _surface.Devices)
                {
                    if (dev.DeviceInfo.DeviceType != RGBDeviceType.Keyboard) continue;
                    foreach (var led in dev)
                    {
                        if (Array.IndexOf(WasdLedIds, led.Id) >= 0)
                            led.Color = black;
                    }
                }
                try { _surface.Update(); } catch { }
            }
            return;
        }

        _surface.Updating += OnSurfaceUpdating_WriteWasd;
        _wasdHookSubscribed = true;
    }

    // Read AsdwHighlightColor + selected keyboards EACH tick so a colour
    // picker change updates immediately without re-subscribing.
    private static readonly LedId[] WasdLedIds =
    [
        LedId.Keyboard_W, LedId.Keyboard_A, LedId.Keyboard_S, LedId.Keyboard_D,
    ];

    private void OnSurfaceUpdating_WriteWasd(UpdatingEventArgs args)
    {
        if (!ParamAsdwHighlight) return;

        var col = ToRgb(AsdwHighlightColor);
        foreach (var dev in _surface.Devices)
        {
            if (dev.DeviceInfo.DeviceType != RGBDeviceType.Keyboard) continue;
            foreach (var led in dev)
            {
                if (Array.IndexOf(WasdLedIds, led.Id) >= 0)
                    led.Color = col;
            }
        }
    }

    [RelayCommand]
    private void Restart()
    {
        Stop();
        Start();
    }

    [RelayCommand]
    private void SavePreset()
    {
        if (SelectedEffect is null) return;
        var name = $"Custom: {SelectedEffect.Name} #{AllEffects.Count(e => e.Category == "Saved") + 1}";
        var saved = new EffectEntry(SelectedEffect.Name, "Saved", name)
        {
            // We clone the current effect's Name so BuildEffect dispatches the same way.
            // The DisplayName shows the custom label via BasedOn.
        };

        var snap = new SavedParams(
            ParamBpm, ParamSpeed, ParamInterval, ParamFadeSpeed, ParamDensity,
            ParamWaveSpeed, ParamWaveFreq, ParamFadeTime, ParamGroupSize,
            ParamBlockSize, ParamBlocks, ParamSize,
            ColorBase, ColorSlots.Select(s => s.Color).ToArray());
        _savedParams[name] = snap;

        AllEffects.Add(saved);
        SelectedEffect = saved;
        RunningStatus = $"Saved: {name}";
    }

    private readonly Dictionary<string, SavedParams> _savedParams = new();

    private record SavedParams(
        int Bpm, int Speed, double Interval, double FadeSpeed, double Density,
        double WaveSpeed, double WaveFreq, double FadeTime, int GroupSize,
        int BlockSize, int Blocks, int Size,
        AvColor Base, AvColor[] Colors);

    // ── Code snippet generator ──────────────────────────────────────────

    public static string[] CodeModes { get; } = ["Raid Effect", "Raid Effect (Phase Transition)", "Reactive Weather"];
    [ObservableProperty] private string _selectedCodeMode = "Raid Effect";
    [ObservableProperty] private string _generatedCode = "";

    partial void OnSelectedCodeModeChanged(string value) => RegenerateCode();
    partial void OnParamBpmChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamSpeedChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamIntervalChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFadeSpeedChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamDensityChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamWaveSpeedChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamWaveFreqChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFadeTimeChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamGroupSizeChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamSizeChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamBlocksChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamBlockSizeChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamNumberOfLedsChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamStepSpeedChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamSustainChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamReleaseChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamRepetitionsChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamDirectionChanged(bool value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamRandomiseChanged(bool value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamTextureTypeChanged(string value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamDiagonalDirChanged(string value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFallDirChanged(string value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamIntensityChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFlickerSpeedChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamBeamWidthChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamPulseRadiusChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFadeWidthChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamTailLengthChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamSpawnIntervalChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamRippleSpeedChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamLaserDirChanged(string value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamBpmSpeedChanged(string value) { ShowBeatsPerCycle = ShowBpmSpeed && value == "Custom"; RegenerateCode(); LiveRestart(); }
    partial void OnParamBeatsPerCycleChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamSimultaneousBeamsChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFlickerOpacityChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamMatrixDirChanged(string value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamFadeBetweenChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamAccentEveryChanged(int value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamDecayChanged(double value) { RegenerateCode(); LiveRestart(); }
    partial void OnParamWedgeDegreesChanged(double value) { RegenerateCode(); LiveRestart(); }

    private void RegenerateCode()
    {
        if (SelectedEffect is null) { GeneratedCode = ""; return; }
        GeneratedCode = BuildCodeSnippet(SelectedEffect);
    }

    public Func<string, Task>? CopyToClipboard { get; set; }

    [RelayCommand]
    private async Task CopyCode()
    {
        if (string.IsNullOrEmpty(GeneratedCode)) return;
        if (CopyToClipboard is not null)
            await CopyToClipboard(GeneratedCode);
    }

    private static string Rgb(AvColor c) => $"new Color({c.R}, {c.G}, {c.B})";

    private string BuildCodeSnippet(EffectEntry effect)
    {
        var t = "    ";

        var effectName = effect.Name;
        var bS = Rgb(ColorBase);
        var arr = SlotsArrayCode();
        var s0 = SlotCode(0);
        var s1 = SlotCode(1);
        var s2 = SlotCode(2);

        string inner = effectName switch
        {
            "Summit of Everkeep (Zoraal Ja)" or "BPMFastStarfieldDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var effect = new BPMFastStarfieldDecorator(layer, {ParamNumberOfLeds}, {ParamBpm}, {ParamFadeSpeed}, animationCol, surface, {ParamDensity}, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(effect, layer, runningEffects);",

            "BPMStarfieldDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var starfield = new BPMStarfieldDecorator(layer, {ParamNumberOfLeds}, {ParamBpm}, {ParamFadeSpeed}, animationCol, surface, {ParamDensity}, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(starfield, layer, runningEffects);",

            "FastStarfieldDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var starfield = new FastStarfieldDecorator(layer, {ParamNumberOfLeds}, {ParamInterval}, {ParamFadeSpeed}, animationCol, surface, {ParamDensity}, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(starfield, layer, runningEffects);",

            "StarfieldDecorator" or "Moon Dust (Mare Lamentorum)" or "Rain" or "Showers" or "Snow" or "Blizzards" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var starfield = new StarfieldDecorator(layer, {ParamNumberOfLeds}, {ParamInterval}, {ParamFadeSpeed}, animationCol, surface, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(starfield, layer, runningEffects);",

            "Interphos (Queen Eternal)" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol1 = {s0};\nvar animationCol2 = {s1};\nvar animationCol3 = {s2};\n\n" +
                "var animationGradient = new LinearGradient(\n" +
                "    new GradientStop(0f, baseCol), new GradientStop(0.20f, animationCol1),\n" +
                "    new GradientStop(0.35f, animationCol2), new GradientStop(0.50f, animationCol3),\n" +
                "    new GradientStop(0.65f, animationCol1), new GradientStop(0.80f, animationCol2),\n" +
                "    new GradientStop(0.95f, animationCol3));\n\n" +
                $"var gradientMove = new MoveGradientDecorator(surface, {ParamSpeed}, true);\n" +
                "SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);",

            "Scratching Ring (Black Cat)" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol1 = {s0};\nvar animationCol2 = {s1};\nvar animationCol3 = {s2};\n\n" +
                "var animationGradient = new LinearGradient(\n" +
                "    new GradientStop(0f, baseCol), new GradientStop(0.20f, animationCol1),\n" +
                "    new GradientStop(0.35f, animationCol2), new GradientStop(0.50f, animationCol3),\n" +
                "    new GradientStop(0.65f, animationCol1), new GradientStop(0.80f, animationCol2),\n" +
                "    new GradientStop(0.95f, animationCol3));\n\n" +
                $"var gradientMove = new MoveBPMGradientDecorator(surface, {ParamBpm}, {ParamDirection.ToString().ToLower()});\n" +
                "SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);",

            "Lovely Lovering (Honey B. Lovely)" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var arenaLightShow = new ArenaLightShowDecorator(layer, {ParamInterval}, {ParamWaveSpeed}, {ParamWaveFreq}, animationCol, surface, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(arenaLightShow, layer, runningEffects);",

            "Sphere of Naught (Cloud of Darkness)" or "ArenaLightShowDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var arenaLightShow = new ArenaLightShowDecorator(layer, {ParamInterval}, {ParamWaveSpeed}, {ParamWaveFreq}, animationCol, surface, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(arenaLightShow, layer, runningEffects);",

            "Groovy Ring (Dancing Green)" or "Demolition Site (Brute Abombinator)" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var ripple = new BPMRippleDecorator(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamFadeWidth}, colors, surface, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(ripple, layer, runningEffects);",

            "Rebel Ring (Sugar Riot)" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var chase = new BPMChaseDecorator(layer, {ParamBpm}, {ParamTailLength}, colors, surface, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(chase, layer, runningEffects);",

            "Hunter's Ring — Howling Blade (M8)" or "Arcadia P1 Effect 1 — Intro" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var starfield = new BPMStarfieldDecorator(layer, {ParamNumberOfLeds}, {ParamBpm}, {ParamFadeSpeed}, animationCol, surface, {ParamDensity}, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(starfield, layer, runningEffects);",

            "Hunter's Ring — Howling Blade (M8S)" or "Arcadia P2 (M12)" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var pulse = new BPMCircularPulseEffect(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamPulseRadius}, {ParamFadeWidth}, colors, surface, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(pulse, layer, runningEffects);",

            "Arcadia P1 Effect 2 — Post-Flash" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var strike = new BPMThunderstrikeEffect(layer, {ParamBpm}, {ParamAccentEvery}, {ParamDecay}, colors, surface, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(strike, layer, runningEffects);",

            "Blasting Ring (Brute Bomber)" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol1 = {s0};\nvar animationCol2 = {s1};\nvar animationCol3 = {s2};\n\n" +
                "var animationGradient = new LinearGradient(\n" +
                "    new GradientStop(0f, baseCol), new GradientStop(0.20f, animationCol1),\n" +
                "    new GradientStop(0.35f, animationCol2), new GradientStop(0.50f, animationCol3),\n" +
                "    new GradientStop(0.65f, animationCol1), new GradientStop(0.80f, animationCol2),\n" +
                "    new GradientStop(0.95f, animationCol3));\n\n" +
                $"var gradientMove = new MoveBPMDiagonalGradientDecorator(surface, {ParamBpm}, DiagonalDirection.{ParamDiagonalDir});\n" +
                "SetLinearGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, masterlayer.layerID);",

            "The Thundering (Wicked Thunder)" or "BPMPWMDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var bpmEffect = new BPMPWMDecorator(layer, {ParamBpm}, {ParamWaveFreq}, animationCol, {ParamFadeTime}, {ParamGroupSize}, surface, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(bpmEffect, layer, runningEffects);",

            "BPMSINDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var bpmEffect = new BPMSINDecorator(layer, {ParamBpm}, {ParamWaveFreq}, animationCol, {ParamFadeTime}, {ParamGroupSize}, surface, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(bpmEffect, layer, runningEffects);",

            "StrobeDecorator" or "Thunder / Thunderstorms" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol = {arr};\n" +
                $"var storms = new StrobeDecorator(layer, {(int)ParamInterval}, {ParamFadeSpeed}, {ParamRandomise.ToString().ToLower()}, animationCol, surface, false, baseCol);\n\n" +
                "layer.Brush = new SolidColorBrush(baseCol);\nSetEffect(storms, layer, runningEffects);",

            "PulseDecorator" =>
                $"var highlightColor = {s0};\n" +
                $"var pulse = new PulseDecorator(layer, {(int)ParamInterval}, {ParamStepSpeed}, {ParamFadeSpeed}, highlightColor, surface, RGBDeviceType.Keyboard);\n\n" +
                "SetEffect(pulse, layer, runningEffects);",

            "BlockFallEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var blockFall = new BlockFallEffect(layer, {ParamBlocks}, {ParamBlockSize}, {ParamFadeSpeed}, colors, surface, BlockFallEffect.Direction.{ParamFallDir}, baseCol);\n\n" +
                "SetEffect(blockFall, layer, runningEffects);",

            "ShotFlashDecorator" =>
                $"var brush = new SolidColorBrush({s0});\n" +
                $"var shotFlash = new ShotFlashDecorator(surface)\n" +
                $"{{\n    Attack = {(float)ParamFadeTime}f,\n    Decay = {(float)ParamFadeSpeed}f,\n    Sustain = {(float)ParamSustain}f,\n    Release = {(float)ParamRelease}f,\n    Repetitions = {ParamRepetitions},\n}};\n" +
                "brush.AddDecorator(shotFlash);\nlayer.Brush = brush;",

            "MoveBPMGradientDecorator" =>
                $"var animationGradient = new LinearGradient(\n" +
                $"    new GradientStop(0f, {s0}), new GradientStop(0.33f, {s1}), new GradientStop(0.66f, {s2}));\n\n" +
                $"var gradientMove = new MoveBPMGradientDecorator(surface, {ParamBpm}, {ParamDirection.ToString().ToLower()});\n\n" +
                $"Set{(ParamTextureType == "Conical" ? "Radial" : "Linear")}GradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, _gradientEffects);",

            "MoveBPMDiagonalGradientDecorator" =>
                $"var animationGradient = new LinearGradient(\n" +
                $"    new GradientStop(0f, {s0}), new GradientStop(0.33f, {s1}), new GradientStop(0.66f, {s2}));\n\n" +
                $"var gradientMove = new MoveBPMDiagonalGradientDecorator(surface, {ParamBpm}, DiagonalDirection.{ParamDiagonalDir});\n\n" +
                $"Set{(ParamTextureType == "Conical" ? "Radial" : "Linear")}GradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, _gradientEffects);",

            "AudioVisualizerEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                "var visualizer = new AudioVisualizerEffect(layer, colors, surface, baseCol);\n\n" +
                "SetEffect(visualizer, layer, runningEffects);",

            "Wind" or "Gales" or "Umbral Wind" =>
                $"var baseCol = {bS};\nvar animationCol = {s0};\n\n" +
                "var animationGradient = new LinearGradient(new GradientStop(0f, baseCol), new GradientStop(0.25f, animationCol), new GradientStop(0.75f, baseCol), new GradientStop(1f, animationCol));\n" +
                $"var gradientMove = new MoveGradientDecorator(surface, {ParamSpeed}, {ParamDirection.ToString().ToLower()});\n\n" +
                "SetLinearGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, _gradientEffects);",

            "Sandstorms / Dust Storms" =>
                $"var baseCol = {bS};\nvar animationCol = {s0};\n\n" +
                "var animationGradient = new LinearGradient(new GradientStop(0f, baseCol), new GradientStop(0.25f, animationCol), new GradientStop(0.75f, baseCol), new GradientStop(1f, animationCol));\n" +
                $"var gradientMove = new MoveGradientDecorator(surface, {ParamSpeed}, {ParamDirection.ToString().ToLower()});\n\n" +
                "SetLinearGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, _gradientEffects);",

            "Astromagnetic Storm" =>
                $"var baseCol = {bS};\n" +
                $"var animationCol1 = {s0};\nvar animationCol2 = {s1};\nvar animationCol3 = {s2};\n\n" +
                "var animationGradient = new LinearGradient(\n" +
                "    new GradientStop(0f, baseCol), new GradientStop(0.20f, animationCol1),\n" +
                "    new GradientStop(0.35f, animationCol2), new GradientStop(0.50f, animationCol3),\n" +
                "    new GradientStop(0.65f, animationCol1), new GradientStop(0.80f, animationCol2),\n" +
                "    new GradientStop(0.95f, animationCol3));\n\n" +
                $"var gradientMove = new MoveGradientDecorator(surface, {ParamSpeed}, {ParamDirection.ToString().ToLower()});\n\n" +
                "SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, _gradientEffects);",

            "Umbral Static" =>
                $"var baseCol = {bS};\nvar animationCol = {s0};\n\n" +
                "var animationGradient = new LinearGradient(new GradientStop(0f, baseCol), new GradientStop(0.35f, animationCol), new GradientStop(0.75f, baseCol), new GradientStop(1f, animationCol));\n" +
                $"var gradientMove = new MoveGradientDecorator(surface, {ParamSpeed}, {ParamDirection.ToString().ToLower()});\n\n" +
                "SetRadialGradientEffect(animationGradient, gradientMove, layer, new Size(100, 100), runningEffects, _gradientEffects);",

            "Everlasting Light" =>
                $"var baseCol = {bS};\nvar animationCol = {s0};\n\n" +
                "var animationGradient = new LinearGradient(new GradientStop(0f, baseCol), new GradientStop(0.25f, animationCol), new GradientStop(0.50f, baseCol), new GradientStop(0.75f, animationCol), new GradientStop(1f, baseCol));\n" +
                $"var gradientMove = new MoveGradientDecorator(surface, {ParamSpeed}, {ParamDirection.ToString().ToLower()});\n\n" +
                $"SetLinearGradientEffect(animationGradient, gradientMove, layer, new Size({ParamSize}, {ParamSize}), runningEffects, _gradientEffects);",

            "FireEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var fire = new FireEffect(layer, {ParamIntensity}, {ParamFlickerSpeed}, colors, surface, baseCol);\n\n" +
                "SetEffect(fire, layer, runningEffects);",

            "LaserEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var laser = new LaserEffect(layer, {ParamSpeed}, {ParamBeamWidth}, {ParamSpawnInterval}, colors, surface, LaserEffect.LaserDirection.{ParamLaserDir}, baseCol);\n\n" +
                "SetEffect(laser, layer, runningEffects);",

            "CircularPulseEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var pulse = new CircularPulseEffect(layer, {ParamRippleSpeed}, {ParamPulseRadius}, {ParamSpawnInterval}, {ParamFadeWidth}, colors, surface, baseCol);\n\n" +
                "SetEffect(pulse, layer, runningEffects);",

            "BPMRippleDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var ripple = new BPMRippleDecorator(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamFadeWidth}, colors, surface, baseCol);\n\n" +
                "SetEffect(ripple, layer, runningEffects);",

            "BPMChaseDecorator" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var chase = new BPMChaseDecorator(layer, {ParamBpm}, {ParamTailLength}, colors, surface, baseCol);\n\n" +
                "SetEffect(chase, layer, runningEffects);",

            "BPMLaserEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var laser = new BPMLaserEffect(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamBeamWidth}, colors, surface, LaserEffect.LaserDirection.{ParamLaserDir}, baseCol);\n\n" +
                "SetEffect(laser, layer, runningEffects);",

            "BPMCircularPulseEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var pulse = new BPMCircularPulseEffect(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamPulseRadius}, {ParamFadeWidth}, colors, surface, baseCol);\n\n" +
                "SetEffect(pulse, layer, runningEffects);",

            "BPMLaserEffect2" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var laser = new BPMLaserEffect2(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamBeamWidth}, {ParamSimultaneousBeams}, colors, surface, LaserEffect.LaserDirection.{ParamLaserDir}, baseCol);\n\n" +
                "SetEffect(laser, layer, runningEffects);",

            "MatrixEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var matrix = new MatrixEffect(layer, {ParamSpeed}, {ParamFlickerOpacity}, {ParamFlickerSpeed}, colors, surface, MatrixEffect.MatrixDirection.{ParamMatrixDir}, baseCol);\n\n" +
                "SetEffect(matrix, layer, runningEffects);",

            "BPMMatrixEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var matrix = new BPMMatrixEffect(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamFlickerOpacity}, {ParamFlickerSpeed}, colors, surface, MatrixEffect.MatrixDirection.{ParamMatrixDir}, baseCol);\n\n" +
                "SetEffect(matrix, layer, runningEffects);",

            "BPMChaseRandom" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var chase = new BPMChaseRandom(layer, {ParamBpm}, {ParamFadeBetween}, {ParamGroupSize}, colors, surface, baseCol);\n\n" +
                "SetEffect(chase, layer, runningEffects);",

            "BPMThunderstrikeEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var strike = new BPMThunderstrikeEffect(layer, {ParamBpm}, {ParamAccentEvery}, {ParamDecay}, colors, surface, baseCol);\n\n" +
                "SetEffect(strike, layer, runningEffects);",

            "BPMHeartbeatEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var heartbeat = new BPMHeartbeatEffect(layer, {ParamBpm}, {ParamBeamWidth}, colors, surface, baseCol);\n\n" +
                "SetEffect(heartbeat, layer, runningEffects);",

            "BPMEqualizerEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var eq = new BPMEqualizerEffect(layer, {ParamBpm}, {ParamDecay}, colors, surface, baseCol);\n\n" +
                "SetEffect(eq, layer, runningEffects);",

            "BPMSpinnerEffect" =>
                $"var baseCol = {bS};\n" +
                $"var colors = {arr};\n" +
                $"var spinner = new BPMSpinnerEffect(layer, {ParamBpm}, {ResolveBeatsPerCycle()}, {ParamWedgeDegrees}, colors, surface, baseCol);\n\n" +
                "SetEffect(spinner, layer, runningEffects);",

            _ => $"// Select an effect to generate code",
        };

        return WrapInContext(inner, SelectedCodeMode, t, IsGradientEffect(effectName));
    }

    // True when the effect installs its decorator on a LinearGradient object
    // rather than on the overlay ListLedGroup. These effects can't use the
    // usual `layer.Decorators.Count == 0` rebuild gate (their decorator never
    // touches `layer`, so Decorators.Count is permanently 0 and the effect
    // rebuilds every tick → flicker + runaway speed). Rebuild guard on the
    // overlay's TextureBrush presence instead.
    private static bool IsGradientEffect(string name) => name is
        "Interphos (Queen Eternal)"
        or "Scratching Ring (Black Cat)"
        or "Blasting Ring (Brute Bomber)"
        or "MoveBPMGradientDecorator"
        or "MoveBPMDiagonalGradientDecorator";

    // Wraps the per-effect inner snippet with the appropriate switch-case scaffolding.
    //
    // Raid Effect templates target the current standalone RaidEffectProcessor pattern:
    // qualified `RaidEffectState.raidEffectsRunning`, init guard inside the case,
    // `return RaidEffectState.raidEffectsRunning;` as the fall-through return so the
    // overlay stays attached on subsequent ticks without rebuilding.
    //
    // The Phase Transition variant additionally rebuilds on currentBgmId change and
    // wraps the build code in a BGM switch with a single placeholder phase branch
    // that the user is expected to customize for their fight's phase-2 visual.
    private static string WrapInContext(string inner, string mode, string t, bool isGradient = false)
    {
        var lines = inner.Split('\n');

        // Per-overlay rebuild gate. For decorator-based effects, the decorator
        // attaches to the overlay, so a rebuild is needed when Decorators.Count
        // is 0 OR the global state says no raid is running. For gradient-based
        // effects, the decorator lives on the LinearGradient (not on the
        // overlay), so Decorators.Count is permanently 0 — gate on the
        // TextureBrush presence instead to avoid rebuilding every tick.
        string gate = isGradient
            ? "layer.Brush is not TextureBrush || !RaidEffectState.raidEffectsRunning"
            : "layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning";

        string phaseGate = isGradient
            ? "layer.Brush is not TextureBrush || !RaidEffectState.raidEffectsRunning || currentBgmId != RaidEffectState.currentRaidBgmId"
            : "layer.Decorators.Count == 0 || !RaidEffectState.raidEffectsRunning || currentBgmId != RaidEffectState.currentRaidBgmId";

        if (mode == "Raid Effect")
        {
            var indented = string.Join("\n", lines.Select(l => l.Length > 0 ? t + t + t + t + l : ""));
            return
                $"{t}{t}case \"<ZoneName>\":\n" +
                $"{t}{t}{t}if ({gate})\n" +
                $"{t}{t}{t}{{\n" +
                indented + "\n\n" +
                $"{t}{t}{t}{t}RaidEffectState.raidEffectsRunning = true;\n" +
                $"{t}{t}{t}{t}return true;\n" +
                $"{t}{t}{t}}}\n" +
                $"{t}{t}{t}return RaidEffectState.raidEffectsRunning;";
        }

        if (mode == "Raid Effect (Phase Transition)")
        {
            // Inner block sits 6 levels deep: case → if → switch → case/default → block.
            var indented = string.Join("\n", lines.Select(l => l.Length > 0 ? t + t + t + t + t + t + l : ""));
            return
                $"{t}{t}case \"<ZoneName>\":\n" +
                $"{t}{t}{t}// Rebuild on first activation, on a per-overlay basis, OR when BGM transitions to a new phase.\n" +
                $"{t}{t}{t}if ({phaseGate})\n" +
                $"{t}{t}{t}{{\n" +
                $"{t}{t}{t}{t}switch (currentBgmId)\n" +
                $"{t}{t}{t}{t}{{\n" +
                $"{t}{t}{t}{t}{t}case 0u: // TODO: replace 0u with the real phase BGM id\n" +
                $"{t}{t}{t}{t}{t}{{\n" +
                $"{t}{t}{t}{t}{t}{t}// TODO: customize this branch for the phase visual.\n" +
                $"{t}{t}{t}{t}{t}{t}// Placeholder duplicates the default below — swap the\n" +
                $"{t}{t}{t}{t}{t}{t}// decorator type, BPM, palette indices, etc. as needed.\n" +
                indented + "\n" +
                $"{t}{t}{t}{t}{t}{t}break;\n" +
                $"{t}{t}{t}{t}{t}}}\n" +
                $"{t}{t}{t}{t}{t}default:\n" +
                $"{t}{t}{t}{t}{t}{{\n" +
                indented + "\n" +
                $"{t}{t}{t}{t}{t}{t}break;\n" +
                $"{t}{t}{t}{t}{t}}}\n" +
                $"{t}{t}{t}{t}}}\n\n" +
                $"{t}{t}{t}{t}RaidEffectState.raidEffectsRunning = true;\n" +
                $"{t}{t}{t}{t}RaidEffectState.currentRaidBgmId = currentBgmId;\n" +
                $"{t}{t}{t}{t}return true;\n" +
                $"{t}{t}{t}}}\n" +
                $"{t}{t}{t}return RaidEffectState.raidEffectsRunning;";
        }

        // Reactive Weather (unchanged).
        var indentedW = string.Join("\n", lines.Select(l => l.Length > 0 ? t + t + t + t + t + l : ""));
        return
            $"{t}{t}{t}case \"<WeatherName>\":\n" +
            $"{t}{t}{t}{t}if (reactiveWeatherEffects && effectSettings.weather_<name>_animation)\n" +
            $"{t}{t}{t}{t}{{\n" +
            indentedW + "\n\n" +
            $"{t}{t}{t}{t}{t}return true;\n" +
            $"{t}{t}{t}{t}}}\n" +
            $"{t}{t}{t}{t}break;";
    }

    // ── Effect factory ───────────────────────────────────────────────────

    private Action BuildEffect(EffectEntry effect, ListLedGroup group)
    {
        var baseCol = ToRgb(ColorBase);
        var colors = SlotsRgb();
        var s0 = SlotRgb(0);
        var s1 = SlotRgb(1);
        var s2 = SlotRgb(2);

        switch (effect.Name)
        {
            // ── Raid presets ──
            case "Summit of Everkeep (Zoraal Ja)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMFastStarfieldDecorator(group, Math.Max(4, group.Count() / 6), ParamBpm, ParamFadeSpeed, colors, _surface, ParamDensity, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Interphos (Queen Eternal)":
            {
                var g = Build3ColorGradient(baseCol, s0, s1, s2); g.WrapGradient = true;
                var dec = new MoveGradientDecorator(_surface, ParamSpeed, ParamDirection);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            case "Scratching Ring (Black Cat)":
            {
                var g = Build3ColorGradient(baseCol, s0, s1, s2); g.WrapGradient = true;
                var dec = new MoveBPMGradientDecorator(_surface, ParamBpm, ParamDirection);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            case "Lovely Lovering (Honey B. Lovely)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new ArenaLightShowDecorator(group, ParamInterval, ParamWaveSpeed, ParamWaveFreq, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Blasting Ring (Brute Bomber)":
            {
                var g = Build3ColorGradient(baseCol, s0, s1, s2); g.WrapGradient = true;
                var dec = new MoveBPMDiagonalGradientDecorator(_surface, ParamBpm, Enum.Parse<DiagonalDirection>(ParamDiagonalDir));
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            case "The Thundering (Wicked Thunder)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMPWMDecorator(group, ParamBpm, ParamWaveFreq, colors, ParamFadeTime, ParamGroupSize, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Sphere of Naught (Cloud of Darkness)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new ArenaLightShowDecorator(group, ParamInterval, ParamWaveSpeed, ParamWaveFreq, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Groovy Ring (Dancing Green)":
            case "Demolition Site (Brute Abombinator)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMRippleDecorator(group, ParamBpm, ResolveBeatsPerCycle(), ParamFadeWidth, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Rebel Ring (Sugar Riot)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMChaseDecorator(group, ParamBpm, ParamTailLength, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Hunter's Ring — Howling Blade (M8)":
            case "Arcadia P1 Effect 1 — Intro":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMStarfieldDecorator(group, Math.Max(1, ParamNumberOfLeds), ParamBpm, ParamFadeSpeed, colors, _surface, ParamDensity, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Hunter's Ring — Howling Blade (M8S)":
            case "Arcadia P2 (M12)":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMCircularPulseEffect(group, ParamBpm, ResolveBeatsPerCycle(), ParamPulseRadius, ParamFadeWidth, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "Arcadia P1 Effect 2 — Post-Flash":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new BPMThunderstrikeEffect(group, ParamBpm, ParamAccentEvery, ParamDecay, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }

            // ── Weather presets (starfield-based) ──
            case "Moon Dust (Mare Lamentorum)":
            case "Rain":
            case "Showers":
            case "Snow":
            case "Blizzards":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new StarfieldDecorator(group, Math.Max(4, group.Count() / 4), ParamInterval, ParamFadeSpeed, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            // ── Weather presets (strobe) ──
            case "Thunder / Thunderstorms":
            {
                group.Brush = new SolidColorBrush(baseCol);
                var dec = new StrobeDecorator(group, (int)ParamInterval, ParamFadeSpeed, ParamRandomise, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            // ── Weather presets (linear gradient) ──
            case "Wind":
            case "Gales":
            case "Umbral Wind":
            {
                var g = Build2ColorOscGradient(baseCol, s0); g.WrapGradient = true;
                var dec = new MoveGradientDecorator(_surface, ParamSpeed, ParamDirection);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            case "Sandstorms / Dust Storms":
            {
                var g = Build2ColorOscGradient(baseCol, s0); g.WrapGradient = true;
                var dec = new MoveGradientDecorator(_surface, ParamSpeed, ParamDirection);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new LinearGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            // ── Weather presets (radial gradient) ──
            case "Astromagnetic Storm":
            {
                var g = Build3ColorGradient(baseCol, s0, s1, s2); g.WrapGradient = true;
                var dec = new MoveGradientDecorator(_surface, ParamSpeed, ParamDirection);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            case "Umbral Static":
            {
                var g = new LinearGradient(new GradientStop(0f, baseCol), new GradientStop(0.35f, s0), new GradientStop(0.75f, baseCol), new GradientStop(1f, s0));
                g.WrapGradient = true;
                var dec = new MoveGradientDecorator(_surface, ParamSpeed, ParamDirection);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new ConicalGradientTexture(new Size(100, 100), g));
                return () => g.RemoveDecorator(dec);
            }
            case "Everlasting Light":
            {
                var g = new LinearGradient(new GradientStop(0f, baseCol), new GradientStop(0.25f, s0), new GradientStop(0.5f, baseCol), new GradientStop(0.75f, s0), new GradientStop(1f, baseCol));
                g.WrapGradient = true;
                var dec = new MoveGradientDecorator(_surface, ParamSpeed, true);
                g.AddDecorator(dec);
                group.Brush = new TextureBrush(new LinearGradientTexture(new Size(ParamSize, ParamSize), g));
                return () => g.RemoveDecorator(dec);
            }

            // ── Raw decorators ──
            case "StarfieldDecorator":
            {
                var dec = new StarfieldDecorator(group, Math.Max(1, ParamNumberOfLeds), ParamInterval, ParamFadeSpeed, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "FastStarfieldDecorator":
            {
                var dec = new FastStarfieldDecorator(group, Math.Max(1, ParamNumberOfLeds), ParamInterval, ParamFadeSpeed, colors, _surface, ParamDensity, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMStarfieldDecorator":
            {
                var dec = new BPMStarfieldDecorator(group, Math.Max(1, ParamNumberOfLeds), ParamBpm, ParamFadeSpeed, colors, _surface, ParamDensity, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMFastStarfieldDecorator":
            {
                var dec = new BPMFastStarfieldDecorator(group, Math.Max(1, ParamNumberOfLeds), ParamBpm, ParamFadeSpeed, colors, _surface, ParamDensity, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "StrobeDecorator":
            {
                var dec = new StrobeDecorator(group, (int)ParamInterval, ParamFadeSpeed, ParamRandomise, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "PulseDecorator":
            {
                var devType = _surface.Devices.First().DeviceInfo.DeviceType;
                var dec = new PulseDecorator(group, (int)ParamInterval, ParamStepSpeed, ParamFadeSpeed, s0, _surface, devType);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMPWMDecorator":
            {
                var dec = new BPMPWMDecorator(group, ParamBpm, ParamWaveFreq, colors, ParamFadeTime, ParamGroupSize, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMSINDecorator":
            {
                var dec = new BPMSINDecorator(group, ParamBpm, ParamWaveFreq, colors, ParamFadeTime, ParamGroupSize, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "ArenaLightShowDecorator":
            {
                var dec = new ArenaLightShowDecorator(group, ParamInterval, ParamWaveSpeed, ParamWaveFreq, colors, _surface, false, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BlockFallEffect":
            {
                var dir = Enum.Parse<BlockFallEffect.Direction>(ParamFallDir);
                var dec = new BlockFallEffect(group, ParamBlocks, ParamBlockSize, ParamFadeSpeed, colors, _surface, dir, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "ShotFlashDecorator":
            {
                var brush = new SolidColorBrush(s0);
                var dec = new ShotFlashDecorator(_surface)
                {
                    Attack = (float)ParamFadeTime, Decay = (float)ParamFadeSpeed,
                    Sustain = (float)ParamSustain, Release = (float)ParamRelease,
                    Repetitions = ParamRepetitions,
                };
                brush.AddDecorator(dec);
                group.Brush = brush;
                return () => brush.RemoveDecorator(dec);
            }
            case "MoveBPMGradientDecorator":
            {
                var g = BuildEvenGradient(colors); g.WrapGradient = true;
                var dec = new MoveBPMGradientDecorator(_surface, ParamBpm, ParamDirection);
                g.AddDecorator(dec);
                var tex = ParamTextureType == "Conical"
                    ? (ITexture)new ConicalGradientTexture(new Size(100, 100), g)
                    : new LinearGradientTexture(new Size(100, 100), g);
                group.Brush = new TextureBrush(tex);
                return () => g.RemoveDecorator(dec);
            }
            case "MoveBPMDiagonalGradientDecorator":
            {
                var g = BuildEvenGradient(colors); g.WrapGradient = true;
                var dec = new MoveBPMDiagonalGradientDecorator(_surface, ParamBpm, Enum.Parse<DiagonalDirection>(ParamDiagonalDir));
                g.AddDecorator(dec);
                var tex = ParamTextureType == "Conical"
                    ? (ITexture)new ConicalGradientTexture(new Size(100, 100), g)
                    : new LinearGradientTexture(new Size(100, 100), g);
                group.Brush = new TextureBrush(tex);
                return () => g.RemoveDecorator(dec);
            }
            case "AudioVisualizerEffect":
            {
                var dec = new AudioVisualizerEffect(group, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "FireEffect":
            {
                var dec = new FireEffect(group, ParamIntensity, ParamFlickerSpeed, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "LaserEffect":
            {
                var dir = Enum.Parse<LaserEffect.LaserDirection>(ParamLaserDir);
                var dec = new LaserEffect(group, ParamSpeed, ParamBeamWidth, ParamSpawnInterval, colors, _surface, dir, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "CircularPulseEffect":
            {
                var dec = new CircularPulseEffect(group, ParamRippleSpeed, ParamPulseRadius, ParamSpawnInterval, ParamFadeWidth, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMRippleDecorator":
            {
                var dec = new BPMRippleDecorator(group, ParamBpm, ResolveBeatsPerCycle(), ParamFadeWidth, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMChaseDecorator":
            {
                var dec = new BPMChaseDecorator(group, ParamBpm, ParamTailLength, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMLaserEffect":
            {
                var dir = Enum.Parse<LaserEffect.LaserDirection>(ParamLaserDir);
                var dec = new BPMLaserEffect(group, ParamBpm, ResolveBeatsPerCycle(), ParamBeamWidth, colors, _surface, dir, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMCircularPulseEffect":
            {
                var dec = new BPMCircularPulseEffect(group, ParamBpm, ResolveBeatsPerCycle(), ParamPulseRadius, ParamFadeWidth, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMLaserEffect2":
            {
                var dir = Enum.Parse<LaserEffect.LaserDirection>(ParamLaserDir);
                var dec = new BPMLaserEffect2(group, ParamBpm, ResolveBeatsPerCycle(), ParamBeamWidth, ParamSimultaneousBeams, colors, _surface, dir, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "MatrixEffect":
            {
                var dir = Enum.Parse<MatrixEffect.MatrixDirection>(ParamMatrixDir);
                var dec = new MatrixEffect(group, ParamSpeed, ParamFlickerOpacity, ParamFlickerSpeed, colors, _surface, dir, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMMatrixEffect":
            {
                var dir = Enum.Parse<MatrixEffect.MatrixDirection>(ParamMatrixDir);
                var dec = new BPMMatrixEffect(group, ParamBpm, ResolveBeatsPerCycle(), ParamFlickerOpacity, ParamFlickerSpeed, colors, _surface, dir, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMChaseRandom":
            {
                var dec = new BPMChaseRandom(group, ParamBpm, ParamFadeBetween, ParamGroupSize, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMThunderstrikeEffect":
            {
                var dec = new BPMThunderstrikeEffect(group, ParamBpm, ParamAccentEvery, ParamDecay, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMHeartbeatEffect":
            {
                var dec = new BPMHeartbeatEffect(group, ParamBpm, ParamBeamWidth, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMEqualizerEffect":
            {
                var dec = new BPMEqualizerEffect(group, ParamBpm, ParamDecay, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            case "BPMSpinnerEffect":
            {
                var dec = new BPMSpinnerEffect(group, ParamBpm, ResolveBeatsPerCycle(), ParamWedgeDegrees, colors, _surface, baseCol);
                group.AddDecorator(dec);
                return () => group.RemoveDecorator(dec);
            }
            default:
                throw new InvalidOperationException($"Unknown effect: {effect.Name}");
        }
    }

    // ── Gradient helpers ─────────────────────────────────────────────────

    private static LinearGradient Build3ColorGradient(RGBColor b, RGBColor c1, RGBColor c2, RGBColor c3)
        => new(new GradientStop(0f, b), new GradientStop(0.20f, c1), new GradientStop(0.35f, c2),
               new GradientStop(0.50f, c3), new GradientStop(0.65f, c1), new GradientStop(0.80f, c2), new GradientStop(0.95f, c3));

    private static LinearGradient Build2ColorOscGradient(RGBColor b, RGBColor a)
        => new(new GradientStop(0f, b), new GradientStop(0.25f, a), new GradientStop(0.75f, b), new GradientStop(1f, a));

    private static LinearGradient BuildEvenGradient(RGBColor[] palette)
    {
        var stops = new List<GradientStop>();
        for (var i = 0; i < palette.Length; i++)
            stops.Add(new GradientStop((float)i / palette.Length, palette[i]));
        return new LinearGradient(stops.ToArray());
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static AvColor ToAv(byte r, byte g, byte b) => AvColor.FromRgb(r, g, b);
    private static RGBColor ToRgb(AvColor c) => new(c.R, c.G, c.B);

    private void StartSurfaceTick()
    {
        _surfaceTimer = new Timer(SurfaceTickMs) { AutoReset = true };
        _surfaceTimer.Elapsed += (_, _) => { try { _surface.Update(); } catch { } };
        _surfaceTimer.Start();
    }

    private void BlackoutSurface()
    {
        try
        {
            var all = _surface.Devices.SelectMany(d => d).ToArray();
            if (all.Length == 0) return;
            var g = new ListLedGroup(_surface, all) { Brush = new SolidColorBrush(new RGBColor(0, 0, 0)) };
            _surface.Update();
            g.Detach();
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
        _surfaceTimer?.Stop();
        _surfaceTimer?.Dispose();
        try { _surface.Dispose(); } catch { }
    }
}
