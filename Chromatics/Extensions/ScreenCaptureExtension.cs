using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace Chromatics.Extensions
{
    /// <summary>
    /// Samples the FFXIV game window on a background thread and produces a
    /// horizontal colour breakdown for ambient base-layer lighting.
    /// Falls back to the primary monitor when FFXIV is not running.
    /// </summary>
    public sealed class ScreenCaptureExtension : IDisposable
    {
        public event EventHandler ColorGenerated;

        // Pixels whose R, G AND B are all below this threshold are treated as
        // near-black (UI overlays, letterboxes) and excluded from the average.
        private const int DarkPixelLimit = 30;

        // Sample every Nth pixel in each direction — balances accuracy vs speed.
        private const int PixelSampleStride = 8;

        // Number of horizontal colour bands sampled left→right across the frame.
        public const int HorizontalSampleCount = 8;

        private const int RefreshIntervalMs = 200;
        private const string FfxivWindowClass = "FFXIVGAME";

        private Thread _workerThread;
        private volatile bool _isStarted;
        private volatile bool _stopRequested;
        private volatile ScreenColor _latestScreenColor;
        private bool _disposed;

        // ── Win32 ──────────────────────────────────────────────────────────

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        // ── Lifecycle ──────────────────────────────────────────────────────

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
        }

        public ScreenColor GetScreenColours() =>
            _isStarted ? _latestScreenColor : null;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            ColorGenerated = null;
        }

        // ── Capture loop ───────────────────────────────────────────────────

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
            var (bmp, isGameWindow) = CaptureTarget();
            if (bmp == null) return;

            using (bmp)
            {
                var (main, columns) = SampleBitmap(bmp, HorizontalSampleCount);
                _latestScreenColor = new ScreenColor
                {
                    MainColor       = main,
                    HorizontalSamples = columns,
                    GameWindowActive  = isGameWindow,
                };
            }

            ColorGenerated?.Invoke(this, EventArgs.Empty);
        }

        // ── Window / screen capture ────────────────────────────────────────

        private (Bitmap bmp, bool isGame) CaptureTarget()
        {
            var hwnd = FindWindow(FfxivWindowClass, null);
            if (hwnd != IntPtr.Zero && GetClientRect(hwnd, out var cr))
            {
                int w = cr.Right  - cr.Left;
                int h = cr.Bottom - cr.Top;
                if (w > 0 && h > 0)
                {
                    var origin = new POINT { X = 0, Y = 0 };
                    ClientToScreen(hwnd, ref origin);
                    var bmp = new Bitmap(w, h);
                    try
                    {
                        using var g = Graphics.FromImage(bmp);
                        g.CopyFromScreen(origin.X, origin.Y, 0, 0, new Size(w, h));
                        return (bmp, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"ScreenCaptureExtension screenshot failed: {ex.Message}");
                        bmp.Dispose();
                    }
                }
            }

            // Fall back to primary monitor
            int sw = GetSystemMetrics(SM_CXSCREEN);
            int sh = GetSystemMetrics(SM_CYSCREEN);
            if (sw <= 0 || sh <= 0) return (null, false);

            var fallback = new Bitmap(sw, sh);
            try
            {
                using var g = Graphics.FromImage(fallback);
                g.CopyFromScreen(0, 0, 0, 0, new Size(sw, sh));
                return (fallback, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ScreenCaptureExtension screenshot failed: {ex.Message}");
                fallback.Dispose();
                return (null, false);
            }
        }

        // ── Pixel sampling ─────────────────────────────────────────────────

        // One LockBits pass over the full bitmap; computes the overall average
        // colour AND per-column averages for the horizontal gradient in a single
        // sweep so we only pay the lock/unlock cost once per frame.
        private static unsafe (Color main, Color[] columns) SampleBitmap(Bitmap bmp, int columnCount)
        {
            var data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int w           = data.Width;
                int h           = data.Height;
                int stride      = data.Stride;
                int colWidth    = Math.Max(1, w / columnCount);

                var colR   = new long[columnCount];
                var colG   = new long[columnCount];
                var colB   = new long[columnCount];
                var colCnt = new int[columnCount];
                long totR = 0, totG = 0, totB = 0;
                int  totCnt = 0;

                byte* scan0 = (byte*)data.Scan0;

                for (int row = 0; row < h; row += PixelSampleStride)
                {
                    byte* rowPtr = scan0 + row * stride;
                    for (int col = 0; col < w; col += PixelSampleStride)
                    {
                        // Format32bppArgb layout per pixel: B G R A
                        byte* px = rowPtr + col * 4;
                        byte b = px[0], g = px[1], r = px[2];

                        if (r < DarkPixelLimit && g < DarkPixelLimit && b < DarkPixelLimit)
                            continue;

                        int seg = Math.Min(col / colWidth, columnCount - 1);
                        colR[seg] += r; colG[seg] += g; colB[seg] += b;
                        colCnt[seg]++;
                        totR += r; totG += g; totB += b;
                        totCnt++;
                    }
                }

                var columns = new Color[columnCount];
                for (int i = 0; i < columnCount; i++)
                {
                    int c = colCnt[i];
                    columns[i] = c > 0
                        ? Color.FromArgb((int)(colR[i] / c), (int)(colG[i] / c), (int)(colB[i] / c))
                        : Color.Black;
                }

                var main = totCnt > 0
                    ? Color.FromArgb((int)(totR / totCnt), (int)(totG / totCnt), (int)(totB / totCnt))
                    : Color.Black;

                return (main, columns);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        // ── Result type ────────────────────────────────────────────────────

        public sealed class ScreenColor
        {
            /// <summary>Average of all non-dark pixels across the whole frame.</summary>
            public Color MainColor { get; set; }

            /// <summary>
            /// Left-to-right horizontal colour samples (length == HorizontalSampleCount).
            /// Index 0 = left edge, last index = right edge.
            /// </summary>
            public Color[] HorizontalSamples { get; set; }

            /// <summary>True when the source was the FFXIV window; false for the fallback monitor capture.</summary>
            public bool GameWindowActive { get; set; }
        }
    }
}
