using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sharlayan;
using Sharlayan.Models;

namespace Chromatics.FFXIVInterfaces
{
    internal class FfxivMenu
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginitA;
        private static bool _siginitB;
        private static bool _memoryreadyA;
        private static bool _memoryreadyB;
        private static List<Signature> _sList;
        private static bool _inMainMenu;
        private static bool _inCharMenu;
        private static bool _inGame;
        private static bool _initialized;
        private static bool _contentInit;
        private static int _raw;
        private static int _Contentraw;

        private static readonly object RefreshLock = new object();

        private static readonly object CacheLock = new object();

        public static void RefreshData()
        {
            lock (RefreshLock)
            {
                if (!_memoryreadyA)
                {
                    if (!Scanner.Instance.Locations.ContainsKey("MENUTRACK") || !_siginitA)
                    {


                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "MENUTRACK",
                                PointerPath = new List<long>
                                {
                                    0x18F3518,
                                    0xD8,
                                    0x88,
                                    0x1D0,
                                    0x468,
                                    0xA4
                                }
                            }
                        };

                        /*
                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "MENUTRACK",
                                Value = "4883fb**7c**e9********488b0d",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {
                                    0x0,
                                    0x0,
                                    0xD8,
                                    0x28,
                                    0x128,
                                    0x38,
                                    0xA4
                                }
                            }
                        };
                        */

                        Scanner.Instance.LoadOffsets(_sList);

                        Thread.Sleep(100);

                        if (Scanner.Instance.Locations.ContainsKey("MENUTRACK"))
                        {
                            Debug.WriteLine("Initializing MENUTRACK done: " +
                                            Scanner.Instance.Locations["MENUTRACK"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginitA = true;
                        }

                        if (_siginitA)
                            _memoryreadyA = true;
                    }

                    if (!Scanner.Instance.Locations.ContainsKey("CONTENTFINDER") || !_siginitB)
                    {
                        _sList = new List<Signature>
                        {
                            new Signature
                            {
                                Key = "CONTENTFINDER",
                                Value = "75**33d2488d0d********e8********48393d",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {
                                    0x0,
                                    0x0
                                }
                            }
                        };

                        /*
                        _sList.Add(new Signature
                        {
                            Key = "WEATHER",
                            PointerPath = new List<long>
                            {
                                0x018FE408,
                                0x38,
                                0x18,
                                0x190,
                                0x20,
                                0x0
                            }
                        });
                        */

                        Scanner.Instance.LoadOffsets(_sList);

                        Thread.Sleep(100);

                        if (Scanner.Instance.Locations.ContainsKey("CONTENTFINDER"))
                        {
                            Debug.WriteLine("Initializing CONTENTFINDER done: " +
                                            Scanner.Instance.Locations["CONTENTFINDER"].GetAddress().ToInt64()
                                                .ToString("X"));

                            _siginitB = true;
                        }

                        if (_siginitB)
                            _memoryreadyB = true;
                    }
                }

                if (_memoryreadyA)
                {
                    if (Scanner.Instance.Locations.ContainsKey("MENUTRACK"))
                    {
                        var address = Scanner.Instance.Locations["MENUTRACK"];

                        //PluginController.debug(" " + address.ToString("X8"));
                        var MenuState = MemoryHandler.Instance.GetByte(address.GetAddress(), 0x0);
                        //var instanceLock = MemoryHandler.Instance.GetByte(address.GetAddress(), 7);
                        //_isPopped = isPopped == 2;

                        _raw = MenuState;
                        _inMainMenu = MenuState == 1;
                        _inCharMenu = MenuState == 2;
                        _inGame = MenuState > 2;

                        //_countdown = MemoryHandler.Instance.GetInt32(address.GetAddress(), 4);
                        _initialized = true;
                        //Console.WriteLine(CutsceneState);
                    }


                    LastUpdated = DateTime.Now;
                }

                if (_memoryreadyB)
                {
                    if (Scanner.Instance.Locations.ContainsKey("CONTENTFINDER"))
                    {
                        var address = Scanner.Instance.Locations["CONTENTFINDER"];

                        var MenuState = MemoryHandler.Instance.GetByte(address.GetAddress(), 0xC);

                        _Contentraw = MenuState;

                        _contentInit = true;
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
                    RefreshData();
            }
        }

        public static int GetContentID()
        {
            if (!_contentInit)
                return 0;

            CheckCache();

            return _Contentraw;
        }

        public static bool InInstance()
        {
            if (!_contentInit)
                return false;

            CheckCache();

            return _Contentraw != 0;
        }

        public static int GetState()
        {
            if (!_initialized)
                return 0;

            CheckCache();

            return _raw;
        }

        public static bool InMainMenu()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _inMainMenu;
        }

        public static bool InCharacterSelect()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _inCharMenu;
        }

        public static bool InGame()
        {
            if (!_initialized)
                return false;

            CheckCache();

            return _inGame;
        }
    }
}