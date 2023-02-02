using Chromatics.Core;
using Chromatics.Extensions;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Extensions.RGB.NET.Gradients;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using Microsoft.VisualBasic;
using RGB.NET.Core;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MetroFramework.Drawing.MetroPaint.ForeColor;
using Color = RGB.NET.Core.Color;
using Size = RGB.NET.Core.Size;
using TextureBrush = RGB.NET.Core.TextureBrush;

namespace Chromatics.Layers
{
    public class ScreenCaptureProcessor : LayerProcessor
    {
        private static Dictionary<int, ScreenCaptureBaseModel> layerProcessorModel = new Dictionary<int, ScreenCaptureBaseModel>();
        private static ScreenCaptureExtension screenCapture;

        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview() || RGBController.IsBaseLayerEffectRunning()) return;

            ScreenCaptureBaseModel model;

            if (!layerProcessorModel.ContainsKey(layer.layerID))
            {
                model = new ScreenCaptureBaseModel();
                layerProcessorModel.Add(layer.layerID, model);
            }
            else
            {
                model = layerProcessorModel[layer.layerID];
            }
            
            //Screen Capture Base Layer Implementation
            var _colorPalette = RGBController.GetActivePalette();
            var _layergroupledcollections = new Dictionary<int, HashSet<Led>>();
            var _layergroups = RGBController.GetLiveLayerGroups();

            //loop through all LED's and assign to device layer (Order of LEDs is not important for a base layer)
            var surface = RGBController.GetLiveSurfaces();
            var devices = surface.GetDevices(layer.deviceType);

            PublicListLedGroup layergroup;
            var ledArray = devices.SelectMany(d => d).Where(led => layer.deviceLeds.Any(v => v.Value.Equals(led.Id))).ToArray();

            if (_layergroups.ContainsKey(layer.layerID))
            {
                layergroup = _layergroups[layer.layerID].FirstOrDefault();
                layergroup.ZIndex = layer.zindex;
            }
            else
            {
                layergroup = new PublicListLedGroup(surface, ledArray)
                {
                    ZIndex = layer.zindex,
                };

                var lg = new PublicListLedGroup[] { layergroup };
                _layergroups.Add(layer.layerID, lg);
                layergroup.Detach();
            }

            if (!layer.Enabled)
            {
                layergroup.Brush = new SolidColorBrush(ColorHelper.ColorToRGBColor(System.Drawing.Color.Black));

            }
            else
            {

                //Get screen capture data

                if (screenCapture == null)
                {
                    screenCapture = new ScreenCaptureExtension();
                    screenCapture.Start();
                }

                var screenColours = screenCapture.GetScreenColours();
                if (screenColours == null) return;

                foreach (var colour in screenColours.screenColors )
                {
                    Debug.WriteLine($"{colour.Key}: { colour.Value }");
                }
                
                Debug.WriteLine($"Average: {screenColours.mainColor}. Count Segments: {screenColours.screenColors.Count}");

                //var gradient = new RectangularGradient(System.Drawing.Color.Red, System.Drawing.Color.Yellow, System.Drawing.Color.Green, System.Drawing.Color.Blue);
                //var gradientTexture = new RectangularGradientTexture(new Size(15,6), gradient);

                if (screenColours.screenColors.Count == 4)
                {
                    var gradientTexture = new LinearGradient(new GradientStop((float)0, ColorHelper.ColorToRGBColor(screenColours.screenColors[0])), 
                        new GradientStop((float)0.25, ColorHelper.ColorToRGBColor(screenColours.screenColors[1])), 
                        new GradientStop((float)0.85, ColorHelper.ColorToRGBColor(screenColours.screenColors[2])),
                        new GradientStop((float)1.0, ColorHelper.ColorToRGBColor(screenColours.screenColors[3])));

                    layergroup.Brush = new TextureBrush(new LinearGradientTexture(new Size(100,100), gradientTexture));
                }
                else
                {
                    layergroup.Brush = new SolidColorBrush(ColorHelper.ColorToRGBColor(screenColours.mainColor));
                }

                

                
                
                
            }
            

            //Apply lighting
            layergroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;

        }

        private class ScreenCaptureBaseModel
        {
            public HashSet<LinearGradient> _gradientEffects { get; set; } = new HashSet<LinearGradient>();
            public string _currentWeather { get; set; }
            public bool layerCounted { get; set; }
        }
    }
}
