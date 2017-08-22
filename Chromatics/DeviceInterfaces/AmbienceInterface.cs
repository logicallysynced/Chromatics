using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using Chromatics.DeviceInterfaces.AmbienceLibs;
using Cyotek.Windows.Forms;
using Point = System.Drawing.Point;

namespace Chromatics.DeviceInterfaces
{
    class AmbienceInterface
    {
        private static readonly DesktopMirror _mirror = new DesktopMirror();
        private static Dictionary<int, Collection<Point>> leftCoordinates = new Dictionary<int, Collection<Point>>();
        private static Dictionary<int, Collection<Point>> topCoordinates = new Dictionary<int, Collection<Point>>();
        private static Dictionary<int, Collection<Point>> bottomCoordinates = new Dictionary<int, Collection<Point>>();
        private static Dictionary<int, Collection<Point>> rightCoordinates = new Dictionary<int, Collection<Point>>();
        private static Func<int, int> leftIterator;
        private static Func<int, int> rightIterator;
        private static Func<int, int> topIterator;
        private static Func<int, int> bottomIterator;
        private static Stopwatch frameTimer;
        private static Stopwatch sectionTimer;
        private static Bitmap bmpScreenshot;
        private static Thread a;

        private static int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        private static int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private static byte[] gammaTable = new byte[256];
        private static byte[] bufferScreen = new byte[125];
        private static byte[] bufferScreen2 = new byte[125];
        const byte ledStripsPerChannel = 4; // Always assume max amount of strips is connected or serial stream fails (for now)
        const byte headerBits = 5;
        const byte magicBit = 75;
        const byte colorBits = 3;
        const byte animationMode = 0;
        const byte animationDirection = 0;
        const byte animationOptions = 0;
        const byte animationGroup = 0;
        const byte animationSpeed = 2;
        private static byte delay = 0;
        private static double gamma = 1;
        private static int scanDepth = 100;
        private static int pixelsToSkipPerCoordinate = 100; // Every LED region has (scanDepth * ScreenBorderPixelsInRegion / pixelsToSkipPerCoordinate) = possible coordinates. E.g. (100 * 144 / 100) = 144 coordinates;
        private static int rightLedCount = 10;
        private static int leftLedCount = 10;
        private static int topLedCount = 20;
        private static int bottomLedCount = 20;
        private static int totalRed;
        private static int totalGreen;
        private static int totalBlue;
        
        private static bool isEngineEnabled = true;
        private static bool isSendingFrame = false;
        private static bool isReadingScreen = false;

        private static bool init = false;

        public static List<Color> AmbIndex = new List<Color>();

        private static void InitAmbience()
        {
            SetupBuffer(bufferScreen, 1);
            SetupBuffer(bufferScreen2, 2);
            SetupGammaTable();
            SetupPixelIterators();
        }

        public static void StartAmbience()
        {
            if (!init)
            {
                InitAmbience();
            }

            SetupCoordinates();
            ConnectMirrorDriver();
            StartEngine();
        }
        
        public static void StopAmbience()
        {
            while (isReadingScreen)
            {
                //Do nothing
            }

            isEngineEnabled = false;
            _mirror.Dispose();
        }

        private static void SetupBuffer(byte[] buffer, byte channel)
        {
            buffer[0] = magicBit;
            buffer[1] = channel;
            buffer[2] = animationMode;
            buffer[3] = animationDirection << 4 | animationOptions << 3 | ledStripsPerChannel;
            buffer[4] = 0 << 5 | animationGroup << 3 | animationSpeed;
            
        }

        private static void SetupGammaTable()
        {
            for (int i = 0; i < 256; i++)
            {
                gammaTable[i] = (byte)(Math.Pow((float)i / 255.0, gamma) * 255 + 0.5);
            }
            
        }
        
        private static void SetupPixelIterators()
        {
            leftIterator = (i) => (i + rightLedCount + topLedCount);
            rightIterator = (i) => (rightLedCount - i - 1);
            topIterator = (i) => (topLedCount - i + rightLedCount - 1);
            bottomIterator = (i) => (bottomLedCount - i - 1);
        }

        private static void SetupCoordinates()
        {
            SetupCoordinatesWith(leftCoordinates, leftLedCount, 0, screenHeight, true);
            SetupCoordinatesWith(rightCoordinates, rightLedCount, screenWidth - (scanDepth + 1), screenHeight, true);
            SetupCoordinatesWith(topCoordinates, topLedCount, 0, screenWidth, false);
            SetupCoordinatesWith(bottomCoordinates, bottomLedCount, screenHeight - (scanDepth + 1), screenWidth, false);
            
        }

        private static void SetupCoordinatesWith(Dictionary<int, Collection<Point>> coordinates, int ledCount, int xOrigin, int xMax, bool isHorizontal)
        {
            int ratio = xMax / ledCount;
            int count = 0;

            for (int ledIndex = 0; ledIndex < ledCount; ledIndex++)
            {
                {
                    coordinates.Add(ledIndex, new Collection<Point>());
                    for (int x = xOrigin; x < xOrigin + scanDepth; x++)
                    {
                        int yOrigin = ledIndex * ratio;
                        int yMax = yOrigin + ratio;

                        for (int y = yOrigin; y < yMax; y++)
                        {
                            count++;
                            if ((count % pixelsToSkipPerCoordinate) == 0)
                            {
                                if (isHorizontal)
                                {
                                    coordinates[ledIndex].Add(new Point(x, y));
                                }
                                else
                                {
                                    coordinates[ledIndex].Add(new Point(y, x));
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void StartEngine()
        {
            a = new Thread(AmbiEngine);
            a.Start();
        }

        private static void AmbiEngine()
        {
            while (isEngineEnabled)
            {
                frameTimer = new Stopwatch();
                frameTimer.Start();

                CalculateBuffers();
                SendBuffers();

                frameTimer.Stop();
            }
        }

        private static void CalculateBuffers()
        {
            sectionTimer = new Stopwatch();
            sectionTimer.Start();

            try
            {
                FillBuffersFromScreen();
            }
            catch (Exception e)
            {
                //logger.Add("Error during calculations: " + e);
                Debug.WriteLine(e.StackTrace);
            }

            sectionTimer.Stop();
        }

        private static void SendBuffers()
        {
            sectionTimer = new Stopwatch();
            sectionTimer.Start();

            if (!isSendingFrame)
            {
                isSendingFrame = true;
                //fpsCounter++;
                //SendBuffersToPort();

            }

            sectionTimer.Stop();
        }

        private static void EnableNextFrame()
        {
            isSendingFrame = false;
        }

        /*
        private static void FillBuffersWithColor(Color color)
        {
            SetAllLedsToColor(bufferScreen, color);
            SetAllLedsToColor(bufferScreen2, color);
        }
        */

        private static void FillBuffersFromScreen()
        {
            if (delay > 0)
            {
                Thread.Sleep(delay);
            }

            UpdateScreenShot();

            FillBufferFromScreenWith(bufferScreen, leftCoordinates, leftIterator, leftLedCount);
            FillBufferFromScreenWith(bufferScreen, rightCoordinates, rightIterator, rightLedCount);
            FillBufferFromScreenWith(bufferScreen, topCoordinates, topIterator, topLedCount);
            FillBufferFromScreenWith(bufferScreen2, bottomCoordinates, bottomIterator, bottomLedCount);

            DisposeScreenShot();
        }

        private static void FillBufferFromScreenWith(byte[] buffer, Dictionary<int, Collection<Point>> coordinates, Func<int, int> LedIterator, int ledCount)
        {
            Debug.WriteLine(AmbIndex[0].GetHashCode());
            AmbIndex.Clear();

            for (int ledIndex = 0; ledIndex < ledCount; ledIndex++)
            {
                totalRed = totalGreen = totalBlue = 0;
                int totalColorsParsed = 0;

                for (int coordinateIndex = 0; coordinateIndex < coordinates[ledIndex].Count; coordinateIndex++)
                {
                    Color currentColor = bmpScreenshot.GetPixel(coordinates[ledIndex][coordinateIndex].X, coordinates[ledIndex][coordinateIndex].Y);
                    totalRed += currentColor.R;
                    totalGreen += currentColor.G;
                    totalBlue += currentColor.B;
                    totalColorsParsed++;
                }

                AmbIndex.Add(Color.FromArgb(totalRed / totalColorsParsed, totalGreen / totalColorsParsed, totalBlue / totalColorsParsed));
                //SetOneLedToColor(buffer, LedIterator(ledIndex), Color.FromArgb(totalRed / totalColorsParsed, totalGreen / totalColorsParsed, totalBlue / totalColorsParsed));
            }
        }

        /*
        private static void SetOneLedToColor(byte[] buffer, int ledIndex, Color color)
        {
            int bufferIndex = colorBits * ledIndex + headerBits;
            SetBufferColorAt(buffer, bufferIndex, color);
        }

        private static void SetAllLedsToColor(byte[] buffer, Color color)
        {

            for (int bufferIndex = headerBits; bufferIndex < buffer.Length; bufferIndex += colorBits)
            {
                SetBufferColorAt(buffer, bufferIndex, color);
            }
            
        }

        
        private static void SetBufferColorAt(byte[] buffer, int bufferIndex, Color color)
        {
            buffer[bufferIndex++] = gammaTable[color.G];
            buffer[bufferIndex++] = gammaTable[color.R];
            buffer[bufferIndex++] = gammaTable[color.B];
        }
        */
        
        

        private static void ConnectMirrorDriver()
        {
            if (_mirror.Load())
            {
                _mirror.Connect();
            }
            else
            {
                Debug.WriteLine("Error Loading Ambience.");
            }
        }

        private static Bitmap UpdateScreenShot()
        {
            isReadingScreen = true;
            bmpScreenshot = _mirror.GetScreen();
            isReadingScreen = false;

            return bmpScreenshot;
        }

        private static void DisposeScreenShot()
        {
            bmpScreenshot.Dispose();
            bmpScreenshot = null;
        }

        private static void SaveScreenShot()
        {
            UpdateScreenShot().Save("screenshot.bmp");
        }
    }
}
