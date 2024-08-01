using Microsoft.VisualBasic;
using Sharlayan;
using Sharlayan.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chromatics.Extensions.Sharlayan
{
    public class MusicExtension
    {
        public static DateTime LastUpdated = DateTime.MinValue;

        private static readonly string memoryName = @"MUSICBGM";
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);
        private static bool _siginit;
        private static bool _memoryready;
        private static ushort _musicResource;
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

                        _sList = new List<Signature>
                        {
                            new Signature //Not used
                            {
                                Key = memoryName,
                                Value = "488B05????????4885C074518378080B",
                                ASMSignature = true,
                                PointerPath = new List<long>
                                {   
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
                        var address = _memoryHandler.Scanner.Locations[memoryName].GetAddress() + 0xC0;
                        //var musicPointer = _memoryHandler.GetInt64(address, 0xC0); //Not used

                        Debug.WriteLine($"Music Resource: {address:X}");
                        //var currentSong = (ushort)0;
                        //var activePriority = 0;


                        if (address != IntPtr.Zero)
                        {
                            //
                        }
                        else
                        {
                            Debug.WriteLine("Address is null or invalid.");
                        }


                        //nint baseAddress = _memoryHandler.Configuration.ProcessModel.Process.MainModule.BaseAddress;
                        //var musicPointer = _memoryHandler.GetByte(baseAddress + 0x2550CA4);

                        _musicResource = 0;

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

        public static ushort musicResource()
        {
            if (!_initialized)
                return 0;

            CheckCache();

            return _musicResource;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct BGMScene
        {
            public int SceneIndex;
            public SceneFlags Flags;
            private int Padding1;
            // often writing songId will cause songId2 and 3 to be written automatically
            // songId3 is sometimes not updated at all, and I'm unsure of its use
            // zeroing out songId2 seems to be necessary to actually cancel playback without using
            // an invalid id (which is the only way to do it with just songId1)
            public ushort BgmReference;       // Reference to sheet; BGM, BGMSwitch, BGMSituation
            public ushort BgmId;              // Actual BGM that's playing. Game will manage this if it's a switch or situation
            public ushort PreviousBgmId;      // BGM that was playing before this one; I think it only changed if the previous BGM 
            public byte TimerEnable;            // whether the timer automatically counts up
            private byte Padding2;
            public float Timer;                 // if enabled, seems to always count from 0 to 6
                                                // if 0x30 is 0, up through 0x4F are 0
                                                // in theory function params can be written here if 0x30 is non-zero but I've never seen it
            private fixed byte DisableRestartList[24]; // 'vector' of bgm ids that will be restarted - managed by game. it is 3 pointers
            private byte Unknown1;
            private uint Unknown2;
            private uint Unknown3;
            private uint Unknown4;
            private uint Unknown5;
            private uint Unknown6;
            private ulong Unknown7;
            private uint Unknown8;
            private byte Unknown9;
            private byte Unknown10;
            private byte Unknown11;
            private byte Unknown12;
            private float Unknown13;
            private uint Unknown14;
        }

        [Flags]
        public enum SceneFlags : byte
        {
            None = 0,
            Unknown = 1,
            Resume = 2,
            EnablePassEnd = 4,
            ForceAutoReset = 8,
            EnableDisableRestart = 16,
            IgnoreBattle = 32,
        }
    }
}
