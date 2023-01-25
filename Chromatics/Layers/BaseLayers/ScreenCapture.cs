using Chromatics.Core;
using Chromatics.Extensions.RGB.NET;
using Chromatics.Helpers;
using Chromatics.Interfaces;
using Chromatics.Models;
using RGB.NET.Core;
using RGB.NET.Presets.Textures;
using RGB.NET.Presets.Textures.Gradients;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Layers
{
    public class ScreenCaptureProcessor : LayerProcessor
    {
        public override void Process(IMappingLayer layer)
        {
            //Do not apply if currently in Preview mode
            if (MappingLayers.IsPreview()) return;
            
            //Static Base Layer Implementation
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

            var brush = new SolidColorBrush(ColorHelper.ColorToRGBColor(System.Drawing.Color.Black));

            if (!layer.Enabled)
            {
                layergroup.Brush = brush;
            }
            else
            {
                //Get screen capture data
                var screenCapture = GetGameWindowPixelColors();

                if (screenCapture != null)
                {
                    var stopTop = new GradientStop[2];
                    var stopBottom = new GradientStop[2];
                    Debug.WriteLine($"TL: {screenCapture[0]}. BL: {screenCapture[1]}. TR: {screenCapture[2]}. BR: {screenCapture[3]}");

                
                    stopTop[0] = new GradientStop(0, ColorHelper.ColorToRGBColor(screenCapture[0]));
                    stopTop[1] = new GradientStop(0, ColorHelper.ColorToRGBColor(screenCapture[2]));
                    //stopBottom[0] = new GradientStop(0, ColorHelper.ColorToRGBColor(screenCapture[1]));
                    //stopBottom[1] = new GradientStop(0, ColorHelper.ColorToRGBColor(screenCapture[4]));

                    var gradientTop = new LinearGradient(stopTop);
                    //var gradientBottom = new LinearGradient(stopBottom);

                    var textureTop = new LinearGradientTexture(new RGB.NET.Core.Size(100, 100), gradientTop);
                    //var textureBottom = new LinearGradientTexture(new RGB.NET.Core.Size(100, 100), gradientBottom);

                    layergroup.Brush = new RGB.NET.Core.TextureBrush(textureTop);
                
                }
            }
            

            //Apply lighting
            layergroup.Attach(surface);
            _init = true;
            layer.requestUpdate = false;

        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private IntPtr GetGameWindowHandle(Process process)
        {
            var gameWindowHandle = FindWindow(null, process.MainWindowTitle);

            if (gameWindowHandle != IntPtr.Zero)
                return gameWindowHandle;

            return IntPtr.Zero;
        }

        private System.Drawing.Color[] GetGameWindowPixelColors()
        {
            // Get the handle of the main window of the process
            if (!GameController.IsGameConnected()) return null;

            var gameWindowHandle = GetGameWindowHandle(GameController.GetGameProcess());

            if (gameWindowHandle != IntPtr.Zero)
            {
                var appSettings = AppSettings.GetSettings();

                // Get the rectangle of the game window
                var rect = new RECT();
                GetClientRect(gameWindowHandle, out rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                   
                using (var bmp = new Bitmap(width, height))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        IntPtr hdc = g.GetHdc();
                        PrintWindow(gameWindowHandle, hdc, 0);
                        g.ReleaseHdc(hdc);
                    }

                    // Create an array to store the pixel colors
                    System.Drawing.Color[] pixelColors = new System.Drawing.Color[4];

                    // Lock the bits of the bitmap
                    var bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

                    // Get a pointer to the first pixel in the bitmap
                    var ptr = bmpData.Scan0;

                    // Get the size of a pixel in bytes
                    int bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) / 8;

                    // Read the pixel colors at the corners of the game window
                    int topLeftX = appSettings.screenCaptureTopLeftOffsetX;
                    int topLeftY = appSettings.screenCaptureTopLeftOffsetY;
                    int bottomLeftX = appSettings.screenCaptureBottomLeftOffsetX;
                    int bottomLeftY = appSettings.screenCaptureBottomLeftOffsetY;
                    int topRightX = appSettings.screenCaptureTopRightOffsetX;
                    int topRightY = appSettings.screenCaptureTopRightOffsetY;
                    int bottomRightX = appSettings.screenCaptureBottomRightOffsetX;
                    int bottomRightY = appSettings.screenCaptureBottomRightOffsetY;

                    int topLeftPixelPos = (topLeftX + (topLeftY * bmpData.Stride)) * bytesPerPixel;
                    int bottomLeftPixelPos = (bottomLeftX + (bottomLeftY * bmpData.Stride) + (bmpData.Stride * (bmpData.Height - 1))) * bytesPerPixel;
                    int topRightPixelPos = (topRightX + (topRightY * bmpData.Stride) + (bmpData.Stride - (bytesPerPixel * 4))) * bytesPerPixel;
                    int bottomRightPixelPos = (bottomRightX + (bottomRightY * bmpData.Stride) + (bmpData.Stride * (bmpData.Height - 1)) + (bmpData.Stride - (bytesPerPixel * 4))) * bytesPerPixel;
                    
                    pixelColors[0] = System.Drawing.Color.FromArgb(Marshal.ReadInt32(ptr + topLeftPixelPos));
                    pixelColors[1] = System.Drawing.Color.FromArgb(Marshal.ReadInt32(ptr + bottomLeftPixelPos));
                    pixelColors[2] = System.Drawing.Color.FromArgb(Marshal.ReadInt32(ptr + topRightPixelPos));
                    pixelColors[3] = System.Drawing.Color.FromArgb(Marshal.ReadInt32(ptr + bottomRightPixelPos));

                    // Unlock the bits of the bitmap
                    bmp.UnlockBits(bmpData);
        
                    return pixelColors;
                }
            }

            return null;
        }
    }
}
