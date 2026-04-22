using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Layers.Effects
{
    // Standalone raid highlight overlay. Runs once per active dynamic
    // highlight layer (Highlight, JobClassesHighlight, ReactiveWeatherHighlight)
    // and paints the user's selected LEDs with the raid highlight colour
    // for the current zone. ZIndex sits above normal dynamic layers so it
    // visibly overrides whichever highlight type the user picked.
    //
    // Like RaidEffectProcessor, overlays live in a private dictionary and
    // are not subject to GameController's requestUpdate / type-switch
    // cleanup of RGBController.GetLiveLayerGroups().
    public sealed class RaidEffectHighlightProcessor : LayerProcessor
    {
        private static RaidEffectHighlightProcessor _instance;
        private readonly Dictionary<int, ListLedGroup> _overlays = new();
        private bool _disposed;

        // Above normal dynamic layers (typically 1-10) and above
        // RaidEffectProcessor's base overlay (500) so the highlight's
        // colour wins on the keys it claims.
        private const int RaidHighlightZIndex = 600;

        private RaidEffectHighlightProcessor() { }

        public static RaidEffectHighlightProcessor Instance => _instance ??= new RaidEffectHighlightProcessor();

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            var effectSettings = RGBController.GetEffectsSettings();

            if (!layer.Enabled || !effectSettings.effect_raideffects)
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var handler = GameController.GetGameData();
            if (handler?.Reader == null || !handler.Reader.CanGetActors())
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var player = handler.Reader.GetCurrentPlayer();
            if (player.Entity == null)
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var zone = GameHelper.GetZoneNameById(player.Entity.MapTerritory);
            var gameState = handler.Reader.GetGameState();
            uint currentBgmId = gameState.CurrentBgmId;

            // Highlight processor runs after the base raid processor in the
            // same tick, so RaidEffectState.UpdateState() has already been
            // called for this frame. We only consume the resulting state.
            if (RaidEffectState.dutyComplete || string.IsNullOrEmpty(zone) || zone == "???")
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var palette = RGBController.GetActivePalette();
            if (!TryGetRaidHighlightColor(zone, palette, currentBgmId, out var color))
            {
                DetachOverlay(layer.layerID);
                return;
            }

            var ledArray = GetLedArray(layer);
            var overlay = GetOrCreateOverlay(layer.layerID, ledArray);
            overlay.Brush = new SolidColorBrush(color);
            overlay.Attach(surface);
        }

        public override void CleanupLayer(int layerID) => DetachOverlay(layerID);

        private ListLedGroup GetOrCreateOverlay(int layerID, Led[] ledArray)
        {
            if (_overlays.TryGetValue(layerID, out var existing))
            {
                if (!existing.SequenceEqual(ledArray))
                {
                    existing.RemoveLeds(existing);
                    existing.AddLeds(ledArray);
                }
                return existing;
            }

            var group = new ListLedGroup(surface, ledArray) { ZIndex = RaidHighlightZIndex };
            group.Detach();
            _overlays[layerID] = group;
            return group;
        }

        private void DetachOverlay(int layerID)
        {
            if (!_overlays.TryGetValue(layerID, out var overlay)) return;
            overlay.Brush = new SolidColorBrush(Color.Transparent);
            overlay.Detach();
        }

        // Per-zone raid highlight colour. Cases moved verbatim from
        // ReactiveWeatherHighlightProcessor.SetReactiveWeather. Returns
        // false when the zone has no raid highlight, so the overlay can be
        // detached.
        private static bool TryGetRaidHighlightColor(string zone, PaletteColorModel palette, uint currentBgmId, out Color color)
        {
            switch (zone)
            {
                case "Summit of Everkeep":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectEverkeepKeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Interphos":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectInterphosKeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Scratching Ring":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM1KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Lovely Lovering":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM2KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Blasting Ring":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM3KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "The Thundering":
                case "Sphere of Naught":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM4KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Groovy Ring":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM5KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Rebel Ring":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM6KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;
                case "Demolition Site":
                    color = ColorHelper.ColorToRGBColor(palette.RaidEffectM7KeyHighlight.Color);
                    RaidEffectState.raidEffectsRunning = true;
                    return true;

                // BGM-based phase switching demo. Mirrors the base-layer
                // demo case in RaidEffectProcessor.
                case "Hunter's Ring":
                case "Hunting Ground":
                case "Mist":
                case "Limsa Lominsa Lower Decks":
                    color = currentBgmId switch
                    {
                        186u => ColorHelper.ColorToRGBColor(palette.RaidEffectM7Highlight2.Color), // TODO: real phase-2 BGM ID
                        _    => ColorHelper.ColorToRGBColor(palette.RaidEffectM7KeyHighlight.Color),
                    };
                    RaidEffectState.raidEffectsRunning = true;
                    RaidEffectState.currentRaidBgmId = currentBgmId;
                    return true;

                default:
                    color = default;
                    return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var overlay in _overlays.Values)
                    {
                        overlay.Detach();
                    }
                    _overlays.Clear();
                }
                _disposed = true;
            }

            base.Dispose(disposing);
            _instance = null;
        }
    }
}
