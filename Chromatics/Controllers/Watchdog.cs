using GalaSoft.MvvmLight.Ioc;
using System.Timers;

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
        private static Timer Timer;
        private static ILogWrite write = SimpleIoc.Default.GetInstance<ILogWrite>();

        public static void WatchdogGo()
        {
            write.WriteConsole(ConsoleTypes.SYSTEM, "Watchdog Started");
            Timer.Start();
        }

        public static void WatchdogStop()
        {
            if (Timer.Enabled)
            {
                write.WriteConsole(ConsoleTypes.SYSTEM, "Watchdog Stopped");
                Timer.Stop();
            }
        }

        public static void WatchdogReset()
        {
            if (Timer.Enabled)
            {
                Timer.Stop();
                Timer.Start();
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
            write.WriteConsole(ConsoleTypes.ERROR, "Watchdog Triggered");
            Timer.Stop();
            //RestartServices();
            write.FFXIVGameStop();

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
            Timer = new Timer();
            Timer.Elapsed += (source, e) => { WatchdogOnTimerExpired(); };
            Timer.AutoReset = false;
            Timer.Interval = 6000;
        }

        private delegate void ResetDelegate();

        private delegate void ExpireDelegate();
    }
}