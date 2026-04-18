using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Chromatics.Extensions
{
    /// <summary>
    /// Samples the primary screen on a background thread and produces a dominant-colour
    /// breakdown for consumers that want to drive lighting from on-screen content.
    /// </summary>
    public sealed class ScreenCaptureExtension : IDisposable
    {
        public event EventHandler ColorGenerated;

        private const int DarkPixelLimit = 100;
        private const int PixelSampleStride = 10;
        private const int DefaultSegmentCount = 2;
        private const int RefreshIntervalMs = 500;

        private Thread _workerThread;
        private volatile bool _isStarted;
        private volatile bool _stopRequested;
        private ScreenColor _latestScreenColor;
        private bool _disposed;

        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ScreenCaptureExtension));
            if (_isStarted) return;

            _stopRequested = false;
            _isStarted = true;

            _workerThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "ScreenCaptureExtension",
            };
            _workerThread.Start();
        }

        public void Stop()
        {
            if (!_isStarted) return;

            _stopRequested = true;
            _isStarted = false;
            // Leave the worker thread to exit on its own next tick; it's a background
            // thread so it won't block process shutdown if Dispose isn't called.
        }

        public ScreenColor GetScreenColours()
        {
            return _isStarted ? _latestScreenColor : null;
        }

        private void CaptureLoop()
        {
            while (!_stopRequested)
            {
                try
                {
                    AnalyzeOnce();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ScreenCaptureExtension analyze failed: {ex.Message}");
                }

                Thread.Sleep(RefreshIntervalMs);
            }
        }

        private void AnalyzeOnce()
        {
            using var screenshot = CaptureScreenshot();
            if (screenshot == null) return;

            var result = new ScreenColor
            {
                MainColor = GetAverageColor(screenshot),
            };

            int segmentWidth = Math.Max(1, screenshot.Width / DefaultSegmentCount);
            int segmentHeight = Math.Max(1, screenshot.Height / DefaultSegmentCount);

            int segmentIndex = 0;
            for (int y = 0; y < screenshot.Height; y += segmentHeight)
            {
                for (int x = 0; x < screenshot.Width; x += segmentWidth)
                {
                    // Clamp the segment to the screenshot so Clone doesn't throw near the edges.
                    int width = Math.Min(segmentWidth, screenshot.Width - x);
                    int height = Math.Min(segmentHeight, screenshot.Height - y);
                    if (width <= 0 || height <= 0) continue;

                    var segmentRect = new Rectangle(x, y, width, height);
                    using var segment = screenshot.Clone(segmentRect, screenshot.PixelFormat);
                    result.ScreenColors.TryAdd(segmentIndex++, GetAverageColor(segment));
                }
            }

            _latestScreenColor = result;
            ColorGenerated?.Invoke(this, new ColorGeneratedEventArgs(result));
        }

        private static Bitmap CaptureScreenshot()
        {
            var primary = Screen.PrimaryScreen;
            if (primary == null) return null;

            var bounds = primary.Bounds;
            var bmp = new Bitmap(bounds.Width, bounds.Height);
            try
            {
                using var g = Graphics.FromImage(bmp);
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                return bmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScreenCaptureExtension screenshot failed: {ex.Message}");
                bmp.Dispose();
                return null;
            }
        }

        private static Color GetAverageColor(Bitmap picture)
        {
            int totalR = 0, totalG = 0, totalB = 0;
            int brightPixels = 0, darkPixels = 0;

            for (int y = 0; y < picture.Height; y += PixelSampleStride)
            {
                for (int x = 0; x < picture.Width; x += PixelSampleStride)
                {
                    Color pixel = picture.GetPixel(x, y);
                    if (pixel.R < DarkPixelLimit && pixel.G < DarkPixelLimit && pixel.B < DarkPixelLimit)
                    {
                        darkPixels++;
                    }
                    else
                    {
                        brightPixels++;
                        totalR += pixel.R;
                        totalG += pixel.G;
                        totalB += pixel.B;
                    }
                }
            }

            if (brightPixels == 0)
            {
                return Color.Black;
            }

            double avgR = (double)totalR / brightPixels;
            double avgG = (double)totalG / brightPixels;
            double avgB = (double)totalB / brightPixels;

            // If the frame is dominated by dark pixels, scale the averaged bright colour
            // down proportionally so the output feels "darker" rather than saturated.
            if (brightPixels * 2 < darkPixels)
            {
                double ratio = (double)brightPixels / darkPixels;
                avgR *= ratio;
                avgG *= ratio;
                avgB *= ratio;
            }

            return Color.FromArgb(
                Clamp255(avgR),
                Clamp255(avgG),
                Clamp255(avgB));
        }

        private static int Clamp255(double value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (int)value;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();
            // Drop handler references so subscribers can be garbage-collected.
            ColorGenerated = null;
        }

        public sealed class ScreenColor
        {
            public Color MainColor { get; set; }
            public ConcurrentDictionary<int, Color> ScreenColors { get; } = new ConcurrentDictionary<int, Color>();
        }

        public sealed class ColorGeneratedEventArgs : EventArgs
        {
            public ScreenColor ScreenColor { get; }

            public ColorGeneratedEventArgs(ScreenColor screenColor)
            {
                ScreenColor = screenColor;
            }
        }
    }
}
