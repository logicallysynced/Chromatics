using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Sharlayan;
using Sharlayan.Models;

namespace Chromatics.FFXIVInterfaces
{
    public class Cooldowns
    {
        public enum BardSongs
        {
            None = 0,
            MagesBallad = 5,
            ArmysPaeon = 10,
            WanderersMinuet = 15
        }

        public enum CardTypes
        {
            None = 0,
            Balance = 1,
            Bole = 2,
            Arrow = 3,
            Spear = 4,
            Ewer = 5,
            Spire = 6,
            Lady = 7,
            Lord = 8
        }

        public enum RoyalRoadTypes
        {
            None = 0,
            Enhanced = 1,
            Extended = 2,
            Spread = 3
        }

        private static CooldownRawData _rawData;
        public static DateTime LastUpdated = DateTime.MinValue;
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.05);

        public static byte[] RawResourceData;

        public static bool Initialized;
        public static bool CooldownsInitialized;
        public static bool ResourcesInitialized;
        public static bool JobsInitialized;
        public static bool InitializedActor = false;
        private static List<Signature> _sList;
        private static readonly object RefreshLock = new object();

        private static IntPtr _characterAddress = IntPtr.Zero;

        private static readonly object CacheLock = new object();


        /**/
        // the autoAttackCount data no longer seems to exist in memory...
        //private static int cachedAutoAttackCount = 0;
        private static int _cachedActionCount;


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


        private static float _previousAfterSkillLockTime;
        private static DateTime _previousAfterSkillLockTimeTimestamp = DateTime.MinValue;

        private static float _previousCastTimeRemaining;
        private static DateTime _previousCastTimeRemainingTimestamp = DateTime.MinValue;

        private static float _previousGlobalCooldownRemaining;
        private static DateTime _previousGlobalCooldownRemainingTimestamp = DateTime.MinValue;

        private static DateTime _predictGcdUntil = DateTime.Now;
        private static DateTime _predictGcdDone = DateTime.Now;

        private static DateTime _predictCastUntil = DateTime.Now;
        private static DateTime _predictCastDone = DateTime.Now;

        public static float GlobalCooldown = 2.5f;
        public static float BaseGlobalCooldown = 2.5f;

        public static float CurrentTimeshift { get; set; } = 0;

        public static int ActionCount
        {
            get
            {
                if (!Initialized)
                    return 0;

                CheckCache();

                if (_rawData.actionCount != 0)
                    _cachedActionCount = _rawData.actionCount;

                return _cachedActionCount; // -cachedAutoAttackCount;
            }
        }

        public static float AfterSkillLockTime
        {
            get
            {
                if (!Initialized)
                    return 0;

                CheckCache();
                if (_rawData.afterSkillLockTime > 10 || _rawData.castTimeTotal < 0)
                {
                    Initialized = false;
                    RefreshData();
                }
                var newVal = _rawData.afterSkillLockTime;

                if (newVal != 0)
                    if (_previousAfterSkillLockTimeTimestamp + TimeSpan.FromSeconds(0.1) > DateTime.Now)
                    {
                        if (_previousAfterSkillLockTime == newVal)
                        {
                            Initialized = false;
                            RefreshData();
                        }

                        _previousAfterSkillLockTime = newVal;
                        _previousAfterSkillLockTimeTimestamp = DateTime.Now;
                    }

                newVal -= CurrentTimeshift;

                if (newVal < 0)
                    newVal = 0;

                return newVal;
            }
        }

        public static bool CurrentlyCasting
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                if (CurrentTimeshift > 0 && CastTimeElapsed <= 0)
                    return false;

                return _rawData.currentlyCasting;
            }
        }

        public static float CastTimeElapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (_rawData.castTimeTotal - _rawData.castTimeElapsed < CurrentTimeshift)
                    return 0;

                return _rawData.castTimeElapsed + CurrentTimeshift;
            }
        }

        public static float CastTimeTotal
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (_rawData.castTimeTotal - _rawData.castTimeElapsed <= CurrentTimeshift)
                    return 0;

                return _rawData.castTimeTotal;
            }
        }

        public static float CastTimeRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;

                CheckCache();


                if (_predictCastUntil > DateTime.Now)
                {
                    if ((float) (_predictCastDone - DateTime.Now).TotalSeconds < CurrentTimeshift)
                        return 0;

                    return (float) (_predictCastDone - DateTime.Now).TotalSeconds;
                }


                if (_rawData.castTimeElapsed <= 0)
                    return 0;
                if (_rawData.castTimeTotal > 10 || _rawData.castTimeTotal < 0 || _rawData.castTimeElapsed > 10 ||
                    _rawData.castTimeElapsed < 0)
                {
                    Initialized = false;
                    RefreshData();
                }
                var newVal = _rawData.castTimeTotal - _rawData.castTimeElapsed;

                if (newVal > 0)
                    if (_previousCastTimeRemainingTimestamp + TimeSpan.FromSeconds(0.1) > DateTime.Now)
                    {
                        if (_previousCastTimeRemaining == newVal)
                        {
                            Initialized = false;
                            RefreshData();
                        }

                        _previousCastTimeRemaining = newVal;
                        _previousCastTimeRemainingTimestamp = DateTime.Now;
                    }

                return Math.Max(newVal - CurrentTimeshift, 0);
            }
        }


        public static float ComboTimeRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return Math.Max(_rawData.comboTimeRemaining - CurrentTimeshift, 0);
            }
        }


        public static float CooldownType99Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return _rawData.cooldownType99Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType99Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType99Total;
            }
        }

        public static float CooldownType99Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType99Total - CooldownType99Elapsed, 0);
            }
        }


        public static float CooldownType0Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType0Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType0Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType0Total;
            }
        }

        public static float CooldownType0Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType0Total - CooldownType0Elapsed, 0);
            }
        }


        public static float CooldownType1Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType1Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType1Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType1Total;
            }
        }

        public static float CooldownType1Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType1Total - CooldownType1Elapsed, 0);
            }
        }


        public static float CooldownType2Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType2Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType2Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType2Total;
            }
        }

        public static float CooldownType2Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType2Total - CooldownType2Elapsed, 0);
            }
        }


        public static float CooldownType3Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType3Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType3Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType3Total;
            }
        }

        public static float CooldownType3Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType3Total - CooldownType3Elapsed, 0);
            }
        }


        public static float CooldownType4Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType4Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType4Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType4Total;
            }
        }

        public static float CooldownType4Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType4Total - CooldownType4Elapsed, 0);
            }
        }


        public static float CooldownType5Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType5Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType5Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType5Total;
            }
        }

        public static float CooldownType5Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType5Total - CooldownType5Elapsed, 0);
            }
        }


        public static float CooldownType6Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType6Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType6Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType6Total;
            }
        }

        public static float CooldownType6Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType6Total - CooldownType6Elapsed, 0);
            }
        }


        public static float CooldownType7Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType7Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType7Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType7Total;
            }
        }

        public static float CooldownType7Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType7Total - CooldownType7Elapsed, 0);
            }
        }


        public static float CooldownType8Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType8Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType8Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType8Total;
            }
        }

        public static float CooldownType8Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType8Total - CooldownType8Elapsed, 0);
            }
        }


        public static float CooldownType9Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType9Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType9Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType9Total;
            }
        }

        public static float CooldownType9Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType9Total - CooldownType9Elapsed, 0);
            }
        }


        public static float CooldownType10Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType10Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType10Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType10Total;
            }
        }

        public static float CooldownType10Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType10Total - CooldownType10Elapsed, 0);
            }
        }


        public static float CooldownType11Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType11Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType11Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType11Total;
            }
        }

        public static float CooldownType11Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType11Total - CooldownType11Elapsed, 0);
            }
        }


        public static float CooldownType12Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType12Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType12Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType12Total;
            }
        }

        public static float CooldownType12Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType12Total - CooldownType12Elapsed, 0);
            }
        }


        public static float CooldownType13Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType13Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType13Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType13Total;
            }
        }

        public static float CooldownType13Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType13Total - CooldownType13Elapsed, 0);
            }
        }


        public static float CooldownType14Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType14Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType14Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType14Total;
            }
        }

        public static float CooldownType14Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType14Total - CooldownType14Elapsed, 0);
            }
        }


        public static float CooldownType15Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType15Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType15Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType15Total;
            }
        }

        public static float CooldownType15Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType15Total - CooldownType15Elapsed, 0);
            }
        }


        public static float CooldownType16Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType16Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType16Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType16Total;
            }
        }

        public static float CooldownType16Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType16Total - CooldownType16Elapsed, 0);
            }
        }


        public static float CooldownType17Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType17Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType17Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType17Total;
            }
        }

        public static float CooldownType17Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType17Total - CooldownType17Elapsed, 0);
            }
        }


        public static float CooldownType18Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType18Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType18Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType18Total;
            }
        }

        public static float CooldownType18Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType18Total - CooldownType18Elapsed, 0);
            }
        }


        public static float CooldownType19Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType19Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType19Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType19Total;
            }
        }

        public static float CooldownType19Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType19Total - CooldownType19Elapsed, 0);
            }
        }


        public static float CooldownType20Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType20Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType20Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType20Total;
            }
        }

        public static float CooldownType20Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType20Total - CooldownType20Elapsed, 0);
            }
        }

        public static float CooldownType21Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType21Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType21Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType21Total;
            }
        }

        public static float CooldownType21Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType21Total - CooldownType21Elapsed, 0);
            }
        }

        public static float CooldownType22Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType22Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType22Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType22Total;
            }
        }

        public static float CooldownType22Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType22Total - CooldownType22Elapsed, 0);
            }
        }

        public static float CooldownType23Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType23Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownType23Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownType23Total;
            }
        }

        public static float CooldownType23Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownType23Total - CooldownType23Elapsed, 0);
            }
        }

        public static float CooldownCrossClassSlot1Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot1Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot1Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot1Total;
            }
        }

        public static float CooldownCrossClassSlot1Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot1Total - CooldownCrossClassSlot1Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot2Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot2Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot2Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot2Total;
            }
        }

        public static float CooldownCrossClassSlot2Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot2Total - CooldownCrossClassSlot2Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot3Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot3Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot3Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot3Total;
            }
        }

        public static float CooldownCrossClassSlot3Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot3Total - CooldownCrossClassSlot3Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot4Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot4Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot4Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot4Total;
            }
        }

        public static float CooldownCrossClassSlot4Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot4Total - CooldownCrossClassSlot4Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot5Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot5Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot5Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot5Total;
            }
        }

        public static float CooldownCrossClassSlot5Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot5Total - CooldownCrossClassSlot5Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot6Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot6Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot6Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot6Total;
            }
        }

        public static float CooldownCrossClassSlot6Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot6Total - CooldownCrossClassSlot6Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot7Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot7Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot7Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot7Total;
            }
        }

        public static float CooldownCrossClassSlot7Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot7Total - CooldownCrossClassSlot7Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot8Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot8Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot8Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot8Total;
            }
        }

        public static float CooldownCrossClassSlot8Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot8Total - CooldownCrossClassSlot8Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot9Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot9Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot9Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot9Total;
            }
        }

        public static float CooldownCrossClassSlot9Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot9Total - CooldownCrossClassSlot9Elapsed, 0);
            }
        }


        public static float CooldownCrossClassSlot10Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot10Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownCrossClassSlot10Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownCrossClassSlot10Total;
            }
        }

        public static float CooldownCrossClassSlot10Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownCrossClassSlot10Total - CooldownCrossClassSlot10Elapsed, 0);
            }
        }


        public static float CooldownSprintElapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownSprintElapsed + CurrentTimeshift;
            }
        }

        public static float CooldownSprintTotal
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownSprintTotal;
            }
        }

        public static float CooldownSprintRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownSprintTotal - CooldownSprintElapsed, 0);
            }
        }


        public static float GlobalCooldownElapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.globalCooldownElapsed + CurrentTimeshift;
            }
        }

        public static float GlobalCooldownTotal
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.globalCooldownTotal;
            }
        }

        public static bool GlobalCooldownReady
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                if (CurrentTimeshift > 0 && GlobalCooldownRemaining <= 0)
                    return true;

                return !_rawData.globalCooldownInUse;
            }
        }

        public static float GlobalCooldownRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;

                CheckCache();
                if (GlobalCooldownTotal > 30 || CastTimeElapsed < 0 || GlobalCooldownElapsed > 30 ||
                    GlobalCooldownElapsed < 0)
                {
                    Initialized = false;
                    RefreshData();
                }

                if (_predictGcdUntil > DateTime.Now)
                    return (float) (_predictGcdDone - DateTime.Now).TotalSeconds;

                if (GlobalCooldownElapsed == 0 || !_rawData.globalCooldownInUse)
                    return 0;

                var newVal = GlobalCooldownTotal - GlobalCooldownElapsed;

                if (newVal != 0)
                    if (_previousGlobalCooldownRemainingTimestamp + TimeSpan.FromSeconds(0.1) > DateTime.Now)
                    {
                        if (_previousGlobalCooldownRemaining == newVal)
                        {
                            Initialized = false;
                            RefreshData();
                        }

                        _previousGlobalCooldownRemaining = newVal;
                        _previousGlobalCooldownRemainingTimestamp = DateTime.Now;
                    }

                return Math.Max(newVal - CurrentTimeshift, 0);
            }
        }


        public static float CooldownPotionElapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPotionElapsed + CurrentTimeshift;
            }
        }

        public static float CooldownPotionTotal
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPotionTotal;
            }
        }

        public static float CooldownPotionRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownPotionTotal - CooldownPotionElapsed, 0);
            }
        }


        public static float CooldownPetAbility1Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility1Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownPetAbility1Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility1Total;
            }
        }

        public static float CooldownPetAbility1Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownPetAbility1Total - CooldownPetAbility1Elapsed, 0);
            }
        }


        public static float CooldownPetAbility2Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility2Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownPetAbility2Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility2Total;
            }
        }

        public static float CooldownPetAbility2Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownPetAbility2Total - CooldownPetAbility2Elapsed, 0);
            }
        }


        public static float CooldownPetAbility3Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility3Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownPetAbility3Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility3Total;
            }
        }

        public static float CooldownPetAbility3Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownPetAbility3Total - CooldownPetAbility3Elapsed, 0);
            }
        }


        public static float CooldownPetAbility4Elapsed
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility4Elapsed + CurrentTimeshift;
            }
        }

        public static float CooldownPetAbility4Total
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return _rawData.cooldownPetAbility4Total;
            }
        }

        public static float CooldownPetAbility4Remaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                return Math.Max(_rawData.cooldownPetAbility4Total - CooldownPetAbility4Elapsed, 0);
            }
        }


        // Paladin
        public static float CircleOfScorn => CooldownType0Remaining;

        public static float SpiritsWithin => CooldownType1Remaining;

        public static float Provoke => CooldownType2Remaining;

        public static float Rampart => CooldownType4Remaining;

        public static float FightOrFlight => CooldownType5Remaining;

        public static float Convalescence => CooldownType6Remaining;

        public static float Awareness => CooldownType7Remaining;

        public static float Cover => CooldownType8Remaining;

        public static float Sentinel => CooldownType9Remaining;

        public static float TemperedWill => CooldownType10Remaining;

        public static float Bulwark => CooldownType11Remaining;

        public static float HallowedGround => CooldownType13Remaining;
        
        public static int OathGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[4];
            }
        }


        //Dark Knight
        public static int BloodGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[4];
            }
        }


        // Warrior
        public static float Defiance => CooldownType0Remaining;

        public static float BrutalSwing => CooldownType1Remaining;

        public static float Infuriate => CooldownType3Remaining;

        public static float Bloodbath => CooldownType4Remaining;

        public static float MercyStroke => CooldownType5Remaining;

        public static float Berserk => CooldownType6Remaining;

        public static float Foresight => CooldownType7Remaining;

        public static float ThrillOfBattle => CooldownType8Remaining;

        public static float Vengeance => CooldownType9Remaining;

        public static float Unchained => CooldownType10Remaining;

        public static float Holmgang => CooldownType11Remaining;

        public static int Wrath
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[4];
            }
        }


        // Monk
        public static float FistsOfEarthFireWind => CooldownType0Remaining;

        public static float ShoulderTackle => CooldownType2Remaining;

        public static float InternalRelease => CooldownType3Remaining;

        public static float SteelPeak => CooldownType4Remaining;

        public static float HowlingFist => CooldownType5Remaining;

        public static float Featherfoot => CooldownType7Remaining;

        public static float SecondWind => CooldownType8Remaining;

        public static float Mantra => CooldownType9Remaining;

        public static float PerfectBalance => CooldownType10Remaining;


        public static float GreasedLightningTimeRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(4);
            }
        }

        public static int GreasedLightningStacks
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[6];
            }
        }


        // Dragoon
        public static float Geirskogul => CooldownType0Remaining;

        public static float LegSweep => CooldownType1Remaining;

        public static float Jump => CooldownType2Remaining;

        public static float PowerSurge => CooldownType3Remaining;

        public static float SpineshatterDive => CooldownType4Remaining;

        public static float BloodForBlood => CooldownType5Remaining;

        public static float KeenFlurry => CooldownType6Remaining;

        public static float LifeSurge => CooldownType7Remaining;

        public static float Invigorate => CooldownType8Remaining;

        public static float DragonfireDive => CooldownType9Remaining;

        public static float ElusiveJump => CooldownType10Remaining;

        public static float BattleLitany => CooldownType11Remaining;

        public static float BloodOfTheDragon => CooldownType15Remaining;


        public static float BloodOfTheDragonTimeRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(4);
            }
        }

        public static int DragonGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[7];
            }
        }

        public static bool LifeOfTheDragon
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                if (RawResourceData[6] == 2)
                    return true;

                return false;
            }
        }


        // Ninja
        public static float KissOfTheWaspViper => CooldownType0Remaining;

        public static float Hide => CooldownType1Remaining;

        public static float Jugulate => CooldownType2Remaining;

        public static float Assassinate => CooldownType3Remaining;

        public static float Shukuchi => CooldownType4Remaining;

        public static float SneakAttack => CooldownType5Remaining;

        public static float TrickAttack => CooldownType5Remaining;

        public static float Ninjitsu => CooldownType6Remaining;

        public static float Mug => CooldownType7Remaining;

        public static float Kassatsu => CooldownType9Remaining;

        public static float Goad => CooldownType10Remaining;

        public static float DreamWithinADream => CooldownType15Remaining;

        public static float Duality => CooldownType16Remaining;

        public static float ShadeShift => CooldownType8Remaining;

        public static float SmokeScreen => CooldownType11Remaining;

        public static float Shadewalker => CooldownType12Remaining;

        public static float HutonTimeRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(4);
            }
        }

        public static int NinkiGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[9];
            }
        }


        // Bard
        public static float Bloodletter
        {
            get { return CooldownType4Remaining; }
        }
        public static float RepellingShot
        {
            get { return CooldownType9Remaining; }
        }
        public static float MagesBallad
        {
            get { return CooldownType15Remaining; }
        }
        public static float ArmysPaeon
        {
            get { return CooldownType16Remaining; }
        }
        public static float WanderersMinuet
        {
            get { return CooldownType17Remaining; }
        }
        public static float RagingStrikes
        {
            get { return CooldownType13Remaining; }
        }
        public static float Barrage
        {
            get { return CooldownType14Remaining; }
        }
        public static float BattleVoice
        {
            get { return CooldownType22Remaining; }
        }
        public static float EmpyrealArrow
        {
            get { return CooldownType7Remaining; }
        }
        public static float Sidewinder
        {
            get { return CooldownType12Remaining; }
        }
        public static float PitchPerfect
        {
            get { return CooldownType0Remaining; }
        }
        public static float Troubadour
        {
            get
            {
                return CooldownType23Remaining;
            }
        }
        public static float NaturesMinne
        {
            get
            {
                return CooldownType19Remaining;
            }
        }


        public static BardSongs Song
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (RawResourceData[8] == 5)
                {
                    return BardSongs.MagesBallad;
                }
                if (RawResourceData[8] == 10)
                {
                    return BardSongs.ArmysPaeon;
                }
                if (RawResourceData[8] == 15)
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
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(4);
            }
        }

        public static int RepertoireStacks
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[6];

            }
        }

        public static int BardSoulGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[7];

            }
        }


        // Black mage
        public static float Transpose => CooldownType0Remaining;

        public static float Surecast => CooldownType1Remaining;

        public static float Lethargy => CooldownType2Remaining;

        public static float AetherialManipulation => CooldownType3Remaining;

        public static float Swiftcast => CooldownType5Remaining;

        public static float Manaward => CooldownType7Remaining;

        public static float Manawall => CooldownType8Remaining;

        public static float Convert => CooldownType10Remaining;

        public static float Apocatastasis => CooldownType11Remaining;

        public static float LeyLines => CooldownType6Remaining;

        public static float Sharpcast => CooldownType4Remaining;

        public static float Enochian => CooldownType15Remaining;

        public static float EnochianTimeRemaining
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(6);
            }
        }

        public static float EnochianCharge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[5];
            }
        }

        public static bool PolyglotActive
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                if (RawResourceData[10] == 1)
                    return true;

                return false;
            }
        }

        public static int UmbralIce
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (RawResourceData[8] == 255)
                    return 1;
                if (RawResourceData[8] == 254)
                    return 2;
                if (RawResourceData[8] == 253)
                    return 3;

                return 0;
            }
        }

        public static int AstralFire
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (RawResourceData[8] > 3)
                    return 0;

                return RawResourceData[8];
            }
        }

        public static int UmbralHearts
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[9];
            }
        }

        public static bool EnochianActive
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                return RawResourceData[11] == 1;
            }
        }


        // Arcanist
        public static float EnergyDrain => CooldownType1Remaining;

        public static float Bane => CooldownType2Remaining;

        public static float Aetherflow => CooldownType5Remaining;

        public static float Virus => CooldownType6Remaining;

        public static float Rouse => CooldownType7Remaining;

        public static float EyeForAnEye => CooldownType10Remaining;

        public static int AetherflowCount
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // Only the 4 low bits hold the aetherflow count. Higher holds aethertrail attunement.
                return RawResourceData[6] & 0xf;
            }
        }


        // Summoner
        public static float Fester => CooldownType3Remaining;

        public static float Spur => CooldownType8Remaining;

        public static float Enkindle => CooldownType12Remaining;

        public static float Painflare => CooldownType0Remaining;

        public static float TriDisaster => CooldownType4Remaining;

        public static float DreadwyrmTrance => CooldownType14Remaining;

        public static float Deathflare => CooldownType15Remaining;

        public static int AethertrailCount
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // Only the 4 high bits hold the aethertrail count. Lower holds aetherflow stacks.
                return RawResourceData[6] >> 4;
            }
        }

        //Samurai
        public static int SenGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                
                return RawResourceData[7];
            }
        }

        public static int KenkiCharge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();
                
                return RawResourceData[6];
            }
        }
        
        // Scholar
        public static float Lustrate => CooldownType0Remaining;

        public static float SacredSoil => CooldownType4Remaining;

        public static float Dissipation => CooldownType11Remaining;

        public static int FaerieGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[7];
            }
        }


        // White Mage
        public static float ClericStance => CooldownType0Remaining;

        public static float ShroudOfSaints => CooldownType7Remaining;

        public static float FluidAura => CooldownType2Remaining;

        public static float PresenceOfMind => CooldownType10Remaining;

        public static float DivineSeal => CooldownType5Remaining;

        public static float Benediction => CooldownType12Remaining;

        public static int FlowerPetals
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[8];
            }
        }

        public static int FlowerCharge //Builds to 116
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[7];
            }
        }

        public static bool BloodFlowerReady
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                if (RawResourceData[9] == 3)
                    return true;

                return false;
            }
        }


        // Astrologian
        public static float LuminiferousAether => CooldownType9Remaining;

        public static float CurrentCardRemainingTime
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(6);
            }
        }

        public static CardTypes CurrentCard
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // Chop off the high order bits containing the held card
                var card = RawResourceData[8] & 0xf;

                if (card > 6 || card < 1)
                    return CardTypes.None;

                return (CardTypes) card;
            }
        }

        public static CardTypes HeldCard
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // Chop off the low order bits containing the current card
                var card = RawResourceData[8] >> 4;

                if (card > 6 || card < 1)
                    return CardTypes.None;

                return (CardTypes) card;
            }
        }

        public static RoyalRoadTypes RoyalRoadEffect
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // Chop off the low order bits containing the current card
                var card = RawResourceData[11] >> 4;

                if (card > 3 || card < 1)
                    return RoyalRoadTypes.None;

                return (RoyalRoadTypes) card;
            }
        }


        // Machinist
        public static float Wildfire
        {
            get { return CooldownType19Remaining; }
        }
        public static float Reassemble
        {
            get { return CooldownType11Remaining; }
        }
        public static float Autoturret
        {
            get { return CooldownType3Remaining; }
        }
        public static float AutomationQueen
        {
            get { return CooldownType1Remaining; }
        }
        public static float Tactician
        {
            get { return CooldownType23Remaining; }
        }
        public static float Drill
        {
            get { return CooldownType6Remaining; }
        }
        public static float HotShot
        {
            get { return CooldownType9Remaining; }
        }
        public static float AirAnchor
        {
            get { return CooldownType10Remaining; }
        }



        public static float GaussRound
        {
            get { return CooldownType7Remaining; }
        }

        public static float Ricochet
        {
            get { return CooldownType8Remaining; }
        }

        public static float Hypercharge
        {
            get { return CooldownType4Remaining; }
        }

        public static float BarrelStabilizer
        {
            get
            {
                return CooldownType20Remaining;
            }
        }
        public static float Flamethrower
        {
            get
            {
                return CooldownType12Remaining;
            }
        }





        public static int HeatGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[8];
            }
        }

        public static int Battery
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[9];
            }
        }

        public static float HyperchargeTime
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return GetTimer(4);
            }
        }


        public static bool HyperchargeActive
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                return (RawResourceData[11] & 1) > 0;
            }
        }

        public static bool TurretActive
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                return (RawResourceData[11] & 2) > 0;
            }
        }

        // Dancer

        public static float ShieldSamba
        {
            get { return CooldownType22Remaining; }
        }

        public static float CuringWaltz
        {
            get { return CooldownType14Remaining; }
        }



        public static float EnAvant
        {
            get { return CooldownType9Remaining; }
        }
        
        public static float Flourish
        {
            get { return CooldownType13Remaining; }
        }

        public static float Devilment
        {
            get { return CooldownType20Remaining; }
        }

        public static float Improvisation
        {
            get { return CooldownType99Remaining; }
        }

        public static float ClosedPosition
        {
            get { return CooldownType0Remaining; }
        }

        public static float StandardStep
        {
            get { return CooldownType8Remaining; }
        }

        public static float TechnicalStep
        {
            get { return CooldownType19Remaining; }
        }

        public static float FanDance
        {
            get { return CooldownType1Remaining; }
        }

        public static float FanDance2
        {
            get { return CooldownType2Remaining; }
        }

        public static float FanDance3
        {
            get { return CooldownType3Remaining; }
        }


        public static int FourfoldFeathers
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[4];

            }
        }

        public static int Espirit
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[5];

            }
        }

        public static int DanceStepCount
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[10];

            }
        }


        // Red Mage

        public static float CorpsACorps => CooldownType0Remaining;

        public static float Displacement => CooldownType1Remaining;

        public static float Fleche => CooldownType2Remaining;

        public static float Acceleration => CooldownType3Remaining;

        public static float ContreSixte => CooldownType4Remaining;

        public static float Embolden => CooldownType5Remaining;

        public static float Manafication => CooldownType6Remaining;


        public static int WhiteMana
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[4];
            }
        }

        public static int BlackMana
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[5];
            }
        }
        
        public static void RefreshData()
        {
            lock (RefreshLock)
            {
                try
                {
                    if (!Initialized)
                    {
                        if (!Scanner.Instance.Locations.ContainsKey("COOLDOWNS") || !CooldownsInitialized)
                        {
                            //PluginController.debug("Initializing cooldowns...");

                            /*
                            _sList = new List<Signature>
                            {

                                new Signature
                                {
                                    Key = "COOLDOWNS",
                                    PointerPath = new List<long>
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
                                    //0x1740518
                                    //0x17BB2A8
                                    //0x1860658
                                    //0x1861658
                                    0x18AED98
                                }
                            };
                            */
                            _sList = new List<Signature>
                            {
                                new Signature
                                {
                                    Key = "COOLDOWNS",
                                    Value = "8D812DF7FFFFA9F9FFFFFF750881F9D9080000750EF3410F104710F30F1105",
                                    ASMSignature = true,
                                    PointerPath = new List<long>
                                    {
                                        0,
                                        0
                                    }
                                }
                            };
                            Scanner.Instance.LoadOffsets(_sList);

                            Thread.Sleep(100);

                            if (Scanner.Instance.Locations.ContainsKey("COOLDOWNS"))
                            {
                                Debug.WriteLine("Initializing COOLDOWNS done: " +
                                                Scanner.Instance.Locations["COOLDOWNS"].GetAddress().ToInt64()
                                                    .ToString("X"));

                                CooldownsInitialized = true;
                            }
                        }


                        if (!Scanner.Instance.Locations.ContainsKey("CLASSRESOURCES") || !ResourcesInitialized)
                        {
                            //PluginController.debug("Initializing cooldowns...");

                            /*
                            _sList = new List<Signature>
                            {
                                new Signature
                                {
                                    Key = "CLASSRESOURCES",
                                    PointerPath = new List<long>
                                    {
                                        //0x178BDAA
                                        //0x1806742
                                        //0x18087C9
                                        //0x18087C2
                                        //0x18AC4B2
                                        //0x18AD4B2
                                        0x1B2D4BC
                                    }
                                }
                            };
                            */
                            
                            
                            _sList = new List<Signature>
                            {
                                new Signature
                                {
                                    Key = "CLASSRESOURCES",
                                    Value = "488BCEE8********83F81B0F85********807D**020F85********488B1D",
                                    ASMSignature = true,
                                    PointerPath = new List<long>
                                    {
                                        0,
                                        12
                                    }
                                }
                            };
                            

                            Scanner.Instance.LoadOffsets(_sList);

                            Thread.Sleep(100);

                            if (Scanner.Instance.Locations.ContainsKey("CLASSRESOURCES"))
                            {
                                Debug.WriteLine("Initializing CLASSRESOURCES done: " +
                                                Scanner.Instance.Locations["CLASSRESOURCES"].GetAddress().ToInt64()
                                                    .ToString("X"));

                                ResourcesInitialized = true;
                            }
                        }
                        

                        if (CooldownsInitialized && ResourcesInitialized)
                            Initialized = true;
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

                    if (Initialized)
                    {
                        if (Scanner.Instance.Locations.ContainsKey("COOLDOWNS"))
                        {
                            var address = Scanner.Instance.Locations["COOLDOWNS"];

                            //PluginController.debug(" " + address.ToString("X8"));
                            _rawData = MemoryHandler.Instance.GetStructure<CooldownRawData>(address.GetAddress());
                        }
                        //cachedAutoAttackCount = Sharlayan.MemoryHandler.Instance.GetInt16(Sharlayan.Scanner.Locations["AUTO_ATTACK_COUNT"].GetAddress().ToInt64());

                        if (Scanner.Instance.Locations.ContainsKey("CLASSRESOURCES"))
                        {
                            var address = Scanner.Instance.Locations["CLASSRESOURCES"];

                            //PluginController.debug(" " + address.ToString("X8"));
                            RawResourceData = MemoryHandler.Instance.GetByteArray(address.GetAddress(), 20);
                        }


                        LastUpdated = DateTime.Now;
                    }
                }
                catch
                {
                    Initialized = false;
                }
            }
        }

        public static void CheckCache()
        {
            lock (CacheLock)
            {
                if (LastUpdated + UpdateInterval <= DateTime.Now)
                    RefreshData();
                /*
                if (initializedActor)
                {
                    GetRecentAction();
                }
                */
            }
        }

        public static float GetTimer(int i)
        {
            if (!Initialized)
                return 0;

            CheckCache();

            return Math.Max(BitConverter.ToUInt16(RawResourceData, i) / 1000f, 0);
        }

        public static byte GetRaw(int i)
        {
            if (!Initialized)
                return 0;

            CheckCache();

            return RawResourceData[i];
        }

        public static void StartGcdPredict()
        {
            _predictGcdUntil = DateTime.Now + TimeSpan.FromSeconds(0.5);
            _predictGcdDone = DateTime.Now + TimeSpan.FromSeconds(2.5);
        }

        public static void StartCastPredict()
        {
            _predictCastUntil = DateTime.Now + TimeSpan.FromSeconds(0.5);
            _predictCastDone = DateTime.Now + TimeSpan.FromSeconds(2.5);
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct ClassResourceRawData
        {
            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(6)] // B0
            public short resource1;
            
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(8)] // B2
            public byte resource2;
            
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(9)] // B3
            public byte resource3;
            
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(10)] // B4
            public byte resource4;
            
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(11)] // B5
            public byte resource5;
            
            [MarshalAs(UnmanagedType.I1)]
            [FieldOffset(12)] // B6
            public byte resource6;
            
            [MarshalAs(UnmanagedType.I2)]
            [FieldOffset(13)] // B7
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
            //  (98)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x184)]
            public float cooldownType98Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x188)]
            public float cooldownType98Total;


            // 
            //  (99)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x198)]
            public float cooldownType99Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x19C)]
            public float cooldownType99Total;



            // 
            // (0)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1AC)] 
            public float cooldownType0Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1B0)]
            public float cooldownType0Total;


            // 
            // (1)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1C0)] // 0x128
            public float cooldownType1Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1C4)] // 0x12C
            public float cooldownType1Total;


            // 
            // (2)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1D4)] // 0x150
            public float cooldownType2Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1D8)] // 0x154
            public float cooldownType2Total;


            // 
            // (3)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1E8)] // 0x18C
            public float cooldownType3Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1EC)] // 0x190
            public float cooldownType3Total;



            // 
            // (4)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x1FC)] // 0x204
            public float cooldownType4Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x200)] // 0x208
            public float cooldownType4Total;




            // 
            // (5)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x210)] // 0x114
            public float cooldownType5Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x214)] // 0x118
            public float cooldownType5Total;




            // 
            // (6)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x224)] // 0x13C
            public float cooldownType6Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x228)] // 0x140
            public float cooldownType6Total;


            // 
            // (7)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x238)] // 0x178
            public float cooldownType7Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x23C)] // 0x17C
            public float cooldownType7Total;


            // 
            // (8)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x24C)] // 0x1B4
            public float cooldownType8Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x250)] // 0x1B8
            public float cooldownType8Total;



            // 
            // (9)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x260)] // 0x1DC
            public float cooldownType9Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x264)] // 0x1E0
            public float cooldownType9Total;




            // 
            // (10)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x274)] // 0x164
            public float cooldownType10Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x278)] // 0x168
            public float cooldownType10Total;


            // 
            // (11)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x288)] // 0x1A0
            public float cooldownType11Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x28C)] // 0x1A4
            public float cooldownType11Total;



            // 02138AC8
            // (12)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x29C)] // 0x1C8
            public float cooldownType12Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2A0)] // 0x1CC
            public float cooldownType12Total;




            // 
            // (13)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2B0)] // 0x1F0
            public float cooldownType13Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2B4)] // 0x1F4
            public float cooldownType13Total;


            // 02138AF0
            // (14)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2C4)] // 0x1F0
            public float cooldownType14Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2C8)] // 0x1F4
            public float cooldownType14Total;



            // 02138AF0
            // (15)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2D8)] // 0x1F0
            public float cooldownType15Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2DC)] // 0x1F4
            public float cooldownType15Total;


            // 02138AF0
            //  (16)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2EC)] // 0x1F0
            public float cooldownType16Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x2F0)] // 0x1F4
            public float cooldownType16Total;


            // 02138AF0
            //  (17)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x300)] // 0x1F0
            public float cooldownType17Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x304)] // 0x1F4
            public float cooldownType17Total;


            // 02138AF0
            //  (18)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x314)] // 0x1F0
            public float cooldownType18Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x318)] // 0x1F4
            public float cooldownType18Total;


            //  (19)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x328)] // 0x1F0
            public float cooldownType19Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x32C)] // 0x1F4
            public float cooldownType19Total;


            //  (20)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x33C)] // 0x1F0
            public float cooldownType20Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x340)] // 0x1F4
            public float cooldownType20Total;


            //  (21)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x350)] // 0x1F0
            public float cooldownType21Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x354)] // 0x1F4
            public float cooldownType21Total;

            //  (22)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x364)] // 0x1F0
            public float cooldownType22Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x368)] // 0x1F4
            public float cooldownType22Total;

            //  (23)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x378)] // 0x1F0
            public float cooldownType23Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x37C)] // 0x1F4
            public float cooldownType23Total;

            //  (24)
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x38C)] // 0x1F0
            public float cooldownType24Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x390)] // 0x1F4
            public float cooldownType24Total;


            // 022FA638
            // 10FA638
            // 01CED0DC
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4CC)] // 0x434
            public float cooldownCrossClassSlot1Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4D0)] // 0x438
            public float cooldownCrossClassSlot1Total;


            // 01CED0F0
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4E0)] // 0x448
            public float cooldownCrossClassSlot2Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4E4)] // 0x44C
            public float cooldownCrossClassSlot2Total;




            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4F4)] // 02140B30
            public float cooldownCrossClassSlot3Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x4F8)] // 02140B34
            public float cooldownCrossClassSlot3Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x508)] // 02140B44
            public float cooldownCrossClassSlot4Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x50C)] // 02140B48
            public float cooldownCrossClassSlot4Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x51C)] // 02140B58
            public float cooldownCrossClassSlot5Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x520)] // 02140B5C
            public float cooldownCrossClassSlot5Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x530)] // 
            public float cooldownCrossClassSlot6Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x534)] // 
            public float cooldownCrossClassSlot6Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x544)] // 
            public float cooldownCrossClassSlot7Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x548)] // 
            public float cooldownCrossClassSlot7Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x558)] // 
            public float cooldownCrossClassSlot8Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x55C)] // 
            public float cooldownCrossClassSlot8Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x56C)] // 
            public float cooldownCrossClassSlot9Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x570)] // 
            public float cooldownCrossClassSlot9Total;



            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x580)] // 
            public float cooldownCrossClassSlot10Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x584)] // 
            public float cooldownCrossClassSlot10Total;



            // 01CED208
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5F8)] // 0x560
            public float cooldownSprintElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x5FC)] // 0x564
            public float cooldownSprintTotal;



            // 01CED230
            [MarshalAs(UnmanagedType.Bool)]
            [FieldOffset(0x618)] // 0x588
            public bool globalCooldownInUse;



            // 01CED230
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x620)] // 0x588
            public float globalCooldownElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x624)] // 0x58C
            public float globalCooldownTotal;



            // 01CED244
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x634)] // 02140C70
            public float cooldownPotionElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x638)] // 02140C74
            public float cooldownPotionTotal;



            // 01CED244
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x648)] // 02140C70
            public float cooldownPoisonPotionElapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x64C)] // 02140C74
            public float cooldownPoisonPotionTotal;




            // 01CED3AC
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x79C)] // 0x704
            public float cooldownPetAbility1Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7A0)] // 0x708
            public float cooldownPetAbility1Total;


            // 01CED3C0
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7B0)] // 0x718
            public float cooldownPetAbility2Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7B4)] // 0x71C
            public float cooldownPetAbility2Total;


            // 01CED3D4
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7C4)] // 0x72C
            public float cooldownPetAbility3Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7C8)] // 0x730
            public float cooldownPetAbility3Total;


            // 01CED3E8
            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7D8)] // 0x740
            public float cooldownPetAbility4Elapsed;

            [MarshalAs(UnmanagedType.R4)]
            [FieldOffset(0x7DC)] // 0x744
            public float cooldownPetAbility4Total;



        }

        public class Cooldown
        {
            private DateTime _started = DateTime.MinValue;
            private TimeSpan _totalTime = TimeSpan.Zero;

            public Cooldown(float totalTime)
            {
                _totalTime = TimeSpan.FromSeconds(totalTime);
            }

            public float TimeElapsed => (float) (DateTime.Now - _started).TotalSeconds + CurrentTimeshift;

            public float TimeTotal
            {
                get => (float) _totalTime.TotalSeconds;
                set => _totalTime = TimeSpan.FromSeconds(value);
            }

            public float TimeRemaining => (float) Math.Max(
                (_started + _totalTime - DateTime.Now).TotalSeconds - CurrentTimeshift, 0);

            public void Start(bool irrelivent = false)
            {
                _started = DateTime.Now;
            }
        }
    }
}