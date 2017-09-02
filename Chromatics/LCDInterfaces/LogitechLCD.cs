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
using System.Windows.Forms;
using Chromatics.FFXIVInterfaces;
using Chromatics.Properties;
using GalaSoft.MvvmLight.Ioc;
using Sharlayan.Core;
using Sharlayan.Core.Enums;

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
        public const int LOGI_LCD_COLOR_BUTTON_LEFT = 0x00000100;
        public const int LOGI_LCD_COLOR_BUTTON_RIGHT = 0x00000200;
        public const int LOGI_LCD_COLOR_BUTTON_OK = 0x00000400;
        public const int LOGI_LCD_COLOR_BUTTON_CANCEL = 0x00000800;
        public const int LOGI_LCD_COLOR_BUTTON_UP = 0x00001000;
        public const int LOGI_LCD_COLOR_BUTTON_DOWN = 0x00002000;
        public const int LOGI_LCD_COLOR_BUTTON_MENU = 0x00004000;
        public const int LOGI_LCD_MONO_BUTTON_0 = 0x00000001;
        public const int LOGI_LCD_MONO_BUTTON_1 = 0x00000002;
        public const int LOGI_LCD_MONO_BUTTON_2 = 0x00000004;
        public const int LOGI_LCD_MONO_BUTTON_3 = 0x00000008;
        public const int LOGI_LCD_MONO_WIDTH = 160;
        public const int LOGI_LCD_MONO_HEIGHT = 43;
        public const int LOGI_LCD_COLOR_WIDTH = 320;
        public const int LOGI_LCD_COLOR_HEIGHT = 240;
        public const int LOGI_LCD_TYPE_MONO = 0x00000001;
        public const int LOGI_LCD_TYPE_COLOR = 0x00000002;

        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdInit(string friendlyName, int lcdType);

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdIsConnected(int lcdType);

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdIsButtonPressed(int button);

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiLcdUpdate();

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiLcdShutdown();

        //	Monochrome	LCD	functions
        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdMonoSetBackground(byte[] monoBitmap);

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdMonoSetText(int lineNumber, string text);

        //	Color	LCD	functions
        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdColorSetBackground(byte[] colorBitmap);

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdColorSetTitle(string text, int red, int green,
            int blue);

        [DllImport("LogitechLcdEnginesWrapper.dll", CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdColorSetText(int lineNumber, string text, int red,
            int green, int blue);
    }

    public interface ILogitechLcd
    {
        bool InitializeLcd();
        void ShutdownLcd();
        void DrawLCDInfo(ActorEntity _pI, ActorEntity _tI);
        void StatusLCDInfo(string text);
    }

    public class LCDEventArguments : EventArgs
    {
        public int Command { get; set; }
        public bool isColor { get; set; }
    }

    public class LogitechLcd : ILogitechLcd
    {
        private static ILogWrite _write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private bool _shutdown;
        private Thread EventThread;
        private event EventHandler<LCDEventArguments> OnLCDButtonRaise;

        private int page = 0;
        private Image buffer;

        #region Events

        private void OnLCDButtonRaiseEvent(LCDEventArguments e)
        {
            OnLCDButtonRaise?.Invoke(this, e);
        }

        private void EventManager()
        {
            try
            {
                while (!_shutdown)
                {
                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_CANCEL))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_CANCEL,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_DOWN))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_DOWN,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_LEFT))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_LEFT,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_MENU))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_MENU,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_OK))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_OK,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_RIGHT))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_RIGHT,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_UP))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_COLOR_BUTTON_UP,
                            isColor = true
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_0))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_0,
                            isColor = false
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_1))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_1,
                            isColor = false
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_2))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_2,
                            isColor = false
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    if (LogitechLcdWrapper.LogiLcdIsButtonPressed(LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_3))
                    {
                        LCDEventArguments args = new LCDEventArguments
                        {
                            Command = LogitechLcdWrapper.LOGI_LCD_MONO_BUTTON_3,
                            isColor = false
                        };
                        OnLCDButtonRaiseEvent(args);
                        Thread.Sleep(100);
                    }

                    //if (!_shutdown)
                        //LogitechLcdWrapper.LogiLcdUpdate();
                }
            }
            catch (Exception e)
            {
                //
            }
        }
        #endregion

        public bool InitializeLcd()
        {
            try
            {
                LogitechLcdWrapper.LogiLcdInit("Final Fantasy XIV", LogitechLcdWrapper.LOGI_LCD_TYPE_MONO | LogitechLcdWrapper.LOGI_LCD_TYPE_COLOR);
                LogitechLcdWrapper.LogiLcdColorSetTitle("Final Fantasy XIV", 255, 0, 0);
                LogitechLcdWrapper.LogiLcdMonoSetText(0, "Final Fantasy XIV");
                
                /*
                var pixelMatrix = new byte[LogitechLcdWrapper.LOGI_LCD_COLOR_WIDTH * LogitechLcdWrapper.LOGI_LCD_COLOR_HEIGHT * 4];
                
                LogitechLcdWrapper.LogiLcdColorSetBackground(pixelMatrix);
                */

                ThreadStart _EventThread = EventManager;
                EventThread = new Thread(_EventThread);
                EventThread.Start();
                
                OnLCDButtonRaise += dia_OnLCDButtonRaise;

                //Draw Color LCD
                var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
                var path = enviroment + @"/ffxiv_back1.bmp";
                var file = Image.FromFile(path);

                var img = file.ToByteArray(ImageFormat.Bmp);
                LogitechLcdWrapper.LogiLcdColorSetBackground(img);

                return true;
            }
            catch (Exception e)
            {
                _write.WriteConsole(ConsoleTypes.Error, e.StackTrace);
                _write.WriteConsole(ConsoleTypes.Logitech, "Unable to load LCD SDK.");
                return false;
            }
        }

        public void DrawLCDInfo(ActorEntity _pI, ActorEntity _tI)
        {
            //0 - Eorzea Time
            //1 - 
            //2 - 
            switch (page)
            {
                case 0:
                    //Eorzea Time
                    var _et = FFXIVHelpers.FetchEorzeaTime();
                    var eorzeatime = _et.ToString("hh:mm tt");
                    
                    LogitechLcdWrapper.LogiLcdMonoSetText(0, @"Clocks");
                    LogitechLcdWrapper.LogiLcdMonoSetText(1, @"ET: " + eorzeatime);
                    LogitechLcdWrapper.LogiLcdMonoSetText(2, @"ST: " + DateTime.UtcNow.ToString("hh:mm tt"));
                    LogitechLcdWrapper.LogiLcdMonoSetText(3, @"LT: " + DateTime.Now.ToString("hh:mm tt"));
                    

                    break;
                case 1:
                    break;
                case 2:
                    break;
            }

            if (!_shutdown)
                LogitechLcdWrapper.LogiLcdUpdate();
        }

        public void StatusLCDInfo(string text)
        {
            LogitechLcdWrapper.LogiLcdMonoSetText(1, text);
        }

        private void dia_OnLCDButtonRaise(object sender, LCDEventArguments e)
        {
            Console.WriteLine(e.Command);
        }

        public void ShutdownLcd()
        {
            LogitechLcdWrapper.LogiLcdShutdown();
            _shutdown = true;
        }
        
    }
}