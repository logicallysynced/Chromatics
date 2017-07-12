using System;
using System.Collections.Generic;

using System.Runtime.InteropServices;
using System.Threading;
using Sharlayan.Helpers;
using Sharlayan.Core.Enums;
using Sharlayan.Core;
using Sharlayan.Models;
using System.Diagnostics;

namespace Chromatics.FFXIVInterfaces
{
    public class Cooldowns
    {
        private static CooldownRawData _rawData;
        public static DateTime lastUpdated = DateTime.MinValue;
        private static TimeSpan updateInterval = TimeSpan.FromSeconds(0.05);

        public static Byte[] _rawResourceData;

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct ClassResourceRawData
        {
            // Dragoon: Blood of the dragon timer (1000 = 1 second)
            // Ninja: Huton timer
            // Bard: Song remaining time
            // Warrior: Wrath
            // MCH: Overheat timer (countdown from 10 seconds, 1000 = 1 second). Also the timer for when Gauss Barrel can be turned back on after overheat.
            // Monk: Greased Lightning timer
            // AST: Card timer
            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(0x6)] // B0
            public short resource1;

            // Ninja: 0 most of the time. 1 briefly after casting huton
            // MCH: Heat Gauge (stays at 100 when overheated)
            // BLM: Combine resource2 and resource3 into an astral fire timer (ugly)
            // Monk: Greased Lightning Stacks
            // Bard: Song Repertoire stacks
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(0x8)] // B2
            public byte resource2;

            // Bard: 15 = Wanderer's Minuet, 10 = Army's Paeon, 5 = Mage's Ballad, Anything else = nothing. May be a bit mask.
            // MCH: Ammo count
            // BLM: Combine resource2 and resource3 into an astral fire timer (ugly)
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(0x9)] // B3
            public byte resource3;

            // MCH: Gauss barrel (bool)
            // BLM: 255 = Umbral Ice 1; 254 = Umbral Ice 2; 253 = Umbral Ice 3; 1 = Astral Fire 1; 2 = Astral Fire 2; 3 = Astral Fire 3. If converted to signed, umbral 1/2/3 would be -1/-2/-3 respectively.
            // SMN: Aetherflow stacks and Aethertrail stacks. Bitmask - low bits (1 and 2) represent aetherflow, bits 3 and 4 represent aethertrail stacks
            // SCH: Aetherflow stacks
            // AST: Split in half, low bits contain current card, high bits contain held card. (1 = Balance, 2 = Bole, 3 = Arrow, 4 = Spear, 5 = Ewer, 6 = Spire). 
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(0x10)] // B4
            public byte resource4;


            // BLM: Umbral Heart count
            // AST: Royal Road effect (16 = Enhanced, 32 = Extended, 48 = spread). Looks like it's the top 4 bits. Not sure if the lower 4 bits are used for anything yet.
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(0x11)] // B5
            public byte resource5;


            // BLM: Enochian active (bool)
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(0x12)] // B6
            public byte resource6;


            // Ninja: total number of times Huton has been used or refreshed
            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(0x13)] // B7
            public short resource7;


        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct CooldownRawData
        {
            // 01CECC84
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x0)] // 
            public float afterSkillLockTime;

            [MarshalAs(UnmanagedType.Bool)]
            [FieldOffset(0x20)]
            public bool currentlyCasting;

            // 01CECCB0
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x28)] // 
            public float castTimeElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2C)] // 
            public float castTimeTotal;



            // 01CECCE0
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x58)] // 
            public float comboTimeRemaining;



            // 01DF98A4
            // 01DF99A0
            [MarshalAs(UnmanagedType.U4)]
            [FieldOffset(0x118)] // 
            public int actionCount;



            // 
            //  (99)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x13C)]
            public float cooldownType99Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x140)]
            public float cooldownType99Total;



            // 
            // Painflare (0)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x150)]
            public float cooldownType0Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x154)]
            public float cooldownType0Total;


            // 
            // EnergyDrain (1)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x164)] // 0x128
            public float cooldownType1Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x168)] // 0x12C
            public float cooldownType1Total;


            // 
            // Bane (2)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x178)] // 0x150
            public float cooldownType2Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x17C)] // 0x154
            public float cooldownType2Total;


            // 
            // Fester (3)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x18C)] // 0x18C
            public float cooldownType3Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x190)] // 0x190
            public float cooldownType3Total;



            // 
            // Tri-Disaster (4)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1A0)] // 0x204
            public float cooldownType4Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1A4)] // 0x208
            public float cooldownType4Total;




            // 
            // Aetherflow (5)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1B4)] // 0x114
            public float cooldownType5Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1B8)] // 0x118
            public float cooldownType5Total;




            // 
            // Virus (6)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1C8)] // 0x13C
            public float cooldownType6Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1CC)] // 0x140
            public float cooldownType6Total;


            // 
            // Rouse (7)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1DC)] // 0x178
            public float cooldownType7Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1E0)] // 0x17C
            public float cooldownType7Total;


            // 
            // Spur (8)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1F0)] // 0x1B4
            public float cooldownType8Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1F4)] // 0x1B8
            public float cooldownType8Total;



            // 
            // Kassatsu (9)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x204)] // 0x1DC
            public float cooldownType9Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x208)] // 0x1E0
            public float cooldownType9Total;




            // 
            // Eye for an Eye (10)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x218)] // 0x164
            public float cooldownType10Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x21C)] // 0x168
            public float cooldownType10Total;


            // 
            // Bulwark (11)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x22C)] // 0x1A0
            public float cooldownType11Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x230)] // 0x1A4
            public float cooldownType11Total;



            // 02138AC8
            // Enkindle (12)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x240)] // 0x1C8
            public float cooldownType12Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x244)] // 0x1CC
            public float cooldownType12Total;




            // 
            // Hallowed Ground (13)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x254)] // 0x1F0
            public float cooldownType13Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x258)] // 0x1F4
            public float cooldownType13Total;


            // 02138AF0
            // Dreadwyrm Trance (14)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x268)] // 0x1F0
            public float cooldownType14Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x26C)] // 0x1F4
            public float cooldownType14Total;



            // 02138AF0
            // Deathflare (15)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x27C)] // 0x1F0
            public float cooldownType15Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x280)] // 0x1F4
            public float cooldownType15Total;


            // 02138AF0
            //  (16)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x290)] // 0x1F0
            public float cooldownType16Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x294)] // 0x1F4
            public float cooldownType16Total;


            // 02138AF0
            //  (17)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2A4)] // 0x1F0
            public float cooldownType17Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2A8)] // 0x1F4
            public float cooldownType17Total;


            // 02138AF0
            //  (18)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2B8)] // 0x1F0
            public float cooldownType18Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2BC)] // 0x1F4
            public float cooldownType18Total;


            //  (19)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2CC)] // 0x1F0
            public float cooldownType19Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2D0)] // 0x1F4
            public float cooldownType19Total;


            //  (20)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2E0)] // 0x1F0
            public float cooldownType20Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2E4)] // 0x1F4
            public float cooldownType20Total;








            // 022FA638
            // 10FA638
            // 01CED0DC
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x470)] // 0x434
            public float cooldownCrossClassSlot1Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x474)] // 0x438
            public float cooldownCrossClassSlot1Total;


            // 01CED0F0
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x484)] // 0x448
            public float cooldownCrossClassSlot2Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x488)] // 0x44C
            public float cooldownCrossClassSlot2Total;




            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x498)] // 02140B30
            public float cooldownCrossClassSlot3Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x49C)] // 02140B34
            public float cooldownCrossClassSlot3Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4AC)] // 02140B44
            public float cooldownCrossClassSlot4Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4B0)] // 02140B48
            public float cooldownCrossClassSlot4Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4C0)] // 02140B58
            public float cooldownCrossClassSlot5Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4C4)] // 02140B5C
            public float cooldownCrossClassSlot5Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4D4)] // 
            public float cooldownCrossClassSlot6Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4D8)] // 
            public float cooldownCrossClassSlot6Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4E8)] // 
            public float cooldownCrossClassSlot7Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4EC)] // 
            public float cooldownCrossClassSlot7Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4FC)] // 
            public float cooldownCrossClassSlot8Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x500)] // 
            public float cooldownCrossClassSlot8Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x510)] // 
            public float cooldownCrossClassSlot9Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x514)] // 
            public float cooldownCrossClassSlot9Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x524)] // 
            public float cooldownCrossClassSlot10Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x528)] // 
            public float cooldownCrossClassSlot10Total;



            // 01CED208
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x59C)] // 0x560
            public float cooldownSprintElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5A0)] // 0x564
            public float cooldownSprintTotal;



            // 01CED230
            [MarshalAs(UnmanagedType.Bool)]
            [FieldOffset(0x5BC)] // 0x588
            public bool globalCooldownInUse;



            // 01CED230
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5C4)] // 0x588
            public float globalCooldownElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5C8)] // 0x58C
            public float globalCooldownTotal;



            // 01CED244
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5D8)] // 02140C70
            public float cooldownPotionElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5DC)] // 02140C74
            public float cooldownPotionTotal;



            // 01CED244
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5EC)] // 02140C70
            public float cooldownPoisonPotionElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5F0)] // 02140C74
            public float cooldownPoisonPotionTotal;




            // 01CED3AC
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x740)] // 0x704
            public float cooldownPetAbility1Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x744)] // 0x708
            public float cooldownPetAbility1Total;


            // 01CED3C0
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x754)] // 0x718
            public float cooldownPetAbility2Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x758)] // 0x71C
            public float cooldownPetAbility2Total;


            // 01CED3D4
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x768)] // 0x72C
            public float cooldownPetAbility3Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x76C)] // 0x730
            public float cooldownPetAbility3Total;


            // 01CED3E8
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x77C)] // 0x740
            public float cooldownPetAbility4Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x780)] // 0x744
            public float cooldownPetAbility4Total;



        }


        public static bool initialized = false;
        public static bool cooldownsInitialized = false;
        public static bool resourcesInitialized = false;
        public static bool initializedActor = false;
        static List<Signature> sList;
        static object refreshLock = new object();

        private static IntPtr characterAddress = IntPtr.Zero;


        public static void refreshData()
        {
            lock (refreshLock)
            {
                try
                {

                    if (!initialized)
                    {
                        if (!Sharlayan.Scanner.Instance.Locations.ContainsKey("COOLDOWNS") || !cooldownsInitialized)
                        {
                            //PluginController.debug("Initializing cooldowns...");

                            sList = new List<Signature>();

                            // 021386E4
                            // 021388A4


                            /*
                            // doesn't seem to exist anymore
                            sList.Add(new Signature
                            {
                                Key = "AUTO_ATTACK_COUNT",
                                PointerPath = new List<long>()
                                {
                                    0x00F1BCB0,
                                    0X30,
                                    0X6c4,
                                    0X310
                                }
                            });
                            */

                            // 022FA1D4
                            // 01200000


                            // 01360ED4
                            // 001F0000

                            sList.Add(new Signature
                            {
                                Key = "COOLDOWNS",
                                PointerPath = new List<long>()
                                {
                                    //0x10FA1D4
                                    //0x10FB1D4
                                    //0x10FE2B4
                                    //0x1170ED4
                                    //0x1171ED4
                                    //0x118EB24
                                    //0x118FB24
                                    //0x11CCF84
                                    //0x11CCEF4
                                    //0x11CDEF4
                                    //0x11CDF84
                                    //0x11CEF84
                                    0x173F518
                                }
                            });
                            Sharlayan.Scanner.Instance.LoadOffsets(sList);

                            Thread.Sleep(100);

                            if (Sharlayan.Scanner.Instance.Locations.ContainsKey("COOLDOWNS"))
                            {
                                Debug.WriteLine("Initializing COOLDOWNS done: " + Sharlayan.Scanner.Instance.Locations["COOLDOWNS"].GetAddress().ToInt64().ToString("X"));

                                cooldownsInitialized = true;
                            }
                            else
                            {
                                //PluginController.debug("Couldn't locate cooldowns...");
                            }
                        }



                        if (!Sharlayan.Scanner.Instance.Locations.ContainsKey("CLASSRESOURCES") || !resourcesInitialized)
                        {
                            //PluginController.debug("Initializing cooldowns...");

                            sList = new List<Signature>();

                            sList.Add(new Signature
                            {
                                Key = "CLASSRESOURCES",
                                PointerPath = new List<long>()
                                {
                                    0x178ADAA
                                }
                            });
                            Sharlayan.Scanner.Instance.LoadOffsets(sList);

                            Thread.Sleep(100);

                            if (Sharlayan.Scanner.Instance.Locations.ContainsKey("CLASSRESOURCES"))
                            {
                                Debug.WriteLine("Initializing CLASSRESOURCES done: " + Sharlayan.Scanner.Instance.Locations["CLASSRESOURCES"].GetAddress().ToInt64().ToString("X"));

                                resourcesInitialized = true;
                            }
                        }

                        if (cooldownsInitialized && resourcesInitialized)
                        {
                            initialized = true;
                        }
                    }

                    /*
                    if (!initializedActor)
                    {
                        if (Sharlayan.Scanner.Instance.Locations.ContainsKey("CHARMAP"))
                        {
                            characterAddress =
                                Sharlayan.MemoryHandler.Instance.ReadPointer(Sharlayan.Scanner.Instance.Locations["CHARMAP"].GetAddress());

                            if (characterAddress != IntPtr.Zero)
                            {
                                PluginController.debug("Initializing actor done.");
                                initializedActor = true;
                            }
                        }
                    }
                    */

                    if (initialized)
                    {
                        if (Sharlayan.Scanner.Instance.Locations.ContainsKey("COOLDOWNS"))
                        {
                            Signature address = Sharlayan.Scanner.Instance.Locations["COOLDOWNS"];

                            //PluginController.debug(" " + address.ToString("X8"));
                            _rawData = Sharlayan.MemoryHandler.Instance.GetStructure<CooldownRawData>(address.GetAddress());
                        }
                        //cachedAutoAttackCount = Sharlayan.MemoryHandler.Instance.GetInt16(Sharlayan.Scanner.Locations["AUTO_ATTACK_COUNT"].GetAddress().ToInt64());

                        if (Sharlayan.Scanner.Instance.Locations.ContainsKey("CLASSRESOURCES"))
                        {
                            Signature address = Sharlayan.Scanner.Instance.Locations["CLASSRESOURCES"];

                            //PluginController.debug(" " + address.ToString("X8"));
                            _rawResourceData = Sharlayan.MemoryHandler.Instance.GetByteArray(address.GetAddress(), 20);
                        }


                        lastUpdated = DateTime.Now;


                    }

                }
                catch
                {
                    initialized = false;
                }
            }
        }

        private static object cacheLock = new object();
        public static void checkCache()
        {
            lock (cacheLock)
            {
                if (lastUpdated + updateInterval <= DateTime.Now)
                {
                    refreshData();
                }
                /*
                if (initializedActor)
                {
                    GetRecentAction();
                }
                */
            }
        }

        public static float getTimer(int i)
        {
            if (!initialized)
                return 0;

            checkCache();

            return Math.Max(BitConverter.ToUInt16(_rawResourceData, i) / 1000f - currentTimeshift, 0);
        }

        public static byte getRaw(int i)
        {
            if (!initialized)
                return 0;

            checkCache();

            return _rawResourceData[i];
        }


        private static float m_currentTimeshift = 0;
        public static float currentTimeshift
        {
            get
            {
                return m_currentTimeshift;
            }
            set
            {
                m_currentTimeshift = value;
            }
        }



        /**/
        // the autoAttackCount data no longer seems to exist in memory...
        //private static int cachedAutoAttackCount = 0;
        private static int cachedActionCount = 0;
        public static int actionCount
        {
            get
            {
                if (!initialized)
                    return 0;

                checkCache();

                if (_rawData.actionCount != 0)
                {
                    cachedActionCount = _rawData.actionCount;
                }

                return cachedActionCount; // -cachedAutoAttackCount;
            }
        }


        /** /
        public static bool anyActionProcessing 
        {
            get
            {
                if (!initialized)
                    return 0;

                checkCache();
                if (_rawData.actionCount == 0)
                {
                    return false;
                }
                if (rawAction == 0 || rawAction == 7)
                {
                    return false;
                }

                return true;
            }
        }

        public static void GetRecentAction()
        {
            // base: 00F50000
            // actor base: 13C00030
            // skill: 13C00588
            // offset: 558

            try
            {
                rawAction = Sharlayan.MemoryHandler.Instance.GetInt32(characterAddress, 0x558);
            }
            catch (Exception e)
            {
                PluginController.debug("GetRecentAction error while reading memory: " + e.Message);
            }

            if (rawAction != 0 && rawAction != 7)
            {
                cachedRecentAction = rawAction;
            }
        }


        private static int rawAction = 0;
        private static int cachedRecentAction = 0;
        public static int mostRecentAction
        {
            get
            {
                GetRecentAction();

                return cachedRecentAction;
            }
        }

        /**/





        private static float previousAfterSkillLockTime = 0;
        private static DateTime previousAfterSkillLockTimeTimestamp = DateTime.MinValue;

        public static float afterSkillLockTime
        {
            get
            {
                if (!initialized)
                    return 0;

                checkCache();
                if (_rawData.afterSkillLockTime > 10 || _rawData.castTimeTotal < 0)
                {
                    initialized = false;
                    refreshData();
                }
                float newVal = _rawData.afterSkillLockTime;

                if (newVal != 0)
                {
                    if (previousAfterSkillLockTimeTimestamp + TimeSpan.FromSeconds(0.1) > DateTime.Now)
                    {
                        if (previousAfterSkillLockTime == newVal)
                        {
                            initialized = false;
                            refreshData();
                        }

                        previousAfterSkillLockTime = newVal;
                        previousAfterSkillLockTimeTimestamp = DateTime.Now;
                    }
                }

                newVal -= currentTimeshift;

                if (newVal < 0)
                {
                    newVal = 0;
                }

                return newVal;
            }
        }

        public static bool currentlyCasting
        {
            get
            {
                if (!initialized)
                    return false;
                checkCache();

                if (currentTimeshift > 0 && castTimeElapsed <= 0)
                {
                    return false;
                }

                return _rawData.currentlyCasting;
            }
        }

        public static float castTimeElapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                if (_rawData.castTimeTotal - _rawData.castTimeElapsed < currentTimeshift)
                {
                    return 0;
                }

                return _rawData.castTimeElapsed + currentTimeshift;
            }
        }
        public static float castTimeTotal
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                if (_rawData.castTimeTotal - _rawData.castTimeElapsed <= currentTimeshift)
                {
                    return 0;
                }

                return _rawData.castTimeTotal;
            }
        }

        private static float previousCastTimeRemaining = 0;
        private static DateTime previousCastTimeRemainingTimestamp = DateTime.MinValue;
        public static float castTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;

                checkCache();


                if (predictCastUntil > DateTime.Now)
                {

                    if ((float)(predictCastDone - DateTime.Now).TotalSeconds < currentTimeshift)
                    {
                        return 0;
                    }

                    return (float)(predictCastDone - DateTime.Now).TotalSeconds;
                }


                if (_rawData.castTimeElapsed <= 0)
                {
                    return 0;
                }
                if (_rawData.castTimeTotal > 10 || _rawData.castTimeTotal < 0 || _rawData.castTimeElapsed > 10 || _rawData.castTimeElapsed < 0)
                {
                    initialized = false;
                    refreshData();
                }
                float newVal = _rawData.castTimeTotal - _rawData.castTimeElapsed;

                if (newVal > 0)
                {
                    if (previousCastTimeRemainingTimestamp + TimeSpan.FromSeconds(0.1) > DateTime.Now)
                    {
                        if (previousCastTimeRemaining == newVal)
                        {
                            initialized = false;
                            refreshData();
                        }

                        previousCastTimeRemaining = newVal;
                        previousCastTimeRemainingTimestamp = DateTime.Now;
                    }
                }

                return (float)Math.Max(newVal - currentTimeshift, 0);
            }
        }


        public static float comboTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return (float)Math.Max(_rawData.comboTimeRemaining - currentTimeshift, 0);
            }
        }



        public static float cooldownType99Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawData.cooldownType99Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType99Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType99Total;
            }
        }
        public static float cooldownType99Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType99Total - cooldownType99Elapsed, 0);
            }
        }



        public static float cooldownType0Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType0Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType0Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType0Total;
            }
        }
        public static float cooldownType0Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType0Total - cooldownType0Elapsed, 0);
            }
        }



        public static float cooldownType1Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType1Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType1Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType1Total;
            }
        }
        public static float cooldownType1Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType1Total - cooldownType1Elapsed, 0);
            }
        }



        public static float cooldownType2Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType2Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType2Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType2Total;
            }
        }
        public static float cooldownType2Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType2Total - cooldownType2Elapsed, 0);
            }
        }


        public static float cooldownType3Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType3Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType3Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType3Total;
            }
        }
        public static float cooldownType3Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType3Total - cooldownType3Elapsed, 0);
            }
        }


        public static float cooldownType4Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType4Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType4Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType4Total;
            }
        }
        public static float cooldownType4Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType4Total - cooldownType4Elapsed, 0);
            }
        }




        public static float cooldownType5Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType5Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType5Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType5Total;
            }
        }
        public static float cooldownType5Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType5Total - cooldownType5Elapsed, 0);
            }
        }


        public static float cooldownType6Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType6Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType6Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType6Total;
            }
        }
        public static float cooldownType6Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType6Total - cooldownType6Elapsed, 0);
            }
        }



        public static float cooldownType7Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType7Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType7Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType7Total;
            }
        }
        public static float cooldownType7Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType7Total - cooldownType7Elapsed, 0);
            }
        }



        public static float cooldownType8Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType8Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType8Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType8Total;
            }
        }
        public static float cooldownType8Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType8Total - cooldownType8Elapsed, 0);
            }
        }


        public static float cooldownType9Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType9Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType9Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType9Total;
            }
        }
        public static float cooldownType9Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType9Total - cooldownType9Elapsed, 0);
            }
        }




        public static float cooldownType10Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType10Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType10Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType10Total;
            }
        }
        public static float cooldownType10Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType10Total - cooldownType10Elapsed, 0);
            }
        }





        public static float cooldownType11Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType11Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType11Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType11Total;
            }
        }
        public static float cooldownType11Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType11Total - cooldownType11Elapsed, 0);
            }
        }



        public static float cooldownType12Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType12Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType12Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType12Total;
            }
        }
        public static float cooldownType12Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType12Total - cooldownType12Elapsed, 0);
            }
        }



        public static float cooldownType13Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType13Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType13Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType13Total;
            }
        }
        public static float cooldownType13Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType13Total - cooldownType13Elapsed, 0);
            }
        }



        public static float cooldownType14Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType14Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType14Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType14Total;
            }
        }
        public static float cooldownType14Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType14Total - cooldownType14Elapsed, 0);
            }
        }


        public static float cooldownType15Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType15Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType15Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType15Total;
            }
        }
        public static float cooldownType15Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType15Total - cooldownType15Elapsed, 0);
            }
        }





        public static float cooldownType16Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType16Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType16Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType16Total;
            }
        }
        public static float cooldownType16Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType16Total - cooldownType16Elapsed, 0);
            }
        }




        public static float cooldownType17Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType17Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType17Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType17Total;
            }
        }
        public static float cooldownType17Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType17Total - cooldownType17Elapsed, 0);
            }
        }








        public static float cooldownType18Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType18Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType18Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType18Total;
            }
        }
        public static float cooldownType18Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType18Total - cooldownType18Elapsed, 0);
            }
        }






        public static float cooldownType19Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType19Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType19Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType19Total;
            }
        }
        public static float cooldownType19Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType19Total - cooldownType19Elapsed, 0);
            }
        }



        public static float cooldownType20Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType20Elapsed + currentTimeshift;
            }
        }
        public static float cooldownType20Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownType20Total;
            }
        }
        public static float cooldownType20Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownType20Total - cooldownType20Elapsed, 0);
            }
        }







        public static float cooldownCrossClassSlot1Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot1Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot1Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot1Total;
            }
        }
        public static float cooldownCrossClassSlot1Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot1Total - cooldownCrossClassSlot1Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot2Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot2Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot2Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot2Total;
            }
        }
        public static float cooldownCrossClassSlot2Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot2Total - cooldownCrossClassSlot2Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot3Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot3Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot3Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot3Total;
            }
        }
        public static float cooldownCrossClassSlot3Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot3Total - cooldownCrossClassSlot3Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot4Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot4Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot4Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot4Total;
            }
        }
        public static float cooldownCrossClassSlot4Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot4Total - cooldownCrossClassSlot4Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot5Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot5Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot5Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot5Total;
            }
        }
        public static float cooldownCrossClassSlot5Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot5Total - cooldownCrossClassSlot5Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot6Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot6Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot6Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot6Total;
            }
        }
        public static float cooldownCrossClassSlot6Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot6Total - cooldownCrossClassSlot6Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot7Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot7Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot7Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot7Total;
            }
        }
        public static float cooldownCrossClassSlot7Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot7Total - cooldownCrossClassSlot7Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot8Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot8Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot8Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot8Total;
            }
        }
        public static float cooldownCrossClassSlot8Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot8Total - cooldownCrossClassSlot8Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot9Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot9Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot9Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot9Total;
            }
        }
        public static float cooldownCrossClassSlot9Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot9Total - cooldownCrossClassSlot9Elapsed, 0);
            }
        }



        public static float cooldownCrossClassSlot10Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot10Elapsed + currentTimeshift;
            }
        }
        public static float cooldownCrossClassSlot10Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownCrossClassSlot10Total;
            }
        }
        public static float cooldownCrossClassSlot10Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownCrossClassSlot10Total - cooldownCrossClassSlot10Elapsed, 0);
            }
        }




        public static float cooldownSprintElapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownSprintElapsed + currentTimeshift;
            }
        }
        public static float cooldownSprintTotal
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownSprintTotal;
            }
        }
        public static float cooldownSprintRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownSprintTotal - cooldownSprintElapsed, 0);
            }
        }




        public static float globalCooldownElapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.globalCooldownElapsed + currentTimeshift;
            }
        }
        public static float globalCooldownTotal
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.globalCooldownTotal;
            }
        }

        private static float previousGlobalCooldownRemaining = 0;
        private static DateTime previousGlobalCooldownRemainingTimestamp = DateTime.MinValue;

        public static bool globalCooldownReady
        {
            get
            {
                if (!initialized)
                    return false;
                checkCache();

                if (currentTimeshift > 0 && globalCooldownRemaining <= 0)
                {
                    return true;
                }

                return !_rawData.globalCooldownInUse;
            }
        }

        static DateTime predictGCDUntil = DateTime.Now;
        static DateTime predictGCDDone = DateTime.Now;

        public static void startGCDPredict()
        {
            predictGCDUntil = DateTime.Now + TimeSpan.FromSeconds(0.5);
            predictGCDDone = DateTime.Now + TimeSpan.FromSeconds(2.5);
        }

        static DateTime predictCastUntil = DateTime.Now;
        static DateTime predictCastDone = DateTime.Now;

        public static void startCastPredict()
        {
            predictCastUntil = DateTime.Now + TimeSpan.FromSeconds(0.5);
            predictCastDone = DateTime.Now + TimeSpan.FromSeconds(2.5);
        }

        public static float globalCooldown = 2.5f;
        public static float baseGlobalCooldown = 2.5f;

        public static float globalCooldownRemaining
        {
            get
            {
                if (!initialized)
                    return 0;

                checkCache();
                if (globalCooldownTotal > 30 || castTimeElapsed < 0 || globalCooldownElapsed > 30 || globalCooldownElapsed < 0)
                {
                    initialized = false;
                    refreshData();
                }

                if (predictGCDUntil > DateTime.Now)
                {
                    return (float)(predictGCDDone - DateTime.Now).TotalSeconds;
                }

                if (globalCooldownElapsed == 0 || !_rawData.globalCooldownInUse)
                {
                    return 0;
                }

                float newVal = globalCooldownTotal - globalCooldownElapsed;

                if (newVal != 0)
                {
                    if (previousGlobalCooldownRemainingTimestamp + TimeSpan.FromSeconds(0.1) > DateTime.Now)
                    {
                        if (previousGlobalCooldownRemaining == newVal)
                        {
                            initialized = false;
                            refreshData();
                        }

                        previousGlobalCooldownRemaining = newVal;
                        previousGlobalCooldownRemainingTimestamp = DateTime.Now;
                    }
                }

                return (float)Math.Max(newVal - currentTimeshift, 0);
            }
        }



        public static float cooldownPotionElapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPotionElapsed + currentTimeshift;
            }
        }
        public static float cooldownPotionTotal
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPotionTotal;
            }
        }
        public static float cooldownPotionRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownPotionTotal - cooldownPotionElapsed, 0);
            }
        }






        public static float cooldownPetAbility1Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility1Elapsed + currentTimeshift;
            }
        }
        public static float cooldownPetAbility1Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility1Total;
            }
        }
        public static float cooldownPetAbility1Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownPetAbility1Total - cooldownPetAbility1Elapsed, 0);
            }
        }



        public static float cooldownPetAbility2Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility2Elapsed + currentTimeshift;
            }
        }
        public static float cooldownPetAbility2Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility2Total;
            }
        }
        public static float cooldownPetAbility2Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownPetAbility2Total - cooldownPetAbility2Elapsed, 0);
            }
        }



        public static float cooldownPetAbility3Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility3Elapsed + currentTimeshift;
            }
        }
        public static float cooldownPetAbility3Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility3Total;
            }
        }
        public static float cooldownPetAbility3Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownPetAbility3Total - cooldownPetAbility3Elapsed, 0);
            }
        }



        public static float cooldownPetAbility4Elapsed
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility4Elapsed + currentTimeshift;
            }
        }
        public static float cooldownPetAbility4Total
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return _rawData.cooldownPetAbility4Total;
            }
        }
        public static float cooldownPetAbility4Remaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();
                return (float)Math.Max(_rawData.cooldownPetAbility4Total - cooldownPetAbility4Elapsed, 0);
            }
        }








        // Paladin
        public static float CircleOfScorn
        {
            get { return cooldownType0Remaining; }
        }
        public static float SpiritsWithin
        {
            get { return cooldownType1Remaining; }
        }
        public static float Provoke
        {
            get { return cooldownType2Remaining; }
        }
        public static float Rampart
        {
            get { return cooldownType4Remaining; }
        }
        public static float FightOrFlight
        {
            get { return cooldownType5Remaining; }
        }
        public static float Convalescence
        {
            get { return cooldownType6Remaining; }
        }
        public static float Awareness
        {
            get { return cooldownType7Remaining; }
        }
        public static float Cover
        {
            get { return cooldownType8Remaining; }
        }
        public static float Sentinel
        {
            get { return cooldownType9Remaining; }
        }
        public static float TemperedWill
        {
            get { return cooldownType10Remaining; }
        }
        public static float Bulwark
        {
            get { return cooldownType11Remaining; }
        }
        public static float HallowedGround
        {
            get { return cooldownType13Remaining; }
        }




        // Warrior
        public static float Defiance
        {
            get { return cooldownType0Remaining; }
        }
        public static float BrutalSwing
        {
            get { return cooldownType1Remaining; }
        }
        public static float Infuriate
        {
            get { return cooldownType3Remaining; }
        }
        public static float Bloodbath
        {
            get { return cooldownType4Remaining; }
        }
        public static float MercyStroke
        {
            get { return cooldownType5Remaining; }
        }
        public static float Berserk
        {
            get { return cooldownType6Remaining; }
        }
        public static float Foresight
        {
            get { return cooldownType7Remaining; }
        }
        public static float ThrillOfBattle
        {
            get { return cooldownType8Remaining; }
        }
        public static float Vengeance
        {
            get { return cooldownType9Remaining; }
        }
        public static float Unchained
        {
            get { return cooldownType10Remaining; }
        }
        public static float Holmgang
        {
            get { return cooldownType11Remaining; }
        }

        public static int Wrath
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[6];
            }
        }




        // Monk
        public static float FistsOfEarthFireWind
        {
            get { return cooldownType0Remaining; }
        }
        public static float ShoulderTackle
        {
            get { return cooldownType2Remaining; }
        }
        public static float InternalRelease
        {
            get { return cooldownType3Remaining; }
        }
        public static float SteelPeak
        {
            get { return cooldownType4Remaining; }
        }
        public static float HowlingFist
        {
            get { return cooldownType5Remaining; }
        }
        public static float Featherfoot
        {
            get { return cooldownType7Remaining; }
        }
        public static float SecondWind
        {
            get { return cooldownType8Remaining; }
        }
        public static float Mantra
        {
            get { return cooldownType9Remaining; }
        }
        public static float PerfectBalance
        {
            get { return cooldownType10Remaining; }
        }


        public static float GreasedLightningTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return getTimer(6);

            }
        }

        public static int GreasedLightningStacks
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[8];
            }
        }



        // Dragoon
        public static float Geirskogul
        {
            get { return cooldownType0Remaining; }
        }
        public static float LegSweep
        {
            get { return cooldownType1Remaining; }
        }
        public static float Jump
        {
            get { return cooldownType2Remaining; }
        }
        public static float PowerSurge
        {
            get { return cooldownType3Remaining; }
        }
        public static float SpineshatterDive
        {
            get { return cooldownType4Remaining; }
        }
        public static float BloodForBlood
        {
            get { return cooldownType5Remaining; }
        }
        public static float KeenFlurry
        {
            get { return cooldownType6Remaining; }
        }
        public static float LifeSurge
        {
            get { return cooldownType7Remaining; }
        }
        public static float Invigorate
        {
            get { return cooldownType8Remaining; }
        }
        public static float DragonfireDive
        {
            get { return cooldownType9Remaining; }
        }
        public static float ElusiveJump
        {
            get { return cooldownType10Remaining; }
        }
        public static float BattleLitany
        {
            get { return cooldownType11Remaining; }
        }
        public static float BloodOfTheDragon
        {
            get { return cooldownType15Remaining; }
        }


        public static float BloodOfTheDragonTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return getTimer(6);

            }
        }



        // Ninja
        public static float KissOfTheWaspViper
        {
            get { return cooldownType0Remaining; }
        }
        public static float Hide
        {
            get { return cooldownType1Remaining; }
        }
        public static float Jugulate
        {
            get { return cooldownType2Remaining; }
        }
        public static float Assassinate
        {
            get { return cooldownType3Remaining; }
        }
        public static float Shukuchi
        {
            get { return cooldownType4Remaining; }
        }
        public static float SneakAttack
        {
            get { return cooldownType5Remaining; }
        }
        public static float TrickAttack
        {
            get { return cooldownType5Remaining; }
        }
        public static float Ninjitsu
        {
            get { return cooldownType6Remaining; }
        }
        public static float Mug
        {
            get { return cooldownType7Remaining; }
        }
        public static float Kassatsu
        {
            get { return cooldownType9Remaining; }
        }
        public static float Goad
        {
            get { return cooldownType10Remaining; }
        }
        public static float DreamWithinADream
        {
            get { return cooldownType15Remaining; }
        }
        public static float Duality
        {
            get { return cooldownType16Remaining; }
        }
        public static float ShadeShift
        {
            get { return cooldownType8Remaining; }
        }
        public static float SmokeScreen
        {
            get { return cooldownType11Remaining; }
        }
        public static float Shadewalker
        {
            get { return cooldownType12Remaining; }
        }

        public static float HutonTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return getTimer(6);

            }
        }




        // Bard
        public static float MiserysEnd
        {
            get { return cooldownType0Remaining; }
        }
        public static float Bloodletter
        {
            get { return cooldownType1Remaining; }
        }
        public static float RepellingShot
        {
            get { return cooldownType3Remaining; }
        }
        public static float MagesBallad
        {
            get { return cooldownType5Remaining; }
        }
        public static float RagingStrikes
        {
            get { return cooldownType6Remaining; }
        }
        public static float Barrage
        {
            get { return cooldownType7Remaining; }
        }
        public static float BattleVoice
        {
            get { return cooldownType12Remaining; }
        }
        public static float EmpyrealArrow
        {
            get { return cooldownType14Remaining; }
        }
        public static float Sidewinder
        {
            get { return cooldownType15Remaining; }
        }
        public static float ArmysPaeon
        {
            get { return cooldownType16Remaining; }
        }
        public static float WanderersMinuet
        {
            get { return cooldownType17Remaining; }
        }
        public static float PitchPerfect
        {
            get { return cooldownType19Remaining; }
        }




        public enum BardSongs : int
        {
            None = 0,
            MagesBallad = 5,
            ArmysPaeon = 10,
            WanderersMinuet = 15
        }


        public static BardSongs Song
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                if (_rawResourceData[9] == 5)
                {
                    return BardSongs.MagesBallad;
                }
                if (_rawResourceData[9] == 10)
                {
                    return BardSongs.ArmysPaeon;
                }
                if (_rawResourceData[9] == 15)
                {
                    return BardSongs.WanderersMinuet;
                }

                return BardSongs.None;
            }
        }

        public static float SongTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return getTimer(6);

            }
        }

        public static int RepertoireStacks
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[8];

            }
        }



        // Black mage
        public static float Transpose
        {
            get { return cooldownType0Remaining; }
        }
        public static float Surecast
        {
            get { return cooldownType1Remaining; }
        }
        public static float Lethargy
        {
            get { return cooldownType2Remaining; }
        }
        public static float AetherialManipulation
        {
            get { return cooldownType3Remaining; }
        }
        public static float Swiftcast
        {
            get { return cooldownType5Remaining; }
        }
        public static float Manaward
        {
            get { return cooldownType7Remaining; }
        }
        public static float Manawall
        {
            get { return cooldownType8Remaining; }
        }
        public static float Convert
        {
            get { return cooldownType10Remaining; }
        }
        public static float Apocatastasis
        {
            get { return cooldownType11Remaining; }
        }
        public static float LeyLines
        {
            get { return cooldownType6Remaining; }
        }
        public static float Sharpcast
        {
            get { return cooldownType4Remaining; }
        }
        public static float Enochian
        {
            get { return cooldownType15Remaining; }
        }

        public static float EnochianTimeRemaining
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return getTimer(8);

            }
        }


        public static int UmbralIce
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                if (_rawResourceData[10] == 255)
                {
                    return 1;
                }
                if (_rawResourceData[10] == 254)
                {
                    return 2;
                }
                if (_rawResourceData[10] == 253)
                {
                    return 3;
                }

                return 0;
            }
        }

        public static int AstralFire
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                if (_rawResourceData[10] > 3)
                {
                    return 0;
                }

                return _rawResourceData[10];
            }
        }

        public static int UmbralHearts
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[11];
            }
        }

        public static bool EnochianActive
        {
            get
            {
                if (!initialized)
                    return false;
                checkCache();

                return _rawResourceData[12] == 1;
            }
        }




        // Arcanist
        public static float EnergyDrain
        {
            get { return cooldownType1Remaining; }
        }
        public static float Bane
        {
            get { return cooldownType2Remaining; }
        }
        public static float Aetherflow
        {
            get { return cooldownType5Remaining; }
        }
        public static float Virus
        {
            get { return cooldownType6Remaining; }
        }
        public static float Rouse
        {
            get { return cooldownType7Remaining; }
        }
        public static float EyeForAnEye
        {
            get { return cooldownType10Remaining; }
        }

        public static int AetherflowCount
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // Only the 4 low bits hold the aetherflow count. Higher holds aethertrail attunement.
                return _rawResourceData[10] & 0xf;
            }
        }





        // Summoner
        public static float Fester
        {
            get { return cooldownType3Remaining; }
        }
        public static float Spur
        {
            get { return cooldownType8Remaining; }
        }
        public static float Enkindle
        {
            get { return cooldownType12Remaining; }
        }

        public static float Painflare
        {
            get { return cooldownType0Remaining; }
        }
        public static float TriDisaster
        {
            get { return cooldownType4Remaining; }
        }
        public static float DreadwyrmTrance
        {
            get { return cooldownType14Remaining; }
        }
        public static float Deathflare
        {
            get { return cooldownType15Remaining; }
        }

        public static int AethertrailCount
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // Only the 4 high bits hold the aethertrail count. Lower holds aetherflow stacks.
                return _rawResourceData[10] >> 4;
            }
        }



        // Scholar
        public static float Lustrate
        {
            get { return cooldownType0Remaining; }
        }
        public static float SacredSoil
        {
            get { return cooldownType4Remaining; }
        }
        public static float Dissipation
        {
            get { return cooldownType11Remaining; }
        }





        // White Mage
        public static float ClericStance
        {
            get { return cooldownType0Remaining; }
        }
        public static float ShroudOfSaints
        {
            get { return cooldownType7Remaining; }
        }
        public static float FluidAura
        {
            get { return cooldownType2Remaining; }
        }
        public static float PresenceOfMind
        {
            get { return cooldownType10Remaining; }
        }
        public static float DivineSeal
        {
            get { return cooldownType5Remaining; }
        }
        public static float Benediction
        {
            get { return cooldownType12Remaining; }
        }




        // Astrologian
        public static float LuminiferousAether
        {
            get
            {
                return cooldownType9Remaining;
            }
        }

        public static float CurrentCardRemainingTime
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return getTimer(6);

            }
        }

        public enum CardTypes : int
        {
            None = 0,
            Balance = 1,
            Bole = 2,
            Arrow = 3,
            Spear = 4,
            Ewer = 5,
            Spire = 6
        }

        public enum RoyalRoadTypes : int
        {
            None = 0,
            Enhanced = 1,
            Extended = 2,
            Spread = 3,
        }

        public static CardTypes CurrentCard
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // Chop off the high order bits containing the held card
                int card = _rawResourceData[10] & 0xf;

                if (card > 6 || card < 1)
                {
                    return CardTypes.None;
                }

                return (CardTypes)(card);
            }
        }

        public static CardTypes HeldCard
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // Chop off the low order bits containing the current card
                int card = _rawResourceData[10] >> 4;

                if (card > 6 || card < 1)
                {
                    return CardTypes.None;
                }

                return (CardTypes)(card);
            }
        }

        public static RoyalRoadTypes RoyalRoadEffect
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // Chop off the low order bits containing the current card
                int card = _rawResourceData[11] >> 4;

                if (card > 3 || card < 1)
                {
                    return RoyalRoadTypes.None;
                }

                return (RoyalRoadTypes)(card);
            }
        }






        // Machinist
        public static float RapidFire
        {
            get { return cooldownType11Remaining; }
        }
        public static float Wildfire
        {
            get { return cooldownType10Remaining; }
        }
        public static float Reload
        {
            get { return cooldownType7Remaining; }
        }
        public static float QuickReload
        {
            get { return cooldownType6Remaining; }
        }
        public static float Reassemble
        {
            get { return cooldownType17Remaining; }
        }
        public static float Heartbreak
        {
            get { return cooldownType4Remaining; }
        }
        public static float Blank
        {
            get { return cooldownType5Remaining; }
        }
        public static float Autoturret
        {
            get { return cooldownType3Remaining; }
        }

        public static int HeatGauge
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // if we don't have gauss barrel on, we don't have any heat 
                // (although sometimes the data in memory says we do, it'll be reset to 0 next time gauss barrel is turned on)
                if (_rawResourceData[10] == 0)
                {
                    return 0;
                }

                return _rawResourceData[8];
            }
        }

        public static int AmmoCount
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[9];
            }
        }

        public static float OverHeatTime
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // if we have gauss barrel on, return the actual cooldown timer
                if (_rawResourceData[10] > 0)
                {
                    return getTimer(6);
                }

                // Otherwise, return whichever has more time
                return 0;
            }
        }

        public static float GaussBarrel
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                // if we have gauss barrel on, return the actual cooldown timer
                if (_rawResourceData[10] > 0)
                {
                    return cooldownType0Remaining;
                }

                // Otherwise, return whichever has more time
                return Math.Max(cooldownType0Remaining, getTimer(6));
            }
        }

        public static bool GaussBarrelEnabled
        {
            get
            {
                if (!initialized)
                    return false;
                checkCache();

                return _rawResourceData[10] > 0;
            }
        }



        public static float GaussRound
        {
            get { return cooldownType14Remaining; }
        }

        public static float Hypercharge
        {
            get { return cooldownType13Remaining; }
        }

        public static float Ricochet
        {
            get { return cooldownType8Remaining; }
        }

        public static float BarrelStabilizer
        {
            get
            {
                return cooldownType9Remaining;
            }
        }
        public static float Flamethrower
        {
            get
            {
                return cooldownType15Remaining;
            }
        }











        // Red Mage

        public static float CorpsACorps
        {
            get { return cooldownType0Remaining; }
        }

        public static float Displacement
        {
            get { return cooldownType1Remaining; }
        }

        public static float Fleche
        {
            get { return cooldownType2Remaining; }
        }

        public static float Acceleration
        {
            get { return cooldownType3Remaining; }
        }

        public static float ContreSixte
        {
            get { return cooldownType4Remaining; }
        }

        public static float Embolden
        {
            get { return cooldownType5Remaining; }
        }

        public static float Manafication
        {
            get { return cooldownType6Remaining; }
        }




        public static int WhiteMana
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[6];
            }
        }

        public static int BlackMana
        {
            get
            {
                if (!initialized)
                    return 0;
                checkCache();

                return _rawResourceData[7];
            }
        }

        public class Cooldown
        {
            private DateTime _started = DateTime.MinValue;
            private TimeSpan _totalTime = TimeSpan.Zero;

            public Cooldown(float totalTime)
            {
                _totalTime = TimeSpan.FromSeconds(totalTime);
            }

            public void start(bool irrelivent = false)
            {
                _started = DateTime.Now;
            }

            public float timeElapsed
            {
                get { return (float)(DateTime.Now - _started).TotalSeconds + currentTimeshift; }
            }
            public float timeTotal
            {
                get { return (float)_totalTime.TotalSeconds; }
                set { _totalTime = TimeSpan.FromSeconds(value); }
            }
            public float timeRemaining
            {
                get
                {
                    return (float)Math.Max(((_started + _totalTime) - DateTime.Now).TotalSeconds - currentTimeshift, 0);
                }
            }
        }

    }
}