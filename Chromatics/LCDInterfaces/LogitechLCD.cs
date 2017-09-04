using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Chromatics.FFXIVInterfaces;
using Chromatics.Properties;
using GalaSoft.MvvmLight.Ioc;
using Logitech_LCD;
using Logitech_LCD.Applets;
using Sharlayan.Core;
using Sharlayan.Core.Enums;
using Exception = System.Exception;

namespace Chromatics.LCDInterfaces
{
    public class LogitechLcdInterface
    {
        private static ILogWrite _write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static LogitechLcd InitializeLcdSdk()
        {
            LogitechLcd lcd = null;
            if (Process.GetProcessesByName("LCore").Length > 0)
            {
                lcd = new LogitechLcd();
                var result = lcd.InitializeLcd();
                if (!result)
                    return null;
            }
            return lcd;
        }
    }

    public class LogitechLcdWrapper
    {
        public const int MONO_WIDTH = 160;
        public const int MONO_HEIGHT = 43;
        public const int COL_WIDTH = 320;
        public const int COL_HEIGHT = 240;

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern int DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "GetDIBits")]
        static extern int GetDIBits([In] IntPtr hdc, [In] IntPtr hbmp, uint uStartScan, uint cScanLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbi, DIB_Color_Mode uUsage);

        enum DIB_Color_Mode : uint
        {
            DIB_RGB_COLORS = 0,
            DIB_PAL_COLORS = 1
        }
        
        static uint BI_RGB = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public uint biSize;
            public int biWidth, biHeight;
            public short biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public uint[] cols;
        }

        static uint MAKERGB(int r, int g, int b)
        {
            return ((uint)(b & 255)) | ((uint)((g & 255) << 8)) | ((uint)((r & 255) << 16));
        }

        public static byte[] ConvertColor(Bitmap b)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            IntPtr hBitmap = b.GetHbitmap();

            BITMAPINFO bitmapInfo = new BITMAPINFO { biSize = 40 };

            GetDIBits(hdc, hBitmap, 0, 0, null, ref bitmapInfo, DIB_Color_Mode.DIB_RGB_COLORS);

            bitmapInfo.biCompression = BI_RGB;
            bitmapInfo.biHeight = -bitmapInfo.biHeight;

            var byteBitmap = new byte[COL_WIDTH * COL_HEIGHT * 4];

            GetDIBits(hdc, hBitmap, 0, (uint)-bitmapInfo.biHeight, byteBitmap, ref bitmapInfo, DIB_Color_Mode.DIB_RGB_COLORS);
            
            DeleteDC(hdc);
            DeleteDC(hBitmap);
            DeleteObject(hdc);
            DeleteObject(hBitmap);

            return byteBitmap;
        }

        public static byte[] ConvertMono(Bitmap b)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            IntPtr hBitmap = b.GetHbitmap();

            BITMAPINFO bitmapInfoMono = new BITMAPINFO {biSize = 40};

            GetDIBits(hdc, hBitmap, 0, 0, null, ref bitmapInfoMono, DIB_Color_Mode.DIB_RGB_COLORS);

            bitmapInfoMono.biCompression = BI_RGB;
            bitmapInfoMono.biHeight = -bitmapInfoMono.biHeight;

            var byteBitmapMono = new byte[MONO_WIDTH * MONO_HEIGHT * 4];

            GetDIBits(hdc, hBitmap, 0, (uint)-bitmapInfoMono.biHeight, byteBitmapMono, ref bitmapInfoMono, DIB_Color_Mode.DIB_RGB_COLORS);

            var byteBitmapMono8bpp = new byte[MONO_WIDTH * MONO_HEIGHT];

            // Transform 32bpp data into 8bpp. Let's just take the value of first of 4 bytes (Blue)
            for (int ii = 0; ii < (MONO_WIDTH * MONO_HEIGHT * 4); ii = ii + 4)
            {
                byteBitmapMono8bpp[ii / 4] = byteBitmapMono[ii];
            }

            DeleteDC(hdc);
            DeleteDC(hBitmap);
            DeleteObject(hdc);
            DeleteObject(hBitmap);

            return byteBitmapMono8bpp;
        }
    }

    public interface ILogitechLcd
    {
        bool InitializeLcd();
        void DrawLCDInfo(ActorEntity _pI, ActorEntity _tI);
        void StatusLCDInfo(string text);
        void ShutdownLcd();

        bool SetStartupFlag { get; set; }
    }
    

    public class LogitechLcd : ILogitechLcd
    {
        private static ILogWrite _write = SimpleIoc.Default.GetInstance<ILogWrite>();
        
        private static int _page = 1;
        private const int MaxPage = 4;
        private static BaseAppletM _selectedMonoControl;
        private static BaseAppletM _selectedColorControl;

        private ActorEntity PlayerInfo;
        private ActorEntity TargetInfo;

        private System.Timers.Timer _buttonCheckTimer;
        
        private bool startup;

        //Color LCD button events
        private event EventHandler LcdColorLeftButtonPressed;
        private event EventHandler LcdColorRightButtonPressed;
        private event EventHandler LcdColorUpButtonPressed;
        private event EventHandler LcdColorDownButtonPressed;
        private event EventHandler LcdColorOkButtonPressed;
        private event EventHandler LcdColorCancelButtonPressed;
        private event EventHandler LcdColorMenuButtonPressed;

        //Mono LCD Button Events
        private event EventHandler LcdMonoButton0Pressed;
        private event EventHandler LcdMonoButton1Pressed;
        private event EventHandler LcdMonoButton2Pressed;
        private event EventHandler LcdMonoButton3Pressed;

        public bool SetStartupFlag
        {
            get => startup;
            set => startup = value;
        }

        public bool InitializeLcd()
        {
            Logitech_LCD.LogitechLcd.Instance.Init("Final Fantasy XIV", LcdType.Mono | LcdType.Color);

            if (!Logitech_LCD.LogitechLcd.Instance.IsConnected(LcdType.Mono | LcdType.Color))
            {
                _write.WriteConsole(ConsoleTypes.Logitech, "No LCD Screens Connected.");
                return false;
            }

            try
            {
                Logitech_LCD.LogitechLcd.Instance.MonoSetText(0, @"Final Fantasy XIV");

                _buttonCheckTimer = new System.Timers.Timer
                {
                    Interval = 80,
                    AutoReset = true
                };
                _buttonCheckTimer.Elapsed += CheckButtons;

                _buttonCheckTimer.Start();

                LcdMonoButton0Pressed += SelectedMonoControlOnLcdMonoButton0Pressed;
                LcdMonoButton1Pressed += SelectedMonoControlOnLcdMonoButton1Pressed;
                LcdMonoButton2Pressed += SelectedMonoControlOnLcdMonoButton2Pressed;
                LcdMonoButton3Pressed += SelectedMonoControlOnLcdMonoButton3Pressed;

                startup = false;

                return true;
            }
            catch (Exception e)
            {
                _write.WriteConsole(ConsoleTypes.Error, e.StackTrace);
                _write.WriteConsole(ConsoleTypes.Logitech, "Unable to load LCD SDK.");
                return false;
            }
        }

        public void ShutdownLcd()
        {
            try
            {
                _write.WriteConsole(ConsoleTypes.Logitech, "Disabling LCD SDK.");

                if (_selectedMonoControl != null)
                {
                    _selectedMonoControl.IsActive = false;
                    _selectedMonoControl.Dispose();
                    _selectedMonoControl = null;
                }

                _buttonCheckTimer.Stop();
                LcdMonoButton0Pressed -= SelectedMonoControlOnLcdMonoButton0Pressed;
                LcdMonoButton1Pressed -= SelectedMonoControlOnLcdMonoButton1Pressed;
                LcdMonoButton2Pressed -= SelectedMonoControlOnLcdMonoButton2Pressed;
                LcdMonoButton3Pressed -= SelectedMonoControlOnLcdMonoButton3Pressed;

                _buttonCheckTimer.Dispose();

                Logitech_LCD.LogitechLcd.Instance.Shutdown();
            }
            catch (Exception ex)
            {
                _write.WriteConsole(ConsoleTypes.Error, ex.Message);
                _write.WriteConsole(ConsoleTypes.Error, ex.StackTrace);
            }
        }

        
        public void DrawLCDInfo(ActorEntity _pI, ActorEntity _tI)
        {
            PlayerInfo = _pI;
            TargetInfo = _tI;

            if (startup) return;

            new Task(() =>
            {
                SwitchPages(1);
            }).Start();
            startup = true;
        }

        private static void SwitchPages(int page)
        {
            
            try
            {
                //if (_page == _lastpage) return;

                Logitech_LCD.LogitechLcd.Instance.MonoSetText(0, @"");
                Logitech_LCD.LogitechLcd.Instance.MonoSetText(1, @"");
                Logitech_LCD.LogitechLcd.Instance.MonoSetText(2, @"");
                Logitech_LCD.LogitechLcd.Instance.MonoSetText(3, @"");

                if (_selectedMonoControl != null)
                {
                    _selectedMonoControl.IsActive = false;
                    _selectedMonoControl.Dispose();
                    _selectedMonoControl = null;
                }

                Console.WriteLine(@"Page: " + page);


                //0 - Eorzea Time
                //1 - Server Time
                //2 - Local Time
                
                switch (page)
                {
                    case 1:
                        //Eorzea Time
                        _selectedMonoControl = new LCD_MONO_EorzeaTime();

                        break;
                    case 2:
                        //Server Time
                        _selectedMonoControl = new LCD_MONO_ServerTime();

                        break;
                    case 3:
                        //Local Time
                        _selectedMonoControl = new LCD_MONO_LocalTime();

                        break;
                    case 4:
                        //Server Latency
                        _selectedMonoControl = new LCD_MONO_Latency();

                        break;
                }
                
                _page = page;
                
            }
            catch (Exception e)
            {
                //
            }
        }

        private void SelectedMonoControlOnLcdMonoButton3Pressed(object sender, EventArgs eventArgs)
        {
            Console.WriteLine(@"Forward");

            _page++;
            if (_page > MaxPage) _page = 1;
            SwitchPages(_page);
        }

        private void SelectedMonoControlOnLcdMonoButton0Pressed(object o, EventArgs eventArgs)
        {
            Console.WriteLine(@"Back");

            _page--;
            if (_page <= 0) _page = MaxPage;
            SwitchPages(_page);
        }

        private void SelectedMonoControlOnLcdMonoButton1Pressed(object o, EventArgs eventArgs)
        {
            Console.WriteLine(@"Server Back");

            if (_page == 4)
            {
                var p = (LCD_MONO_Latency)_selectedMonoControl;
                p.CurrentServer--;

                if (p.CurrentServer <= 0)
                {
                    p.CurrentServer = p.MaxServer;
                }
            }
        }

        private void SelectedMonoControlOnLcdMonoButton2Pressed(object o, EventArgs eventArgs)
        {
            Console.WriteLine(@"Server Forward");

            if (_page == 4)
            {
                var p = (LCD_MONO_Latency)_selectedMonoControl;
                p.CurrentServer++;

                if (p.CurrentServer > p.MaxServer)
                {
                    p.CurrentServer = 1;
                }
            }
        }

        public void StatusLCDInfo(string text)
        {
            Logitech_LCD.LogitechLcd.Instance.MonoSetText(0, @"");
            Logitech_LCD.LogitechLcd.Instance.MonoSetText(1, @"");
            Logitech_LCD.LogitechLcd.Instance.MonoSetText(2, @"");
            Logitech_LCD.LogitechLcd.Instance.MonoSetText(3, @"");

            //Logitech_LCD.LogitechLcd.Instance.MonoSetText(1, text);

            //Mono
            if (_selectedMonoControl != null)
            {
                if (_selectedMonoControl.GetType().ToString() != "Chromatics.LCDInterfaces.LCD_MONO_Boot")
                {
                    _selectedMonoControl.IsActive = false;
                    _selectedMonoControl.Dispose();
                    _selectedMonoControl = null;

                    new Task(() =>
                    {
                        _selectedMonoControl = new LCD_MONO_Boot();
                        if (_selectedMonoControl is LCD_MONO_Boot m1) m1.SetBootText = text;
                    }).Start();
                }
            }
            else
            {
                new Task(() =>
                {
                    _selectedMonoControl = new LCD_MONO_Boot();
                    if (_selectedMonoControl is LCD_MONO_Boot m2) m2.SetBootText = text;
                }).Start();
            }

            if (_selectedMonoControl is LCD_MONO_Boot m) m.SetBootText = text;
        }

        private void CheckButtons(object sender, ElapsedEventArgs e)
        {
            if (_selectedMonoControl == null) return;

            if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorLeft))
            {
                LcdColorLeftButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorRight))
            {
                LcdColorRightButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorUp))
            {
                LcdColorUpButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorDown))
            {
                LcdColorDownButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorOk))
            {
                LcdColorOkButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorCancel))
            {
                LcdColorCancelButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.ColorMenu))
            {
                LcdColorMenuButtonPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.MonoButton0))
            {
                LcdMonoButton0Pressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.MonoButton1))
            {
                LcdMonoButton1Pressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.MonoButton2))
            {
                LcdMonoButton2Pressed?.Invoke(this, EventArgs.Empty);
            }
            else if (Logitech_LCD.LogitechLcd.Instance.IsButtonPressed(Buttons.MonoButton3))
            {
                LcdMonoButton3Pressed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}