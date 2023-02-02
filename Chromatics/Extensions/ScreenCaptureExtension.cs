using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace Chromatics.Extensions
{

    public class ScreenCaptureExtension
    {
        public event EventHandler colorGeneratedEvent;

        private const int DARK_PIXEL_LIMIT = 100;
        private const int SKIP_PIXEL = 10;
        private const int DEFAULT_SEGMENT_NUMBER = 2;
        private const int refreshFrequency = 500;

        private bool isStarted;
        private bool _stopRequested;
        private Thread background;
        private ScreenColor _screenTracker;

        public void Start()
        {
            if (isStarted) return;

            _stopRequested = false;

            background = new Thread(startAnalyzeInBackground);
            background.IsBackground = true;
            background.Start();


            colorGeneratedEvent += ColorGeneratedEventHandler;
            isStarted = true;

        }

        public void Stop()
        {
            if (!isStarted) return;

            _stopRequested = true;
            background = null;
            isStarted = false;
        }

        public ScreenColor GetScreenColours()
        {
            if (!isStarted || _screenTracker == null) return null;

            return _screenTracker;
        }

        private void startAnalyzeInBackground()
        {
            while (!_stopRequested)
            {
                if (isStarted)
                {
                    startAnalyze();
                }

                Thread.Sleep(refreshFrequency);

            }
        }

        private void startAnalyze()
        {
            Bitmap screenshot = GetScreenshot();
            ScreenColor screenColor = new ScreenColor();

            // First of all calculate the color for the whole screen
            System.Drawing.Rectangle screen = new System.Drawing.Rectangle(0, 0, screenshot.Width, screenshot.Height);
            Bitmap wholeScreen = screenshot.Clone(screen, screenshot.PixelFormat);
            System.Drawing.Color mainColor = getPictureAverageColor(wholeScreen);
            screenColor.mainColor = mainColor;


            // Then divide the screen and calculate the segments            
            int segmentX = 0;
            int segmentY = 0;
            int segmentWidth = (int)screenshot.Width / DEFAULT_SEGMENT_NUMBER;
            int segmentHeight = (int)screenshot.Height / DEFAULT_SEGMENT_NUMBER;

            // Segment cannot be bigger then the screen size
            if (segmentWidth > screenshot.Width) { segmentHeight = screenshot.Width; }
            if (segmentHeight > screenshot.Height) { segmentHeight = screenshot.Height; }


            // Go troug the segments from the upper left to right and down
            // *-----> |
            // *<------¡
            int segmentCounter = 0;
            for (var j = 0; j < screenshot.Height; j = j + segmentHeight)
            {
                for (var i = 0; i < screenshot.Width; i = i + segmentWidth)
                {
                    System.Drawing.Rectangle segmentArea = new System.Drawing.Rectangle(i, j, segmentWidth, segmentHeight);
                    Bitmap segmentPicture = screenshot.Clone(segmentArea, screenshot.PixelFormat);
                    System.Drawing.Color segmenColor = getPictureAverageColor(segmentPicture);
                    try
                    {
                        screenColor.screenColors.TryAdd(segmentCounter++, segmenColor);
                    }
                    catch
                    {
                        Debug.WriteLine("Cannot add color to the list. This should not happen.");
                    }
                }
            }

            System.Drawing.Rectangle screenPart = new System.Drawing.Rectangle(segmentX, segmentY, segmentWidth, segmentHeight);
            Bitmap segmentPic = screenshot.Clone(screenPart, screenshot.PixelFormat);

            System.Drawing.Color c = getPictureAverageColor(screenshot);

            //Debug.WriteLine("Timeout:" + refreshFrequency);

            colorGeneratedEvent.Invoke(this, new ColorEvent(screenColor));

            // Otherwise the memory consumption is high till the normal GC running
            System.GC.Collect();
        }

        private Bitmap GetScreenshot()
        {
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            try
            {
                Graphics g = Graphics.FromImage(bmp);
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size);

                // bmp.Save("screenshot.bmp");
            }
            catch
            {
                Debug.WriteLine("Cannot take a screenshot. Log off screen?");
            }

            return bmp;
        }

        private async void ColorGeneratedEventHandler(object sender, EventArgs e)
        {
            // We expecting a ScreenColorEvent here
            ColorEvent colorEvent = (ColorEvent)e;
            _screenTracker = colorEvent.screenColor;
        }


        private System.Drawing.Color getPictureAverageColor(Bitmap picture)
        {

            for (int h = 0; h < picture.Height; h += SKIP_PIXEL)
            {
                for (int w = 0; w < picture.Width; w += SKIP_PIXEL)
                {
                    System.Drawing.Color pixelColor = picture.GetPixel(w, h);
                    addPixel(pixelColor);
                }
            }

            System.Drawing.Color c = getAverageColor();
            string hex = ColorTranslator.ToHtml(c);

            //Debug.WriteLine("Hex color " + hex);
            //Debug.WriteLine("RGB color " + c.R + ", " + c.G + ", " + c.B);
            return c;
        }

        private int allPixels { get; set; }
        private int darkPixels { get; set; }
        private int brightPixels { get; set; }

        private RGB colorCollercor { get; } = new RGB();

        private void addPixel(System.Drawing.Color pixelColor)
        {
            int r = pixelColor.R;
            int g = pixelColor.G;
            int b = pixelColor.B;

            bool addPixel = false;
            if (r < DARK_PIXEL_LIMIT && g < DARK_PIXEL_LIMIT && b < DARK_PIXEL_LIMIT)
            {
                darkPixels++;
            }
            else
            {
                brightPixels++;
                addPixel = true;
            }

            if (addPixel)
            {
                colorCollercor.red += r;
                colorCollercor.green += g;
                colorCollercor.blue += b;
                allPixels++;
            }
        }
        private System.Drawing.Color getAverageColor()
        {
            RGB avgRGB = getAverageRGB();
            if (avgRGB.red < 0) { avgRGB.red = 0; }
            if (avgRGB.green < 0) { avgRGB.green = 0; }
            if (avgRGB.blue < 0) { avgRGB.blue = 0; }
            if (avgRGB.red > 255) { avgRGB.red = 255; }
            if (avgRGB.green > 255) { avgRGB.green = 255; }
            if (avgRGB.blue > 255) { avgRGB.blue = 255; }

            return System.Drawing.Color.FromArgb(avgRGB.red, avgRGB.green, avgRGB.blue);
        }

        private RGB getAverageRGB()
        {

            double calculatedRed;
            double calculatedGreen;
            double calculatedBlue;

            if (allPixels == 0) { allPixels = 1; }

            calculatedRed = (double)colorCollercor.red / allPixels;
            calculatedGreen = (double)colorCollercor.green / allPixels;
            calculatedBlue = (double)colorCollercor.blue / allPixels;

            // Make darker color if most of the screen surface is black
            if (brightPixels * 2 < darkPixels)
            {
                double ratio = (double)brightPixels / darkPixels;

                calculatedRed = Convert.ToInt32(calculatedRed * ratio);
                calculatedGreen = Convert.ToInt32(calculatedGreen * ratio);
                calculatedBlue = Convert.ToInt32(calculatedBlue * ratio);
            }

            //Debug.WriteLine("RGB color " + calculatedRed + ", " + calculatedGreen + ", " + calculatedBlue);

            return new RGB()
            {
                red = (int)calculatedRed,
                green = (int)calculatedGreen,
                blue = (int)calculatedBlue
            };
        }

        private class RGB
        {
            public int red { get; set; }
            public int green { get; set; }
            public int blue { get; set; }

        }

        public class ScreenColor
        {
            public System.Drawing.Color mainColor { get; set; }
            public ConcurrentDictionary<int, System.Drawing.Color> screenColors = new ConcurrentDictionary<int, System.Drawing.Color>();
        }

        private class ColorEvent : EventArgs
        {
            public ScreenColor screenColor { get; set; }
            public ColorEvent(ScreenColor c)
            {
                this.screenColor = c;
            }

        }


    }
}
