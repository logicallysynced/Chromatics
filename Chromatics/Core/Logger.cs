using Chromatics.Enums;
using Chromatics.Extensions;
using Chromatics.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Chromatics.Core
{
    public delegate void OnConsoleLoggedEventHandler(object source, OnConsoleLoggedEventArgs e);

    public static class Logger
    {
        public static event OnConsoleLoggedEventHandler OnConsoleLogged = delegate { };

        // Messages logged before the console subscriber attaches (e.g. the
        // .chromatics3 file migration in Program.Main) are buffered here and
        // replayed atomically by the first caller of Attach.
        private const int MaxBuffered = 500;
        private static readonly List<OnConsoleLoggedEventArgs> _buffer = new();
        private static readonly System.Threading.Lock _gate = new();

        public static void WriteConsole(LoggerTypes type, string message)
        {
            var color = (Color)EnumExtensions.GetAttribute<DefaultValueAttribute>(type).Value;
            var timestamp = DateTime.Now.ToString("MM-dd HH:mm:ss");
            var args = new OnConsoleLoggedEventArgs($"[{timestamp}] {message}", color);

            lock (_gate)
            {
                if (_buffer.Count >= MaxBuffered)
                    _buffer.RemoveAt(0);
                _buffer.Add(args);
                OnConsoleLogged(null, args);
            }
        }

        // Atomically replays any pre-subscription messages to the handler,
        // then subscribes the handler for future messages. Called by the
        // ConsoleViewModel constructor so messages logged during Program.Main
        // (before Avalonia boots) are not lost.
        public static void AttachSubscriberAndDrain(OnConsoleLoggedEventHandler handler)
        {
            lock (_gate)
            {
                foreach (var e in _buffer)
                    handler(null, e);
                _buffer.Clear();
                OnConsoleLogged += handler;
            }
        }
    }
}
