using RGB.NET.Core;
using System;

namespace Chromatics.Extensions.RGB.NET.Devices.Hue;

public class HueDevice : AbstractRGBDevice<HueDeviceInfo>
{
    public HueDevice(HueDeviceInfo deviceInfo, HueUpdateQueue updateQueue) : base(deviceInfo, updateQueue)
    {
        InitializeLayout();
    }

    // Hue model id → RGB.NET layout (LedId + size). Specific models match
    // first; family prefixes catch newer/related SKUs that aren't in the
    // hardcoded list (Philips' table at developers.meethue.com is no longer
    // maintained, so the prefix fallbacks keep us correct for new releases
    // without needing a code update).
    //
    // Naming convention (mostly):
    //   LCT, LCA — Color/Extended A19 bulbs and reflectors
    //   LWB, LWA — White-only A19 bulbs
    //   LTW, LTA — White Ambiance (color-temperature) A19 bulbs
    //   LCL      — Color light strip
    //   LST      — LightStrip (Plus is LST002)
    //   LLC      — Living Colors (Iris, Bloom, Aura)
    //   LLM      — Light/Color modules
    //   LTP/LTC/LTF/LTT/LDT/LFF/LDD/LDF/LTD — Pendant/Ceiling/Floor/Table luminaires
    //   HBL/HEL/HIL/HML — Multi-source luminaires (Beyond, Entity, Impulse, Phoenix)
    //   MWM      — Wall/inline dimmer
    private void InitializeLayout()
    {
        string model = DeviceInfo.Model ?? "";

        Led led = model switch
        {
            // ── Light strips: long thin rectangle, gets LedStripe1 ──────────
            "LST001" or "LST002" or "LCL001"
                => AddLed(LedId.LedStripe1, new Point(0, 0), new Size(2000, 14)),

            // ── BR30 reflectors: large round ────────────────────────────────
            "LCT002" or "LCT011" or "LTW011"
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(80)),

            // ── GU10 spots: small round ─────────────────────────────────────
            "LCT003" or "LTW013" or "LTW014"
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(50)),

            // ── Candles: slimmer round ──────────────────────────────────────
            "LCT012" or "LTW012"
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(39)),

            // ── Hue Go: portable, larger ────────────────────────────────────
            "LLC020"
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(150)),

            // ── A19 color bulbs (gen1+gen2 Extended Color, gen3 LCA) ────────
            "LCT001" or "LCT007" or "LCT010" or "LCT014" or "LCT015" or "LCT016"
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),

            // ── 1-10V wall dimmer ───────────────────────────────────────────
            "MWM001"
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(80)),

            // ── Color Light Module (LLM001) and CT modules (LLM010-012) ─────
            _ when model.StartsWith("LLM", StringComparison.OrdinalIgnoreCase)
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(50)),

            // ── Multi-source luminaires (Beyond/Entity/Impulse/Phoenix) ─────
            _ when model.StartsWith("HBL", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("HEL", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("HIL", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("HML", StringComparison.OrdinalIgnoreCase)
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(120)),

            // ── Pendant/Ceiling/Floor/Table/Downlight luminaires ────────────
            _ when model.StartsWith("LTP", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LTC", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LTF", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LTT", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LDT", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LFF", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LDD", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LDF", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LTD", StringComparison.OrdinalIgnoreCase)
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(100)),

            // ── A19 color bulb families (LCA prefix catches gen3+) ──────────
            _ when model.StartsWith("LCA", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LCT", StringComparison.OrdinalIgnoreCase)
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),

            // ── White / White Ambiance A19-style bulbs ──────────────────────
            _ when model.StartsWith("LWB", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LWA", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LTW", StringComparison.OrdinalIgnoreCase)
                || model.StartsWith("LTA", StringComparison.OrdinalIgnoreCase)
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(62)),

            // ── Living Colors generic (Iris/Bloom/Aura/Disney) ──────────────
            _ when model.StartsWith("LLC", StringComparison.OrdinalIgnoreCase)
                => AddLed(LedId.Custom1, new Point(0, 0), new Size(100)),

            // ── Default: medium round ───────────────────────────────────────
            _ => AddLed(LedId.Custom1, new Point(0, 0), new Size(50))
        };

        // Light strips render as elongated rectangles; everything else is a circle.
        bool isStrip = model is "LST001" or "LST002" or "LCL001";
        if (led != null && !isStrip)
            led.Shape = Shape.Circle;
    }
}
