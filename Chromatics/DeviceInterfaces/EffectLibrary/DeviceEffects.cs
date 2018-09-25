using System;
using System.Collections.Generic;
using System.Drawing;

namespace Chromatics.DeviceInterfaces.EffectLibrary
{
    public class DeviceEffects
    {
        //Key Groups

        //All Global Keys
        public static readonly string[] GlobalKeysAll =
        {
            "Y", "D5", "D6", "D7", "T", "U", "G", "H", "J", "D4", "D8", "R", "F", "C",
            "V", "B", "N", "M", "K", "I", "D3", "E", "D", "X", "Space", "OemComma", "L", "O", "D9",
            "D2", "D0", "W", "S", "Z", "LeftAlt", "P", "OemSemicolon", "OemPeriod", "RightAlt", "D1", "Q", "A",
            "LeftWindows", "OemMinus", "OemLeftBracket", "OemApostrophe", "OemSlash", "Function", "Escape",
            "OemTilde", "Tab", "CapsLock", "LeftShift", "LeftControl", "OemEquals", "RightMenu",
            "OemRightBracket", "Macro1", "Macro2", "Macro3", "Macro4", "Macro5", "Backspace", "OemBackslash",
            "Enter", "RightShift", "RightControl", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "NumLock", "Num0", "Num1",
            "Num2", "Num3", "Num4", "Num5", "Num6", "Num7", "Num8", "Num9", "NumDivide", "NumMultiply", "NumSubtract",
            "NumAdd", "NumEnter", "NumDecimal", "PrintScreen", "Scroll", "Pause", "Insert", "Home", "PageUp", "PageDown", "Delete", "End", "Up", "Down", "Right", "Left",
            "Macro6", "Macro7", "Macro8", "Macro9", "Macro10","Macro11", "Macro12", "Macro13", "Macro14", "Macro15", "Macro16", "Macro17", "Macro18",
            "Lightbar1", "Lightbar2", "Lightbar3", "Lightbar4", "Lightbar5", "Lightbar6", "Lightbar7", "Lightbar8", "Lightbar9", "Lightbar10", "Lightbar11", "Lightbar12", "Lightbar13", "Lightbar14", "Lightbar15", "Lightbar16",
            "Lightbar17", "Lightbar18", "Lightbar19"
        };

        //All Keys
        public static readonly string[] GlobalKeys =
        {
            "Y", "D5", "D6", "D7", "T", "U", "G", "H", "J", "F3", "F4", "F5", "F6", "F7", "D4", "D8", "R", "F", "C",
            "V", "B", "N", "M", "K", "I", "F2", "F8", "D3", "E", "D", "X", "Space", "OemComma", "L", "O", "D9", "F1",
            "F9", "D2", "D0", "W", "S", "Z", "LeftAlt", "P", "OemSemicolon", "OemPeriod", "RightAlt", "D1", "Q", "A",
            "LeftWindows", "F10", "OemMinus", "OemLeftBracket", "OemApostrophe", "OemSlash", "Function", "Escape",
            "OemTilde", "Tab", "CapsLock", "LeftShift", "LeftControl", "F11", "OemEquals", "RightMenu",
            "OemRightBracket", "Macro1", "Macro2", "Macro3", "Macro4", "Macro5", "F12", "Backspace", "OemBackslash",
            "Enter", "RightShift", "RightControl"
        };

        //Central Keys
        public static readonly string[] GlobalKeys2 =
        {
            "Y", "D5", "D6", "D7", "T", "U", "G", "H", "J", "D4", "D8", "R", "F", "C", "V", "B", "N", "M", "K", "I",
            "D3", "E", "D", "X", "Space", "OemComma", "L", "O", "D9", "D2", "D0", "W", "S", "Z", "LeftAlt", "P",
            "OemSemicolon", "OemPeriod", "RightAlt", "D1", "Q", "A", "LeftWindows", "OemMinus", "OemLeftBracket",
            "OemApostrophe", "OemSlash", "Function", "Escape", "OemTilde", "Tab", "CapsLock", "LeftShift",
            "LeftControl", "OemEquals", "RightMenu", "OemRightBracket", "Macro1", "Macro2", "Macro3", "Macro4",
            "Macro5", "Backspace", "OemBackslash", "Enter", "RightShift", "RightControl"
        };

        //Outer Keys
        public static readonly string[] GlobalKeys3 =
        {
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "NumLock", "Num0", "Num1",
            "Num2", "Num3", "Num4", "Num5", "Num6", "Num7", "Num8", "Num9", "NumDivide", "NumMultiply", "NumSubtract",
            "NumAdd", "NumEnter", "NumDecimal"
        };

        //Pulse Out From Centre
        public static readonly string[] PulseOutStep0 = {"Y"};

        public static readonly string[] PulseOutStep1 = {"D5", "D6", "D7", "T", "U", "G", "H", "J"};

        public static readonly string[] PulseOutStep2 =
            {"F3", "F4", "F5", "F6", "F7", "D4", "D8", "R", "F", "C", "V", "B", "N", "M", "K", "I"};

        public static readonly string[] PulseOutStep3 =
            {"F2", "F8", "D3", "E", "D", "X", "Space", "OemComma", "L", "O", "D9"};

        public static readonly string[] PulseOutStep4 =
            {"F1", "F9", "D2", "D0", "W", "S", "Z", "LeftAlt", "P", "OemSemicolon", "OemPeriod", "RightAlt"};

        public static readonly string[] PulseOutStep5 =
        {
            "D1", "Q", "A", "LeftWindows", "F10", "OemMinus", "OemLeftBracket", "OemApostrophe", "OemSlash", "Function"
        };

        public static readonly string[] PulseOutStep6 =
        {
            "Escape", "OemTilde", "Tab", "CapsLock", "LeftShift", "LeftControl", "F11", "OemEquals", "RightMenu",
            "OemRightBracket", "OemBackslash"
        };

        public static readonly string[] PulseOutStep7 =
        {
            "Macro1", "Macro2", "Macro3", "Macro4", "Macro5", "F12", "Backspace", "Enter", "RightShift", "RightControl"
        };

        //Numpad Flash Effect
        public static readonly string[] NumFlash =
        {
            "NumLock", "NumDivide", "NumMultiply", "Num7", "Num8", "Num9", "Num4", "Num5", "Num6", "Num1", "Num2",
            "Num3"
        };

        public static readonly string[] Functions =
        {
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"
        };

        public static readonly string[] FunctionL =
        {
            "F1", "F2", "F3", "F4", "F5", "F6"
        };

        public static readonly string[] FunctionR =
        {
            "F7", "F8", "F9", "F10", "F11", "F12"
        };

        public static readonly string[] Function1 =
        {
            "F1", "F2", "F3", "F4"
        };

        public static readonly string[] Function2 =
        {
            "F5", "F6", "F7", "F8"
        };

        public static readonly string[] Function3 =
        {
            "F9", "F10", "F11", "F12"
        };

        public static readonly string[] Keypadzones =
        {
            "0", "1", "2", "3", "4"
        };

        public static readonly string[] Multikeyzones =
        {
            "0", "1", "2", "3", "4", "5"
        };

        public static readonly string[] MouseStripsLeft =
        {
            "LeftSide7", "LeftSide6", "LeftSide5", "LeftSide4", "LeftSide3", "LeftSide2", "LeftSide1"
        };

        public static readonly string[] MouseStripsRight =
        {
            "RightSide7", "RightSide6", "RightSide5", "RightSide4", "RightSide3", "RightSide2", "RightSide1"
        };

        public static readonly string[] LightbarZones =
        {
            "Lightbar1", "Lightbar2", "Lightbar3", "Lightbar4", "Lightbar5", "Lightbar6", "Lightbar7", "Lightbar8", "Lightbar9", "Lightbar10", "Lightbar11", "Lightbar12", "Lightbar13", "Lightbar14", "Lightbar15", "Lightbar16", "Lightbar17", "Lightbar18", "Lightbar19"
        };

        public static readonly string[] LightbarZonesL =
        {
            "Lightbar1", "Lightbar2", "Lightbar3", "Lightbar4", "Lightbar5", "Lightbar6", "Lightbar7", "Lightbar8", "Lightbar9", "Lightbar10"
        };

        public static readonly string[] LightbarZonesR =
        {
            "Lightbar11", "Lightbar12", "Lightbar13", "Lightbar14", "Lightbar15", "Lightbar16", "Lightbar17", "Lightbar18", "Lightbar19"
        };

        public static readonly string[] MacroTarget =
        {
            "Macro5", "Macro4", "Macro3", "Macro2", "Macro1"
        };
    }

    public class ColorFader
    {
        private readonly Color _From;
        private readonly Color _To;

        private readonly double _StepR;
        private readonly double _StepG;
        private readonly double _StepB;

        private readonly uint _Steps;

        public ColorFader(Color from, Color to, uint steps)
        {
            if (steps == 0)
                throw new ArgumentException("Steps must be a positive number");

            _From = from;
            _To = to;
            _Steps = steps;

            _StepR = (double)(_To.R - _From.R) / _Steps;
            _StepG = (double)(_To.G - _From.G) / _Steps;
            _StepB = (double)(_To.B - _From.B) / _Steps;
        }

        public IEnumerable<Color> Fade()
        {
            for (uint i = 0; i < _Steps; ++i)
            {
                yield return Color.FromArgb((int)(_From.R + i * _StepR), (int)(_From.G + i * _StepG), (int)(_From.B + i * _StepB));
            }
            yield return _To; // make sure we always return the exact target color last
        }
    }
}