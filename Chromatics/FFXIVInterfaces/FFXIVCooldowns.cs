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
            Spire = 6
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

                return RawResourceData[6];
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

                return GetTimer(6);
            }
        }

        public static int GreasedLightningStacks
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[8];
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

                return GetTimer(6);
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

                return GetTimer(6);
            }
        }


        // Bard
        public static float MiserysEnd => CooldownType0Remaining;

        public static float Bloodletter => CooldownType1Remaining;

        public static float RepellingShot => CooldownType3Remaining;

        public static float MagesBallad => CooldownType5Remaining;

        public static float RagingStrikes => CooldownType6Remaining;

        public static float Barrage => CooldownType7Remaining;

        public static float BattleVoice => CooldownType12Remaining;

        public static float EmpyrealArrow => CooldownType14Remaining;

        public static float Sidewinder => CooldownType15Remaining;

        public static float ArmysPaeon => CooldownType16Remaining;

        public static float WanderersMinuet => CooldownType17Remaining;

        public static float PitchPerfect => CooldownType19Remaining;


        public static BardSongs Song
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (RawResourceData[9] == 5)
                    return BardSongs.MagesBallad;
                if (RawResourceData[9] == 10)
                    return BardSongs.ArmysPaeon;
                if (RawResourceData[9] == 15)
                    return BardSongs.WanderersMinuet;

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

                return GetTimer(6);
            }
        }

        public static int RepertoireStacks
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[8];
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

                return GetTimer(8);
            }
        }


        public static int UmbralIce
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                if (RawResourceData[10] == 255)
                    return 1;
                if (RawResourceData[10] == 254)
                    return 2;
                if (RawResourceData[10] == 253)
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

                if (RawResourceData[10] > 3)
                    return 0;

                return RawResourceData[10];
            }
        }

        public static int UmbralHearts
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[11];
            }
        }

        public static bool EnochianActive
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                return RawResourceData[12] == 1;
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
                return RawResourceData[10] & 0xf;
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
                return RawResourceData[10] >> 4;
            }
        }


        // Scholar
        public static float Lustrate => CooldownType0Remaining;

        public static float SacredSoil => CooldownType4Remaining;

        public static float Dissipation => CooldownType11Remaining;


        // White Mage
        public static float ClericStance => CooldownType0Remaining;

        public static float ShroudOfSaints => CooldownType7Remaining;

        public static float FluidAura => CooldownType2Remaining;

        public static float PresenceOfMind => CooldownType10Remaining;

        public static float DivineSeal => CooldownType5Remaining;

        public static float Benediction => CooldownType12Remaining;


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
                var card = RawResourceData[10] & 0xf;

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
                var card = RawResourceData[10] >> 4;

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
        public static float RapidFire => CooldownType11Remaining;

        public static float Wildfire => CooldownType10Remaining;

        public static float Reload => CooldownType7Remaining;

        public static float QuickReload => CooldownType6Remaining;

        public static float Reassemble => CooldownType17Remaining;

        public static float Heartbreak => CooldownType4Remaining;

        public static float Blank => CooldownType5Remaining;

        public static float Autoturret => CooldownType3Remaining;

        public static int HeatGauge
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // if we don't have gauss barrel on, we don't have any heat 
                // (although sometimes the data in memory says we do, it'll be reset to 0 next time gauss barrel is turned on)
                if (RawResourceData[10] == 0)
                    return 0;

                return RawResourceData[8];
            }
        }

        public static int AmmoCount
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[9];
            }
        }

        public static float OverHeatTime
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // if we have gauss barrel on, return the actual cooldown timer
                if (RawResourceData[10] > 0)
                    return GetTimer(6);

                // Otherwise, return whichever has more time
                return 0;
            }
        }

        public static float GaussBarrel
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                // if we have gauss barrel on, return the actual cooldown timer
                if (RawResourceData[10] > 0)
                    return CooldownType0Remaining;

                // Otherwise, return whichever has more time
                return Math.Max(CooldownType0Remaining, GetTimer(6));
            }
        }

        public static bool GaussBarrelEnabled
        {
            get
            {
                if (!Initialized)
                    return false;
                CheckCache();

                return RawResourceData[10] > 0;
            }
        }


        public static float GaussRound => CooldownType14Remaining;

        public static float Hypercharge => CooldownType13Remaining;

        public static float Ricochet => CooldownType8Remaining;

        public static float BarrelStabilizer => CooldownType9Remaining;

        public static float Flamethrower => CooldownType15Remaining;


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

                return RawResourceData[6];
            }
        }

        public static int BlackMana
        {
            get
            {
                if (!Initialized)
                    return 0;
                CheckCache();

                return RawResourceData[7];
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

                            _sList = new List<Signature>();

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

                            _sList.Add(new Signature
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
                                    0x173F518
                                }
                            });
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

                            _sList = new List<Signature>();

                            _sList.Add(new Signature
                            {
                                Key = "CLASSRESOURCES",
                                PointerPath = new List<long>
                                {
                                    0x178ADAA
                                }
                            });
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

            return Math.Max(BitConverter.ToUInt16(RawResourceData, i) / 1000f - CurrentTimeshift, 0);
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
            // Dragoon: Blood of the dragon timer (1000 = 1 second)
            // Ninja: Huton timer
            // Bard: Song remaining time
            // Warrior: Wrath
            // MCH: Overheat timer (countdown from 10 seconds, 1000 = 1 second). Also the timer for when Gauss Barrel can be turned back on after overheat.
            // Monk: Greased Lightning timer
            // AST: Card timer
            [MarshalAs(UnmanagedType.I2)] [FieldOffset(0x6)] // B0
            public readonly short resource1;

            // Ninja: 0 most of the time. 1 briefly after casting huton
            // MCH: Heat Gauge (stays at 100 when overheated)
            // BLM: Combine resource2 and resource3 into an astral fire timer (ugly)
            // Monk: Greased Lightning Stacks
            // Bard: Song Repertoire stacks
            [MarshalAs(UnmanagedType.I1)] [FieldOffset(0x8)] // B2
            public readonly byte resource2;

            // Bard: 15 = Wanderer's Minuet, 10 = Army's Paeon, 5 = Mage's Ballad, Anything else = nothing. May be a bit mask.
            // MCH: Ammo count
            // BLM: Combine resource2 and resource3 into an astral fire timer (ugly)
            [MarshalAs(UnmanagedType.I1)] [FieldOffset(0x9)] // B3
            public readonly byte resource3;

            // MCH: Gauss barrel (bool)
            // BLM: 255 = Umbral Ice 1; 254 = Umbral Ice 2; 253 = Umbral Ice 3; 1 = Astral Fire 1; 2 = Astral Fire 2; 3 = Astral Fire 3. If converted to signed, umbral 1/2/3 would be -1/-2/-3 respectively.
            // SMN: Aetherflow stacks and Aethertrail stacks. Bitmask - low bits (1 and 2) represent aetherflow, bits 3 and 4 represent aethertrail stacks
            // SCH: Aetherflow stacks
            // AST: Split in half, low bits contain current card, high bits contain held card. (1 = Balance, 2 = Bole, 3 = Arrow, 4 = Spear, 5 = Ewer, 6 = Spire). 
            [MarshalAs(UnmanagedType.I1)] [FieldOffset(0x10)] // B4
            public readonly byte resource4;


            // BLM: Umbral Heart count
            // AST: Royal Road effect (16 = Enhanced, 32 = Extended, 48 = spread). Looks like it's the top 4 bits. Not sure if the lower 4 bits are used for anything yet.
            [MarshalAs(UnmanagedType.I1)] [FieldOffset(0x11)] // B5
            public readonly byte resource5;


            // BLM: Enochian active (bool)
            [MarshalAs(UnmanagedType.I1)] [FieldOffset(0x12)] // B6
            public readonly byte resource6;


            // Ninja: total number of times Huton has been used or refreshed
            [MarshalAs(UnmanagedType.I2)] [FieldOffset(0x13)] // B7
            public readonly short resource7;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct CooldownRawData
        {
            // 01CECC84
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x0)] // 
            public readonly float afterSkillLockTime;

            [MarshalAs(UnmanagedType.Bool)] [FieldOffset(0x20)] public readonly bool currentlyCasting;

            // 01CECCB0
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x28)] // 
            public readonly float castTimeElapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2C)] // 
            public readonly float castTimeTotal;


            // 01CECCE0
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x58)] // 
            public readonly float comboTimeRemaining;


            // 01DF98A4
            // 01DF99A0
            [MarshalAs(UnmanagedType.U4)] [FieldOffset(0x118)] // 
            public readonly int actionCount;


            // 
            //  (99)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x13C)] public readonly float cooldownType99Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x140)] public readonly float cooldownType99Total;


            // 
            // Painflare (0)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x150)] public readonly float cooldownType0Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x154)] public readonly float cooldownType0Total;


            // 
            // EnergyDrain (1)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x164)] // 0x128
            public readonly float cooldownType1Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x168)] // 0x12C
            public readonly float cooldownType1Total;


            // 
            // Bane (2)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x178)] // 0x150
            public readonly float cooldownType2Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x17C)] // 0x154
            public readonly float cooldownType2Total;


            // 
            // Fester (3)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x18C)] // 0x18C
            public readonly float cooldownType3Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x190)] // 0x190
            public readonly float cooldownType3Total;


            // 
            // Tri-Disaster (4)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1A0)] // 0x204
            public readonly float cooldownType4Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1A4)] // 0x208
            public readonly float cooldownType4Total;


            // 
            // Aetherflow (5)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1B4)] // 0x114
            public readonly float cooldownType5Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1B8)] // 0x118
            public readonly float cooldownType5Total;


            // 
            // Virus (6)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1C8)] // 0x13C
            public readonly float cooldownType6Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1CC)] // 0x140
            public readonly float cooldownType6Total;


            // 
            // Rouse (7)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1DC)] // 0x178
            public readonly float cooldownType7Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1E0)] // 0x17C
            public readonly float cooldownType7Total;


            // 
            // Spur (8)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1F0)] // 0x1B4
            public readonly float cooldownType8Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x1F4)] // 0x1B8
            public readonly float cooldownType8Total;


            // 
            // Kassatsu (9)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x204)] // 0x1DC
            public readonly float cooldownType9Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x208)] // 0x1E0
            public readonly float cooldownType9Total;


            // 
            // Eye for an Eye (10)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x218)] // 0x164
            public readonly float cooldownType10Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x21C)] // 0x168
            public readonly float cooldownType10Total;


            // 
            // Bulwark (11)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x22C)] // 0x1A0
            public readonly float cooldownType11Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x230)] // 0x1A4
            public readonly float cooldownType11Total;


            // 02138AC8
            // Enkindle (12)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x240)] // 0x1C8
            public readonly float cooldownType12Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x244)] // 0x1CC
            public readonly float cooldownType12Total;


            // 
            // Hallowed Ground (13)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x254)] // 0x1F0
            public readonly float cooldownType13Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x258)] // 0x1F4
            public readonly float cooldownType13Total;


            // 02138AF0
            // Dreadwyrm Trance (14)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x268)] // 0x1F0
            public readonly float cooldownType14Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x26C)] // 0x1F4
            public readonly float cooldownType14Total;


            // 02138AF0
            // Deathflare (15)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x27C)] // 0x1F0
            public readonly float cooldownType15Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x280)] // 0x1F4
            public readonly float cooldownType15Total;


            // 02138AF0
            //  (16)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x290)] // 0x1F0
            public readonly float cooldownType16Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x294)] // 0x1F4
            public readonly float cooldownType16Total;


            // 02138AF0
            //  (17)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2A4)] // 0x1F0
            public readonly float cooldownType17Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2A8)] // 0x1F4
            public readonly float cooldownType17Total;


            // 02138AF0
            //  (18)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2B8)] // 0x1F0
            public readonly float cooldownType18Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2BC)] // 0x1F4
            public readonly float cooldownType18Total;


            //  (19)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2CC)] // 0x1F0
            public readonly float cooldownType19Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2D0)] // 0x1F4
            public readonly float cooldownType19Total;


            //  (20)
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2E0)] // 0x1F0
            public readonly float cooldownType20Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x2E4)] // 0x1F4
            public readonly float cooldownType20Total;


            // 022FA638
            // 10FA638
            // 01CED0DC
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x470)] // 0x434
            public readonly float cooldownCrossClassSlot1Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x474)] // 0x438
            public readonly float cooldownCrossClassSlot1Total;


            // 01CED0F0
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x484)] // 0x448
            public readonly float cooldownCrossClassSlot2Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x488)] // 0x44C
            public readonly float cooldownCrossClassSlot2Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x498)] // 02140B30
            public readonly float cooldownCrossClassSlot3Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x49C)] // 02140B34
            public readonly float cooldownCrossClassSlot3Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4AC)] // 02140B44
            public readonly float cooldownCrossClassSlot4Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4B0)] // 02140B48
            public readonly float cooldownCrossClassSlot4Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4C0)] // 02140B58
            public readonly float cooldownCrossClassSlot5Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4C4)] // 02140B5C
            public readonly float cooldownCrossClassSlot5Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4D4)] // 
            public readonly float cooldownCrossClassSlot6Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4D8)] // 
            public readonly float cooldownCrossClassSlot6Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4E8)] // 
            public readonly float cooldownCrossClassSlot7Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4EC)] // 
            public readonly float cooldownCrossClassSlot7Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x4FC)] // 
            public readonly float cooldownCrossClassSlot8Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x500)] // 
            public readonly float cooldownCrossClassSlot8Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x510)] // 
            public readonly float cooldownCrossClassSlot9Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x514)] // 
            public readonly float cooldownCrossClassSlot9Total;


            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x524)] // 
            public readonly float cooldownCrossClassSlot10Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x528)] // 
            public readonly float cooldownCrossClassSlot10Total;


            // 01CED208
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x59C)] // 0x560
            public readonly float cooldownSprintElapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5A0)] // 0x564
            public readonly float cooldownSprintTotal;


            // 01CED230
            [MarshalAs(UnmanagedType.Bool)] [FieldOffset(0x5BC)] // 0x588
            public readonly bool globalCooldownInUse;


            // 01CED230
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5C4)] // 0x588
            public readonly float globalCooldownElapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5C8)] // 0x58C
            public readonly float globalCooldownTotal;


            // 01CED244
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5D8)] // 02140C70
            public readonly float cooldownPotionElapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5DC)] // 02140C74
            public readonly float cooldownPotionTotal;


            // 01CED244
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5EC)] // 02140C70
            public readonly float cooldownPoisonPotionElapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x5F0)] // 02140C74
            public readonly float cooldownPoisonPotionTotal;


            // 01CED3AC
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x740)] // 0x704
            public readonly float cooldownPetAbility1Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x744)] // 0x708
            public readonly float cooldownPetAbility1Total;


            // 01CED3C0
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x754)] // 0x718
            public readonly float cooldownPetAbility2Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x758)] // 0x71C
            public readonly float cooldownPetAbility2Total;


            // 01CED3D4
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x768)] // 0x72C
            public readonly float cooldownPetAbility3Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x76C)] // 0x730
            public readonly float cooldownPetAbility3Total;


            // 01CED3E8
            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x77C)] // 0x740
            public readonly float cooldownPetAbility4Elapsed;

            [MarshalAs(UnmanagedType.R4)] [FieldOffset(0x780)] // 0x744
            public readonly float cooldownPetAbility4Total;
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