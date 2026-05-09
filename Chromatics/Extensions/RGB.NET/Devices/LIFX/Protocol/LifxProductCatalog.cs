using System.Collections.Frozen;
using System.Collections.Generic;

namespace Chromatics.Extensions.RGB.NET.Devices.LIFX.Protocol
{
    // Capability map keyed by LIFX product id (returned in StateVersion).
    // Source: https://github.com/LIFX/products/blob/master/products.json — every
    // product currently published. Keep in sync when LIFX adds new SKUs.
    //
    // ExtendedMultizone follows the JSON exactly. Note: products 31, 32 and 38
    // gain extended_multizone via a firmware "upgrades" entry (e.g. firmware
    // 2.77 for Beam/Z); the JSON's top-level features for these still says
    // "extended_multizone": false (or missing). We treat them as legacy here
    // by default. The queue can promote them to extended via runtime firmware
    // detection if we add that later — for now legacy works on every firmware.
    //
    // Unknown products fall back to a "single colour bulb" entry via
    // GetOrDefault, which is the safe default — the bulb still works as a
    // single LED via SetColor.
    internal static class LifxProductCatalog
    {
        public readonly struct ProductInfo
        {
            public ProductInfo(string name, bool hasColor, bool isMultizone, bool isExtendedMultizone, bool isMatrix, bool isChain, int hintZones, int hintMatrixWidth, int hintMatrixHeight)
            {
                Name = name;
                HasColor = hasColor;
                IsMultizone = isMultizone;
                IsExtendedMultizone = isExtendedMultizone;
                IsMatrix = isMatrix;
                IsChain = isChain;
                HintZones = hintZones;
                HintMatrixWidth = hintMatrixWidth;
                HintMatrixHeight = hintMatrixHeight;
            }

            public string Name { get; }
            public bool HasColor { get; }
            public bool IsMultizone { get; }
            public bool IsExtendedMultizone { get; }
            public bool IsMatrix { get; }
            public bool IsChain { get; }
            public int HintZones { get; }
            public int HintMatrixWidth { get; }
            public int HintMatrixHeight { get; }
        }

        // Helper to keep the table compact.
        private static ProductInfo Bulb(string name, bool hasColor = true)
            => new(name, hasColor, false, false, false, false, 0, 0, 0);
        private static ProductInfo Multizone(string name, bool extended)
            => new(name, true, true, extended, false, false, 0, 0, 0);
        private static ProductInfo Matrix(string name, int w, int h, bool isChain = false)
            => new(name, true, false, false, true, isChain, 0, w, h);
        private static ProductInfo NonLight(string name)
            => new(name, false, false, false, false, false, 0, 0, 0);

        private static readonly FrozenDictionary<uint, ProductInfo> _byId =
            new Dictionary<uint, ProductInfo>
            {
                // ── Original colour A19 / BR30 / specialty bulbs ──────────────
                [1]   = Bulb("LIFX Original 1000"),
                [3]   = Bulb("LIFX Color 650"),
                [10]  = Bulb("LIFX White 800 (Low Voltage)", hasColor: false),
                [11]  = Bulb("LIFX White 800 (High Voltage)", hasColor: false),
                [15]  = Bulb("LIFX Color 1000"),
                [18]  = Bulb("LIFX White 900 BR30 (Low Voltage)", hasColor: false),
                [19]  = Bulb("LIFX White 900 BR30 (High Voltage)", hasColor: false),
                [20]  = Bulb("LIFX Color 1000 BR30"),
                [22]  = Bulb("LIFX Color 1000"),
                [27]  = Bulb("LIFX A19"),
                [28]  = Bulb("LIFX BR30"),
                [29]  = Bulb("LIFX A19 Night Vision"),
                [30]  = Bulb("LIFX BR30 Night Vision"),
                [36]  = Bulb("LIFX Downlight"),
                [37]  = Bulb("LIFX Downlight"),
                [39]  = Bulb("LIFX Downlight White to Warm", hasColor: false),
                [40]  = Bulb("LIFX Downlight"),
                [43]  = Bulb("LIFX A19"),
                [44]  = Bulb("LIFX BR30"),
                [45]  = Bulb("LIFX A19 Night Vision"),
                [46]  = Bulb("LIFX BR30 Night Vision"),
                [49]  = Bulb("LIFX Mini Color"),
                [50]  = Bulb("LIFX Mini White to Warm", hasColor: false),
                [51]  = Bulb("LIFX Mini White", hasColor: false),
                [52]  = Bulb("LIFX GU10"),
                [53]  = Bulb("LIFX GU10"),
                [59]  = Bulb("LIFX Mini Color"),
                [60]  = Bulb("LIFX Mini White to Warm", hasColor: false),
                [61]  = Bulb("LIFX Mini White", hasColor: false),
                [62]  = Bulb("LIFX A19"),
                [63]  = Bulb("LIFX BR30"),
                [64]  = Bulb("LIFX A19 Night Vision"),
                [65]  = Bulb("LIFX BR30 Night Vision"),
                [66]  = Bulb("LIFX Mini White", hasColor: false),
                [81]  = Bulb("LIFX Candle White to Warm", hasColor: false),
                [82]  = Bulb("LIFX Filament Clear", hasColor: false),
                [85]  = Bulb("LIFX Filament Amber", hasColor: false),
                [87]  = Bulb("LIFX Mini White", hasColor: false),
                [88]  = Bulb("LIFX Mini White", hasColor: false),
                [90]  = Bulb("LIFX Clean"),
                [91]  = Bulb("LIFX Color"),
                [92]  = Bulb("LIFX Color"),
                [93]  = Bulb("LIFX A19 US"),
                [94]  = Bulb("LIFX BR30"),
                [96]  = Bulb("LIFX Candle White to Warm", hasColor: false),
                [97]  = Bulb("LIFX A19"),
                [98]  = Bulb("LIFX BR30"),
                [99]  = Bulb("LIFX Clean"),
                [100] = Bulb("LIFX Filament Clear", hasColor: false),
                [101] = Bulb("LIFX Filament Amber", hasColor: false),
                [109] = Bulb("LIFX A19 Night Vision"),
                [110] = Bulb("LIFX BR30 Night Vision"),
                [111] = Bulb("LIFX A19 Night Vision"),
                [112] = Bulb("LIFX BR30 Night Vision Intl"),
                [113] = Bulb("LIFX Mini WW US", hasColor: false),
                [114] = Bulb("LIFX Mini WW Intl", hasColor: false),
                [121] = Bulb("LIFX Downlight Intl"),
                [122] = Bulb("LIFX Downlight US"),
                [123] = Bulb("LIFX Color US"),
                [124] = Bulb("LIFX Colour Intl"),
                [125] = Bulb("LIFX White to Warm US", hasColor: false),
                [126] = Bulb("LIFX White to Warm Intl", hasColor: false),
                [127] = Bulb("LIFX White US", hasColor: false),
                [128] = Bulb("LIFX White Intl", hasColor: false),
                [129] = Bulb("LIFX Color US"),
                [130] = Bulb("LIFX Colour Intl"),
                [131] = Bulb("LIFX White To Warm US", hasColor: false),
                [132] = Bulb("LIFX White To Warm Intl", hasColor: false),
                [133] = Bulb("LIFX White US", hasColor: false),
                [134] = Bulb("LIFX White Intl", hasColor: false),
                [135] = Bulb("LIFX GU10 Color US"),
                [136] = Bulb("LIFX GU10 Colour Intl"),
                [163] = Bulb("LIFX A19 US"),
                [164] = Bulb("LIFX BR30 US"),
                [165] = Bulb("LIFX A19 Intl"),
                [166] = Bulb("LIFX BR30 Intl"),
                [167] = Bulb("LIFX Downlight"),
                [168] = Bulb("LIFX Downlight"),
                [169] = Bulb("LIFX A21 1600lm US"),
                [170] = Bulb("LIFX A21 1600lm Intl"),
                [175] = Bulb("LIFX PAR38 US"),
                [178] = Bulb("LIFX Downlight US"),
                [179] = Bulb("LIFX Downlight US"),
                [180] = Bulb("LIFX Downlight US"),
                [181] = Bulb("LIFX Color US"),
                [182] = Bulb("LIFX Colour Intl"),
                [187] = Bulb("LIFX Candle Color US"),
                [188] = Bulb("LIFX Candle Colour Intl"),
                [223] = Bulb("LIFX Downlight US"),
                [224] = Bulb("LIFX Downlight Intl"),
                [225] = Bulb("LIFX PAR38 INTL"),

                // ── Multizone strips ──────────────────────────────────────────
                // PIDs 31/32/38 gained extended_multizone via firmware 2.77+
                // (released 2018). These products predate the flag in the
                // public products.json, but virtually every device in the
                // wild today is on firmware ≥ 2.77 — and the chunked
                // SetExtendedColorZones path's small (≤22 zones) packets
                // are far more reliable than legacy SetColorZones for
                // sparse decorator effects (starfield), which produce many
                // RLE runs and overflow the firmware's small UDP queue.
                // Treat as extended; if a user surfaces a truly old Beam
                // we'd add runtime firmware detection.
                [31]  = Multizone("LIFX Z",                extended: true),
                [32]  = Multizone("LIFX Z",                extended: true),
                [38]  = Multizone("LIFX Beam",             extended: true),
                [117] = Multizone("LIFX Z US",             extended: true),
                [118] = Multizone("LIFX Z Intl",           extended: true),
                [119] = Multizone("LIFX Beam US",          extended: true),
                [120] = Multizone("LIFX Beam Intl",        extended: true),
                [141] = Multizone("LIFX Neon US",          extended: true),
                [142] = Multizone("LIFX Neon Intl",        extended: true),
                [143] = Multizone("LIFX String US",        extended: true),
                [144] = Multizone("LIFX String Intl",      extended: true),
                [161] = Multizone("LIFX Outdoor Neon US",  extended: true),
                [162] = Multizone("LIFX Outdoor Neon Intl", extended: true),
                [203] = Multizone("LIFX String US",        extended: true),
                [204] = Multizone("LIFX String Intl",      extended: true),
                [205] = Multizone("LIFX Indoor Neon US",   extended: true),
                [206] = Multizone("LIFX Indoor Neon Intl", extended: true),
                [213] = Multizone("LIFX Permanent Outdoor US",   extended: true),
                [214] = Multizone("LIFX Permanent Outdoor Intl", extended: true),

                // ── Matrix devices (Tile chain, Candle, Path/Spot, Ceiling, Tube, Luna) ──
                [55]  = Matrix("LIFX Tile",                w: 8, h: 8, isChain: true),
                [57]  = Matrix("LIFX Candle",              w: 5, h: 6),
                [68]  = Matrix("LIFX Candle",              w: 5, h: 6),
                [137] = Matrix("LIFX Candle Color US",     w: 5, h: 6),
                [138] = Matrix("LIFX Candle Colour Intl",  w: 5, h: 6),
                [171] = Matrix("LIFX Round Spot US",       w: 1, h: 1),
                [173] = Matrix("LIFX Round Path US",       w: 1, h: 1),
                [174] = Matrix("LIFX Square Path US",      w: 1, h: 1),
                [176] = Matrix("LIFX Ceiling US",          w: 8, h: 8),
                [177] = Matrix("LIFX Ceiling Intl",        w: 8, h: 8),
                [185] = Matrix("LIFX Candle Color US",     w: 5, h: 6),
                [186] = Matrix("LIFX Candle Colour Intl",  w: 5, h: 6),
                [201] = Matrix("LIFX Ceiling 13x26\" US",  w: 8, h: 8),
                [202] = Matrix("LIFX Ceiling 13x26\" Intl",w: 8, h: 8),
                [215] = Matrix("LIFX Candle Color US",     w: 5, h: 6),
                [216] = Matrix("LIFX Candle Colour Intl",  w: 5, h: 6),
                [217] = Matrix("LIFX Tube US",             w: 8, h: 1),
                [218] = Matrix("LIFX Tube Intl",           w: 8, h: 1),
                [219] = Matrix("LIFX Luna US",             w: 8, h: 8),
                [220] = Matrix("LIFX Luna Intl",           w: 8, h: 8),
                [221] = Matrix("LIFX Round Spot Intl",     w: 1, h: 1),
                [222] = Matrix("LIFX Round Path Intl",     w: 1, h: 1),

                // ── Non-light products (relay switches; Chromatics doesn't drive these) ──
                [70]  = NonLight("LIFX Switch"),
                [71]  = NonLight("LIFX Switch"),
                [89]  = NonLight("LIFX Switch"),
                [115] = NonLight("LIFX Switch"),
                [116] = NonLight("LIFX Switch"),
            }.ToFrozenDictionary();

        public static bool TryGet(uint productId, out ProductInfo info) => _byId.TryGetValue(productId, out info);

        public static ProductInfo GetOrDefault(uint productId)
        {
            return _byId.TryGetValue(productId, out var info)
                ? info
                : new ProductInfo($"LIFX (product {productId})", hasColor: true, false, false, false, false, 0, 0, 0);
        }
    }
}
