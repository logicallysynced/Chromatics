using Chromatics.Core;
using Chromatics.Extensions;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using RGB.NET.Core;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System.Drawing;
using System.Linq;
using Color = RGB.NET.Core.Color;
using Size = RGB.NET.Core.Size;
using TextureBrush = RGB.NET.Core.TextureBrush;

namespace Chromatics.Layers
{
    public class ScreenCaptureProcessor : LayerProcessor
    {
        private static ScreenCaptureProcessor _instance;
        private static ScreenCaptureExtension _screenCapture;

        private ScreenCaptureProcessor() { }

        // Rebuilt on demand rather than held in a readonly field: DisposeAll
        // runs every time the player returns to the title screen and nulls
        // the cached surface, so the processor has to be replaceable or every
        // later tick attaches its group to a dead surface.
        public static ScreenCaptureProcessor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ScreenCaptureProcessor();
                }
                return _instance;
            }
        }

        public override void Process(IMappingLayer layer)
        {
            // Read once: Dispose runs from the UI thread at app exit and nulls
            // the field mid-tick, so a second read here would attach to null.
            var activeSurface = surface;
            if (activeSurface == null) return;

            if (RGBController.IsBaseLayerEffectRunning()) return;

            var _layergroups = RGBController.GetLiveLayerGroups();
            var ledArray     = GetLedArray(layer);

            ListLedGroup layergroup;
            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new ListLedGroup(activeSurface, ledArray) { ZIndex = layer.zindex };
                _layergroups[layer.layerID] = new[] { layergroup };
                layergroup.Detach();
            }

            if (!layer.Enabled)
            {
                _screenCapture?.Stop();
                layergroup.Brush = new SolidColorBrush(ColorHelper.ColorToRGBColor(System.Drawing.Color.Black));
            }
            else
            {
                EnsureCaptureRunning();

                var screenColours = _screenCapture?.GetScreenColours();
                layergroup.Brush = screenColours != null
                    ? BuildGradientBrush(screenColours)
                    : new SolidColorBrush(ColorHelper.ColorToRGBColor(System.Drawing.Color.Black));
            }

            layergroup.Attach(activeSurface);
            _init = true;
            layer.requestUpdate = false;
        }

        private static void EnsureCaptureRunning()
        {
            if (_screenCapture == null)
                _screenCapture = new ScreenCaptureExtension();
            _screenCapture.Start();
        }

        // Builds a horizontal LinearGradient from the left-to-right column samples.
        private static TextureBrush BuildGradientBrush(ScreenCaptureExtension.ScreenColor colours)
        {
            var samples = colours.HorizontalSamples;

            GradientStop[] stops;
            if (samples != null && samples.Length > 1)
            {
                stops = new GradientStop[samples.Length];
                for (int i = 0; i < samples.Length; i++)
                {
                    float pos = (float)i / (samples.Length - 1);
                    stops[i] = new GradientStop(pos, ColorHelper.ColorToRGBColor(samples[i]));
                }
            }
            else
            {
                var solid = colours.MainColor != System.Drawing.Color.Empty
                    ? ColorHelper.ColorToRGBColor(colours.MainColor)
                    : ColorHelper.ColorToRGBColor(System.Drawing.Color.Black);
                stops = new[]
                {
                    new GradientStop(0f, solid),
                    new GradientStop(1f, solid),
                };
            }

            return new TextureBrush(new LinearGradientTexture(new Size(100, 100), new LinearGradient(stops)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _screenCapture?.Stop();
                _screenCapture?.Dispose();
                _screenCapture = null;
            }
            base.Dispose(disposing);
            _instance = null;
        }
    }
}
