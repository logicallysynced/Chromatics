using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using RGB.NET.Presets.Decorators;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Chromatics.Layers
{
    // Gold Saucer "vegas mode" base-layer override. When the player is in The
    // Gold Saucer and the Vegas Mode effect is enabled, this processor paints
    // an animated rainbow gradient over every device's base layer: a conical
    // sweep on keyboards / LED matrices, a linear sweep on everything else.
    // The user's chosen base layer is suppressed while vegas is active so the
    // gradient is the only thing painting on the base slot; dynamic layers
    // (gauges, castbars, raid highlights) keep rendering on top of it.
    //
    // Architecture mirrors RaidEffectProcessor's gradient case: per-EffectLayer
    // overlay group attached at the base layer's ZIndex (so dynamic layers
    // paint over it), plus a surface.Updating hook that re-detaches the user's
    // base ledgroup every render tick. The hook is necessary because the base
    // processors run at game-loop rate (~5 Hz) while the surface renders at
    // ~50-100 Hz; without it, the base group would briefly re-attach between
    // game-loop ticks and flicker through the gradient.
    public sealed class GoldSaucerVegasProcessor : LayerProcessor
    {
        private static GoldSaucerVegasProcessor _instance;
        public static GoldSaucerVegasProcessor Instance => _instance ??= new GoldSaucerVegasProcessor();

        private readonly Dictionary<int, ListLedGroup> _overlays = new();
        private readonly Dictionary<int, RainbowGradient> _gradients = new();
        private readonly Dictionary<int, string> _activeZones = new();
        // effectLayerId -> baseLayerId so the surface.Updating hook can find
        // every base group it needs to keep detached, and TeardownOverlay can
        // re-attach the right base group on deactivation.
        private readonly Dictionary<int, int> _suppressedBaseIds = new();
        private readonly Lock _suppressLock = new();
        private bool _hookedUpdating;
        private bool _disposed;

        // Zones that trigger vegas. Currently just the main Gold Saucer
        // (TerritoryType 144 -> "The Gold Saucer"). Add zone names here if
        // vegas should extend to housing minigames / event maps / etc.
        // Names must match Sharlayan's English Lumina lookup verbatim
        // (GameHelper.GetZoneNameById defaults to "en" so client language
        // doesn't matter here).
        private static readonly FrozenSet<string> _vegasZones = new[]
        {
            "The Gold Saucer",
        }.ToFrozenSet(StringComparer.Ordinal);

        private GoldSaucerVegasProcessor() { }

        public override void Process(IMappingLayer layer)
        {
            if (_disposed) return;

            var effectSettings = RGBController.GetEffectsSettings();

            // Per-device "all effects off" gate, mirrors the same check in
            // every other effect-class processor (DamageFlash, DutyFinderBell, ...).
            if (!MappingLayers.IsDeviceEffectsEnabled(layer.deviceGuid))
            {
                TeardownOverlay(layer.layerID);
                return;
            }

            // Vegas-mode toggle off => no overlay, base layer paints normally.
            if (!effectSettings.effect_vegasmode)
            {
                TeardownOverlay(layer.layerID);
                return;
            }

            // Sharlayan / Lumina not attached yet. Without zone data we can't
            // tell whether we're in the Gold Saucer, so leave the base layer
            // alone rather than painting vegas blindly.
            var handler = GameController.GetGameData();
            if (handler?.Reader == null || !handler.Reader.CanGetActors())
            {
                TeardownOverlay(layer.layerID);
                return;
            }

            var player = handler.Reader.GetCurrentPlayer();
            if (player.Entity == null)
            {
                TeardownOverlay(layer.layerID);
                return;
            }

            var zone = GameHelper.GetZoneNameById(player.Entity.MapTerritory);
            if (string.IsNullOrEmpty(zone) || !_vegasZones.Contains(zone))
            {
                TeardownOverlay(layer.layerID);
                return;
            }

            // Resolve THIS device's base layer. Match by deviceGuid (not just
            // deviceType) so a multi-keyboard / multi-mouse setup paints each
            // device's vegas overlay on that device's own base ledset.
            var baseLayer = MappingLayers.GetLayers().Values
                .FirstOrDefault(x => x.rootLayerType == Enums.LayerType.BaseLayer
                                    && x.deviceGuid == layer.deviceGuid);
            if (baseLayer == null) return;

            var ledArray = GetLedBaseArray(layer, baseLayer);
            if (ledArray.Length == 0)
            {
                TeardownOverlay(layer.layerID);
                return;
            }

            bool zoneChanged = !_activeZones.TryGetValue(layer.layerID, out var lastZone) || lastZone != zone;
            bool needsRebuild = zoneChanged || !_overlays.ContainsKey(layer.layerID);

            if (needsRebuild)
            {
                BuildOverlay(layer, baseLayer, ledArray);
            }

            // Track the base layer id so the surface.Updating hook can keep
            // it detached while vegas owns this device's base slot.
            lock (_suppressLock)
            {
                _suppressedBaseIds[layer.layerID] = baseLayer.layerID;
            }
            EnsureUpdatingHook();
            SuppressBaseLayerGroupNow(baseLayer.layerID);
            RGBController.SetBaseLayerEffect(true);

            if (_overlays.TryGetValue(layer.layerID, out var overlay) && overlay.Surface == null)
            {
                overlay.ZIndex = baseLayer.zindex;
                overlay.Attach(surface);
            }

            _activeZones[layer.layerID] = zone;
            layer.requestUpdate = false;
        }

        private void BuildOverlay(IMappingLayer layer, Layer baseLayer, Led[] ledArray)
        {
            // Tear down anything that exists for this id first.
            if (_overlays.TryGetValue(layer.layerID, out var existing))
            {
                existing.RemoveAllDecorators();
                existing.Detach();
                _overlays.Remove(layer.layerID);
            }
            if (_gradients.TryGetValue(layer.layerID, out var existingGradient))
            {
                existingGradient.RemoveAllDecorators();
                _gradients.Remove(layer.layerID);
            }

            var overlay = new ListLedGroup(surface, ledArray) { ZIndex = baseLayer.zindex };
            overlay.Detach();

            var gradient = new RainbowGradient();
            gradient.AddDecorator(new MoveGradientDecorator(surface)
            {
                IsEnabled = true,
                Speed = 100,
            });

            // Conical sweep on grid devices (keyboards, LED matrices) so the
            // rainbow wheels around the keys; linear sweep on flat / zonal
            // devices (mice, headsets, mousepads, chassis, Hue, LIFX) where
            // a conical pattern collapses to a single hue because the LEDs
            // don't have a 2D layout to sweep across.
            bool isGrid = layer.deviceType == RGBDeviceType.Keyboard
                       || layer.deviceType == RGBDeviceType.LedMatrix;
            IBrush brush = isGrid
                ? new TextureBrush(new ConicalGradientTexture(new Size(100, 100), gradient))
                : new TextureBrush(new LinearGradientTexture(new Size(100, 100), gradient));

            overlay.Brush = brush;

            _overlays[layer.layerID] = overlay;
            _gradients[layer.layerID] = gradient;
        }

        // Detach the base layer's user-side ledgroup now. The surface.Updating
        // hook keeps detaching it on every render tick so the base processor's
        // re-attach (game-loop rate) can't slip through between hook fires.
        private void SuppressBaseLayerGroupNow(int baseLayerId)
        {
            var live = RGBController.GetLiveLayerGroups();
            if (!live.TryGetValue(baseLayerId, out var groups)) return;
            foreach (var g in groups) g?.Detach();
        }

        private void EnsureUpdatingHook()
        {
            if (_hookedUpdating || surface == null) return;
            surface.Updating += OnSurfaceUpdating;
            _hookedUpdating = true;
        }

        private void OnSurfaceUpdating(UpdatingEventArgs args)
        {
            var live = RGBController.GetLiveLayerGroups();
            lock (_suppressLock)
            {
                foreach (var baseId in _suppressedBaseIds.Values)
                {
                    if (live.TryGetValue(baseId, out var groups))
                    {
                        foreach (var g in groups) g?.Detach();
                    }
                }
            }
        }

        public override void CleanupLayer(int layerID) => TeardownOverlay(layerID);

        private void TeardownOverlay(int layerID)
        {
            int? baseId;
            lock (_suppressLock)
            {
                if (_suppressedBaseIds.TryGetValue(layerID, out var b))
                {
                    baseId = b;
                    _suppressedBaseIds.Remove(layerID);
                }
                else
                {
                    baseId = null;
                }
            }

            if (_overlays.TryGetValue(layerID, out var overlay))
            {
                overlay.RemoveAllDecorators();
                overlay.Brush = new SolidColorBrush(Color.Transparent);
                overlay.Detach();
                _overlays.Remove(layerID);
            }
            if (_gradients.TryGetValue(layerID, out var gradient))
            {
                gradient.RemoveAllDecorators();
                _gradients.Remove(layerID);
            }
            _activeZones.Remove(layerID);

            // Re-attach the user's base ledgroup so the base processor's next
            // tick has a live group to paint onto. The base processors
            // (Static, JobClasses, ReactiveWeather, ...) only set the brush
            // on the existing layergroup; they don't call Attach themselves.
            // Without this, the base layer would render invisible after vegas
            // deactivates because the group we detached on activation stays
            // detached.
            if (baseId.HasValue && surface != null)
            {
                var live = RGBController.GetLiveLayerGroups();
                if (live.TryGetValue(baseId.Value, out var groups))
                {
                    foreach (var g in groups)
                    {
                        if (g != null && g.Surface == null) g.Attach(surface);
                    }
                }
            }

            // Clear the global flag only when no vegas overlays remain.
            // Otherwise the base processors would resume painting on OTHER
            // devices' still-active vegas overlays.
            if (_overlays.Count == 0)
            {
                RGBController.SetBaseLayerEffect(false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                if (_hookedUpdating && surface != null)
                {
                    surface.Updating -= OnSurfaceUpdating;
                    _hookedUpdating = false;
                }
                foreach (var id in _overlays.Keys.ToList())
                {
                    TeardownOverlay(id);
                }
                _overlays.Clear();
                _gradients.Clear();
                _activeZones.Clear();
                lock (_suppressLock)
                {
                    _suppressedBaseIds.Clear();
                }
            }
            _disposed = true;
            base.Dispose(disposing);
            _instance = null;
        }
    }
}
