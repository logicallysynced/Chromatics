using Microsoft.VisualBasic;
using Sharlayan;
using Sharlayan.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.Sharlayan
{
    public class GameStateExtension
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly string memoryName = @"GAMESTATE";
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static bool _isLoggedIn;
        private static bool _isReadReady;
        private static bool _initialized;
        private static List<Signature> _sList;
        private static readonly object RefreshLock = new object();
        private static readonly object CacheLock = new object();
        private static MemoryHandler _memoryHandler;

        public static void RefreshData(MemoryHandler memoryHandler)
        {
            lock (RefreshLock)
            {
                _memoryHandler = memoryHandler;

                if (!_memoryready)
                {
                    if (!_memoryHandler.Scanner.Locations.ContainsKey(memoryName) || !_siginit)
                    {
                        Debug.WriteLine("Trying to find Game State");

                        _sList = new List<Signature>
                        {
                            ////https://github.com/aers/FFXIVClientStructs/blob/edc749986000d056169791916fa5462a3dff3d53/FFXIVClientStructs/FFXIV/Client/Game/GameMain.cs#L18
                            new Signature
                            {
                                Key = memoryName,
                                Value = "488d0d********3805",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {
                                    0,
                                    0
                                }
                            }
                        };

                        _memoryHandler.Scanner.LoadOffsets(_sList.ToArray());

                        Thread.Sleep(100);

                        if (_memoryHandler.Scanner.Locations.ContainsKey(memoryName))
                        {
                            #if DEBUG
                                Debug.WriteLine($"Found {memoryName}. Location: {_memoryHandler.Scanner.Locations[memoryName].GetAddress().ToInt64():X}");
                            #endif

                            _siginit = true;
                        }

                        if (_siginit)
                            _memoryready = true;
                    }
                }
                else
                {
                    if (_memoryHandler.Scanner.Locations.ContainsKey(memoryName))
                    {
                        var address = _memoryHandler.Scanner.Locations[memoryName];
                        var gameLoginState = _memoryHandler.GetByte(address.GetAddress(), 0x32);
                        var gameReadState = _memoryHandler.GetByte(address.GetAddress(), 0x1);
                        _isLoggedIn = gameLoginState > 0;
                        _isReadReady = gameReadState == 1;

                        _initialized = true;
                    }


                    LastUpdated = DateTime.Now;
                }
            }
        }

        public static void CheckCache()
        {
            lock (CacheLock)
            {
                if (LastUpdated + UpdateInterval <= DateTime.Now)
                    RefreshData(_memoryHandler);
            }
        }

        public static bool IsLoggedIn()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _isLoggedIn;
        }

        public static bool IsReadReady()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _isReadReady;
        }
    }
}
