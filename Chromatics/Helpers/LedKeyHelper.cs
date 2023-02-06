using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public static class LedKeyHelper
    {
        public static string LedIdToHotbarKeyConverter(LedId led)
        {
            switch (led)
            {
                case LedId.Keyboard_Escape:
                    return "Escape";
                case LedId.Keyboard_F1:
                    return "F1";
                case LedId.Keyboard_F2:
                    return "F2";
                case LedId.Keyboard_F3:
                    return "F3";
                case LedId.Keyboard_F4:
                    return "F4";
                case LedId.Keyboard_F5:
                    return "F5";
                case LedId.Keyboard_F6:
                    return "F6";
                case LedId.Keyboard_F7:
                    return "F7";
                case LedId.Keyboard_F8:
                    return "F8";
                case LedId.Keyboard_F9:
                    return "F9";
                case LedId.Keyboard_F10:
                    return "F10";
                case LedId.Keyboard_F11:
                    return "F11";
                case LedId.Keyboard_F12:
                    return "F12";
                case LedId.Keyboard_GraveAccentAndTilde:
                    return "`";
                case LedId.Keyboard_1:
                    return "1";
                case LedId.Keyboard_2:
                    return "2";
                case LedId.Keyboard_3:
                    return "3";
                case LedId.Keyboard_4:
                    return "4";
                case LedId.Keyboard_5:
                    return "5";
                case LedId.Keyboard_6:
                    return "6";
                case LedId.Keyboard_7:
                    return "7";
                case LedId.Keyboard_8:
                    return "8";
                case LedId.Keyboard_9:
                    return "9";
                case LedId.Keyboard_0:
                    return "0";
                case LedId.Keyboard_MinusAndUnderscore:
                    return "-";
                case LedId.Keyboard_EqualsAndPlus:
                    return "=";
                case LedId.Keyboard_Backspace:
                    return "Backspace";
                case LedId.Keyboard_Tab:
                    return "Tab";
                case LedId.Keyboard_Q:
                    return "Q";
                case LedId.Keyboard_W:
                    return "W";
                case LedId.Keyboard_E:
                    return "E";
                case LedId.Keyboard_R:
                    return "R";
                case LedId.Keyboard_T:
                    return "T";
                case LedId.Keyboard_Y:
                    return "Y";
                case LedId.Keyboard_U:
                    return "U";
                case LedId.Keyboard_I:
                    return "I";
                case LedId.Keyboard_O:
                    return "O";
                case LedId.Keyboard_P:
                    return "P";
                case LedId.Keyboard_BracketLeft:
                    return "[";
                case LedId.Keyboard_BracketRight:
                    return "]";
                case LedId.Keyboard_Backslash:
                    return @"\";
                case LedId.Keyboard_Enter:
                    return "Enter";
                case LedId.Keyboard_CapsLock:
                    return "Caps Lock";
                case LedId.Keyboard_A:
                    return "A";
                case LedId.Keyboard_S:
                    return "S";
                case LedId.Keyboard_D:
                    return "D";
                case LedId.Keyboard_F:
                    return "F";
                case LedId.Keyboard_G:
                    return "G";
                case LedId.Keyboard_H:
                    return "H";
                case LedId.Keyboard_J:
                    return "J";
                case LedId.Keyboard_K:
                    return "K";
                case LedId.Keyboard_L:
                    return "L";
                case LedId.Keyboard_SemicolonAndColon:
                    return ";";
                case LedId.Keyboard_ApostropheAndDoubleQuote:
                    return "'";
                case LedId.Keyboard_NonUsTilde:
                    return "`";
                case LedId.Keyboard_LeftShift:
                    return "Left Shift";
                case LedId.Keyboard_NonUsBackslash:
                    return @"\";
                case LedId.Keyboard_Z:
                    return "Z";
                case LedId.Keyboard_X:
                    return "X";
                case LedId.Keyboard_C:
                    return "C";
                case LedId.Keyboard_V:
                    return "V";
                case LedId.Keyboard_B:
                    return "B";
                case LedId.Keyboard_N:
                    return "N";
                case LedId.Keyboard_M:
                    return "M";
                case LedId.Keyboard_CommaAndLessThan:
                    return ",";
                case LedId.Keyboard_PeriodAndBiggerThan:
                    return ".";
                case LedId.Keyboard_SlashAndQuestionMark:
                    return "/";
                case LedId.Keyboard_RightShift:
                    return "Right Shift";
                case LedId.Keyboard_LeftCtrl:
                    return "Left Ctrl";
                case LedId.Keyboard_LeftGui:
                    return "Left Gui";
                case LedId.Keyboard_LeftAlt:
                    return "Left Alt";
                case LedId.Keyboard_Lang2:
                    return "Lang2";
                case LedId.Keyboard_Space:
                    return "Space";
                case LedId.Keyboard_Lang1:
                    return "Lang1";
                case LedId.Keyboard_RightAlt:
                    return "Right Alt";
                case LedId.Keyboard_RightGui:
                    return "Right Gui";
                case LedId.Keyboard_Application:
                    return "Application";
                case LedId.Keyboard_RightCtrl:
                    return "Right Ctrl";
                case LedId.Keyboard_International1:
                    return "International1";
                case LedId.Keyboard_International2:
                    return "International2";
                case LedId.Keyboard_International3:
                    return "International3";
                case LedId.Keyboard_International5:
                    return "International5";
                case LedId.Keyboard_International4:
                    return "International4";
                case LedId.Keyboard_PrintScreen:
                    return "Print Screen";
                case LedId.Keyboard_ScrollLock:
                    return "Scroll Lock";
                case LedId.Keyboard_PauseBreak:
                    return "Pause";
                case LedId.Keyboard_Insert:
                    return "Insert";
                case LedId.Keyboard_Home:
                    return "Home";
                case LedId.Keyboard_PageUp:
                    return "Page Up";
                case LedId.Keyboard_Delete:
                    return "Delete";
                case LedId.Keyboard_End:
                    return "End";
                case LedId.Keyboard_PageDown:
                    return "Page Down";
                case LedId.Keyboard_ArrowUp:
                    return "↑";
                case LedId.Keyboard_ArrowLeft:
                    return "←";
                case LedId.Keyboard_ArrowDown:
                    return "↓";
                case LedId.Keyboard_ArrowRight:
                    return "→";
                case LedId.Keyboard_NumLock:
                    return "Num Lock";
                case LedId.Keyboard_NumSlash:
                    return "NUM/";
                case LedId.Keyboard_NumAsterisk:
                    return "NUM*";
                case LedId.Keyboard_NumMinus:
                    return "NUM-";
                case LedId.Keyboard_Num7:
                    return "NUM7";
                case LedId.Keyboard_Num8:
                    return "NUM8";
                case LedId.Keyboard_Num9:
                    return "NUM9";
                case LedId.Keyboard_NumComma:
                    return "NUM,";
                case LedId.Keyboard_NumPlus:
                    return "NUM+";
                case LedId.Keyboard_Num4:
                    return "NUM4";
                case LedId.Keyboard_Num5:
                    return "NUM5";
                case LedId.Keyboard_Num6:
                    return "NUM6";
                case LedId.Keyboard_Num1:
                    return "NUM1";
                case LedId.Keyboard_Num2:
                    return "NUM2";
                case LedId.Keyboard_Num3:
                    return "NUM3";
                case LedId.Keyboard_NumEnter:
                    return "Enter";
                case LedId.Keyboard_Num0:
                    return "NUM0";
                case LedId.Keyboard_Num00:
                    return "NUM00";
                case LedId.Keyboard_Function:
                    return "Function";
                case LedId.Keyboard_NumPeriodAndDelete:
                    return "Num.";
                case LedId.Keyboard_MediaMute:
                    return "Media Mute";
                case LedId.Keyboard_MediaVolumeDown:
                    return "Media VolumeDown";
                case LedId.Keyboard_MediaVolumeUp:
                    return "Media VolumeUp";
                case LedId.Keyboard_MediaStop:
                    return "Media Stop";
                case LedId.Keyboard_MediaPreviousTrack:
                    return "Media Previous Track";
                case LedId.Keyboard_MediaPlay:
                    return "Media Play";
                case LedId.Keyboard_MediaNextTrack:
                    return "Media Next Track";
                case LedId.Keyboard_WinLock:
                    return "Win Lock";
                case LedId.Keyboard_Macro1:
                    return "M1";
                case LedId.Keyboard_Macro2:
                    return "M2";
                case LedId.Keyboard_Macro3:
                    return "M3";
                case LedId.Keyboard_Macro4:
                    return "M4";
                case LedId.Keyboard_Macro5:
                    return "M5";
                case LedId.Keyboard_Programmable1:
                    return "M1";
                case LedId.Keyboard_Programmable2:
                    return "M2";
                case LedId.Keyboard_Programmable3:
                    return "M3";
                case LedId.Keyboard_Programmable4:
                    return "M4";
                case LedId.Keyboard_Programmable5:
                    return "M5";
                default:
                    return "Unknown";
            }
        }

        public static LedId HotbarKeyToLedIdConverter(string key)
        {
            switch (key)
            {
                case "Escape":
                    return LedId.Keyboard_Escape;
                case "F1":
                    return LedId.Keyboard_F1;
                case "F2":
                    return LedId.Keyboard_F2;
                case "F3":
                    return LedId.Keyboard_F3;
                case "F4":
                    return LedId.Keyboard_F4;
                case "F5":
                    return LedId.Keyboard_F5;
                case "F6":
                    return LedId.Keyboard_F6;
                case "F7":
                    return LedId.Keyboard_F7;
                case "F8":
                    return LedId.Keyboard_F8;
                case "F9":
                    return LedId.Keyboard_F9;
                case "F10":
                    return LedId.Keyboard_F10;
                case "F11":
                    return LedId.Keyboard_F11;
                case "F12":
                    return LedId.Keyboard_F12;
                case "`":
                    return LedId.Keyboard_GraveAccentAndTilde;
                case "1":
                    return LedId.Keyboard_1;
                case "2":
                    return LedId.Keyboard_2;
                case "3":
                    return LedId.Keyboard_3;
                case "4":
                    return LedId.Keyboard_4;
                case "5":
                    return LedId.Keyboard_5;
                case "6":
                    return LedId.Keyboard_6;
                case "7":
                    return LedId.Keyboard_7;
                case "8":
                    return LedId.Keyboard_8;
                case "9":
                    return LedId.Keyboard_9;
                case "0":
                    return LedId.Keyboard_0;
                case "-":
                    return LedId.Keyboard_MinusAndUnderscore;
                case "=":
                    return LedId.Keyboard_EqualsAndPlus;
                case "Backspace":
                    return LedId.Keyboard_Backspace;
                case "Tab":
                    return LedId.Keyboard_Tab;
                case "Q":
                    return LedId.Keyboard_Q;
                case "W":
                    return LedId.Keyboard_W;
                case "E":
                    return LedId.Keyboard_E;
                case "R":
                    return LedId.Keyboard_R;
                case "T":
                    return LedId.Keyboard_T;
                case "Y":
                    return LedId.Keyboard_Y;
                case "U":
                    return LedId.Keyboard_U;
                case "I":
                    return LedId.Keyboard_I;
                case "O":
                    return LedId.Keyboard_O;
                case "P":
                    return LedId.Keyboard_P;
                case "[":
                    return LedId.Keyboard_BracketLeft;
                case "]":
                    return LedId.Keyboard_BracketRight;
                case @"":
                    return LedId.Keyboard_Backslash;
                case "Enter":
                    return LedId.Keyboard_Enter;
                case "Caps Lock":
                    return LedId.Keyboard_CapsLock;
                case "A":
                    return LedId.Keyboard_A;
                case "S":
                    return LedId.Keyboard_S;
                case "D":
                    return LedId.Keyboard_D;
                case "F":
                    return LedId.Keyboard_F;
                case "G":
                    return LedId.Keyboard_G;
                case "H":
                    return LedId.Keyboard_H;
                case "J":
                    return LedId.Keyboard_J;
                case "K":
                    return LedId.Keyboard_K;
                case "L":
                    return LedId.Keyboard_L;
                case ";":
                    return LedId.Keyboard_SemicolonAndColon;
                case "'":
                    return LedId.Keyboard_ApostropheAndDoubleQuote;
                case "Left Shift":
                    return LedId.Keyboard_LeftShift;
                case @"\":
                    return LedId.Keyboard_NonUsBackslash;
                case "Z":
                    return LedId.Keyboard_Z;
                case "X":
                    return LedId.Keyboard_X;
                case "C":
                    return LedId.Keyboard_C;
                case "V":
                    return LedId.Keyboard_V;
                case "B":
                    return LedId.Keyboard_B;
                case "N":
                    return LedId.Keyboard_N;
                case "M":
                    return LedId.Keyboard_M;
                case ",":
                    return LedId.Keyboard_CommaAndLessThan;
                case ".":
                    return LedId.Keyboard_PeriodAndBiggerThan;
                case "/":
                    return LedId.Keyboard_SlashAndQuestionMark;
                case "Right Shift":
                    return LedId.Keyboard_RightShift;
                case "Left Ctrl":
                    return LedId.Keyboard_LeftCtrl;
                case "Left Gui":
                    return LedId.Keyboard_LeftGui;
                case "Left Alt":
                    return LedId.Keyboard_LeftAlt;
                case "Lang2":
                    return LedId.Keyboard_Lang2;
                case "Space":
                    return LedId.Keyboard_Space;
                case "Lang1":
                    return LedId.Keyboard_Lang1;
                case "Right Alt":
                    return LedId.Keyboard_RightAlt;
                case "Right Gui":
                    return LedId.Keyboard_RightGui;
                case "Application":
                    return LedId.Keyboard_Application;
                case "Right Ctrl":
                    return LedId.Keyboard_RightCtrl;
                case "International1":
                    return LedId.Keyboard_International1;
                case "International2":
                    return LedId.Keyboard_International2;
                case "International3":
                    return LedId.Keyboard_International3;
                case "International5":
                    return LedId.Keyboard_International5;
                case "International4":
                    return LedId.Keyboard_International4;
                case "Print Screen":
                    return LedId.Keyboard_PrintScreen;
                case "Scroll Lock":
                    return LedId.Keyboard_ScrollLock;
                case "Pause":
                    return LedId.Keyboard_PauseBreak;
                case "Insert":
                    return LedId.Keyboard_Insert;
                case "Home":
                    return LedId.Keyboard_Home;
                case "Page Up":
                    return LedId.Keyboard_PageUp;
                case "Delete":
                    return LedId.Keyboard_Delete;
                case "End":
                    return LedId.Keyboard_End;
                case "Page Down":
                    return LedId.Keyboard_PageDown;
                case "↑":
                    return LedId.Keyboard_ArrowUp;
                case "←":
                    return LedId.Keyboard_ArrowLeft;
                case "↓":
                    return LedId.Keyboard_ArrowDown;
                case "→":
                    return LedId.Keyboard_ArrowRight;
                case "Num Lock":
                    return LedId.Keyboard_NumLock;
                case "NUM/":
                    return LedId.Keyboard_NumSlash;
                case "NUM*":
                    return LedId.Keyboard_NumAsterisk;
                case "NUM-":
                    return LedId.Keyboard_NumMinus;
                case "NUM7":
                    return LedId.Keyboard_Num7;
                case "NUM8":
                    return LedId.Keyboard_Num8;
                case "NUM9":
                    return LedId.Keyboard_Num9;
                case "NUM,":
                    return LedId.Keyboard_NumComma;
                case "NUM+":
                    return LedId.Keyboard_NumPlus;
                case "NUM4":
                    return LedId.Keyboard_Num4;
                case "NUM5":
                    return LedId.Keyboard_Num5;
                case "NUM6":
                    return LedId.Keyboard_Num6;
                case "NUM1":
                    return LedId.Keyboard_Num1;
                case "NUM2":
                    return LedId.Keyboard_Num2;
                case "NUM3":
                    return LedId.Keyboard_Num3;
                case "NUM0":
                    return LedId.Keyboard_Num0;
                case "NUM00":
                    return LedId.Keyboard_Num00;
                case "Function":
                    return LedId.Keyboard_Function;
                case "Num.":
                    return LedId.Keyboard_NumPeriodAndDelete;
                case "Media Mute":
                    return LedId.Keyboard_MediaMute;
                case "Media VolumeDown":
                    return LedId.Keyboard_MediaVolumeDown;
                case "Media VolumeUp":
                    return LedId.Keyboard_MediaVolumeUp;
                case "Media Stop":
                    return LedId.Keyboard_MediaStop;
                case "Media Previous Track":
                    return LedId.Keyboard_MediaPreviousTrack;
                case "Media Play":
                    return LedId.Keyboard_MediaPlay;
                case "Media Next Track":
                    return LedId.Keyboard_MediaNextTrack;
                case "Win Lock":
                    return LedId.Keyboard_WinLock;
                case "M1":
                    return LedId.Keyboard_Macro1;
                case "M2":
                    return LedId.Keyboard_Macro2;
                case "M3":
                    return LedId.Keyboard_Macro3;
                case "M4":
                    return LedId.Keyboard_Macro4;
                case "M5":
                    return LedId.Keyboard_Macro5;
                default:
                    return LedId.Unknown1;
            }
        }

        public static Dictionary<int, LedId> GetAllKeysForDevice(RGBDeviceType device)
        {
            var AllKeys = new List<LedId>();

            switch (device)
            {
                case RGBDeviceType.None:
                    AllKeys = null;
                    break;
                case RGBDeviceType.Keyboard:
                    AllKeys = AllKeyboardKeys;
                    break;
                case RGBDeviceType.Mouse:
                    AllKeys = AllMouseKeys;
                    break;
                case RGBDeviceType.Headset:
                    AllKeys = AllHeadsetKeys;
                    break;
                case RGBDeviceType.Mousepad:
                    AllKeys = AllMousePadKeys;
                    break;
                case RGBDeviceType.LedStripe:
                    AllKeys = AllLedStripKeys;
                    break;
                case RGBDeviceType.LedMatrix:
                    AllKeys = AllLedMatrixKeys;
                    break;
                case RGBDeviceType.Mainboard:
                    AllKeys = AllMainboardKeys;
                    break;
                case RGBDeviceType.GraphicsCard:
                    AllKeys = AllGraphicsCardKeys;
                    break;
                case RGBDeviceType.DRAM:
                    AllKeys = AllDRAMKeys;
                    break;
                case RGBDeviceType.HeadsetStand:
                    AllKeys = AllHeadsetStandKeys;
                    break;
                case RGBDeviceType.Keypad:
                    AllKeys = AllKeypadKeys;
                    break;
                case RGBDeviceType.Fan:
                    AllKeys = AllFanKeys;
                    break;
                case RGBDeviceType.Speaker:
                    AllKeys = AllSpeakerKeys;
                    break;
                case RGBDeviceType.Cooler:
                    AllKeys = AllCoolerKeys;
                    break;
                case RGBDeviceType.Monitor:
                    AllKeys = null;
                    break;
                case RGBDeviceType.LedController:
                    AllKeys = AllCustomKeys;
                    break;
                case RGBDeviceType.Unknown:
                    AllKeys = AllUnknownKeys;
                    break;
                case RGBDeviceType.All:
                    AllKeys = null;
                    break;
                default:
                    AllKeys = null;
                    break;
            }

            if (AllKeys == null)
                return null;


            var i = 0;
            var _keys = new Dictionary<int, LedId>();

            foreach (var key in AllKeys)
            {
                if (!_keys.ContainsKey(i))
                {
                    _keys.Add(i, key);
                }

                i++;
            }

            return _keys;
        }

        private static List<LedId> AllKeyboardKeys = new List<LedId>()
        {
            LedId.Keyboard_Escape,
            LedId.Keyboard_F1,
            LedId.Keyboard_F2,
            LedId.Keyboard_F3,
            LedId.Keyboard_F4,
            LedId.Keyboard_F5,
            LedId.Keyboard_F6,
            LedId.Keyboard_F7,
            LedId.Keyboard_F8,
            LedId.Keyboard_F9,
            LedId.Keyboard_F10,
            LedId.Keyboard_F11,
            LedId.Keyboard_F12,
            LedId.Keyboard_GraveAccentAndTilde,
            LedId.Keyboard_1,
            LedId.Keyboard_2,
            LedId.Keyboard_3,
            LedId.Keyboard_4,
            LedId.Keyboard_5,
            LedId.Keyboard_6,
            LedId.Keyboard_7,
            LedId.Keyboard_8,
            LedId.Keyboard_9,
            LedId.Keyboard_0,
            LedId.Keyboard_MinusAndUnderscore,
            LedId.Keyboard_EqualsAndPlus,
            LedId.Keyboard_Backspace,
            LedId.Keyboard_Tab,
            LedId.Keyboard_Q,
            LedId.Keyboard_W,
            LedId.Keyboard_E,
            LedId.Keyboard_R,
            LedId.Keyboard_T,
            LedId.Keyboard_Y,
            LedId.Keyboard_U,
            LedId.Keyboard_I,
            LedId.Keyboard_O,
            LedId.Keyboard_P,
            LedId.Keyboard_BracketLeft,
            LedId.Keyboard_BracketRight,
            LedId.Keyboard_Backslash,
            LedId.Keyboard_Enter,
            LedId.Keyboard_CapsLock,
            LedId.Keyboard_A,
            LedId.Keyboard_S,
            LedId.Keyboard_D,
            LedId.Keyboard_F,
            LedId.Keyboard_G,
            LedId.Keyboard_H,
            LedId.Keyboard_J,
            LedId.Keyboard_K,
            LedId.Keyboard_L,
            LedId.Keyboard_SemicolonAndColon,
            LedId.Keyboard_ApostropheAndDoubleQuote,
            LedId.Keyboard_NonUsTilde,
            LedId.Keyboard_LeftShift,
            LedId.Keyboard_NonUsBackslash,
            LedId.Keyboard_Z,
            LedId.Keyboard_X,
            LedId.Keyboard_C,
            LedId.Keyboard_V,
            LedId.Keyboard_B,
            LedId.Keyboard_N,
            LedId.Keyboard_M,
            LedId.Keyboard_CommaAndLessThan,
            LedId.Keyboard_PeriodAndBiggerThan,
            LedId.Keyboard_SlashAndQuestionMark,
            LedId.Keyboard_RightShift,
            LedId.Keyboard_LeftCtrl,
            LedId.Keyboard_LeftGui,
            LedId.Keyboard_LeftAlt,
            LedId.Keyboard_Lang2,
            LedId.Keyboard_Space,
            LedId.Keyboard_Lang1,
            LedId.Keyboard_RightAlt,
            LedId.Keyboard_RightGui,
            LedId.Keyboard_Application,
            LedId.Keyboard_RightCtrl,
            LedId.Keyboard_International1,
            LedId.Keyboard_International2,
            LedId.Keyboard_International3,
            LedId.Keyboard_International5,
            LedId.Keyboard_International4,
            LedId.Keyboard_PrintScreen,
            LedId.Keyboard_ScrollLock,
            LedId.Keyboard_PauseBreak,
            LedId.Keyboard_Insert,
            LedId.Keyboard_Home,
            LedId.Keyboard_PageUp,
            LedId.Keyboard_Delete,
            LedId.Keyboard_End,
            LedId.Keyboard_PageDown,
            LedId.Keyboard_ArrowUp,
            LedId.Keyboard_ArrowLeft,
            LedId.Keyboard_ArrowDown,
            LedId.Keyboard_ArrowRight,
            LedId.Keyboard_NumLock,
            LedId.Keyboard_NumSlash,
            LedId.Keyboard_NumAsterisk,
            LedId.Keyboard_NumMinus,
            LedId.Keyboard_Num7,
            LedId.Keyboard_Num8,
            LedId.Keyboard_Num9,
            LedId.Keyboard_NumComma,
            LedId.Keyboard_NumPlus,
            LedId.Keyboard_Num4,
            LedId.Keyboard_Num5,
            LedId.Keyboard_Num6,
            LedId.Keyboard_Num1,
            LedId.Keyboard_Num2,
            LedId.Keyboard_Num3,
            LedId.Keyboard_NumEnter,
            LedId.Keyboard_Num0,
            LedId.Keyboard_Num00,
            LedId.Keyboard_Function,
            LedId.Keyboard_NumPeriodAndDelete,
            LedId.Keyboard_MediaMute,
            LedId.Keyboard_MediaVolumeDown,
            LedId.Keyboard_MediaVolumeUp,
            LedId.Keyboard_MediaStop,
            LedId.Keyboard_MediaPreviousTrack,
            LedId.Keyboard_MediaPlay,
            LedId.Keyboard_MediaNextTrack,
            LedId.Keyboard_WinLock,
            LedId.Keyboard_Brightness,
            LedId.Keyboard_MacroRecording,
            LedId.Keyboard_Macro1,
            LedId.Keyboard_Macro2,
            LedId.Keyboard_Macro3,
            LedId.Keyboard_Macro4,
            LedId.Keyboard_Macro5,
            LedId.Keyboard_Programmable1,
            LedId.Keyboard_Programmable2,
            LedId.Keyboard_Programmable3,
            LedId.Keyboard_Programmable4,
            LedId.Keyboard_Programmable5,
            LedId.Keyboard_Programmable6,
            LedId.Keyboard_Programmable7,
            LedId.Keyboard_Programmable8,
            LedId.Keyboard_Programmable9,
            LedId.Keyboard_Programmable10,
            LedId.Keyboard_Programmable11,
            LedId.Keyboard_Programmable12,
            LedId.Keyboard_Programmable13,
            LedId.Keyboard_Programmable14,
            LedId.Keyboard_Programmable15,
            LedId.Keyboard_Programmable16,
            LedId.Keyboard_Programmable17,
            LedId.Keyboard_Programmable18,
            LedId.Keyboard_Programmable19,
            LedId.Keyboard_Programmable20,
            LedId.Keyboard_Programmable21,
            LedId.Keyboard_Programmable22,
            LedId.Keyboard_Programmable23,
            LedId.Keyboard_Programmable24,
            LedId.Keyboard_Programmable25,
            LedId.Keyboard_Programmable26,
            LedId.Keyboard_Programmable27,
            LedId.Keyboard_Programmable28,
            LedId.Keyboard_Programmable29,
            LedId.Keyboard_Programmable30,
            LedId.Keyboard_Programmable31,
            LedId.Keyboard_Programmable32,
            LedId.Keyboard_Custom1,
            LedId.Keyboard_Custom2,
            LedId.Keyboard_Custom3,
            LedId.Keyboard_Custom4,
            LedId.Keyboard_Custom5,
            LedId.Keyboard_Custom6,
            LedId.Keyboard_Custom7,
            LedId.Keyboard_Custom8,
            LedId.Keyboard_Custom9,
            LedId.Keyboard_Custom10,
            LedId.Keyboard_Custom11,
            LedId.Keyboard_Custom12,
            LedId.Keyboard_Custom13,
            LedId.Keyboard_Custom14,
            LedId.Keyboard_Custom15,
            LedId.Keyboard_Custom16,
            LedId.Keyboard_Custom17,
            LedId.Keyboard_Custom18,
            LedId.Keyboard_Custom19,
            LedId.Keyboard_Custom20,
            LedId.Keyboard_Custom21,
            LedId.Keyboard_Custom22,
            LedId.Keyboard_Custom23,
            LedId.Keyboard_Custom24,
            LedId.Keyboard_Custom25,
            LedId.Keyboard_Custom26,
            LedId.Keyboard_Custom27,
            LedId.Keyboard_Custom28,
            LedId.Keyboard_Custom29,
            LedId.Keyboard_Custom30,
            LedId.Keyboard_Custom31,
            LedId.Keyboard_Custom32,
            LedId.Keyboard_Custom33,
            LedId.Keyboard_Custom34,
            LedId.Keyboard_Custom35,
            LedId.Keyboard_Custom36,
            LedId.Keyboard_Custom37,
            LedId.Keyboard_Custom38,
            LedId.Keyboard_Custom39,
            LedId.Keyboard_Custom40,
            LedId.Keyboard_Custom41,
            LedId.Keyboard_Custom42,
            LedId.Keyboard_Custom43,
            LedId.Keyboard_Custom44,
            LedId.Keyboard_Custom45,
            LedId.Keyboard_Custom46,
            LedId.Keyboard_Custom47,
            LedId.Keyboard_Custom48,
            LedId.Keyboard_Custom49,
            LedId.Keyboard_Custom50,
            LedId.Keyboard_Custom51,
            LedId.Keyboard_Custom52,
            LedId.Keyboard_Custom53,
            LedId.Keyboard_Custom54,
            LedId.Keyboard_Custom55,
            LedId.Keyboard_Custom56,
            LedId.Keyboard_Custom57,
            LedId.Keyboard_Custom58,
            LedId.Keyboard_Custom59,
            LedId.Keyboard_Custom60,
            LedId.Keyboard_Custom61,
            LedId.Keyboard_Custom62,
            LedId.Keyboard_Custom63,
            LedId.Keyboard_Custom64,
            LedId.Logo
        };

        private static List<LedId> AllMouseKeys = new List<LedId>()
        {
            LedId.Mouse1,
            LedId.Mouse2,
            LedId.Mouse3,
            LedId.Mouse4,
            LedId.Mouse5,
            LedId.Mouse6,
            LedId.Mouse7,
            LedId.Mouse8,
            LedId.Mouse9,
            LedId.Mouse10,
            LedId.Mouse11,
            LedId.Mouse12,
            LedId.Mouse13,
            LedId.Mouse14,
            LedId.Mouse15,
            LedId.Mouse16,
            LedId.Mouse17,
            LedId.Mouse18,
            LedId.Mouse19,
            LedId.Mouse20,
            LedId.Mouse21,
            LedId.Mouse22,
            LedId.Mouse23,
            LedId.Mouse24,
            LedId.Mouse25,
            LedId.Mouse26,
            LedId.Mouse27,
            LedId.Mouse28,
            LedId.Mouse29,
            LedId.Mouse30,
            LedId.Mouse31,
            LedId.Mouse32,
            LedId.Mouse33,
            LedId.Mouse34,
            LedId.Mouse35,
            LedId.Mouse36,
            LedId.Mouse37,
            LedId.Mouse38,
            LedId.Mouse39,
            LedId.Mouse40,
            LedId.Mouse41,
            LedId.Mouse42,
            LedId.Mouse43,
            LedId.Mouse44,
            LedId.Mouse45,
            LedId.Mouse46,
            LedId.Mouse47,
            LedId.Mouse48,
            LedId.Mouse49,
            LedId.Mouse50,
            LedId.Mouse51,
            LedId.Mouse52,
            LedId.Mouse53,
            LedId.Mouse54,
            LedId.Mouse55,
            LedId.Mouse56,
            LedId.Mouse57,
            LedId.Mouse58,
            LedId.Mouse59,
            LedId.Mouse60,
            LedId.Mouse61,
            LedId.Mouse62,
            LedId.Mouse63,
            LedId.Mouse64
        };

        private static List<LedId> AllHeadsetKeys = new List<LedId>()
        {
            LedId.Headset1,
            LedId.Headset2,
            LedId.Headset3,
            LedId.Headset4,
            LedId.Headset5,
            LedId.Headset6,
            LedId.Headset7,
            LedId.Headset8,
            LedId.Headset9,
            LedId.Headset10,
            LedId.Headset11,
            LedId.Headset12,
            LedId.Headset13,
            LedId.Headset14,
            LedId.Headset15,
            LedId.Headset16,
            LedId.Headset17,
            LedId.Headset18,
            LedId.Headset19,
            LedId.Headset20,
            LedId.Headset21,
            LedId.Headset22,
            LedId.Headset23,
            LedId.Headset24,
            LedId.Headset25,
            LedId.Headset26,
            LedId.Headset27,
            LedId.Headset28,
            LedId.Headset29,
            LedId.Headset30,
            LedId.Headset31,
            LedId.Headset32,
            LedId.Headset33,
            LedId.Headset34,
            LedId.Headset35,
            LedId.Headset36,
            LedId.Headset37,
            LedId.Headset38,
            LedId.Headset39,
            LedId.Headset40,
            LedId.Headset41,
            LedId.Headset42,
            LedId.Headset43,
            LedId.Headset44,
            LedId.Headset45,
            LedId.Headset46,
            LedId.Headset47,
            LedId.Headset48,
            LedId.Headset49,
            LedId.Headset50,
            LedId.Headset51,
            LedId.Headset52,
            LedId.Headset53,
            LedId.Headset54,
            LedId.Headset55,
            LedId.Headset56,
            LedId.Headset57,
            LedId.Headset58,
            LedId.Headset59,
            LedId.Headset60,
            LedId.Headset61,
            LedId.Headset62,
            LedId.Headset63,
            LedId.Headset64
        };

        private static List<LedId> AllMousePadKeys = new List<LedId>()
        {
            LedId.Mousepad1,
            LedId.Mousepad2,
            LedId.Mousepad3,
            LedId.Mousepad4,
            LedId.Mousepad5,
            LedId.Mousepad6,
            LedId.Mousepad7,
            LedId.Mousepad8,
            LedId.Mousepad9,
            LedId.Mousepad10,
            LedId.Mousepad11,
            LedId.Mousepad12,
            LedId.Mousepad13,
            LedId.Mousepad14,
            LedId.Mousepad15,
            LedId.Mousepad16,
            LedId.Mousepad17,
            LedId.Mousepad18,
            LedId.Mousepad19,
            LedId.Mousepad20,
            LedId.Mousepad21,
            LedId.Mousepad22,
            LedId.Mousepad23,
            LedId.Mousepad24,
            LedId.Mousepad25,
            LedId.Mousepad26,
            LedId.Mousepad27,
            LedId.Mousepad28,
            LedId.Mousepad29,
            LedId.Mousepad30,
            LedId.Mousepad31,
            LedId.Mousepad32,
            LedId.Mousepad33,
            LedId.Mousepad34,
            LedId.Mousepad35,
            LedId.Mousepad36,
            LedId.Mousepad37,
            LedId.Mousepad38,
            LedId.Mousepad39,
            LedId.Mousepad40,
            LedId.Mousepad41,
            LedId.Mousepad42,
            LedId.Mousepad43,
            LedId.Mousepad44,
            LedId.Mousepad45,
            LedId.Mousepad46,
            LedId.Mousepad47,
            LedId.Mousepad48,
            LedId.Mousepad49,
            LedId.Mousepad50,
            LedId.Mousepad51,
            LedId.Mousepad52,
            LedId.Mousepad53,
            LedId.Mousepad54,
            LedId.Mousepad55,
            LedId.Mousepad56,
            LedId.Mousepad57,
            LedId.Mousepad58,
            LedId.Mousepad59,
            LedId.Mousepad60,
            LedId.Mousepad61,
            LedId.Mousepad62,
            LedId.Mousepad63,
            LedId.Mousepad64
        };

        private static List<LedId> AllLedStripKeys = new List<LedId>()
        {
            LedId.LedStripe1,
            LedId.LedStripe2,
            LedId.LedStripe3,
            LedId.LedStripe4,
            LedId.LedStripe5,
            LedId.LedStripe6,
            LedId.LedStripe7,
            LedId.LedStripe8,
            LedId.LedStripe9,
            LedId.LedStripe10,
            LedId.LedStripe11,
            LedId.LedStripe12,
            LedId.LedStripe13,
            LedId.LedStripe14,
            LedId.LedStripe15,
            LedId.LedStripe16,
            LedId.LedStripe17,
            LedId.LedStripe18,
            LedId.LedStripe19,
            LedId.LedStripe20,
            LedId.LedStripe21,
            LedId.LedStripe22,
            LedId.LedStripe23,
            LedId.LedStripe24,
            LedId.LedStripe25,
            LedId.LedStripe26,
            LedId.LedStripe27,
            LedId.LedStripe28,
            LedId.LedStripe29,
            LedId.LedStripe30,
            LedId.LedStripe31,
            LedId.LedStripe32,
            LedId.LedStripe33,
            LedId.LedStripe34,
            LedId.LedStripe35,
            LedId.LedStripe36,
            LedId.LedStripe37,
            LedId.LedStripe38,
            LedId.LedStripe39,
            LedId.LedStripe40,
            LedId.LedStripe41,
            LedId.LedStripe42,
            LedId.LedStripe43,
            LedId.LedStripe44,
            LedId.LedStripe45,
            LedId.LedStripe46,
            LedId.LedStripe47,
            LedId.LedStripe48,
            LedId.LedStripe49,
            LedId.LedStripe50,
            LedId.LedStripe51,
            LedId.LedStripe52,
            LedId.LedStripe53,
            LedId.LedStripe54,
            LedId.LedStripe55,
            LedId.LedStripe56,
            LedId.LedStripe57,
            LedId.LedStripe58,
            LedId.LedStripe59,
            LedId.LedStripe60,
            LedId.LedStripe61,
            LedId.LedStripe62,
            LedId.LedStripe63,
            LedId.LedStripe64,
            LedId.LedStripe65,
            LedId.LedStripe66,
            LedId.LedStripe67,
            LedId.LedStripe68,
            LedId.LedStripe69,
            LedId.LedStripe70,
            LedId.LedStripe71,
            LedId.LedStripe72,
            LedId.LedStripe73,
            LedId.LedStripe74,
            LedId.LedStripe75,
            LedId.LedStripe76,
            LedId.LedStripe77,
            LedId.LedStripe78,
            LedId.LedStripe79,
            LedId.LedStripe80,
            LedId.LedStripe81,
            LedId.LedStripe82,
            LedId.LedStripe83,
            LedId.LedStripe84,
            LedId.LedStripe85,
            LedId.LedStripe86,
            LedId.LedStripe87,
            LedId.LedStripe88,
            LedId.LedStripe89,
            LedId.LedStripe90,
            LedId.LedStripe91,
            LedId.LedStripe92,
            LedId.LedStripe93,
            LedId.LedStripe94,
            LedId.LedStripe95,
            LedId.LedStripe96,
            LedId.LedStripe97,
            LedId.LedStripe98,
            LedId.LedStripe99,
            LedId.LedStripe100,
            LedId.LedStripe101,
            LedId.LedStripe102,
            LedId.LedStripe103,
            LedId.LedStripe104,
            LedId.LedStripe105,
            LedId.LedStripe106,
            LedId.LedStripe107,
            LedId.LedStripe108,
            LedId.LedStripe109,
            LedId.LedStripe110,
            LedId.LedStripe111,
            LedId.LedStripe112,
            LedId.LedStripe113,
            LedId.LedStripe114,
            LedId.LedStripe115,
            LedId.LedStripe116,
            LedId.LedStripe117,
            LedId.LedStripe118,
            LedId.LedStripe119,
            LedId.LedStripe120,
            LedId.LedStripe121,
            LedId.LedStripe122,
            LedId.LedStripe123,
            LedId.LedStripe124,
            LedId.LedStripe125,
            LedId.LedStripe126,
            LedId.LedStripe127,
            LedId.LedStripe128
        };

        private static List<LedId> AllLedMatrixKeys = new List<LedId>()
        {
            LedId.LedMatrix1,
            LedId.LedMatrix2,
            LedId.LedMatrix3,
            LedId.LedMatrix4,
            LedId.LedMatrix5,
            LedId.LedMatrix6,
            LedId.LedMatrix7,
            LedId.LedMatrix8,
            LedId.LedMatrix9,
            LedId.LedMatrix10,
            LedId.LedMatrix11,
            LedId.LedMatrix12,
            LedId.LedMatrix13,
            LedId.LedMatrix14,
            LedId.LedMatrix15,
            LedId.LedMatrix16,
            LedId.LedMatrix17,
            LedId.LedMatrix18,
            LedId.LedMatrix19,
            LedId.LedMatrix20,
            LedId.LedMatrix21,
            LedId.LedMatrix22,
            LedId.LedMatrix23,
            LedId.LedMatrix24,
            LedId.LedMatrix25,
            LedId.LedMatrix26,
            LedId.LedMatrix27,
            LedId.LedMatrix28,
            LedId.LedMatrix29,
            LedId.LedMatrix30,
            LedId.LedMatrix31,
            LedId.LedMatrix32,
            LedId.LedMatrix33,
            LedId.LedMatrix34,
            LedId.LedMatrix35,
            LedId.LedMatrix36,
            LedId.LedMatrix37,
            LedId.LedMatrix38,
            LedId.LedMatrix39,
            LedId.LedMatrix40,
            LedId.LedMatrix41,
            LedId.LedMatrix42,
            LedId.LedMatrix43,
            LedId.LedMatrix44,
            LedId.LedMatrix45,
            LedId.LedMatrix46,
            LedId.LedMatrix47,
            LedId.LedMatrix48,
            LedId.LedMatrix49,
            LedId.LedMatrix50,
            LedId.LedMatrix51,
            LedId.LedMatrix52,
            LedId.LedMatrix53,
            LedId.LedMatrix54,
            LedId.LedMatrix55,
            LedId.LedMatrix56,
            LedId.LedMatrix57,
            LedId.LedMatrix58,
            LedId.LedMatrix59,
            LedId.LedMatrix60,
            LedId.LedMatrix61,
            LedId.LedMatrix62,
            LedId.LedMatrix63,
            LedId.LedMatrix64,
            LedId.LedMatrix665,
            LedId.LedMatrix666,
            LedId.LedMatrix667,
            LedId.LedMatrix668,
            LedId.LedMatrix669,
            LedId.LedMatrix670,
            LedId.LedMatrix671,
            LedId.LedMatrix672,
            LedId.LedMatrix673,
            LedId.LedMatrix674,
            LedId.LedMatrix675,
            LedId.LedMatrix676,
            LedId.LedMatrix677,
            LedId.LedMatrix678,
            LedId.LedMatrix679,
            LedId.LedMatrix680,
            LedId.LedMatrix681,
            LedId.LedMatrix682,
            LedId.LedMatrix683,
            LedId.LedMatrix684,
            LedId.LedMatrix685,
            LedId.LedMatrix686,
            LedId.LedMatrix687,
            LedId.LedMatrix688,
            LedId.LedMatrix689,
            LedId.LedMatrix690,
            LedId.LedMatrix691,
            LedId.LedMatrix692,
            LedId.LedMatrix693,
            LedId.LedMatrix694,
            LedId.LedMatrix695,
            LedId.LedMatrix696,
            LedId.LedMatrix697,
            LedId.LedMatrix698,
            LedId.LedMatrix699,

        };

        private static List<LedId> AllMainboardKeys = new List<LedId>()
        {
            LedId.Mainboard1,
            LedId.Mainboard2,
            LedId.Mainboard3,
            LedId.Mainboard4,
            LedId.Mainboard5,
            LedId.Mainboard6,
            LedId.Mainboard7,
            LedId.Mainboard8,
            LedId.Mainboard9,
            LedId.Mainboard10,
            LedId.Mainboard11,
            LedId.Mainboard12,
            LedId.Mainboard13,
            LedId.Mainboard14,
            LedId.Mainboard15,
            LedId.Mainboard16,
            LedId.Mainboard17,
            LedId.Mainboard18,
            LedId.Mainboard19,
            LedId.Mainboard20,
            LedId.Mainboard21,
            LedId.Mainboard22,
            LedId.Mainboard23,
            LedId.Mainboard24,
            LedId.Mainboard25,
            LedId.Mainboard26,
            LedId.Mainboard27,
            LedId.Mainboard28,
            LedId.Mainboard29,
            LedId.Mainboard30,
            LedId.Mainboard31,
            LedId.Mainboard32,
            LedId.Mainboard33,
            LedId.Mainboard34,
            LedId.Mainboard35,
            LedId.Mainboard36,
            LedId.Mainboard37,
            LedId.Mainboard38,
            LedId.Mainboard39,
            LedId.Mainboard40,
            LedId.Mainboard41,
            LedId.Mainboard42,
            LedId.Mainboard43,
            LedId.Mainboard44,
            LedId.Mainboard45,
            LedId.Mainboard46,
            LedId.Mainboard47,
            LedId.Mainboard48,
            LedId.Mainboard49,
            LedId.Mainboard50,
            LedId.Mainboard51,
            LedId.Mainboard52,
            LedId.Mainboard53,
            LedId.Mainboard54,
            LedId.Mainboard55,
            LedId.Mainboard56,
            LedId.Mainboard57,
            LedId.Mainboard58,
            LedId.Mainboard59,
            LedId.Mainboard60,
            LedId.Mainboard61,
            LedId.Mainboard62,
            LedId.Mainboard63,
            LedId.Mainboard64
        };

        private static List<LedId> AllGraphicsCardKeys = new List<LedId>()
        {
            LedId.GraphicsCard1,
            LedId.GraphicsCard2,
            LedId.GraphicsCard3,
            LedId.GraphicsCard4,
            LedId.GraphicsCard5,
            LedId.GraphicsCard6,
            LedId.GraphicsCard7,
            LedId.GraphicsCard8,
            LedId.GraphicsCard9,
            LedId.GraphicsCard10,
            LedId.GraphicsCard11,
            LedId.GraphicsCard12,
            LedId.GraphicsCard13,
            LedId.GraphicsCard14,
            LedId.GraphicsCard15,
            LedId.GraphicsCard16,
            LedId.GraphicsCard17,
            LedId.GraphicsCard18,
            LedId.GraphicsCard19,
            LedId.GraphicsCard20,
            LedId.GraphicsCard21,
            LedId.GraphicsCard22,
            LedId.GraphicsCard23,
            LedId.GraphicsCard24,
            LedId.GraphicsCard25,
            LedId.GraphicsCard26,
            LedId.GraphicsCard27,
            LedId.GraphicsCard28,
            LedId.GraphicsCard29,
            LedId.GraphicsCard30,
            LedId.GraphicsCard31,
            LedId.GraphicsCard32,
            LedId.GraphicsCard33,
            LedId.GraphicsCard34,
            LedId.GraphicsCard35,
            LedId.GraphicsCard36,
            LedId.GraphicsCard37,
            LedId.GraphicsCard38,
            LedId.GraphicsCard39,
            LedId.GraphicsCard40,
            LedId.GraphicsCard41,
            LedId.GraphicsCard42,
            LedId.GraphicsCard43,
            LedId.GraphicsCard44,
            LedId.GraphicsCard45,
            LedId.GraphicsCard46,
            LedId.GraphicsCard47,
            LedId.GraphicsCard48,
            LedId.GraphicsCard49,
            LedId.GraphicsCard50,
            LedId.GraphicsCard51,
            LedId.GraphicsCard52,
            LedId.GraphicsCard53,
            LedId.GraphicsCard54,
            LedId.GraphicsCard55,
            LedId.GraphicsCard56,
            LedId.GraphicsCard57,
            LedId.GraphicsCard58,
            LedId.GraphicsCard59,
            LedId.GraphicsCard60,
            LedId.GraphicsCard61,
            LedId.GraphicsCard62,
            LedId.GraphicsCard63,
            LedId.GraphicsCard64
        };

        private static List<LedId> AllDRAMKeys = new List<LedId>()
        {
            LedId.DRAM1,
            LedId.DRAM2,
            LedId.DRAM3,
            LedId.DRAM4,
            LedId.DRAM5,
            LedId.DRAM6,
            LedId.DRAM7,
            LedId.DRAM8,
            LedId.DRAM9,
            LedId.DRAM10,
            LedId.DRAM11,
            LedId.DRAM12,
            LedId.DRAM13,
            LedId.DRAM14,
            LedId.DRAM15,
            LedId.DRAM16,
            LedId.DRAM17,
            LedId.DRAM18,
            LedId.DRAM19,
            LedId.DRAM20,
            LedId.DRAM21,
            LedId.DRAM22,
            LedId.DRAM23,
            LedId.DRAM24,
            LedId.DRAM25,
            LedId.DRAM26,
            LedId.DRAM27,
            LedId.DRAM28,
            LedId.DRAM29,
            LedId.DRAM30,
            LedId.DRAM31,
            LedId.DRAM32,
            LedId.DRAM33,
            LedId.DRAM34,
            LedId.DRAM35,
            LedId.DRAM36,
            LedId.DRAM37,
            LedId.DRAM38,
            LedId.DRAM39,
            LedId.DRAM40,
            LedId.DRAM41,
            LedId.DRAM42,
            LedId.DRAM43,
            LedId.DRAM44,
            LedId.DRAM45,
            LedId.DRAM46,
            LedId.DRAM47,
            LedId.DRAM48,
            LedId.DRAM49,
            LedId.DRAM50,
            LedId.DRAM51,
            LedId.DRAM52,
            LedId.DRAM53,
            LedId.DRAM54,
            LedId.DRAM55,
            LedId.DRAM56,
            LedId.DRAM57,
            LedId.DRAM58,
            LedId.DRAM59,
            LedId.DRAM60,
            LedId.DRAM61,
            LedId.DRAM62,
            LedId.DRAM63,
            LedId.DRAM64
        };

        private static List<LedId> AllHeadsetStandKeys = new List<LedId>()
        {
            LedId.HeadsetStand1,
            LedId.HeadsetStand2,
            LedId.HeadsetStand3,
            LedId.HeadsetStand4,
            LedId.HeadsetStand5,
            LedId.HeadsetStand6,
            LedId.HeadsetStand7,
            LedId.HeadsetStand8,
            LedId.HeadsetStand9,
            LedId.HeadsetStand10,
            LedId.HeadsetStand11,
            LedId.HeadsetStand12,
            LedId.HeadsetStand13,
            LedId.HeadsetStand14,
            LedId.HeadsetStand15,
            LedId.HeadsetStand16,
            LedId.HeadsetStand17,
            LedId.HeadsetStand18,
            LedId.HeadsetStand19,
            LedId.HeadsetStand20,
            LedId.HeadsetStand21,
            LedId.HeadsetStand22,
            LedId.HeadsetStand23,
            LedId.HeadsetStand24,
            LedId.HeadsetStand25,
            LedId.HeadsetStand26,
            LedId.HeadsetStand27,
            LedId.HeadsetStand28,
            LedId.HeadsetStand29,
            LedId.HeadsetStand30,
            LedId.HeadsetStand31,
            LedId.HeadsetStand32,
            LedId.HeadsetStand33,
            LedId.HeadsetStand34,
            LedId.HeadsetStand35,
            LedId.HeadsetStand36,
            LedId.HeadsetStand37,
            LedId.HeadsetStand38,
            LedId.HeadsetStand39,
            LedId.HeadsetStand40,
            LedId.HeadsetStand41,
            LedId.HeadsetStand42,
            LedId.HeadsetStand43,
            LedId.HeadsetStand44,
            LedId.HeadsetStand45,
            LedId.HeadsetStand46,
            LedId.HeadsetStand47,
            LedId.HeadsetStand48,
            LedId.HeadsetStand49,
            LedId.HeadsetStand50,
            LedId.HeadsetStand51,
            LedId.HeadsetStand52,
            LedId.HeadsetStand53,
            LedId.HeadsetStand54,
            LedId.HeadsetStand55,
            LedId.HeadsetStand56,
            LedId.HeadsetStand57,
            LedId.HeadsetStand58,
            LedId.HeadsetStand59,
            LedId.HeadsetStand60,
            LedId.HeadsetStand61,
            LedId.HeadsetStand62,
            LedId.HeadsetStand63,
            LedId.HeadsetStand64
        };

        private static List<LedId> AllKeypadKeys = new List<LedId>()
        {
            LedId.Keypad1,
            LedId.Keypad2,
            LedId.Keypad3,
            LedId.Keypad4,
            LedId.Keypad5,
            LedId.Keypad6,
            LedId.Keypad7,
            LedId.Keypad8,
            LedId.Keypad9,
            LedId.Keypad10,
            LedId.Keypad11,
            LedId.Keypad12,
            LedId.Keypad13,
            LedId.Keypad14,
            LedId.Keypad15,
            LedId.Keypad16,
            LedId.Keypad17,
            LedId.Keypad18,
            LedId.Keypad19,
            LedId.Keypad20,
            LedId.Keypad21,
            LedId.Keypad22,
            LedId.Keypad23,
            LedId.Keypad24,
            LedId.Keypad25,
            LedId.Keypad26,
            LedId.Keypad27,
            LedId.Keypad28,
            LedId.Keypad29,
            LedId.Keypad30,
            LedId.Keypad31,
            LedId.Keypad32,
            LedId.Keypad33,
            LedId.Keypad34,
            LedId.Keypad35,
            LedId.Keypad36,
            LedId.Keypad37,
            LedId.Keypad38,
            LedId.Keypad39,
            LedId.Keypad40,
            LedId.Keypad41,
            LedId.Keypad42,
            LedId.Keypad43,
            LedId.Keypad44,
            LedId.Keypad45,
            LedId.Keypad46,
            LedId.Keypad47,
            LedId.Keypad48,
            LedId.Keypad49,
            LedId.Keypad50,
            LedId.Keypad51,
            LedId.Keypad52,
            LedId.Keypad53,
            LedId.Keypad54,
            LedId.Keypad55,
            LedId.Keypad56,
            LedId.Keypad57,
            LedId.Keypad58,
            LedId.Keypad59,
            LedId.Keypad60,
            LedId.Keypad61,
            LedId.Keypad62,
            LedId.Keypad63,
            LedId.Keypad64
        };

        private static List<LedId> AllFanKeys = new List<LedId>()
        {
            LedId.Fan1,
            LedId.Fan2,
            LedId.Fan3,
            LedId.Fan4,
            LedId.Fan5,
            LedId.Fan6,
            LedId.Fan7,
            LedId.Fan8,
            LedId.Fan9,
            LedId.Fan10,
            LedId.Fan11,
            LedId.Fan12,
            LedId.Fan13,
            LedId.Fan14,
            LedId.Fan15,
            LedId.Fan16,
            LedId.Fan17,
            LedId.Fan18,
            LedId.Fan19,
            LedId.Fan20,
            LedId.Fan21,
            LedId.Fan22,
            LedId.Fan23,
            LedId.Fan24,
            LedId.Fan25,
            LedId.Fan26,
            LedId.Fan27,
            LedId.Fan28,
            LedId.Fan29,
            LedId.Fan30,
            LedId.Fan31,
            LedId.Fan32,
            LedId.Fan33,
            LedId.Fan34,
            LedId.Fan35,
            LedId.Fan36,
            LedId.Fan37,
            LedId.Fan38,
            LedId.Fan39,
            LedId.Fan40,
            LedId.Fan41,
            LedId.Fan42,
            LedId.Fan43,
            LedId.Fan44,
            LedId.Fan45,
            LedId.Fan46,
            LedId.Fan47,
            LedId.Fan48,
            LedId.Fan49,
            LedId.Fan50,
            LedId.Fan51,
            LedId.Fan52,
            LedId.Fan53,
            LedId.Fan54,
            LedId.Fan55,
            LedId.Fan56,
            LedId.Fan57,
            LedId.Fan58,
            LedId.Fan59,
            LedId.Fan60,
            LedId.Fan61,
            LedId.Fan62,
            LedId.Fan63,
            LedId.Fan64
        };

        private static List<LedId> AllSpeakerKeys = new List<LedId>()
        {
            LedId.Speaker1,
            LedId.Speaker2,
            LedId.Speaker3,
            LedId.Speaker4,
            LedId.Speaker5,
            LedId.Speaker6,
            LedId.Speaker7,
            LedId.Speaker8,
            LedId.Speaker9,
            LedId.Speaker10,
            LedId.Speaker11,
            LedId.Speaker12,
            LedId.Speaker13,
            LedId.Speaker14,
            LedId.Speaker15,
            LedId.Speaker16,
            LedId.Speaker17,
            LedId.Speaker18,
            LedId.Speaker19,
            LedId.Speaker20,
            LedId.Speaker21,
            LedId.Speaker22,
            LedId.Speaker23,
            LedId.Speaker24,
            LedId.Speaker25,
            LedId.Speaker26,
            LedId.Speaker27,
            LedId.Speaker28,
            LedId.Speaker29,
            LedId.Speaker30,
            LedId.Speaker31,
            LedId.Speaker32,
            LedId.Speaker33,
            LedId.Speaker34,
            LedId.Speaker35,
            LedId.Speaker36,
            LedId.Speaker37,
            LedId.Speaker38,
            LedId.Speaker39,
            LedId.Speaker40,
            LedId.Speaker41,
            LedId.Speaker42,
            LedId.Speaker43,
            LedId.Speaker44,
            LedId.Speaker45,
            LedId.Speaker46,
            LedId.Speaker47,
            LedId.Speaker48,
            LedId.Speaker49,
            LedId.Speaker50,
            LedId.Speaker51,
            LedId.Speaker52,
            LedId.Speaker53,
            LedId.Speaker54,
            LedId.Speaker55,
            LedId.Speaker56,
            LedId.Speaker57,
            LedId.Speaker58,
            LedId.Speaker59,
            LedId.Speaker60,
            LedId.Speaker61,
            LedId.Speaker62,
            LedId.Speaker63,
            LedId.Speaker64
        };

        private static List<LedId> AllCoolerKeys = new List<LedId>()
        {
            LedId.Cooler1,
            LedId.Cooler2,
            LedId.Cooler3,
            LedId.Cooler4,
            LedId.Cooler5,
            LedId.Cooler6,
            LedId.Cooler7,
            LedId.Cooler8,
            LedId.Cooler9,
            LedId.Cooler10,
            LedId.Cooler11,
            LedId.Cooler12,
            LedId.Cooler13,
            LedId.Cooler14,
            LedId.Cooler15,
            LedId.Cooler16,
            LedId.Cooler17,
            LedId.Cooler18,
            LedId.Cooler19,
            LedId.Cooler20,
            LedId.Cooler21,
            LedId.Cooler22,
            LedId.Cooler23,
            LedId.Cooler24,
            LedId.Cooler25,
            LedId.Cooler26,
            LedId.Cooler27,
            LedId.Cooler28,
            LedId.Cooler29,
            LedId.Cooler30,
            LedId.Cooler31,
            LedId.Cooler32,
            LedId.Cooler33,
            LedId.Cooler34,
            LedId.Cooler35,
            LedId.Cooler36,
            LedId.Cooler37,
            LedId.Cooler38,
            LedId.Cooler39,
            LedId.Cooler40,
            LedId.Cooler41,
            LedId.Cooler42,
            LedId.Cooler43,
            LedId.Cooler44,
            LedId.Cooler45,
            LedId.Cooler46,
            LedId.Cooler47,
            LedId.Cooler48,
            LedId.Cooler49,
            LedId.Cooler50,
            LedId.Cooler51,
            LedId.Cooler52,
            LedId.Cooler53,
            LedId.Cooler54,
            LedId.Cooler55,
            LedId.Cooler56,
            LedId.Cooler57,
            LedId.Cooler58,
            LedId.Cooler59,
            LedId.Cooler60,
            LedId.Cooler61,
            LedId.Cooler62,
            LedId.Cooler63,
            LedId.Cooler64

        };

        private static List<LedId> AllCustomKeys = new List<LedId>()
        {
            LedId.Custom1,
            LedId.Custom2,
            LedId.Custom3,
            LedId.Custom4,
            LedId.Custom5,
            LedId.Custom6,
            LedId.Custom7,
            LedId.Custom8,
            LedId.Custom9,
            LedId.Custom10,
            LedId.Custom11,
            LedId.Custom12,
            LedId.Custom13,
            LedId.Custom14,
            LedId.Custom15,
            LedId.Custom16,
            LedId.Custom17,
            LedId.Custom18,
            LedId.Custom19,
            LedId.Custom20,
            LedId.Custom21,
            LedId.Custom22,
            LedId.Custom23,
            LedId.Custom24,
            LedId.Custom25,
            LedId.Custom26,
            LedId.Custom27,
            LedId.Custom28,
            LedId.Custom29,
            LedId.Custom30,
            LedId.Custom31,
            LedId.Custom32,
            LedId.Custom33,
            LedId.Custom34,
            LedId.Custom35,
            LedId.Custom36,
            LedId.Custom37,
            LedId.Custom38,
            LedId.Custom39,
            LedId.Custom40,
            LedId.Custom41,
            LedId.Custom42,
            LedId.Custom43,
            LedId.Custom44,
            LedId.Custom45,
            LedId.Custom46,
            LedId.Custom47,
            LedId.Custom48,
            LedId.Custom49,
            LedId.Custom50,
            LedId.Custom51,
            LedId.Custom52,
            LedId.Custom53,
            LedId.Custom54,
            LedId.Custom55,
            LedId.Custom56,
            LedId.Custom57,
            LedId.Custom58,
            LedId.Custom59,
            LedId.Custom60,
            LedId.Custom61,
            LedId.Custom62,
            LedId.Custom63,
            LedId.Custom64,
            LedId.Custom65,
            LedId.Custom66,
            LedId.Custom67,
            LedId.Custom68,
            LedId.Custom69,
            LedId.Custom70,
            LedId.Custom71,
            LedId.Custom72,
            LedId.Custom73,
            LedId.Custom74,
            LedId.Custom75,
            LedId.Custom76,
            LedId.Custom77,
            LedId.Custom78,
            LedId.Custom79,
            LedId.Custom80,
            LedId.Custom81,
            LedId.Custom82,
            LedId.Custom83,
            LedId.Custom84,
            LedId.Custom85,
            LedId.Custom86,
            LedId.Custom87,
            LedId.Custom88,
            LedId.Custom89,
            LedId.Custom90,
            LedId.Custom91,
            LedId.Custom92,
            LedId.Custom93,
            LedId.Custom94,
            LedId.Custom95,
            LedId.Custom96,
            LedId.Custom97,
            LedId.Custom98,
            LedId.Custom99,
            LedId.Custom100,
            LedId.Custom101,
            LedId.Custom102,
            LedId.Custom103,
            LedId.Custom104,
            LedId.Custom105,
            LedId.Custom106,
            LedId.Custom107,
            LedId.Custom108,
            LedId.Custom109,
            LedId.Custom110,
            LedId.Custom111,
            LedId.Custom112,
            LedId.Custom113,
            LedId.Custom114,
            LedId.Custom115,
            LedId.Custom116,
            LedId.Custom117,
            LedId.Custom118,
            LedId.Custom119,
            LedId.Custom120,
            LedId.Custom121,
            LedId.Custom122,
            LedId.Custom123,
            LedId.Custom124,
            LedId.Custom125,
            LedId.Custom126,
            LedId.Custom127,
            LedId.Custom128
        };

        private static List<LedId> AllUnknownKeys = new List<LedId>()
        {
            LedId.Unknown1,
            LedId.Unknown2,
            LedId.Unknown3,
            LedId.Unknown4,
            LedId.Unknown5,
            LedId.Unknown6,
            LedId.Unknown7,
            LedId.Unknown8,
            LedId.Unknown9,
            LedId.Unknown10,
            LedId.Unknown11,
            LedId.Unknown12,
            LedId.Unknown13,
            LedId.Unknown14,
            LedId.Unknown15,
            LedId.Unknown16,
            LedId.Unknown17,
            LedId.Unknown18,
            LedId.Unknown19,
            LedId.Unknown20,
            LedId.Unknown21,
            LedId.Unknown22,
            LedId.Unknown23,
            LedId.Unknown24,
            LedId.Unknown25,
            LedId.Unknown26,
            LedId.Unknown27,
            LedId.Unknown28,
            LedId.Unknown29,
            LedId.Unknown30,
            LedId.Unknown31,
            LedId.Unknown32,
            LedId.Unknown33,
            LedId.Unknown34,
            LedId.Unknown35,
            LedId.Unknown36,
            LedId.Unknown37,
            LedId.Unknown38,
            LedId.Unknown39,
            LedId.Unknown40,
            LedId.Unknown41,
            LedId.Unknown42,
            LedId.Unknown43,
            LedId.Unknown44,
            LedId.Unknown45,
            LedId.Unknown46,
            LedId.Unknown47,
            LedId.Unknown48,
            LedId.Unknown49,
            LedId.Unknown50,
            LedId.Unknown51,
            LedId.Unknown52,
            LedId.Unknown53,
            LedId.Unknown54,
            LedId.Unknown55,
            LedId.Unknown56,
            LedId.Unknown57,
            LedId.Unknown58,
            LedId.Unknown59,
            LedId.Unknown60,
            LedId.Unknown61,
            LedId.Unknown62,
            LedId.Unknown63,
            LedId.Unknown64,
            LedId.Unknown65,
            LedId.Unknown66,
            LedId.Unknown67,
            LedId.Unknown68,
            LedId.Unknown69,
            LedId.Unknown70,
            LedId.Unknown71,
            LedId.Unknown72,
            LedId.Unknown73,
            LedId.Unknown74,
            LedId.Unknown75,
            LedId.Unknown76,
            LedId.Unknown77,
            LedId.Unknown78,
            LedId.Unknown79,
            LedId.Unknown80,
            LedId.Unknown81,
            LedId.Unknown82,
            LedId.Unknown83,
            LedId.Unknown84,
            LedId.Unknown85,
            LedId.Unknown86,
            LedId.Unknown87,
            LedId.Unknown88,
            LedId.Unknown89,
            LedId.Unknown90,
            LedId.Unknown91,
            LedId.Unknown92,
            LedId.Unknown93,
            LedId.Unknown94,
            LedId.Unknown95,
            LedId.Unknown96,
            LedId.Unknown97,
            LedId.Unknown98,
            LedId.Unknown99,
            LedId.Unknown100,
            LedId.Unknown101,
            LedId.Unknown102,
            LedId.Unknown103,
            LedId.Unknown104,
            LedId.Unknown105,
            LedId.Unknown106,
            LedId.Unknown107,
            LedId.Unknown108,
            LedId.Unknown109,
            LedId.Unknown110,
            LedId.Unknown111,
            LedId.Unknown112,
            LedId.Unknown113,
            LedId.Unknown114,
            LedId.Unknown115,
            LedId.Unknown116,
            LedId.Unknown117,
            LedId.Unknown118,
            LedId.Unknown119,
            LedId.Unknown120,
            LedId.Unknown121,
            LedId.Unknown122,
            LedId.Unknown123,
            LedId.Unknown124,
            LedId.Unknown125,
            LedId.Unknown126,
            LedId.Unknown127,
            LedId.Unknown128
        };
    }
}
