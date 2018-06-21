using System.Timers;
using GalaSoft.MvvmLight.Ioc;

/* Contains debug code to recover Sharlayan and Colore in case of memory leak or overlap.
 * Uses a timer started as a seperate task which is reset every loop of the MemoryReader loop and if the timer
 * is allowed to elapse (by missing a number of resets) it calls a Reset function in the main thread.
 * Currently really buggy.
 */

namespace Chromatics
{
    public static class Watchdog
    {
        //private Timer Timer;
        private static Timer _timer;

        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static void WatchdogGo()
        {
            Write.WriteConsole(ConsoleTypes.System, @"Watchdog Started");
            _timer.Start();
        }

        public static void WatchdogStop()
        {
            if (_timer.Enabled)
            {
                Write.WriteConsole(ConsoleTypes.System, @"Watchdog Stopped");
                _timer.Stop();
            }
        }

        public static void WatchdogReset()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
                _timer.Start();
            }

            /*
            if (InvokeRequired)
            {
                ResetDelegate del = WatchdogReset;
                Invoke(del);
            }
            else
            {
                if (Timer.Enabled)
                {
                    Timer.Stop();
                    Timer.Start();
                }
            }
            */
        }

        private static void WatchdogOnTimerExpired()
        {
            Write.WriteConsole(ConsoleTypes.Error, @"Watchdog Triggered");
            _timer.Stop();
            //RestartServices();
            Write.FfxivGameStop();

            /*
            if (InvokeRequired)
            {
                ExpireDelegate del = WatchdogOnTimerExpired;
                Invoke(del);
            }
            else
            {
                write.WriteConsole(ConsoleTypes.ERROR, "Watchdog Triggered");
                Timer.Stop();
                //RestartServices();
                write.FFXIVGameStop();
            }
            */
        }

        public static void WatchdogStartup()
        {
            _timer = new Timer();
            _timer.Elapsed += (source, e) => { WatchdogOnTimerExpired(); };
            _timer.AutoReset = false;
            _timer.Interval = 6000;
        }

        private delegate void ResetDelegate();

        private delegate void ExpireDelegate();
    }
}