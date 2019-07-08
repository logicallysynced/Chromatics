using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Chromatics.CorsairLibs;
using Chromatics.DeviceInterfaces.EffectLibrary;
using Chromatics.FFXIVInterfaces;
using CUE.NET;
using CUE.NET.Brushes;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Exceptions;
using CUE.NET.Groups;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.VisualBasic.Devices;

/* Contains all Corsair SDK code for detection, initilization, states and effects.
 * 
 * 
 */

namespace Chromatics.DeviceInterfaces
{
    public class CorsairInterface
    {
        public static CorsairLib InitializeCorsairSdk()
        {
            CorsairLib corsair = null;
            if (Process.GetProcessesByName("CUE").Length > 0 || Process.GetProcessesByName("iCUE").Length > 0)
            {
                corsair = new CorsairLib();

                var corsairstat = corsair.InitializeSdk();

                if (!corsairstat)
                    return null;
            }

            return corsair;
        }
    }

    public class CorsairSdkWrapper
    {
        //
    }

    public interface ICorsairSdk
    {
        bool InitializeSdk();

        void ShutdownSdk();
        void ResetCorsairDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, bool deviceOtherDevices, Color basecol);

        void CorsairUpdateLed();
        void SetLights(Color col);

        void SetAllLights(Color color);
        void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist);
        void ApplyMapMouseLighting(string region, Color col, bool clear);
        void ApplyMapLogoLighting(string key, Color col, bool clear);
        void ApplyMapPadLighting(string region, Color col, bool clear);
        void ApplyMapHeadsetLighting(Color col, bool clear);
        void ApplyMapStandLighting(string region, Color col, bool clear);
        void ApplyMapOtherLighting(Color col, int pos);

        Task Ripple1(Color burstcol, int speed, Color baseColor);
        Task Ripple2(Color burstcol, int speed);
        void Flash1(Color burstcol, int speed, string[] regions);
        void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void Flash3(Color burstcol, int speed, CancellationToken cts);
        void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions);
        void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50);
        void CycleEffect(int interval, CancellationTokenSource token);
        Color GetCurrentKeyColor(string key);
        Color GetCurrentMouseColor(string region);
        Color GetCurrentPadColor(string region);
    }

    public class CorsairLib : ICorsairSdk
    {
        private static readonly ILogWrite Write = SimpleIoc.Default.GetInstance<ILogWrite>();

        private static readonly object Corsairtransition = new object();
        private static readonly object CorsairRipple1 = new object();
        private static readonly object CorsairRipple2 = new object();
        private static readonly object CorsairFlash1 = new object();
        private static readonly object CorsairFlash2 = new object();
        private static readonly object CorsairFlash4 = new object();
        private static readonly object _CorsairtransitionConst = new object();

        private static Dictionary<string, Color> _presets = new Dictionary<string, Color>();

        private static int _corsairFlash3Step;
        private static bool _corsairFlash3Running;
        private static readonly object _Flash3 = new object();

        private static Dictionary<string, Color> _presets4 = new Dictionary<string, Color>();

        //Handle device send/recieve
        private readonly CancellationTokenSource _ccts = new CancellationTokenSource();

        #region Effect Steps

        private readonly List<CorsairLedId> _test = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_1 },
            { CorsairLedId.DIY1_2 },
            { CorsairLedId.DIY1_3 },
            { CorsairLedId.DIY1_4 },
            { CorsairLedId.DIY1_5 },
            { CorsairLedId.DIY1_6 },
            { CorsairLedId.DIY1_7 },
            { CorsairLedId.DIY1_8 },
            { CorsairLedId.DIY1_9 },
            { CorsairLedId.DIY1_10 },
            { CorsairLedId.DIY1_11 },
            { CorsairLedId.DIY1_12 },
            { CorsairLedId.DIY1_13 },
            { CorsairLedId.DIY1_14 },
            { CorsairLedId.DIY1_15 },
            { CorsairLedId.DIY1_16 },
            { CorsairLedId.DIY1_17 },
            { CorsairLedId.DIY1_18 },
            { CorsairLedId.DIY1_19 },
            { CorsairLedId.DIY1_20 },
            { CorsairLedId.DIY1_21 },
            { CorsairLedId.DIY1_22 },
            { CorsairLedId.DIY1_23 },
            { CorsairLedId.DIY1_24 },
            { CorsairLedId.DIY1_25 },
        };

        private readonly List<CorsairLedId> _diyZ1 = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_1 },
            { CorsairLedId.DIY1_2 },
            { CorsairLedId.DIY1_3 },
            { CorsairLedId.DIY1_4 },
            { CorsairLedId.DIY1_5 },
            { CorsairLedId.DIY1_6 },
            { CorsairLedId.DIY1_7 },
            { CorsairLedId.DIY1_8 },
            { CorsairLedId.DIY1_9 },
            { CorsairLedId.DIY1_10 },
            { CorsairLedId.DIY1_11 },
            { CorsairLedId.DIY1_12 },
            { CorsairLedId.DIY1_13 },
            { CorsairLedId.DIY1_14 },
            { CorsairLedId.DIY1_15 },
            { CorsairLedId.DIY1_16 },
            { CorsairLedId.DIY1_17 },
            { CorsairLedId.DIY1_18 },
            { CorsairLedId.DIY1_19 },
            { CorsairLedId.DIY1_20 },
            { CorsairLedId.DIY1_21 },
            { CorsairLedId.DIY1_22 },
            { CorsairLedId.DIY1_23 },
            { CorsairLedId.DIY1_24 },
            { CorsairLedId.DIY1_25 },
            { CorsairLedId.DIY2_1 },
            { CorsairLedId.DIY2_2 },
            { CorsairLedId.DIY2_3 },
            { CorsairLedId.DIY2_4 },
            { CorsairLedId.DIY2_5 },
            { CorsairLedId.DIY2_6 },
            { CorsairLedId.DIY2_7 },
            { CorsairLedId.DIY2_8 },
            { CorsairLedId.DIY2_9 },
            { CorsairLedId.DIY2_10 },
            { CorsairLedId.DIY2_11 },
            { CorsairLedId.DIY2_12 },
            { CorsairLedId.DIY2_13 },
            { CorsairLedId.DIY2_14 },
            { CorsairLedId.DIY2_15 },
            { CorsairLedId.DIY2_16 },
            { CorsairLedId.DIY2_17 },
            { CorsairLedId.DIY2_18 },
            { CorsairLedId.DIY2_19 },
            { CorsairLedId.DIY2_20 },
            { CorsairLedId.DIY2_21 },
            { CorsairLedId.DIY2_22 },
            { CorsairLedId.DIY2_23 },
            { CorsairLedId.DIY2_24 },
            { CorsairLedId.DIY2_25 },
            { CorsairLedId.DIY3_1 },
            { CorsairLedId.DIY3_2 },
            { CorsairLedId.DIY3_3 },
            { CorsairLedId.DIY3_4 },
            { CorsairLedId.DIY3_5 },
            { CorsairLedId.DIY3_6 },
            { CorsairLedId.DIY3_7 },
            { CorsairLedId.DIY3_8 },
            { CorsairLedId.DIY3_9 },
            { CorsairLedId.DIY3_10 },
            { CorsairLedId.DIY3_11 },
            { CorsairLedId.DIY3_12 },
            { CorsairLedId.DIY3_13 },
            { CorsairLedId.DIY3_14 },
            { CorsairLedId.DIY3_15 },
            { CorsairLedId.DIY3_16 },
            { CorsairLedId.DIY3_17 },
            { CorsairLedId.DIY3_18 },
            { CorsairLedId.DIY3_19 },
            { CorsairLedId.DIY3_20 },
            { CorsairLedId.DIY3_21 },
            { CorsairLedId.DIY3_22 },
            { CorsairLedId.DIY3_23 },
            { CorsairLedId.DIY3_24 },
            { CorsairLedId.DIY3_25 },
        };

        private readonly List<CorsairLedId> _diyZ2 = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_26 },
            { CorsairLedId.DIY1_27 },
            { CorsairLedId.DIY1_28 },
            { CorsairLedId.DIY1_29 },
            { CorsairLedId.DIY1_30 },
            { CorsairLedId.DIY1_31 },
            { CorsairLedId.DIY1_32 },
            { CorsairLedId.DIY1_33 },
            { CorsairLedId.DIY1_34 },
            { CorsairLedId.DIY1_35 },
            { CorsairLedId.DIY1_36 },
            { CorsairLedId.DIY1_37 },
            { CorsairLedId.DIY1_38 },
            { CorsairLedId.DIY1_39 },
            { CorsairLedId.DIY1_40 },
            { CorsairLedId.DIY1_41 },
            { CorsairLedId.DIY1_42 },
            { CorsairLedId.DIY1_43 },
            { CorsairLedId.DIY1_44 },
            { CorsairLedId.DIY1_45 },
            { CorsairLedId.DIY1_46 },
            { CorsairLedId.DIY1_47 },
            { CorsairLedId.DIY1_48 },
            { CorsairLedId.DIY1_49 },
            { CorsairLedId.DIY1_50 },
            { CorsairLedId.DIY2_26 },
            { CorsairLedId.DIY2_27 },
            { CorsairLedId.DIY2_28 },
            { CorsairLedId.DIY2_29 },
            { CorsairLedId.DIY2_30 },
            { CorsairLedId.DIY2_31 },
            { CorsairLedId.DIY2_32 },
            { CorsairLedId.DIY2_33 },
            { CorsairLedId.DIY2_34 },
            { CorsairLedId.DIY2_35 },
            { CorsairLedId.DIY2_36 },
            { CorsairLedId.DIY2_37 },
            { CorsairLedId.DIY2_38 },
            { CorsairLedId.DIY2_39 },
            { CorsairLedId.DIY2_40 },
            { CorsairLedId.DIY2_41 },
            { CorsairLedId.DIY2_42 },
            { CorsairLedId.DIY2_43 },
            { CorsairLedId.DIY2_44 },
            { CorsairLedId.DIY2_45 },
            { CorsairLedId.DIY2_46 },
            { CorsairLedId.DIY2_47 },
            { CorsairLedId.DIY2_48 },
            { CorsairLedId.DIY2_49 },
            { CorsairLedId.DIY2_50 },
            { CorsairLedId.DIY3_26 },
            { CorsairLedId.DIY3_27 },
            { CorsairLedId.DIY3_28 },
            { CorsairLedId.DIY3_29 },
            { CorsairLedId.DIY3_30 },
            { CorsairLedId.DIY3_31 },
            { CorsairLedId.DIY3_32 },
            { CorsairLedId.DIY3_33 },
            { CorsairLedId.DIY3_34 },
            { CorsairLedId.DIY3_35 },
            { CorsairLedId.DIY3_36 },
            { CorsairLedId.DIY3_37 },
            { CorsairLedId.DIY3_38 },
            { CorsairLedId.DIY3_39 },
            { CorsairLedId.DIY3_40 },
            { CorsairLedId.DIY3_41 },
            { CorsairLedId.DIY3_42 },
            { CorsairLedId.DIY3_43 },
            { CorsairLedId.DIY3_44 },
            { CorsairLedId.DIY3_45 },
            { CorsairLedId.DIY3_46 },
            { CorsairLedId.DIY3_47 },
            { CorsairLedId.DIY3_48 },
            { CorsairLedId.DIY3_49 },
            { CorsairLedId.DIY3_50 },
        };

        private readonly List<CorsairLedId> _diyZ3 = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_51 },
            { CorsairLedId.DIY1_52 },
            { CorsairLedId.DIY1_53 },
            { CorsairLedId.DIY1_54 },
            { CorsairLedId.DIY1_55 },
            { CorsairLedId.DIY1_56 },
            { CorsairLedId.DIY1_57 },
            { CorsairLedId.DIY1_58 },
            { CorsairLedId.DIY1_59 },
            { CorsairLedId.DIY1_60 },
            { CorsairLedId.DIY1_61 },
            { CorsairLedId.DIY1_62 },
            { CorsairLedId.DIY1_63 },
            { CorsairLedId.DIY1_64 },
            { CorsairLedId.DIY1_65 },
            { CorsairLedId.DIY1_66 },
            { CorsairLedId.DIY1_67 },
            { CorsairLedId.DIY1_68 },
            { CorsairLedId.DIY1_69 },
            { CorsairLedId.DIY1_70 },
            { CorsairLedId.DIY1_71 },
            { CorsairLedId.DIY1_72 },
            { CorsairLedId.DIY1_73 },
            { CorsairLedId.DIY1_74 },
            { CorsairLedId.DIY1_75 },
            { CorsairLedId.DIY2_51 },
            { CorsairLedId.DIY2_52 },
            { CorsairLedId.DIY2_53 },
            { CorsairLedId.DIY2_54 },
            { CorsairLedId.DIY2_55 },
            { CorsairLedId.DIY2_56 },
            { CorsairLedId.DIY2_57 },
            { CorsairLedId.DIY2_58 },
            { CorsairLedId.DIY2_59 },
            { CorsairLedId.DIY2_60 },
            { CorsairLedId.DIY2_61 },
            { CorsairLedId.DIY2_62 },
            { CorsairLedId.DIY2_63 },
            { CorsairLedId.DIY2_64 },
            { CorsairLedId.DIY2_65 },
            { CorsairLedId.DIY2_66 },
            { CorsairLedId.DIY2_67 },
            { CorsairLedId.DIY2_68 },
            { CorsairLedId.DIY2_69 },
            { CorsairLedId.DIY2_70 },
            { CorsairLedId.DIY2_71 },
            { CorsairLedId.DIY2_72 },
            { CorsairLedId.DIY2_73 },
            { CorsairLedId.DIY2_74 },
            { CorsairLedId.DIY2_75 },
            { CorsairLedId.DIY3_51 },
            { CorsairLedId.DIY3_52 },
            { CorsairLedId.DIY3_53 },
            { CorsairLedId.DIY3_54 },
            { CorsairLedId.DIY3_55 },
            { CorsairLedId.DIY3_56 },
            { CorsairLedId.DIY3_57 },
            { CorsairLedId.DIY3_58 },
            { CorsairLedId.DIY3_59 },
            { CorsairLedId.DIY3_60 },
            { CorsairLedId.DIY3_61 },
            { CorsairLedId.DIY3_62 },
            { CorsairLedId.DIY3_63 },
            { CorsairLedId.DIY3_64 },
            { CorsairLedId.DIY3_65 },
            { CorsairLedId.DIY3_66 },
            { CorsairLedId.DIY3_67 },
            { CorsairLedId.DIY3_68 },
            { CorsairLedId.DIY3_69 },
            { CorsairLedId.DIY3_70 },
            { CorsairLedId.DIY3_71 },
            { CorsairLedId.DIY3_72 },
            { CorsairLedId.DIY3_73 },
            { CorsairLedId.DIY3_74 },
            { CorsairLedId.DIY3_75 },
        };

        private readonly List<CorsairLedId> _diyZ4 = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_76 },
            { CorsairLedId.DIY1_77 },
            { CorsairLedId.DIY1_78 },
            { CorsairLedId.DIY1_79 },
            { CorsairLedId.DIY1_80 },
            { CorsairLedId.DIY1_81 },
            { CorsairLedId.DIY1_82 },
            { CorsairLedId.DIY1_83 },
            { CorsairLedId.DIY1_84 },
            { CorsairLedId.DIY1_85 },
            { CorsairLedId.DIY1_86 },
            { CorsairLedId.DIY1_87 },
            { CorsairLedId.DIY1_88 },
            { CorsairLedId.DIY1_89 },
            { CorsairLedId.DIY1_90 },
            { CorsairLedId.DIY1_91 },
            { CorsairLedId.DIY1_92 },
            { CorsairLedId.DIY1_93 },
            { CorsairLedId.DIY1_94 },
            { CorsairLedId.DIY1_95 },
            { CorsairLedId.DIY1_96 },
            { CorsairLedId.DIY1_97 },
            { CorsairLedId.DIY1_98 },
            { CorsairLedId.DIY1_99 },
            { CorsairLedId.DIY1_100 },
            { CorsairLedId.DIY2_76 },
            { CorsairLedId.DIY2_77 },
            { CorsairLedId.DIY2_78 },
            { CorsairLedId.DIY2_79 },
            { CorsairLedId.DIY2_80 },
            { CorsairLedId.DIY2_81 },
            { CorsairLedId.DIY2_82 },
            { CorsairLedId.DIY2_83 },
            { CorsairLedId.DIY2_84 },
            { CorsairLedId.DIY2_85 },
            { CorsairLedId.DIY2_86 },
            { CorsairLedId.DIY2_87 },
            { CorsairLedId.DIY2_88 },
            { CorsairLedId.DIY2_89 },
            { CorsairLedId.DIY2_90 },
            { CorsairLedId.DIY2_91 },
            { CorsairLedId.DIY2_92 },
            { CorsairLedId.DIY2_93 },
            { CorsairLedId.DIY2_94 },
            { CorsairLedId.DIY2_95 },
            { CorsairLedId.DIY2_96 },
            { CorsairLedId.DIY2_97 },
            { CorsairLedId.DIY2_98 },
            { CorsairLedId.DIY2_99 },
            { CorsairLedId.DIY2_100 },
            { CorsairLedId.DIY3_76 },
            { CorsairLedId.DIY3_77 },
            { CorsairLedId.DIY3_78 },
            { CorsairLedId.DIY3_79 },
            { CorsairLedId.DIY3_80 },
            { CorsairLedId.DIY3_81 },
            { CorsairLedId.DIY3_82 },
            { CorsairLedId.DIY3_83 },
            { CorsairLedId.DIY3_84 },
            { CorsairLedId.DIY3_85 },
            { CorsairLedId.DIY3_86 },
            { CorsairLedId.DIY3_87 },
            { CorsairLedId.DIY3_88 },
            { CorsairLedId.DIY3_89 },
            { CorsairLedId.DIY3_90 },
            { CorsairLedId.DIY3_91 },
            { CorsairLedId.DIY3_92 },
            { CorsairLedId.DIY3_93 },
            { CorsairLedId.DIY3_94 },
            { CorsairLedId.DIY3_95 },
            { CorsairLedId.DIY3_96 },
            { CorsairLedId.DIY3_97 },
            { CorsairLedId.DIY3_98 },
            { CorsairLedId.DIY3_99 },
            { CorsairLedId.DIY3_100 },
        };

        private readonly List<CorsairLedId> _diyZ5 = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_101 },
            { CorsairLedId.DIY1_102 },
            { CorsairLedId.DIY1_103 },
            { CorsairLedId.DIY1_104 },
            { CorsairLedId.DIY1_105 },
            { CorsairLedId.DIY1_106 },
            { CorsairLedId.DIY1_107 },
            { CorsairLedId.DIY1_108 },
            { CorsairLedId.DIY1_109 },
            { CorsairLedId.DIY1_110 },
            { CorsairLedId.DIY1_111 },
            { CorsairLedId.DIY1_112 },
            { CorsairLedId.DIY1_113 },
            { CorsairLedId.DIY1_114 },
            { CorsairLedId.DIY1_115 },
            { CorsairLedId.DIY1_116 },
            { CorsairLedId.DIY1_117 },
            { CorsairLedId.DIY1_118 },
            { CorsairLedId.DIY1_119 },
            { CorsairLedId.DIY1_120 },
            { CorsairLedId.DIY1_121 },
            { CorsairLedId.DIY1_122 },
            { CorsairLedId.DIY1_123 },
            { CorsairLedId.DIY1_124 },
            { CorsairLedId.DIY1_125 },
            { CorsairLedId.DIY2_101 },
            { CorsairLedId.DIY2_102 },
            { CorsairLedId.DIY2_103 },
            { CorsairLedId.DIY2_104 },
            { CorsairLedId.DIY2_105 },
            { CorsairLedId.DIY2_106 },
            { CorsairLedId.DIY2_107 },
            { CorsairLedId.DIY2_108 },
            { CorsairLedId.DIY2_109 },
            { CorsairLedId.DIY2_110 },
            { CorsairLedId.DIY2_111 },
            { CorsairLedId.DIY2_112 },
            { CorsairLedId.DIY2_113 },
            { CorsairLedId.DIY2_114 },
            { CorsairLedId.DIY2_115 },
            { CorsairLedId.DIY2_116 },
            { CorsairLedId.DIY2_117 },
            { CorsairLedId.DIY2_118 },
            { CorsairLedId.DIY2_119 },
            { CorsairLedId.DIY2_120 },
            { CorsairLedId.DIY2_121 },
            { CorsairLedId.DIY2_122 },
            { CorsairLedId.DIY2_123 },
            { CorsairLedId.DIY2_124 },
            { CorsairLedId.DIY2_125 },
            { CorsairLedId.DIY3_101 },
            { CorsairLedId.DIY3_102 },
            { CorsairLedId.DIY3_103 },
            { CorsairLedId.DIY3_104 },
            { CorsairLedId.DIY3_105 },
            { CorsairLedId.DIY3_106 },
            { CorsairLedId.DIY3_107 },
            { CorsairLedId.DIY3_108 },
            { CorsairLedId.DIY3_109 },
            { CorsairLedId.DIY3_110 },
            { CorsairLedId.DIY3_111 },
            { CorsairLedId.DIY3_112 },
            { CorsairLedId.DIY3_113 },
            { CorsairLedId.DIY3_114 },
            { CorsairLedId.DIY3_115 },
            { CorsairLedId.DIY3_116 },
            { CorsairLedId.DIY3_117 },
            { CorsairLedId.DIY3_118 },
            { CorsairLedId.DIY3_119 },
            { CorsairLedId.DIY3_120 },
            { CorsairLedId.DIY3_121 },
            { CorsairLedId.DIY3_122 },
            { CorsairLedId.DIY3_123 },
            { CorsairLedId.DIY3_124 },
            { CorsairLedId.DIY3_125 },
        };

        private readonly List<CorsairLedId> _diyZ6 = new List<CorsairLedId>
        {
            { CorsairLedId.DIY1_126 },
            { CorsairLedId.DIY1_127 },
            { CorsairLedId.DIY1_128 },
            { CorsairLedId.DIY1_129 },
            { CorsairLedId.DIY1_130 },
            { CorsairLedId.DIY1_131 },
            { CorsairLedId.DIY1_132 },
            { CorsairLedId.DIY1_133 },
            { CorsairLedId.DIY1_134 },
            { CorsairLedId.DIY1_135 },
            { CorsairLedId.DIY1_136 },
            { CorsairLedId.DIY1_137 },
            { CorsairLedId.DIY1_138 },
            { CorsairLedId.DIY1_139 },
            { CorsairLedId.DIY1_140 },
            { CorsairLedId.DIY1_141 },
            { CorsairLedId.DIY1_142 },
            { CorsairLedId.DIY1_143 },
            { CorsairLedId.DIY1_144 },
            { CorsairLedId.DIY1_145 },
            { CorsairLedId.DIY1_146 },
            { CorsairLedId.DIY1_147 },
            { CorsairLedId.DIY1_148 },
            { CorsairLedId.DIY1_149 },
            { CorsairLedId.DIY1_150 },
            { CorsairLedId.DIY2_126 },
            { CorsairLedId.DIY2_127 },
            { CorsairLedId.DIY2_128 },
            { CorsairLedId.DIY2_129 },
            { CorsairLedId.DIY2_130 },
            { CorsairLedId.DIY2_131 },
            { CorsairLedId.DIY2_132 },
            { CorsairLedId.DIY2_133 },
            { CorsairLedId.DIY2_134 },
            { CorsairLedId.DIY2_135 },
            { CorsairLedId.DIY2_136 },
            { CorsairLedId.DIY2_137 },
            { CorsairLedId.DIY2_138 },
            { CorsairLedId.DIY2_139 },
            { CorsairLedId.DIY2_140 },
            { CorsairLedId.DIY2_141 },
            { CorsairLedId.DIY2_142 },
            { CorsairLedId.DIY2_143 },
            { CorsairLedId.DIY2_144 },
            { CorsairLedId.DIY2_145 },
            { CorsairLedId.DIY2_146 },
            { CorsairLedId.DIY2_147 },
            { CorsairLedId.DIY2_148 },
            { CorsairLedId.DIY2_149 },
            { CorsairLedId.DIY2_150 },
            { CorsairLedId.DIY3_126 },
            { CorsairLedId.DIY3_127 },
            { CorsairLedId.DIY3_128 },
            { CorsairLedId.DIY3_129 },
            { CorsairLedId.DIY3_130 },
            { CorsairLedId.DIY3_131 },
            { CorsairLedId.DIY3_132 },
            { CorsairLedId.DIY3_133 },
            { CorsairLedId.DIY3_134 },
            { CorsairLedId.DIY3_135 },
            { CorsairLedId.DIY3_136 },
            { CorsairLedId.DIY3_137 },
            { CorsairLedId.DIY3_138 },
            { CorsairLedId.DIY3_139 },
            { CorsairLedId.DIY3_140 },
            { CorsairLedId.DIY3_141 },
            { CorsairLedId.DIY3_142 },
            { CorsairLedId.DIY3_143 },
            { CorsairLedId.DIY3_144 },
            { CorsairLedId.DIY3_145 },
            { CorsairLedId.DIY3_146 },
            { CorsairLedId.DIY3_147 },
            { CorsairLedId.DIY3_148 },
            { CorsairLedId.DIY3_149 },
            { CorsairLedId.DIY3_150 },
        };

        private readonly List<CorsairLedId> _coolerZ1 = new List<CorsairLedId>
        {
            { CorsairLedId.Cooler_1 },
            { CorsairLedId.Cooler_2 },
            { CorsairLedId.Cooler_3 },
            { CorsairLedId.Cooler_4 },
            { CorsairLedId.Cooler_5 },
            { CorsairLedId.Cooler_6 },
            { CorsairLedId.Cooler_7 },
            { CorsairLedId.Cooler_8 },
            { CorsairLedId.Cooler_9 },
            { CorsairLedId.Cooler_10 },
            { CorsairLedId.Cooler_11 },
            { CorsairLedId.Cooler_12 },
            { CorsairLedId.Cooler_13 },
            { CorsairLedId.Cooler_14 },
            { CorsairLedId.Cooler_15 },
            { CorsairLedId.Cooler_16 },
            { CorsairLedId.Cooler_17 },
            { CorsairLedId.Cooler_18 },
            { CorsairLedId.Cooler_19 },
            { CorsairLedId.Cooler_20 },
            { CorsairLedId.Cooler_21 },
            { CorsairLedId.Cooler_22 },
            { CorsairLedId.Cooler_23 },
            { CorsairLedId.Cooler_24 },
            { CorsairLedId.Cooler_25 }
        };

        private readonly List<CorsairLedId> _coolerZ2 = new List<CorsairLedId>
        {
            { CorsairLedId.Cooler_26 },
            { CorsairLedId.Cooler_27 },
            { CorsairLedId.Cooler_28 },
            { CorsairLedId.Cooler_29 },
            { CorsairLedId.Cooler_30 },
            { CorsairLedId.Cooler_31 },
            { CorsairLedId.Cooler_32 },
            { CorsairLedId.Cooler_33 },
            { CorsairLedId.Cooler_34 },
            { CorsairLedId.Cooler_35 },
            { CorsairLedId.Cooler_36 },
            { CorsairLedId.Cooler_37 },
            { CorsairLedId.Cooler_38 },
            { CorsairLedId.Cooler_39 },
            { CorsairLedId.Cooler_40 },
            { CorsairLedId.Cooler_41 },
            { CorsairLedId.Cooler_42 },
            { CorsairLedId.Cooler_43 },
            { CorsairLedId.Cooler_44 },
            { CorsairLedId.Cooler_45 },
            { CorsairLedId.Cooler_46 },
            { CorsairLedId.Cooler_47 },
            { CorsairLedId.Cooler_48 },
            { CorsairLedId.Cooler_49 },
            { CorsairLedId.Cooler_50 }
        };

        private readonly List<CorsairLedId> _coolerZ3 = new List<CorsairLedId>
        {
            { CorsairLedId.Cooler_51 },
            { CorsairLedId.Cooler_52 },
            { CorsairLedId.Cooler_53 },
            { CorsairLedId.Cooler_54 },
            { CorsairLedId.Cooler_55 },
            { CorsairLedId.Cooler_56 },
            { CorsairLedId.Cooler_57 },
            { CorsairLedId.Cooler_58 },
            { CorsairLedId.Cooler_59 },
            { CorsairLedId.Cooler_60 },
            { CorsairLedId.Cooler_61 },
            { CorsairLedId.Cooler_62 },
            { CorsairLedId.Cooler_63 },
            { CorsairLedId.Cooler_64 },
            { CorsairLedId.Cooler_65 },
            { CorsairLedId.Cooler_66 },
            { CorsairLedId.Cooler_67 },
            { CorsairLedId.Cooler_68 },
            { CorsairLedId.Cooler_69 },
            { CorsairLedId.Cooler_70 },
            { CorsairLedId.Cooler_71 },
            { CorsairLedId.Cooler_72 },
            { CorsairLedId.Cooler_73 },
            { CorsairLedId.Cooler_74 },
            { CorsairLedId.Cooler_75 }
        };

        private readonly List<CorsairLedId> _coolerZ4 = new List<CorsairLedId>
        {
            { CorsairLedId.Cooler_76 },
            { CorsairLedId.Cooler_77 },
            { CorsairLedId.Cooler_78 },
            { CorsairLedId.Cooler_79 },
            { CorsairLedId.Cooler_80 },
            { CorsairLedId.Cooler_81 },
            { CorsairLedId.Cooler_82 },
            { CorsairLedId.Cooler_83 },
            { CorsairLedId.Cooler_84 },
            { CorsairLedId.Cooler_85 },
            { CorsairLedId.Cooler_86 },
            { CorsairLedId.Cooler_87 },
            { CorsairLedId.Cooler_88 },
            { CorsairLedId.Cooler_89 },
            { CorsairLedId.Cooler_90 },
            { CorsairLedId.Cooler_91 },
            { CorsairLedId.Cooler_92 },
            { CorsairLedId.Cooler_93 },
            { CorsairLedId.Cooler_94 },
            { CorsairLedId.Cooler_95 },
            { CorsairLedId.Cooler_96 },
            { CorsairLedId.Cooler_97 },
            { CorsairLedId.Cooler_98 },
            { CorsairLedId.Cooler_99 },
            { CorsairLedId.Cooler_100 }
        };

        private readonly List<CorsairLedId> _coolerZ5 = new List<CorsairLedId>
        {
            { CorsairLedId.Cooler_101 },
            { CorsairLedId.Cooler_102 },
            { CorsairLedId.Cooler_103 },
            { CorsairLedId.Cooler_104 },
            { CorsairLedId.Cooler_105 },
            { CorsairLedId.Cooler_106 },
            { CorsairLedId.Cooler_107 },
            { CorsairLedId.Cooler_108 },
            { CorsairLedId.Cooler_109 },
            { CorsairLedId.Cooler_110 },
            { CorsairLedId.Cooler_111 },
            { CorsairLedId.Cooler_112 },
            { CorsairLedId.Cooler_113 },
            { CorsairLedId.Cooler_114 },
            { CorsairLedId.Cooler_115 },
            { CorsairLedId.Cooler_116 },
            { CorsairLedId.Cooler_117 },
            { CorsairLedId.Cooler_118 },
            { CorsairLedId.Cooler_119 },
            { CorsairLedId.Cooler_120 },
            { CorsairLedId.Cooler_121 },
            { CorsairLedId.Cooler_122 },
            { CorsairLedId.Cooler_123 },
            { CorsairLedId.Cooler_124 },
            { CorsairLedId.Cooler_125 }
        };

        private readonly List<CorsairLedId> _coolerZ6 = new List<CorsairLedId>
        {
            { CorsairLedId.Cooler_126 },
            { CorsairLedId.Cooler_127 },
            { CorsairLedId.Cooler_128 },
            { CorsairLedId.Cooler_129 },
            { CorsairLedId.Cooler_130 },
            { CorsairLedId.Cooler_131 },
            { CorsairLedId.Cooler_132 },
            { CorsairLedId.Cooler_133 },
            { CorsairLedId.Cooler_134 },
            { CorsairLedId.Cooler_135 },
            { CorsairLedId.Cooler_136 },
            { CorsairLedId.Cooler_137 },
            { CorsairLedId.Cooler_138 },
            { CorsairLedId.Cooler_139 },
            { CorsairLedId.Cooler_140 },
            { CorsairLedId.Cooler_141 },
            { CorsairLedId.Cooler_142 },
            { CorsairLedId.Cooler_143 },
            { CorsairLedId.Cooler_144 },
            { CorsairLedId.Cooler_145 },
            { CorsairLedId.Cooler_146 },
            { CorsairLedId.Cooler_147 },
            { CorsairLedId.Cooler_148 },
            { CorsairLedId.Cooler_149 },
            { CorsairLedId.Cooler_150 }
        };

        private readonly Dictionary<string, CorsairLedId> _corsairkeyids = new Dictionary<string, CorsairLedId>
        {
            //Keys
            {"F1", CorsairLedId.F1},
            {"F2", CorsairLedId.F2},
            {"F3", CorsairLedId.F3},
            {"F4", CorsairLedId.F4},
            {"F5", CorsairLedId.F5},
            {"F6", CorsairLedId.F6},
            {"F7", CorsairLedId.F7},
            {"F8", CorsairLedId.F8},
            {"F9", CorsairLedId.F9},
            {"F10", CorsairLedId.F10},
            {"F11", CorsairLedId.F11},
            {"F12", CorsairLedId.F12},
            {"D1", CorsairLedId.D1},
            {"D2", CorsairLedId.D2},
            {"D3", CorsairLedId.D3},
            {"D4", CorsairLedId.D4},
            {"D5", CorsairLedId.D5},
            {"D6", CorsairLedId.D6},
            {"D7", CorsairLedId.D7},
            {"D8", CorsairLedId.D8},
            {"D9", CorsairLedId.D9},
            {"D0", CorsairLedId.D0},
            {"A", CorsairLedId.A},
            {"B", CorsairLedId.B},
            {"C", CorsairLedId.C},
            {"D", CorsairLedId.D},
            {"E", CorsairLedId.E},
            {"F", CorsairLedId.F},
            {"G", CorsairLedId.G},
            {"H", CorsairLedId.H},
            {"I", CorsairLedId.I},
            {"J", CorsairLedId.J},
            {"K", CorsairLedId.K},
            {"L", CorsairLedId.L},
            {"M", CorsairLedId.M},
            {"N", CorsairLedId.N},
            {"O", CorsairLedId.O},
            {"P", CorsairLedId.P},
            {"Q", CorsairLedId.Q},
            {"R", CorsairLedId.R},
            {"S", CorsairLedId.S},
            {"T", CorsairLedId.T},
            {"U", CorsairLedId.U},
            {"V", CorsairLedId.V},
            {"W", CorsairLedId.W},
            {"X", CorsairLedId.X},
            {"Y", CorsairLedId.Y},
            {"Z", CorsairLedId.Z},
            {"NumLock", CorsairLedId.NumLock},
            {"Num0", CorsairLedId.Keypad0},
            {"Num1", CorsairLedId.Keypad1},
            {"Num2", CorsairLedId.Keypad2},
            {"Num3", CorsairLedId.Keypad3},
            {"Num4", CorsairLedId.Keypad4},
            {"Num5", CorsairLedId.Keypad5},
            {"Num6", CorsairLedId.Keypad6},
            {"Num7", CorsairLedId.Keypad7},
            {"Num8", CorsairLedId.Keypad8},
            {"Num9", CorsairLedId.Keypad9},
            {"NumDivide", CorsairLedId.KeypadSlash},
            {"NumMultiply", CorsairLedId.KeypadAsterisk},
            {"NumSubtract", CorsairLedId.KeypadMinus},
            {"NumAdd", CorsairLedId.KeypadPlus},
            {"NumEnter", CorsairLedId.KeypadEnter},
            {"NumDecimal", CorsairLedId.KeypadPeriodAndDelete},
            {"PrintScreen", CorsairLedId.PrintScreen},
            {"Scroll", CorsairLedId.ScrollLock},
            {"Pause", CorsairLedId.PauseBreak},
            {"Insert", CorsairLedId.Insert},
            {"Home", CorsairLedId.Home},
            {"PageUp", CorsairLedId.PageUp},
            {"PageDown", CorsairLedId.PageDown},
            {"Delete", CorsairLedId.Delete},
            {"End", CorsairLedId.End},
            {"Up", CorsairLedId.UpArrow},
            {"Left", CorsairLedId.LeftArrow},
            {"Right", CorsairLedId.RightArrow},
            {"Down", CorsairLedId.DownArrow},
            {"Tab", CorsairLedId.Tab},
            {"CapsLock", CorsairLedId.CapsLock},
            {"Backspace", CorsairLedId.Backspace},
            {"Enter", CorsairLedId.Enter},
            {"LeftControl", CorsairLedId.LeftCtrl},
            {"LeftWindows", CorsairLedId.WinLock},
            {"LeftAlt", CorsairLedId.LeftAlt},
            {"Space", CorsairLedId.Space},
            {"RightControl", CorsairLedId.RightCtrl},
            {"Function", CorsairLedId.LeftGui},
            {"RightAlt", CorsairLedId.RightAlt},
            {"RightMenu", CorsairLedId.RightGui},
            {"LeftShift", CorsairLedId.LeftShift},
            {"RightShift", CorsairLedId.RightShift},
            {"Macro1", CorsairLedId.G1}, //G1
            {"Macro2", CorsairLedId.G2}, //G2
            {"Macro3", CorsairLedId.G3}, //G3
            {"Macro4", CorsairLedId.G4}, //G4
            {"Macro5", CorsairLedId.G5}, //G5
            {"Macro6", CorsairLedId.G6}, //G6
            {"Macro7", CorsairLedId.G7}, //G7
            {"Macro8", CorsairLedId.G8}, //G8
            {"Macro9", CorsairLedId.G9}, //G9
            {"Macro10", CorsairLedId.G10}, //G10
            {"Macro11", CorsairLedId.G11}, //G11
            {"Macro12", CorsairLedId.G12}, //G12
            {"Macro13", CorsairLedId.G13}, //G13
            {"Macro14", CorsairLedId.G14}, //G14
            {"Macro15", CorsairLedId.G15}, //G15
            {"Macro16", CorsairLedId.G16}, //G16
            {"Macro17", CorsairLedId.G17}, //G17
            {"Macro18", CorsairLedId.G18}, //G18
            {"OemTilde", CorsairLedId.GraveAccentAndTilde},
            {"OemMinus", CorsairLedId.MinusAndUnderscore},
            {"OemEquals", CorsairLedId.EqualsAndPlus},
            {"OemLeftBracket", CorsairLedId.BracketLeft},
            {"OemRightBracket", CorsairLedId.BracketRight},
            {"OemSlash", CorsairLedId.SlashAndQuestionMark},
            {"OemSemicolon", CorsairLedId.SemicolonAndColon},
            {"OemApostrophe", CorsairLedId.ApostropheAndDoubleQuote},
            {"OemComma", CorsairLedId.CommaAndLessThan},
            {"OemPeriod", CorsairLedId.PeriodAndBiggerThan},
            {"OemBackslash", CorsairLedId.Backslash},
            {"EurPound", CorsairLedId.International1},
            {"JpnYen", CorsairLedId.International2},
            {"Escape", CorsairLedId.Escape},
            {"MouseFront", CorsairLedId.B2},
            {"MouseScroll", CorsairLedId.B3},
            {"MouseSide", CorsairLedId.B4},
            {"MouseLogo", CorsairLedId.B1},
            {"Pad1", CorsairLedId.Zone1},
            {"Pad2", CorsairLedId.Zone2},
            {"Pad3", CorsairLedId.Zone3},
            {"Pad4", CorsairLedId.Zone4},
            {"Pad5", CorsairLedId.Zone5},
            {"Pad6", CorsairLedId.Zone6},
            {"Pad7", CorsairLedId.Zone7},
            {"Pad8", CorsairLedId.Zone8},
            {"Pad9", CorsairLedId.Zone9},
            {"Pad10", CorsairLedId.Zone10},
            {"Pad11", CorsairLedId.Zone11},
            {"Pad12", CorsairLedId.Zone12},
            {"Pad13", CorsairLedId.Zone13},
            {"Pad14", CorsairLedId.Zone14},
            {"Pad15", CorsairLedId.Zone15},
            {"Strip1", CorsairLedId.Invalid},
            {"Strip2", CorsairLedId.Invalid},
            {"Strip3", CorsairLedId.Invalid},
            {"Strip4", CorsairLedId.Invalid},
            {"Strip5", CorsairLedId.Invalid},
            {"Strip6", CorsairLedId.Invalid},
            {"Strip7", CorsairLedId.Invalid},
            {"Strip8", CorsairLedId.Invalid},
            {"Strip9", CorsairLedId.Invalid},
            {"Strip10", CorsairLedId.Invalid},
            {"Strip11", CorsairLedId.Invalid},
            {"Strip12", CorsairLedId.Invalid},
            {"Strip13", CorsairLedId.Invalid},
            {"Strip14", CorsairLedId.Invalid},
            {"Lightbar1", CorsairLedId.Lightbar1},
            {"Lightbar2", CorsairLedId.Lightbar2},
            {"Lightbar3", CorsairLedId.Lightbar3},
            {"Lightbar4", CorsairLedId.Lightbar4},
            {"Lightbar5", CorsairLedId.Lightbar5},
            {"Lightbar6", CorsairLedId.Lightbar6},
            {"Lightbar7", CorsairLedId.Lightbar7},
            {"Lightbar8", CorsairLedId.Lightbar8},
            {"Lightbar9", CorsairLedId.Lightbar9},
            {"Lightbar10", CorsairLedId.Lightbar10},
            {"Lightbar11", CorsairLedId.Lightbar11},
            {"Lightbar12", CorsairLedId.Lightbar12},
            {"Lightbar13", CorsairLedId.Lightbar13},
            {"Lightbar14", CorsairLedId.Lightbar14},
            {"Lightbar15", CorsairLedId.Lightbar15},
            {"Lightbar16", CorsairLedId.Lightbar16},
            {"Lightbar17", CorsairLedId.Lightbar17},
            {"Lightbar18", CorsairLedId.Lightbar18},
            {"Lightbar19", CorsairLedId.Lightbar19},
            {"HeadsetStandZone1", CorsairLedId.HeadsetStandZone1},
            {"HeadsetStandZone2", CorsairLedId.HeadsetStandZone2},
            {"HeadsetStandZone3", CorsairLedId.HeadsetStandZone3},
            {"HeadsetStandZone4", CorsairLedId.HeadsetStandZone4},
            {"HeadsetStandZone5", CorsairLedId.HeadsetStandZone5},
            {"HeadsetStandZone6", CorsairLedId.HeadsetStandZone6},
            {"HeadsetStandZone7", CorsairLedId.HeadsetStandZone7},
            {"HeadsetStandZone8", CorsairLedId.HeadsetStandZone8},
            {"HeadsetStandZone9", CorsairLedId.HeadsetStandZone9},
        };

        #endregion

        private ListLedGroup _corsairAllHeadsetLed;

        //Define Corsair LED Groups
        private ListLedGroup _corsairAllKeyboardLed;

        private ListLedGroup _corsairAllMouseLed;
        private ListLedGroup _corsairAllMousepadLed;
        private ListLedGroup _corsairAllStandLed;
        private bool _corsairFlash2Running;
        private int _corsairFlash2Step;
        private bool _corsairFlash4Running;
        private int _corsairFlash4Step;

        private KeyMapBrush _corsairKeyboardIndvBrush;
        private ListLedGroup _corsairKeyboardIndvLed;
        private KeyMapBrush _corsairMouseIndvBrush;
        private ListLedGroup _corsairMouseIndvLed;
        private KeyMapBrush _corsairMousepadIndvBrush;
        private ListLedGroup _corsairMousepadIndvLed;
        private KeyMapBrush _corsairStandIndvBrush;
        private ListLedGroup _corsairStandIndvLed;

        private bool _corsairDeviceHeadset = true;
        private bool _corsairDeviceKeyboard = true;
        private bool _corsairDeviceKeypad = true;
        private bool _corsairDeviceMouse = true;
        private bool _corsairDeviceMousepad = true;
        private bool _corsairDeviceStand = true;
        private bool _corsairOtherDevices = true;

        private Color _corsairLogo;
        private Color _corsairLogoConv;
        private Color _corsairScrollWheel;
        private Color _corsairScrollWheelConv;
        private Dictionary<string, Color> presets = new Dictionary<string, Color>();

        private bool pause;

        public bool InitializeSdk()
        {
            try
            {
                CueSDK.Initialize();

                _corsairKeyboardIndvBrush = new KeyMapBrush();
                _corsairKeyboardIndvLed = new ListLedGroup(CueSDK.KeyboardSDK, CueSDK.KeyboardSDK);
                _corsairAllKeyboardLed = new ListLedGroup(CueSDK.KeyboardSDK, CueSDK.KeyboardSDK);
                _corsairAllKeyboardLed.ZIndex = 1;
                _corsairKeyboardIndvLed.ZIndex = 10;
                _corsairKeyboardIndvLed.Brush = _corsairKeyboardIndvBrush;
                _corsairAllKeyboardLed.Brush = (SolidColorBrush) Color.Black;
                

                _corsairMouseIndvBrush = new KeyMapBrush();
                _corsairMouseIndvLed = new ListLedGroup(CueSDK.MouseSDK, CueSDK.MouseSDK);
                _corsairAllMouseLed = new ListLedGroup(CueSDK.MouseSDK, CueSDK.MouseSDK);
                _corsairAllMouseLed.ZIndex = 1;
                _corsairMouseIndvLed.ZIndex = 10;
                _corsairMouseIndvLed.Brush = _corsairMouseIndvBrush;
                _corsairAllMouseLed.Brush = (SolidColorBrush) Color.Black;

                _corsairStandIndvBrush = new KeyMapBrush();
                _corsairStandIndvLed = new ListLedGroup(CueSDK.HeadsetStandSDK, CueSDK.HeadsetStandSDK);
                _corsairAllStandLed = new ListLedGroup(CueSDK.HeadsetStandSDK, CueSDK.HeadsetStandSDK);
                _corsairAllStandLed.ZIndex = 1;
                _corsairStandIndvLed.ZIndex = 10;
                _corsairStandIndvLed.Brush = _corsairStandIndvBrush;
                _corsairAllStandLed.Brush = (SolidColorBrush) Color.Black;

                _corsairMousepadIndvBrush = new KeyMapBrush();
                _corsairMousepadIndvLed = new ListLedGroup(CueSDK.MousematSDK, CueSDK.MousematSDK);
                _corsairAllMousepadLed = new ListLedGroup(CueSDK.MousematSDK, CueSDK.MousematSDK);
                _corsairAllMousepadLed.ZIndex = 1;
                _corsairMousepadIndvLed.ZIndex = 10;
                _corsairMousepadIndvLed.Brush = _corsairMousepadIndvBrush;
                _corsairAllMousepadLed.Brush = (SolidColorBrush) Color.Black;

                _corsairAllHeadsetLed = new ListLedGroup(CueSDK.HeadsetSDK, CueSDK.HeadsetSDK);
                _corsairAllHeadsetLed.ZIndex = 1;
                _corsairAllHeadsetLed.Brush = (SolidColorBrush) Color.Black;

                var corsairver = CueSDK.ProtocolDetails.ServerVersion.Split('.');
                var cV = int.Parse(corsairver[0]);

                if (cV < 2)
                {
                    Write.WriteConsole(ConsoleTypes.Error,
                        "Corsair device support requires CUE2 or iCUE Version 2.0.0 or higher to operate. Please download the latest version of CUE2 or iCUE from the Corsair website.");
                    return false;
                }

                Write.WriteConsole(ConsoleTypes.Corsair,
                    "CUE SDK Loaded (" + CueSDK.ProtocolDetails.SdkVersion + "/" +
                    CueSDK.ProtocolDetails.ServerVersion + ")");
                

                CueSDK.UpdateMode = UpdateMode.Continuous;
                //ResetCorsairDevices();

                if (_corsairDeviceHeadset && !string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model))
                {
                    try
                    {
                        _corsairDeviceHeadset = CueSDK.HeadsetSDK.DeviceInfo.Model != "VOID Wireless Demo";
                    }
                    catch (Exception e)
                    {
                        _corsairDeviceHeadset = false;
                        Console.WriteLine(e.InnerException);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Corsair, @"CUE SDK failed to load. EX: " + ex.Message);
                return false;
            }
        }

        public void ShutdownSdk()
        {
            try
            {
                if (CueSDK.IsInitialized)
                {
                    CueSDK.Reinitialize();
                }
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Corsair, @"CUE SDK failed to load. EX: " + ex.Message);
                throw;
            }
            
        }

        public void ResetCorsairDevices(bool deviceKeyboard, bool deviceKeypad, bool deviceMouse, bool deviceMousepad,
            bool deviceHeadset, bool deviceOtherDevices, Color basecol)
        {
            pause = true;
            
            if (_corsairDeviceKeyboard && !deviceKeyboard)
                _corsairAllKeyboardLed.Brush = (SolidColorBrush)basecol;

            if (_corsairDeviceHeadset && !deviceHeadset)
                _corsairAllHeadsetLed.Brush = (SolidColorBrush)basecol;
            
            if (_corsairDeviceMouse && !deviceMouse)
                _corsairAllMouseLed.Brush = (SolidColorBrush)basecol;

            if (_corsairDeviceMousepad && !deviceMousepad)
                _corsairAllMousepadLed.Brush = (SolidColorBrush)basecol;

            if (_corsairDeviceHeadset)
                _corsairAllStandLed.Brush = (SolidColorBrush)basecol;

            _corsairDeviceKeyboard = deviceKeyboard;
            _corsairDeviceKeypad = deviceKeypad;
            _corsairDeviceMouse = deviceMouse;
            _corsairDeviceMousepad = deviceMousepad;
            _corsairDeviceHeadset = deviceHeadset;
            _corsairDeviceStand = true;
            _corsairOtherDevices = deviceOtherDevices;

            if (_corsairDeviceHeadset)
            {
                try
                {
                    _corsairDeviceHeadset = CueSDK.HeadsetSDK?.DeviceInfo?.Model != "VOID Wireless Demo";
                }
                catch (Exception e)
                {
                    _corsairDeviceHeadset = false;
                    Console.WriteLine(e.InnerException);
                }
            }
            //UpdateState("static", basecol, false);
            pause = false;
        }


        public void CorsairUpdateLed()
        {
            if (pause) return;

            if (_corsairDeviceHeadset && !string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model)) CueSDK.HeadsetSDK.Update();
            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model)) CueSDK.KeyboardSDK.Update();
            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model)) CueSDK.MouseSDK.Update();
            if (_corsairDeviceMousepad && !string.IsNullOrEmpty(CueSDK.MousematSDK?.MousematDeviceInfo?.Model)) CueSDK.MousematSDK.Update();
            if (_corsairDeviceStand && !string.IsNullOrEmpty(CueSDK.HeadsetStandSDK?.HeadsetStandDeviceInfo?.Model)) CueSDK.HeadsetStandSDK.Update();

            if (_corsairOtherDevices && !string.IsNullOrEmpty(CueSDK.CommanderProSDK?.CommanderProDeviceInfo?.Model)) CueSDK.CommanderProSDK.Update();
            if (_corsairOtherDevices && !string.IsNullOrEmpty(CueSDK.LightingNodeProSDK?.LightingNodeProDeviceInfo?.Model)) CueSDK.LightingNodeProSDK.Update();
            if (_corsairOtherDevices && !string.IsNullOrEmpty(CueSDK.MemoryModuleSDK?.MemoryModuleDeviceInfo?.Model)) CueSDK.MemoryModuleSDK.Update();
            if (_corsairOtherDevices && !string.IsNullOrEmpty(CueSDK.CoolerSDK?.CoolerDeviceInfo?.Model)) CueSDK.CoolerSDK.Update();
        }

        public void SetAllLights(Color color)
        {
            if (pause) return;

            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
            {
                _corsairAllKeyboardLed.Brush = (SolidColorBrush)color;
                CueSDK.KeyboardSDK.Update(true);
            }

            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
            {
                _corsairAllMouseLed.Brush = (SolidColorBrush)color;
                CueSDK.MouseSDK.Update(true);
            }

            if (_corsairDeviceMousepad && !string.IsNullOrEmpty(CueSDK.MousematSDK?.MousematDeviceInfo?.Model))
            {
                _corsairAllMousepadLed.Brush = (SolidColorBrush)color;
                CueSDK.MousematSDK.Update(true);
            }

            if (_corsairDeviceHeadset && !string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model))
            {
                _corsairAllHeadsetLed.Brush = (SolidColorBrush)color;
                CueSDK.HeadsetSDK.Update(true);
            }

            if (_corsairDeviceStand && !string.IsNullOrEmpty(CueSDK.HeadsetStandSDK?.HeadsetStandDeviceInfo?.Model))
            {
                _corsairAllStandLed.Brush = (SolidColorBrush)color;
                CueSDK.HeadsetStandSDK.Update(true);
            }

            if (_corsairOtherDevices)
            {
                ApplyMapOtherLighting(color, 1);
                ApplyMapOtherLighting(color, 2);
                ApplyMapOtherLighting(color, 3);
                ApplyMapOtherLighting(color, 4);
                ApplyMapOtherLighting(color, 5);
            }
        }

        public void SetLights(Color col)
        {
            if (pause) return;

            try
            {
                if (!_corsairDeviceKeyboard || string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model)) return;

                _corsairAllKeyboardLed.Brush = (SolidColorBrush) col;
                CueSDK.KeyboardSDK.Update(false);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Corsair (Static): " + ex.Message);
            }
        }

        public void ApplyMapKeyLighting(string key, Color col, bool clear, [Optional] bool bypasswhitelist)
        {
            if (pause) return;

            if (FfxivHotbar.Keybindwhitelist.Contains(key) && !bypasswhitelist)
                return;
            
            try
            {
                _corsairKeyboardIndvLed.Brush = _corsairKeyboardIndvBrush;

                if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    if (_corsairkeyids.ContainsKey(key))
                        if (CueSDK.KeyboardSDK[_corsairkeyids[key]] != null)
                            _corsairKeyboardIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[key], col);
            }
            catch (Exception ex)
            {
                Write.WriteConsole(ConsoleTypes.Error, @"Corsair (" + key + "): " + ex.Message);
                Write.WriteConsole(ConsoleTypes.Error, @"Internal Error (" + key + "): " + ex.StackTrace);
            }
        }

        public Color GetCurrentKeyColor(string key)
        {
            if (pause) return Color.Black;

            if (_corsairkeyids.ContainsKey(key))
            {
                var _key = _corsairkeyids[key];
                return _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
            }

            return Color.Black;
        }

        public void ApplyMapMouseLighting(string region, Color col, bool clear)
        {
            if (pause) return;

            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                if (_corsairkeyids.ContainsKey(region))
                    if (CueSDK.MouseSDK[_corsairkeyids[region]] != null)
                        _corsairMouseIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[region], col);
        }

        public Color GetCurrentMouseColor(string region)
        {
            if (pause) return Color.Black;

            if (_corsairkeyids.ContainsKey(region))
            {
                var _key = _corsairkeyids[region];
                return _corsairMouseIndvBrush.CorsairGetColorReference(_key);
            }

            return Color.Black;
        }

        public void ApplyMapLogoLighting(string key, Color col, bool clear)
        {
            if (pause) return;
            //Not Implemented
        }

        public void ApplyMapPadLighting(string region, Color col, bool clear)
        {
            if (pause) return;

            if (_corsairDeviceMousepad && !string.IsNullOrEmpty(CueSDK.MousematSDK?.MousematDeviceInfo?.Model))
                if (_corsairkeyids.ContainsKey(region))
                    if (CueSDK.MousematSDK[_corsairkeyids[region]] != null)
                        _corsairMousepadIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[region], col);
        }

        public Color GetCurrentPadColor(string region)
        {
            if (pause) return Color.Black;

            if (_corsairkeyids.ContainsKey(region))
            {
                var _key = _corsairkeyids[region];
                return _corsairMousepadIndvBrush.CorsairGetColorReference(_key);
            }

            return Color.Black;
        }

        public void ApplyMapStandLighting(string region, Color col, bool clear)
        {
            if (pause) return;

            if (_corsairDeviceStand && !string.IsNullOrEmpty(CueSDK.HeadsetStandSDK?.HeadsetStandDeviceInfo?.Model))
                if (_corsairkeyids.ContainsKey(region))
                    if (CueSDK.HeadsetStandSDK[_corsairkeyids[region]] != null)
                        _corsairStandIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[region], col);
        }

        public void ApplyMapHeadsetLighting(Color col, bool clear)
        {
            if (pause) return;

            if (!_corsairDeviceHeadset || string.IsNullOrEmpty(CueSDK.HeadsetSDK?.HeadsetDeviceInfo?.Model)) return;
            var cc = new CorsairColor(col);

            if (CueSDK.HeadsetSDK[CorsairLedId.LeftLogo].Color == cc) return;

            CueSDK.HeadsetSDK[CorsairLedId.LeftLogo].Color = cc;
            CueSDK.HeadsetSDK[CorsairLedId.RightLogo].Color = cc;
        }

        public void ApplyMapOtherLighting(Color col, int pos)
        {
            try
            {
                if (pause) return;

                if (!_corsairOtherDevices) return;
                var cc = new CorsairColor(col);

                
                //Commander Pro
                if (!string.IsNullOrEmpty(CueSDK.CommanderProSDK?.CommanderProDeviceInfo?.Model))
                {

                    switch (pos)
                    {
                        case 1:
                            foreach (var led in _diyZ1)
                            {
                                if (CueSDK.CommanderProSDK[led] != null)
                                {
                                    if (CueSDK.CommanderProSDK[led].Color != cc)
                                    {
                                        CueSDK.CommanderProSDK[led].Color = cc;
                                    }
                                }
                            }

                            break;
                        case 2:
                            foreach (var led in _diyZ2)
                            {
                                if (CueSDK.CommanderProSDK[led] != null)
                                {
                                    if (CueSDK.CommanderProSDK[led].Color != cc)
                                    {
                                        CueSDK.CommanderProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 3:
                            foreach (var led in _diyZ3)
                            {
                                if (CueSDK.CommanderProSDK[led] != null)
                                {
                                    if (CueSDK.CommanderProSDK[led].Color != cc)
                                    {
                                        CueSDK.CommanderProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 4:
                            foreach (var led in _diyZ4)
                            {
                                if (CueSDK.CommanderProSDK[led] != null)
                                {
                                    if (CueSDK.CommanderProSDK[led].Color != cc)
                                    {
                                        CueSDK.CommanderProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 5:
                            foreach (var led in _diyZ5)
                            {
                                if (CueSDK.CommanderProSDK[led] != null)
                                {
                                    if (CueSDK.CommanderProSDK[led].Color != cc)
                                    {
                                        CueSDK.CommanderProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 6:
                            foreach (var led in _diyZ6)
                            {
                                if (CueSDK.CommanderProSDK[led] != null)
                                {
                                    if (CueSDK.CommanderProSDK[led].Color != cc)
                                    {
                                        CueSDK.CommanderProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                    }
                }
                
                //Lighting Node
                if (!string.IsNullOrEmpty(CueSDK.LightingNodeProSDK?.LightingNodeProDeviceInfo?.Model))
                {
                    switch (pos)
                    {
                        case 1:
                            foreach (var led in _diyZ1)
                            {
                                if (CueSDK.LightingNodeProSDK[led] != null)
                                {
                                    if (CueSDK.LightingNodeProSDK[led].Color != cc)
                                    {
                                        CueSDK.LightingNodeProSDK[led].Color = cc;
                                    }
                                }
                            }

                            break;
                        case 2:
                            foreach (var led in _diyZ2)
                            {
                                if (CueSDK.LightingNodeProSDK[led] != null)
                                {
                                    if (CueSDK.LightingNodeProSDK[led].Color != cc)
                                    {
                                        CueSDK.LightingNodeProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 3:
                            foreach (var led in _diyZ3)
                            {
                                if (CueSDK.LightingNodeProSDK[led] != null)
                                {
                                    if (CueSDK.LightingNodeProSDK[led].Color != cc)
                                    {
                                        CueSDK.LightingNodeProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 4:
                            foreach (var led in _diyZ4)
                            {
                                if (CueSDK.LightingNodeProSDK[led] != null)
                                {
                                    if (CueSDK.LightingNodeProSDK[led].Color != cc)
                                    {
                                        CueSDK.LightingNodeProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 5:
                            foreach (var led in _diyZ5)
                            {
                                if (CueSDK.LightingNodeProSDK[led] != null)
                                {
                                    if (CueSDK.LightingNodeProSDK[led].Color != cc)
                                    {
                                        CueSDK.LightingNodeProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 6:
                            foreach (var led in _diyZ6)
                            {
                                if (CueSDK.LightingNodeProSDK[led] != null)
                                {
                                    if (CueSDK.LightingNodeProSDK[led].Color != cc)
                                    {
                                        CueSDK.LightingNodeProSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                    }
                }

                //Cooler
                if (!string.IsNullOrEmpty(CueSDK.CoolerSDK?.CoolerDeviceInfo?.Model))
                {
                    switch (pos)
                    {
                        case 1:
                            foreach (var led in _coolerZ1)
                            {
                                if (CueSDK.CoolerSDK[led] != null)
                                {
                                    if (CueSDK.CoolerSDK[led].Color != cc)
                                    {
                                        CueSDK.CoolerSDK[led].Color = cc;
                                    }
                                }
                            }

                            break;
                        case 2:
                            foreach (var led in _coolerZ2)
                            {
                                if (CueSDK.CoolerSDK[led] != null)
                                {
                                    if (CueSDK.CoolerSDK[led].Color != cc)
                                    {
                                        CueSDK.CoolerSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 3:
                            foreach (var led in _coolerZ3)
                            {
                                if (CueSDK.CoolerSDK[led] != null)
                                {
                                    if (CueSDK.CoolerSDK[led].Color != cc)
                                    {
                                        CueSDK.CoolerSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 4:
                            foreach (var led in _coolerZ4)
                            {
                                if (CueSDK.CoolerSDK[led] != null)
                                {
                                    if (CueSDK.CoolerSDK[led].Color != cc)
                                    {
                                        CueSDK.CoolerSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 5:
                            foreach (var led in _coolerZ5)
                            {
                                if (CueSDK.CoolerSDK[led] != null)
                                {
                                    if (CueSDK.CoolerSDK[led].Color != cc)
                                    {
                                        CueSDK.CoolerSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                        case 6:
                            foreach (var led in _coolerZ6)
                            {
                                if (CueSDK.CoolerSDK[led] != null)
                                {
                                    if (CueSDK.CoolerSDK[led].Color != cc)
                                    {
                                        CueSDK.CoolerSDK[led].Color = cc;
                                    }
                                }
                            }
                            break;
                    }
                }

                //Memory Modules
                if (!string.IsNullOrEmpty(CueSDK.MemoryModuleSDK?.MemoryModuleDeviceInfo?.Model))
                {
                    switch (pos)
                    {
                        case 1:
                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_1].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_1].Color = cc;

                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_2].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_2].Color = cc;
                            break;
                        case 2:
                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_3].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_3].Color = cc;

                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_4].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_4].Color = cc;
                            break;
                        case 3:
                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_5].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_5].Color = cc;

                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_6].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_6].Color = cc;
                            break;
                        case 4:
                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_7].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_7].Color = cc;

                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_8].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_8].Color = cc;
                            break;
                        case 5:
                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_9].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_9].Color = cc;

                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_10].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_10].Color = cc;
                            break;
                        case 6:
                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_11].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_11].Color = cc;

                            if (CueSDK.MemoryModuleSDK[CorsairLedId.RAM_12].Color != null)
                                CueSDK.MemoryModuleSDK[CorsairLedId.RAM_12].Color = cc;
                            break;
                    }
                }
            

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public Task Ripple1(Color burstcol, int speed, Color baseColor)
        {
            return new Task(() =>
            {
                lock (CorsairRipple1)
                {
                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    {
                        var presets = new Dictionary<string, Color>();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                            {
                                //Setup

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    try
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                        {
                                            var _key = _corsairkeyids[key];
                                            //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                            if (CueSDK.KeyboardSDK[_key] != null)
                                            {
                                                Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                                presets.Add(key, ccX);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Write.WriteConsole(ConsoleTypes.Error, @"(" + key + "): " + ex.Message);
                                    }

                                //Keyboard.Instance.SetCustom(keyboard_custom);

                                //HoldReader = true;
                            }
                            else if (i == 1)
                            {
                                //Step 0
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 2)
                            {
                                //Step 1
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 3)
                            {
                                //Step 2
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 4)
                            {
                                //Step 3
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 5)
                            {
                                //Step 4
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 6)
                            {
                                //Step 5
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 7)
                            {
                                //Step 6
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 8)
                            {
                                //Step 7
                                foreach (var key in DeviceEffects.GlobalKeys)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                    if (pos > -1)
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    else
                                    {
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, presets[key], false);
                                    }
                                }
                            }
                            else if (i == 9)
                            {
                                //Spin down

                                foreach (var key in DeviceEffects.GlobalKeys)
                                    if (_corsairkeyids.ContainsKey(key))
                                        ApplyMapKeyLighting(key, presets[key], false);

                                ApplyMapKeyLighting("D1", baseColor, false);
                                ApplyMapKeyLighting("D2", baseColor, false);
                                ApplyMapKeyLighting("D3", baseColor, false);
                                ApplyMapKeyLighting("D4", baseColor, false);
                                ApplyMapKeyLighting("D5", baseColor, false);
                                ApplyMapKeyLighting("D6", baseColor, false);
                                ApplyMapKeyLighting("D7", baseColor, false);
                                ApplyMapKeyLighting("D8", baseColor, false);
                                ApplyMapKeyLighting("D9", baseColor, false);
                                ApplyMapKeyLighting("D0", baseColor, false);
                                ApplyMapKeyLighting("OemMinus", baseColor, false);
                                ApplyMapKeyLighting("OemEquals", baseColor, false);

                                //presets.Clear();
                                //HoldReader = false;
                            }

                            if (i < 9)
                                Thread.Sleep(speed);

                            CueSDK.KeyboardSDK.Update();
                        }
                    }
                }
            });
        }

        public Task Ripple2(Color burstcol, int speed)
        {
            return new Task(() =>
            {
                lock (CorsairRipple2)
                {
                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    {
                        var presets = new Dictionary<string, Color>();
                        //uint burstcol = new Corale.Colore.Core.Color(RzCol.R, RzCol.G, RzCol.B);
                        var safeKeys = DeviceEffects.GlobalKeys.Except(FfxivHotbar.Keybindwhitelist);
                        var enumerable = safeKeys.ToList();

                        for (var i = 0; i <= 9; i++)
                        {
                            if (i == 0)
                                foreach (var s in enumerable)
                                    if (_corsairkeyids.ContainsKey(s))
                                    {
                                        var key = _corsairkeyids[s];
                                        if (CueSDK.KeyboardSDK[key] != null)
                                        {
                                            //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                            Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(key);
                                            presets.Add(s, ccX);
                                        }
                                    }
                            else if (i == 1)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep0, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 2)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep1, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 3)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep2, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 4)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep3, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 5)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep4, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 6)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep5, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 7)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep6, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 8)
                                foreach (var key in enumerable)
                                {
                                    var pos = Array.IndexOf(DeviceEffects.PulseOutStep7, key);
                                    if (pos > -1)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                                }
                            else if (i == 9)
                                foreach (var key in enumerable)
                                    if (_corsairkeyids.ContainsKey(key))
                                    {
                                        //ApplyMapKeyLighting(key, presets[key], false);
                                    }

                            if (i < 9)
                                Thread.Sleep(speed);
                        }
                    }
                }
            });
        }

        public void Flash1(Color burstcol, int speed, string[] regions)
        {
            if (pause) return;

            lock (CorsairFlash1)
            {
                var presets = new Dictionary<string, Color>();
                var scrollWheel = new Color();
                var logo = new Color();
                var backlight = new Color();

                var scrollWheelConv = scrollWheel;
                var logoConv = logo;
                var backlightConv = backlight;

                for (var i = 0; i <= 8; i++)
                {
                    if (i == 0)
                    {
                        //Setup
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                {
                                    var _key = _corsairkeyids[key];
                                    if (CueSDK.KeyboardSDK[_key] != null)
                                    {
                                        //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                        Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                        presets.Add(key, ccX);
                                    }
                                }

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                                scrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                            if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                                logo = CueSDK.MouseSDK[CorsairLedId.B1].Color;
                            if (CueSDK.MouseSDK[CorsairLedId.B4] != null)
                                backlight = CueSDK.MouseSDK[CorsairLedId.B4].Color;
                        }

                        //Keyboard.Instance.SetCustom(keyboard_custom);

                        //HoldReader = true;
                    }
                    else if (i == 1)
                    {
                        //Step 0
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 2)
                    {
                        //Step 1
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }
                    }
                    else if (i == 3)
                    {
                        //Step 2
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 4)
                    {
                        //Step 3
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }
                    }
                    else if (i == 5)
                    {
                        //Step 4
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 6)
                    {
                        //Step 5
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }
                    }
                    else if (i == 7)
                    {
                        //Step 6
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, burstcol, false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                            ApplyMapMouseLighting("Backlight", burstcol, false);
                        }
                    }
                    else if (i == 8)
                    {
                        //Step 7
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairkeyids.ContainsKey(key))
                                    ApplyMapKeyLighting(key, presets[key], false);

                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("ScrollWheel", scrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", logoConv, false);
                            ApplyMapMouseLighting("Backlight", backlightConv, false);
                        }

                        //HoldReader = false;
                    }

                    if (i < 8)
                        Thread.Sleep(speed);
                }
            }
        }

        public void Flash2(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (pause) return;

            lock (CorsairFlash2)
            {
                var presets = new Dictionary<string, Color>();

                if (!_corsairFlash2Running)
                {
                    if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                    {
                        //CorsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                            _corsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                            _corsairLogo = CueSDK.MouseSDK[CorsairLedId.B1].Color;

                        _corsairScrollWheelConv = _corsairScrollWheel;
                        _corsairLogoConv = _corsairLogo;
                    }

                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                        foreach (var key in regions)
                            if (_corsairkeyids.ContainsKey(key))
                            {
                                var _key = _corsairkeyids[key];
                                if (CueSDK.KeyboardSDK[_key] != null)
                                {
                                    //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                    Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                    presets.Add(key, ccX);
                                }
                            }

                    _corsairFlash2Running = true;
                    _corsairFlash2Step = 0;
                    _presets = presets;
                }

                if (_corsairFlash2Running)
                    while (_corsairFlash2Running)
                    {
                        if (cts.IsCancellationRequested)
                            break;

                        if (_corsairFlash2Step == 0)
                        {
                            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                                foreach (var key in regions)
                                    if (_corsairDeviceKeyboard)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, burstcol, false);
                            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", burstcol, false);
                                ApplyMapMouseLighting("Logo", burstcol, false);
                            }
                            _corsairFlash2Step = 1;

                            Thread.Sleep(speed);
                        }
                        else if (_corsairFlash2Step == 1)
                        {
                            if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                                foreach (var key in regions)
                                    if (_corsairDeviceKeyboard)
                                        if (_corsairkeyids.ContainsKey(key))
                                            ApplyMapKeyLighting(key, _presets[key], false);
                            if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                            {
                                ApplyMapMouseLighting("CorsairScrollWheel", _corsairScrollWheelConv, false);
                                ApplyMapMouseLighting("Logo", _corsairLogoConv, false);
                            }
                            _corsairFlash2Step = 0;

                            Thread.Sleep(speed);
                        }
                    }
            }
        }

        public void Flash3(Color burstcol, int speed, CancellationToken cts)
        {
            if (pause) return;

            try
            {
                //DeviceEffects._NumFlash
                lock (_Flash3)
                {
                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                    {
                        var presets = new Dictionary<string, Color>();
                        _corsairFlash3Running = true;
                        _corsairFlash3Step = 0;

                        if (_corsairFlash3Running == false)
                        {
                            /*
                            foreach (var key in DeviceEffects._GlobalKeys)
                                try
                                {
                                    if (Corsairkeyids.ContainsKey(key))
                                    {
                                        var _key = Corsairkeyids[key];
                                        //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                        if (CueSDK.KeyboardSDK[_key].Color != null)
                                        {
                                            Color ccX = _CorsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                            presets.Add(key, ccX);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    write.WriteConsole(ConsoleTypes.ERROR, "(" + key + "): " + ex.Message);
                                }
                            */
                        }
                        else
                        {
                            while (_corsairFlash3Running)
                            {

                                if (cts.IsCancellationRequested)
                                    break;

                                if (_corsairFlash3Step == 0)
                                {
                                    foreach (var key in DeviceEffects.NumFlash)
                                    {
                                        var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                        if (pos > -1)
                                            if (_corsairkeyids.ContainsKey(key))
                                                ApplyMapKeyLighting(key, burstcol, false);
                                    }
                                    _corsairFlash3Step = 1;
                                }
                                else if (_corsairFlash3Step == 1)
                                {
                                    foreach (var key in DeviceEffects.NumFlash)
                                    {
                                        var pos = Array.IndexOf(DeviceEffects.NumFlash, key);
                                        if (pos > -1)
                                            if (_corsairkeyids.ContainsKey(key))
                                                ApplyMapKeyLighting(key, Color.Black, false);
                                    }

                                    _corsairFlash3Step = 0;
                                }

                                CueSDK.KeyboardSDK.Update();
                                Thread.Sleep(speed);
                            }
                        }
                    }
                }
            }
            catch
            {
                //
            }
        }

        public void Flash4(Color burstcol, int speed, CancellationToken cts, string[] regions)
        {
            if (pause) return;

            lock (CorsairFlash4)
            {
                var presets = new Dictionary<string, Color>();

                if (!_corsairFlash4Running)
                {
                    if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                    {
                        //CorsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B3] != null)
                            _corsairScrollWheel = CueSDK.MouseSDK[CorsairLedId.B3].Color;
                        if (CueSDK.MouseSDK[CorsairLedId.B1] != null)
                            _corsairLogo = CueSDK.MouseSDK[CorsairLedId.B1].Color;

                        _corsairScrollWheelConv = _corsairScrollWheel;
                        _corsairLogoConv = _corsairLogo;
                    }

                    if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                        foreach (var key in regions)
                            if (_corsairkeyids.ContainsKey(key))
                            {
                                var _key = _corsairkeyids[key];
                                if (CueSDK.KeyboardSDK[_key] != null)
                                {
                                    //Color ccX = CueSDK.KeyboardSDK[_key].Color;
                                    Color ccX = _corsairKeyboardIndvBrush.CorsairGetColorReference(_key);
                                    presets.Add(key, ccX);
                                }
                            }

                    _corsairFlash4Running = true;
                    _corsairFlash4Step = 0;
                    _presets4 = presets;
                    Thread.Sleep(1);
                    Console.WriteLine(@"Corsair Flash 4 Start");
                }

                while (_corsairFlash4Running)
                {

                    if (cts.IsCancellationRequested)
                        break;

                    if (_corsairFlash4Step == 0)
                    {
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairDeviceKeyboard)
                                    if (_corsairkeyids.ContainsKey(key))
                                        ApplyMapKeyLighting(key, burstcol, false);
                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("CorsairScrollWheel", burstcol, false);
                            ApplyMapMouseLighting("Logo", burstcol, false);
                        }
                        _corsairFlash4Step = 1;

                        Thread.Sleep(speed);
                    }
                    else if (_corsairFlash4Step == 1)
                    {
                        if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                            foreach (var key in regions)
                                if (_corsairDeviceKeyboard)
                                    if (_corsairkeyids.ContainsKey(key))
                                        ApplyMapKeyLighting(key, _presets4[key], false);
                        if (_corsairDeviceMouse && !string.IsNullOrEmpty(CueSDK.MouseSDK?.MouseDeviceInfo?.Model))
                        {
                            ApplyMapMouseLighting("CorsairScrollWheel", _corsairScrollWheelConv, false);
                            ApplyMapMouseLighting("Logo", _corsairLogoConv, false);
                        }
                        _corsairFlash4Step = 0;

                        Thread.Sleep(speed);
                    }
                }
            }
        }

        private void Transition(Color col, bool forward)
        {
            if (pause) return;

            lock (Corsairtransition)
            {
                if (_corsairDeviceKeyboard && !string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
                {
                    //To be implemented

                    /*
                    RectangleF spot = new RectangleF(CueSDK.KeyboardSDK.DeviceRectangle.Width / 2f, CueSDK.KeyboardSDK.DeviceRectangle.Y / 2f, 160, 80);
                    PointF target = new PointF(spot.X, spot.Y);
                    RectangleLedGroup _CorsairKeyRec = new RectangleLedGroup(CueSDK.KeyboardSDK, spot);

                    for (uint c = 0; c < Corale.Colore.Razer.Keyboard.Constants.MaxColumns; c++)
                    {
                        for (uint r = 0; r < Corale.Colore.Razer.Keyboard.Constants.MaxRows; r++)
                        {
                            var row = (forward) ? r : Corale.Colore.Razer.Keyboard.Constants.MaxRows - r - 1;
                            var colu = (forward) ? c : Corale.Colore.Razer.Keyboard.Constants.MaxColumns - c - 1;
                            Keyboard.Instance[Convert.ToInt32(row), Convert.ToInt32(colu)] = RzCol;
                        }
                        Thread.Sleep(15);
                    }
                    */
                }
            }
        }

        private void CorsairtransitionConst(Color col1, Color col2, bool forward, int speed)
        {
            if (pause) return;

            lock (_CorsairtransitionConst)
            {
                //To be implemented
            }
        }

        public void ParticleEffect(Color[] toColor, string[] regions, uint interval, CancellationTokenSource cts, int speed = 50)
        {
            if (!_corsairDeviceKeyboard || string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model)) return;
            if (cts.IsCancellationRequested) return;

            var presets = new Dictionary<string, Color>();
            var _corsairKeyboardIndvBrushEffect = new KeyMapBrush();
            _corsairKeyboardIndvLed.Brush = _corsairKeyboardIndvBrushEffect;
            //_corsairKeyboardIndvBrush.CorsairApplyMapKeyLighting(_corsairkeyids[key], col);

            Dictionary<string, ColorFader> colorFaderDict = new Dictionary<string, ColorFader>();

            //Keyboard.SetCustomAsync(refreshKeyGrid);
            Thread.Sleep(500);
            
            while (true)
            {
                if (cts.IsCancellationRequested) break;

                var rnd = new Random();
                colorFaderDict.Clear();

                foreach (var key in regions)
                {
                    if (cts.IsCancellationRequested) return;

                    if (_corsairkeyids.ContainsKey(key))
                    {
                        var rndCol = Color.Black;
                        var keyid = _corsairkeyids[key];

                        do
                        {
                            rndCol = toColor[rnd.Next(toColor.Length)];
                        }
                        while ((Color)_corsairKeyboardIndvBrush.CorsairGetColorReference(keyid) == rndCol);

                        colorFaderDict.Add(key, new ColorFader(_corsairKeyboardIndvBrush.CorsairGetColorReference(keyid), rndCol, interval));
                    }
                }

                Task t = Task.Factory.StartNew(() =>
                {
                    //Thread.Sleep(500);

                    var _regions = regions.OrderBy(x => rnd.Next()).ToArray();

                    foreach (var key in _regions)
                    {
                        if (cts.IsCancellationRequested) return;
                        if (!_corsairkeyids.ContainsKey(key)) continue;

                        foreach (var color in colorFaderDict[key].Fade())
                        {
                            if (cts.IsCancellationRequested) return;
                            if (_corsairkeyids.ContainsKey(key))
                            {
                                //ApplyMapKeyLighting(key, color, false);
                                if (CueSDK.KeyboardSDK[_corsairkeyids[key]] != null)
                                    _corsairKeyboardIndvBrushEffect.CorsairApplyMapKeyLighting(_corsairkeyids[key], color);
                            }
                        }

                        //Keyboard.SetCustomAsync(refreshKeyGrid);
                        Thread.Sleep(speed);
                    }
                });

                Thread.Sleep(colorFaderDict.Count * speed);
            }
        }

        private readonly object lockObject = new object();
        public void CycleEffect(int interval, CancellationTokenSource token)
        {
            if (!_corsairDeviceKeyboard) return;
            if (string.IsNullOrEmpty(CueSDK.KeyboardSDK?.KeyboardDeviceInfo?.Model))
            {
                return;
            }

            while (true)
            {
                lock (lockObject)
                {
                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var col = Color.FromArgb((int) Math.Ceiling((double) (250 * 100) / 255),
                            (int) Math.Ceiling((double) (x * 100) / 255), 0);

                        SetAllLights(col);

                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var col = Color.FromArgb((int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255), 0);

                        SetAllLights(col);

                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var col = Color.FromArgb((int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255), 0);

                        SetAllLights(col);

                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var col = Color.FromArgb(0, (int) Math.Ceiling((double) (x * 100) / 255),
                            (int) Math.Ceiling((double) (250 * 100) / 255));

                        SetAllLights(col);
                    }

                    for (var x = 0; x <= 250; x += 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var col = Color.FromArgb((int) Math.Ceiling((double) (x * 100) / 255), 0,
                            (int) Math.Ceiling((double) (250 * 100) / 255));

                        SetAllLights(col);

                    }

                    for (var x = 250; x >= 5; x -= 5)
                    {
                        if (token.IsCancellationRequested) break;
                        Thread.Sleep(10);
                        var col = Color.FromArgb((int) Math.Ceiling((double) (250 * 100) / 255), 0,
                            (int) Math.Ceiling((double) (x * 100) / 255));

                        SetAllLights(col);

                    }

                    if (token.IsCancellationRequested) break;
                }
            }
            Thread.Sleep(interval);
        }
    }


    public static class ExceptionExtensions
    {
        public static Exception GetOriginalException(this Exception ex)
        {
            if (ex.InnerException == null) return ex;

            return ex.InnerException.GetOriginalException();
        }
    }
}