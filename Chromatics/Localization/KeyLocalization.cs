using Chromatics.Enums;
using Chromatics.Models;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Localization
{
    public static class KeyLocalization
    {
        public static List<KeyboardKey> GetLocalizedKeys(KeyboardLocalization locale)
        {
            switch (locale)
            {
                case KeyboardLocalization.qwerty:
                    return QWERTY;
                case KeyboardLocalization.qwertz:
                    return QWERTZ;
                case KeyboardLocalization.azerty:
                    return AZERTY;
                default:
                    return QWERTY;
            }
        }
        
        private static List<KeyboardKey> QWERTY = new List<KeyboardKey>()
        {
            new KeyboardKey("ESC", LedId.Keyboard_Escape),

            new KeyboardKey("F1", LedId.Keyboard_F1, true, false, 12, 19),
            new KeyboardKey("F2", LedId.Keyboard_F2),
            new KeyboardKey("F3", LedId.Keyboard_F3),
            new KeyboardKey("F4", LedId.Keyboard_F4),

            new KeyboardKey("F5", LedId.Keyboard_F5, true, false, 12, 34),
            new KeyboardKey("F6", LedId.Keyboard_F6),
            new KeyboardKey("F7", LedId.Keyboard_F7),
            new KeyboardKey("F8", LedId.Keyboard_F8),

            new KeyboardKey("F9", LedId.Keyboard_F9, true, false, 12, 29),
            new KeyboardKey("F10", LedId.Keyboard_F10),
            new KeyboardKey("F11", LedId.Keyboard_F11),
            new KeyboardKey("F12", LedId.Keyboard_F12),

            new KeyboardKey("PRINT", LedId.Keyboard_PrintScreen, true, false, 9, 12),
            new KeyboardKey("SCRL\r\nLOCK", LedId.Keyboard_ScrollLock, true, false, 9),
            new KeyboardKey("PAUSE", LedId.Keyboard_PauseBreak, true, true, 9),

            new KeyboardKey("~", LedId.Keyboard_GraveAccentAndTilde),
            new KeyboardKey("1", LedId.Keyboard_1),
            new KeyboardKey("2", LedId.Keyboard_2),
            new KeyboardKey("3", LedId.Keyboard_3),
            new KeyboardKey("4", LedId.Keyboard_4),
            new KeyboardKey("5", LedId.Keyboard_5),
            new KeyboardKey("6", LedId.Keyboard_6),
            new KeyboardKey("7", LedId.Keyboard_7),
            new KeyboardKey("8", LedId.Keyboard_8),
            new KeyboardKey("9", LedId.Keyboard_9),
            new KeyboardKey("0", LedId.Keyboard_0),
            new KeyboardKey("-", LedId.Keyboard_MinusAndUnderscore),
            new KeyboardKey("=", LedId.Keyboard_EqualsAndPlus),
            new KeyboardKey("BACKSPACE", LedId.Keyboard_Backspace, true, false, 12, 7, 0, 77),

            new KeyboardKey("INS", LedId.Keyboard_Insert, true, false, 9, 12),
            new KeyboardKey("HOME", LedId.Keyboard_Home, true, false, 9),
            new KeyboardKey("PAGE\r\nUP", LedId.Keyboard_PageUp, true, false, 9),

            new KeyboardKey("NUM\r\nLOCK", LedId.Keyboard_NumLock, true, false, 9, 14),
            new KeyboardKey("/", LedId.Keyboard_NumSlash),
            new KeyboardKey("*", LedId.Keyboard_NumAsterisk),
            new KeyboardKey("-", LedId.Keyboard_NumMinus, true, true),

            new KeyboardKey("TAB", LedId.Keyboard_Tab, true, false, 12, 7, 0, 50),
            new KeyboardKey("Q", LedId.Keyboard_Q),
            new KeyboardKey("W", LedId.Keyboard_W),
            new KeyboardKey("E", LedId.Keyboard_E),
            new KeyboardKey("R", LedId.Keyboard_R),
            new KeyboardKey("T", LedId.Keyboard_T),
            new KeyboardKey("Y", LedId.Keyboard_Y),
            new KeyboardKey("U", LedId.Keyboard_U),
            new KeyboardKey("I", LedId.Keyboard_I),
            new KeyboardKey("O", LedId.Keyboard_O),
            new KeyboardKey("P", LedId.Keyboard_P),
            new KeyboardKey("{", LedId.Keyboard_BracketLeft),
            new KeyboardKey("}", LedId.Keyboard_BracketRight),
            new KeyboardKey("\\", LedId.Keyboard_Backslash, true, false, 12, 7, 0, 57),

            new KeyboardKey("DEL", LedId.Keyboard_Delete, true, false, 9, 12),
            new KeyboardKey("END", LedId.Keyboard_End, true, false, 9),
            new KeyboardKey("PAGE\r\nDOWN", LedId.Keyboard_PageDown, true, false, 9),

            new KeyboardKey("7", LedId.Keyboard_Num7, true, false, 12, 14),
            new KeyboardKey("8", LedId.Keyboard_Num8),
            new KeyboardKey("9", LedId.Keyboard_Num9),
            new KeyboardKey("+", LedId.Keyboard_NumPlus, true, true, 12, 7, 0, 30, 69),

            new KeyboardKey("CAPS\r\nLOCK", LedId.Keyboard_CapsLock, true, false, 9, 7, 0, 55),
            new KeyboardKey("A", LedId.Keyboard_A),
            new KeyboardKey("S", LedId.Keyboard_S),
            new KeyboardKey("D", LedId.Keyboard_D),
            new KeyboardKey("F", LedId.Keyboard_F),
            new KeyboardKey("G", LedId.Keyboard_G),
            new KeyboardKey("H", LedId.Keyboard_H),
            new KeyboardKey("J", LedId.Keyboard_J),
            new KeyboardKey("K", LedId.Keyboard_K),
            new KeyboardKey("L", LedId.Keyboard_L),
            new KeyboardKey(":", LedId.Keyboard_SemicolonAndColon),
            new KeyboardKey("\"", LedId.Keyboard_ApostropheAndDoubleQuote),
            new KeyboardKey("ENTER", LedId.Keyboard_Enter, true, false, 12, 7, 0, 87),

            new KeyboardKey("4", LedId.Keyboard_Num4, true, false, 12, 131),
            new KeyboardKey("5", LedId.Keyboard_Num5),
            new KeyboardKey("6", LedId.Keyboard_Num6, true, true),

            new KeyboardKey("SHIFT", LedId.Keyboard_LeftShift, true, false, 12, 7, 0, 75),
            new KeyboardKey("Z", LedId.Keyboard_Z),
            new KeyboardKey("X", LedId.Keyboard_X),
            new KeyboardKey("C", LedId.Keyboard_C),
            new KeyboardKey("V", LedId.Keyboard_V),
            new KeyboardKey("B", LedId.Keyboard_B),
            new KeyboardKey("N", LedId.Keyboard_N),
            new KeyboardKey("M", LedId.Keyboard_M),
            new KeyboardKey("<", LedId.Keyboard_CommaAndLessThan),
            new KeyboardKey(">", LedId.Keyboard_PeriodAndBiggerThan),
            new KeyboardKey("?", LedId.Keyboard_SlashAndQuestionMark),
            new KeyboardKey("SHIFT", LedId.Keyboard_RightShift, true, false, 12, 7, 0, 102),

            new KeyboardKey("UP", LedId.Keyboard_ArrowUp, true, false, 9, 47),

            new KeyboardKey("1", LedId.Keyboard_Num1, true, false, 12, 49),
            new KeyboardKey("2", LedId.Keyboard_Num2),
            new KeyboardKey("3", LedId.Keyboard_Num3),
            new KeyboardKey("ENT", LedId.Keyboard_NumEnter, true, true, 9, 7, 0, 30, 69),

            new KeyboardKey("CTRL", LedId.Keyboard_LeftCtrl, true, false, 12, 7, 0, 49),
            new KeyboardKey("WIN", LedId.Keyboard_LeftGui, true, false, 12, 5, 0, 39),
            new KeyboardKey("ALT", LedId.Keyboard_LeftAlt, true, false, 12, 5, 0, 42),

            new KeyboardKey("SPACE", LedId.Keyboard_Space, true, false, 12, 7, 0, 189),
            new KeyboardKey("ALT", LedId.Keyboard_RightAlt, true, false, 12, 5, 0, 41),
            new KeyboardKey("FN", LedId.Keyboard_RightGui, true, false, 12, 5, 0, 47),
            new KeyboardKey("APP", LedId.Keyboard_Application, true, false, 12, 5, 0, 45),
            new KeyboardKey("CTRL", LedId.Keyboard_RightCtrl, true, false, 12, 5, 0, 45),

            new KeyboardKey("LEFT", LedId.Keyboard_ArrowLeft, true, false, 9, 12),
            new KeyboardKey("DOWN", LedId.Keyboard_ArrowDown, true, false, 9),
            new KeyboardKey("RIGHT", LedId.Keyboard_ArrowRight, true, false, 9),

            new KeyboardKey("0", LedId.Keyboard_Num0, true, false, 12, 14, 0, 65),
            new KeyboardKey(".", LedId.Keyboard_NumPeriodAndDelete, true, true),

            new KeyboardKey("M1", LedId.Keyboard_Programmable1, true, false),
            new KeyboardKey("M2", LedId.Keyboard_Programmable2, true, false),
            new KeyboardKey("M3", LedId.Keyboard_Programmable3, true, false),
            new KeyboardKey("M4", LedId.Keyboard_Programmable4, true, false),
            new KeyboardKey("M5", LedId.Keyboard_Programmable5, true, false),
            new KeyboardKey("M6", LedId.Keyboard_Programmable6, true, false),
            new KeyboardKey("M7", LedId.Keyboard_Programmable7, true, false),
            new KeyboardKey("M8", LedId.Keyboard_Programmable8, true, false),
            new KeyboardKey("M9", LedId.Keyboard_Programmable9, true, false),
            new KeyboardKey("M10", LedId.Keyboard_Programmable10, true, false),
            new KeyboardKey("M11", LedId.Keyboard_Programmable11, true, false),
            new KeyboardKey("M12", LedId.Keyboard_Programmable12, true, false),
            new KeyboardKey("M13", LedId.Keyboard_Programmable13, true, false),
            new KeyboardKey("M14", LedId.Keyboard_Programmable14, true, false),
            new KeyboardKey("M15", LedId.Keyboard_Programmable15, true, false),
            new KeyboardKey("M16", LedId.Keyboard_Programmable16, true, false),
            new KeyboardKey("M17", LedId.Keyboard_Programmable17, true, false),
            new KeyboardKey("M18", LedId.Keyboard_Programmable18, true, false),
            new KeyboardKey("M19", LedId.Keyboard_Programmable19, true, false),
            new KeyboardKey("M20", LedId.Keyboard_Programmable20, true, false),
            new KeyboardKey("M21", LedId.Keyboard_Programmable21, true, false),
            new KeyboardKey("M22", LedId.Keyboard_Programmable22, true, false),
            new KeyboardKey("Logo", LedId.Logo, true, true),
        };

        private static List<KeyboardKey> QWERTZ = new List<KeyboardKey>()
        {
            new KeyboardKey("ESC", LedId.Keyboard_Escape),

            new KeyboardKey("F1", LedId.Keyboard_F1, true, false, 12, 19),
            new KeyboardKey("F2", LedId.Keyboard_F2),
            new KeyboardKey("F3", LedId.Keyboard_F3),
            new KeyboardKey("F4", LedId.Keyboard_F4),

            new KeyboardKey("F5", LedId.Keyboard_F5, true, false, 12, 34),
            new KeyboardKey("F6", LedId.Keyboard_F6),
            new KeyboardKey("F7", LedId.Keyboard_F7),
            new KeyboardKey("F8", LedId.Keyboard_F8),

            new KeyboardKey("F9", LedId.Keyboard_F9, true, false, 12, 29),
            new KeyboardKey("F10", LedId.Keyboard_F10),
            new KeyboardKey("F11", LedId.Keyboard_F11),
            new KeyboardKey("F12", LedId.Keyboard_F12),

            new KeyboardKey("PRINT", LedId.Keyboard_PrintScreen, true, false, 9, 12),
            new KeyboardKey("SCRL\r\nLOCK", LedId.Keyboard_ScrollLock, true, false, 9),
            new KeyboardKey("PAUSE", LedId.Keyboard_PauseBreak, true, true, 9),

            new KeyboardKey("~", LedId.Keyboard_GraveAccentAndTilde),
            new KeyboardKey("1", LedId.Keyboard_1),
            new KeyboardKey("2", LedId.Keyboard_2),
            new KeyboardKey("3", LedId.Keyboard_3),
            new KeyboardKey("4", LedId.Keyboard_4),
            new KeyboardKey("5", LedId.Keyboard_5),
            new KeyboardKey("6", LedId.Keyboard_6),
            new KeyboardKey("7", LedId.Keyboard_7),
            new KeyboardKey("8", LedId.Keyboard_8),
            new KeyboardKey("9", LedId.Keyboard_9),
            new KeyboardKey("0", LedId.Keyboard_0),
            new KeyboardKey("-", LedId.Keyboard_MinusAndUnderscore),
            new KeyboardKey("=", LedId.Keyboard_EqualsAndPlus),
            new KeyboardKey("BACKSPACE", LedId.Keyboard_Backspace, true, false, 12, 7, 0, 77),

            new KeyboardKey("INS", LedId.Keyboard_Insert, true, false, 9, 12),
            new KeyboardKey("HOME", LedId.Keyboard_Home, true, false, 9),
            new KeyboardKey("PAGE\r\nUP", LedId.Keyboard_PageUp, true, false, 9),

            new KeyboardKey("NUM\r\nLOCK", LedId.Keyboard_NumLock, true, false, 9, 14),
            new KeyboardKey("/", LedId.Keyboard_NumSlash),
            new KeyboardKey("*", LedId.Keyboard_NumAsterisk),
            new KeyboardKey("-", LedId.Keyboard_NumMinus, true, true),

            new KeyboardKey("TAB", LedId.Keyboard_Tab, true, false, 12, 7, 0, 50),
            new KeyboardKey("Q", LedId.Keyboard_Q),
            new KeyboardKey("W", LedId.Keyboard_W),
            new KeyboardKey("E", LedId.Keyboard_E),
            new KeyboardKey("R", LedId.Keyboard_R),
            new KeyboardKey("T", LedId.Keyboard_T),
            new KeyboardKey("Z", LedId.Keyboard_Z),
            new KeyboardKey("U", LedId.Keyboard_U),
            new KeyboardKey("I", LedId.Keyboard_I),
            new KeyboardKey("O", LedId.Keyboard_O),
            new KeyboardKey("P", LedId.Keyboard_P),
            new KeyboardKey("{", LedId.Keyboard_BracketLeft),
            new KeyboardKey("}", LedId.Keyboard_BracketRight),
            new KeyboardKey("\\", LedId.Keyboard_Backslash, true, false, 12, 7, 0, 57),

            new KeyboardKey("DEL", LedId.Keyboard_Delete, true, false, 9, 12),
            new KeyboardKey("END", LedId.Keyboard_End, true, false, 9),
            new KeyboardKey("PAGE\r\nDOWN", LedId.Keyboard_PageDown, true, false, 9),

            new KeyboardKey("7", LedId.Keyboard_Num7, true, false, 12, 14),
            new KeyboardKey("8", LedId.Keyboard_Num8),
            new KeyboardKey("9", LedId.Keyboard_Num9),
            new KeyboardKey("+", LedId.Keyboard_NumPlus, true, true, 12, 7, 0, 30, 69),

            new KeyboardKey("CAPS\r\nLOCK", LedId.Keyboard_CapsLock, true, false, 9, 7, 0, 55),
            new KeyboardKey("A", LedId.Keyboard_A),
            new KeyboardKey("S", LedId.Keyboard_S),
            new KeyboardKey("D", LedId.Keyboard_D),
            new KeyboardKey("F", LedId.Keyboard_F),
            new KeyboardKey("G", LedId.Keyboard_G),
            new KeyboardKey("H", LedId.Keyboard_H),
            new KeyboardKey("J", LedId.Keyboard_J),
            new KeyboardKey("K", LedId.Keyboard_K),
            new KeyboardKey("L", LedId.Keyboard_L),
            new KeyboardKey(":", LedId.Keyboard_SemicolonAndColon),
            new KeyboardKey("\"", LedId.Keyboard_ApostropheAndDoubleQuote),
            new KeyboardKey("ENTER", LedId.Keyboard_Enter, true, false, 12, 7, 0, 87),

            new KeyboardKey("4", LedId.Keyboard_Num4, true, false, 12, 131),
            new KeyboardKey("5", LedId.Keyboard_Num5),
            new KeyboardKey("6", LedId.Keyboard_Num6, true, true),

            new KeyboardKey("SHIFT", LedId.Keyboard_LeftShift, true, false, 12, 7, 0, 75),
            new KeyboardKey("Y", LedId.Keyboard_Y),
            new KeyboardKey("X", LedId.Keyboard_X),
            new KeyboardKey("C", LedId.Keyboard_C),
            new KeyboardKey("V", LedId.Keyboard_V),
            new KeyboardKey("B", LedId.Keyboard_B),
            new KeyboardKey("N", LedId.Keyboard_N),
            new KeyboardKey("M", LedId.Keyboard_M),
            new KeyboardKey("<", LedId.Keyboard_CommaAndLessThan),
            new KeyboardKey(">", LedId.Keyboard_PeriodAndBiggerThan),
            new KeyboardKey("?", LedId.Keyboard_SlashAndQuestionMark),
            new KeyboardKey("SHIFT", LedId.Keyboard_RightShift, true, false, 12, 7, 0, 102),

            new KeyboardKey("UP", LedId.Keyboard_ArrowUp, true, false, 9, 47),

            new KeyboardKey("1", LedId.Keyboard_Num1, true, false, 12, 49),
            new KeyboardKey("2", LedId.Keyboard_Num2),
            new KeyboardKey("3", LedId.Keyboard_Num3),
            new KeyboardKey("ENT", LedId.Keyboard_NumEnter, true, true, 9, 7, 0, 30, 69),

            new KeyboardKey("CTRL", LedId.Keyboard_LeftCtrl, true, false, 12, 7, 0, 49),
            new KeyboardKey("WIN", LedId.Keyboard_LeftGui, true, false, 12, 5, 0, 39),
            new KeyboardKey("ALT", LedId.Keyboard_LeftAlt, true, false, 12, 5, 0, 42),

            new KeyboardKey("SPACE", LedId.Keyboard_Space, true, false, 12, 7, 0, 189),
            new KeyboardKey("ALT", LedId.Keyboard_RightAlt, true, false, 12, 5, 0, 41),
            new KeyboardKey("FN", LedId.Keyboard_RightGui, true, false, 12, 5, 0, 47),
            new KeyboardKey("APP", LedId.Keyboard_Application, true, false, 12, 5, 0, 45),
            new KeyboardKey("CTRL", LedId.Keyboard_RightCtrl, true, false, 12, 5, 0, 45),

            new KeyboardKey("LEFT", LedId.Keyboard_ArrowLeft, true, false, 9, 12),
            new KeyboardKey("DOWN", LedId.Keyboard_ArrowDown, true, false, 9),
            new KeyboardKey("RIGHT", LedId.Keyboard_ArrowRight, true, false, 9),

            new KeyboardKey("0", LedId.Keyboard_Num0, true, false, 12, 14, 0, 65),
            new KeyboardKey(".", LedId.Keyboard_NumPeriodAndDelete, true, true),

            new KeyboardKey("M1", LedId.Keyboard_Programmable1, true, false),
            new KeyboardKey("M2", LedId.Keyboard_Programmable2, true, false),
            new KeyboardKey("M3", LedId.Keyboard_Programmable3, true, false),
            new KeyboardKey("M4", LedId.Keyboard_Programmable4, true, false),
            new KeyboardKey("M5", LedId.Keyboard_Programmable5, true, false),
            new KeyboardKey("M6", LedId.Keyboard_Programmable6, true, false),
            new KeyboardKey("M7", LedId.Keyboard_Programmable7, true, false),
            new KeyboardKey("M8", LedId.Keyboard_Programmable8, true, false),
            new KeyboardKey("M9", LedId.Keyboard_Programmable9, true, false),
            new KeyboardKey("M10", LedId.Keyboard_Programmable10, true, false),
            new KeyboardKey("M11", LedId.Keyboard_Programmable11, true, false),
            new KeyboardKey("M12", LedId.Keyboard_Programmable12, true, false),
            new KeyboardKey("M13", LedId.Keyboard_Programmable13, true, false),
            new KeyboardKey("M14", LedId.Keyboard_Programmable14, true, false),
            new KeyboardKey("M15", LedId.Keyboard_Programmable15, true, false),
            new KeyboardKey("I1", LedId.Keyboard_International1, true, false),
            new KeyboardKey("I2", LedId.Keyboard_International2, true, false),
            new KeyboardKey("I3", LedId.Keyboard_International3, true, false),
            new KeyboardKey("I4", LedId.Keyboard_International4, true, false),
            new KeyboardKey("I5", LedId.Keyboard_International5, true, false),
            new KeyboardKey("I6", LedId.Keyboard_Lang1, true, false),
            new KeyboardKey("I7", LedId.Keyboard_Lang2, true, false),
            new KeyboardKey("Logo", LedId.Logo, true, true)
        };

        private static List<KeyboardKey> AZERTY = new List<KeyboardKey>()
        {
            new KeyboardKey("ESC", LedId.Keyboard_Escape),

            new KeyboardKey("F1", LedId.Keyboard_F1, true, false, 12, 19),
            new KeyboardKey("F2", LedId.Keyboard_F2),
            new KeyboardKey("F3", LedId.Keyboard_F3),
            new KeyboardKey("F4", LedId.Keyboard_F4),

            new KeyboardKey("F5", LedId.Keyboard_F5, true, false, 12, 34),
            new KeyboardKey("F6", LedId.Keyboard_F6),
            new KeyboardKey("F7", LedId.Keyboard_F7),
            new KeyboardKey("F8", LedId.Keyboard_F8),

            new KeyboardKey("F9", LedId.Keyboard_F9, true, false, 12, 29),
            new KeyboardKey("F10", LedId.Keyboard_F10),
            new KeyboardKey("F11", LedId.Keyboard_F11),
            new KeyboardKey("F12", LedId.Keyboard_F12),

            new KeyboardKey("PRINT", LedId.Keyboard_PrintScreen, true, false, 9, 12),
            new KeyboardKey("SCRL\r\nLOCK", LedId.Keyboard_ScrollLock, true, false, 9),
            new KeyboardKey("PAUSE", LedId.Keyboard_PauseBreak, true, true, 9),

            new KeyboardKey("~", LedId.Keyboard_GraveAccentAndTilde),
            new KeyboardKey("1", LedId.Keyboard_1),
            new KeyboardKey("2", LedId.Keyboard_2),
            new KeyboardKey("3", LedId.Keyboard_3),
            new KeyboardKey("4", LedId.Keyboard_4),
            new KeyboardKey("5", LedId.Keyboard_5),
            new KeyboardKey("6", LedId.Keyboard_6),
            new KeyboardKey("7", LedId.Keyboard_7),
            new KeyboardKey("8", LedId.Keyboard_8),
            new KeyboardKey("9", LedId.Keyboard_9),
            new KeyboardKey("0", LedId.Keyboard_0),
            new KeyboardKey("-", LedId.Keyboard_MinusAndUnderscore),
            new KeyboardKey("=", LedId.Keyboard_EqualsAndPlus),
            new KeyboardKey("BACKSPACE", LedId.Keyboard_Backspace, true, false, 12, 7, 0, 77),

            new KeyboardKey("INS", LedId.Keyboard_Insert, true, false, 9, 12),
            new KeyboardKey("HOME", LedId.Keyboard_Home, true, false, 9),
            new KeyboardKey("PAGE\r\nUP", LedId.Keyboard_PageUp, true, false, 9),

            new KeyboardKey("NUM\r\nLOCK", LedId.Keyboard_NumLock, true, false, 9, 14),
            new KeyboardKey("/", LedId.Keyboard_NumSlash),
            new KeyboardKey("*", LedId.Keyboard_NumAsterisk),
            new KeyboardKey("-", LedId.Keyboard_NumMinus, true, true),

            new KeyboardKey("TAB", LedId.Keyboard_Tab, true, false, 12, 7, 0, 50),
            new KeyboardKey("A", LedId.Keyboard_A),
            new KeyboardKey("Z", LedId.Keyboard_Z),
            new KeyboardKey("E", LedId.Keyboard_E),
            new KeyboardKey("R", LedId.Keyboard_R),
            new KeyboardKey("T", LedId.Keyboard_T),
            new KeyboardKey("Y", LedId.Keyboard_Y),
            new KeyboardKey("U", LedId.Keyboard_U),
            new KeyboardKey("I", LedId.Keyboard_I),
            new KeyboardKey("O", LedId.Keyboard_O),
            new KeyboardKey("P", LedId.Keyboard_P),
            new KeyboardKey("{", LedId.Keyboard_BracketLeft),
            new KeyboardKey("}", LedId.Keyboard_BracketRight),
            new KeyboardKey("\\", LedId.Keyboard_Backslash, true, false, 12, 7, 0, 57),

            new KeyboardKey("DEL", LedId.Keyboard_Delete, true, false, 9, 12),
            new KeyboardKey("END", LedId.Keyboard_End, true, false, 9),
            new KeyboardKey("PAGE\r\nDOWN", LedId.Keyboard_PageDown, true, false, 9),

            new KeyboardKey("7", LedId.Keyboard_Num7, true, false, 12, 14),
            new KeyboardKey("8", LedId.Keyboard_Num8),
            new KeyboardKey("9", LedId.Keyboard_Num9),
            new KeyboardKey("+", LedId.Keyboard_NumPlus, true, true, 12, 7, 0, 30, 69),

            new KeyboardKey("CAPS\r\nLOCK", LedId.Keyboard_CapsLock, true, false, 9, 7, 0, 55),
            new KeyboardKey("Q", LedId.Keyboard_Q),
            new KeyboardKey("S", LedId.Keyboard_S),
            new KeyboardKey("D", LedId.Keyboard_D),
            new KeyboardKey("F", LedId.Keyboard_F),
            new KeyboardKey("G", LedId.Keyboard_G),
            new KeyboardKey("H", LedId.Keyboard_H),
            new KeyboardKey("J", LedId.Keyboard_J),
            new KeyboardKey("K", LedId.Keyboard_K),
            new KeyboardKey("L", LedId.Keyboard_L),
            new KeyboardKey(":", LedId.Keyboard_SemicolonAndColon),
            new KeyboardKey("\"", LedId.Keyboard_ApostropheAndDoubleQuote),
            new KeyboardKey("ENTER", LedId.Keyboard_Enter, true, false, 12, 7, 0, 87),

            new KeyboardKey("4", LedId.Keyboard_Num4, true, false, 12, 131),
            new KeyboardKey("5", LedId.Keyboard_Num5),
            new KeyboardKey("6", LedId.Keyboard_Num6, true, true),

            new KeyboardKey("SHIFT", LedId.Keyboard_LeftShift, true, false, 12, 7, 0, 75),
            new KeyboardKey("W", LedId.Keyboard_W),
            new KeyboardKey("X", LedId.Keyboard_X),
            new KeyboardKey("C", LedId.Keyboard_C),
            new KeyboardKey("V", LedId.Keyboard_V),
            new KeyboardKey("B", LedId.Keyboard_B),
            new KeyboardKey("N", LedId.Keyboard_N),
            new KeyboardKey("M", LedId.Keyboard_M),
            new KeyboardKey("<", LedId.Keyboard_CommaAndLessThan),
            new KeyboardKey(">", LedId.Keyboard_PeriodAndBiggerThan),
            new KeyboardKey("?", LedId.Keyboard_SlashAndQuestionMark),
            new KeyboardKey("SHIFT", LedId.Keyboard_RightShift, true, false, 12, 7, 0, 102),

            new KeyboardKey("UP", LedId.Keyboard_ArrowUp, true, false, 9, 47),

            new KeyboardKey("1", LedId.Keyboard_Num1, true, false, 12, 49),
            new KeyboardKey("2", LedId.Keyboard_Num2),
            new KeyboardKey("3", LedId.Keyboard_Num3),
            new KeyboardKey("ENT", LedId.Keyboard_NumEnter, true, true, 9, 7, 0, 30, 69),

            new KeyboardKey("CTRL", LedId.Keyboard_LeftCtrl, true, false, 12, 7, 0, 49),
            new KeyboardKey("WIN", LedId.Keyboard_LeftGui, true, false, 12, 5, 0, 39),
            new KeyboardKey("ALT", LedId.Keyboard_LeftAlt, true, false, 12, 5, 0, 42),

            new KeyboardKey("SPACE", LedId.Keyboard_Space, true, false, 12, 7, 0, 189),
            new KeyboardKey("ALT", LedId.Keyboard_RightAlt, true, false, 12, 5, 0, 41),
            new KeyboardKey("FN", LedId.Keyboard_RightGui, true, false, 12, 5, 0, 47),
            new KeyboardKey("APP", LedId.Keyboard_Application, true, false, 12, 5, 0, 45),
            new KeyboardKey("CTRL", LedId.Keyboard_RightCtrl, true, false, 12, 5, 0, 45),

            new KeyboardKey("LEFT", LedId.Keyboard_ArrowLeft, true, false, 9, 12),
            new KeyboardKey("DOWN", LedId.Keyboard_ArrowDown, true, false, 9),
            new KeyboardKey("RIGHT", LedId.Keyboard_ArrowRight, true, false, 9),

            new KeyboardKey("0", LedId.Keyboard_Num0, true, false, 12, 14, 0, 65),
            new KeyboardKey(".", LedId.Keyboard_NumPeriodAndDelete, true, true),

            new KeyboardKey("M1", LedId.Keyboard_Programmable1, true, false),
            new KeyboardKey("M2", LedId.Keyboard_Programmable2, true, false),
            new KeyboardKey("M3", LedId.Keyboard_Programmable3, true, false),
            new KeyboardKey("M4", LedId.Keyboard_Programmable4, true, false),
            new KeyboardKey("M5", LedId.Keyboard_Programmable5, true, false),
            new KeyboardKey("M6", LedId.Keyboard_Programmable6, true, false),
            new KeyboardKey("M7", LedId.Keyboard_Programmable7, true, false),
            new KeyboardKey("M8", LedId.Keyboard_Programmable8, true, false),
            new KeyboardKey("M9", LedId.Keyboard_Programmable9, true, false),
            new KeyboardKey("M10", LedId.Keyboard_Programmable10, true, false),
            new KeyboardKey("M11", LedId.Keyboard_Programmable11, true, false),
            new KeyboardKey("M12", LedId.Keyboard_Programmable12, true, false),
            new KeyboardKey("M13", LedId.Keyboard_Programmable13, true, false),
            new KeyboardKey("M14", LedId.Keyboard_Programmable14, true, false),
            new KeyboardKey("M15", LedId.Keyboard_Programmable15, true, false),
            new KeyboardKey("I1", LedId.Keyboard_International1, true, false),
            new KeyboardKey("I2", LedId.Keyboard_International2, true, false),
            new KeyboardKey("I3", LedId.Keyboard_International3, true, false),
            new KeyboardKey("I4", LedId.Keyboard_International4, true, false),
            new KeyboardKey("I5", LedId.Keyboard_International5, true, false),
            new KeyboardKey("I6", LedId.Keyboard_Lang1, true, false),
            new KeyboardKey("I7", LedId.Keyboard_Lang2, true, false),
            new KeyboardKey("Logo", LedId.Logo, true, true)

            
        };

        private static List<KeyboardKey> Old = new List<KeyboardKey>()
        {
            new KeyboardKey("ESC", LedId.Keyboard_Escape),

            new KeyboardKey("F1", LedId.Keyboard_F1, true, false, 12, 32),
            new KeyboardKey("F2", LedId.Keyboard_F2),
            new KeyboardKey("F3", LedId.Keyboard_F3),
            new KeyboardKey("F4", LedId.Keyboard_F4),

            new KeyboardKey("F5", LedId.Keyboard_F5, true, false, 12, 34),
            new KeyboardKey("F6", LedId.Keyboard_F6),
            new KeyboardKey("F7", LedId.Keyboard_F7),
            new KeyboardKey("F8", LedId.Keyboard_F8),

            new KeyboardKey("F9", LedId.Keyboard_F9, true, false, 12, 29),
            new KeyboardKey("F10", LedId.Keyboard_F10),
            new KeyboardKey("F11", LedId.Keyboard_F11),
            new KeyboardKey("F12", LedId.Keyboard_F12),

            new KeyboardKey("PRINT", LedId.Keyboard_PrintScreen, true, false, 9, 12),
            new KeyboardKey("SCRL\r\nLOCK", LedId.Keyboard_ScrollLock, true, false, 9),
            new KeyboardKey("PAUSE", LedId.Keyboard_PauseBreak, true, true, 9),

            new KeyboardKey("~", LedId.Keyboard_GraveAccentAndTilde),
            new KeyboardKey("1", LedId.Keyboard_1),
            new KeyboardKey("2", LedId.Keyboard_2),
            new KeyboardKey("3", LedId.Keyboard_3),
            new KeyboardKey("4", LedId.Keyboard_4),
            new KeyboardKey("5", LedId.Keyboard_5),
            new KeyboardKey("6", LedId.Keyboard_6),
            new KeyboardKey("7", LedId.Keyboard_7),
            new KeyboardKey("8", LedId.Keyboard_8),
            new KeyboardKey("9", LedId.Keyboard_9),
            new KeyboardKey("0", LedId.Keyboard_0),
            new KeyboardKey("-", LedId.Keyboard_MinusAndUnderscore),
            new KeyboardKey("=", LedId.Keyboard_EqualsAndPlus),
            new KeyboardKey("BACKSPACE", LedId.Keyboard_Backspace, true, false, 12, 7, 0, 67),

            new KeyboardKey("INSERT", LedId.Keyboard_Insert, true, false, 9, 14),
            new KeyboardKey("HOME", LedId.Keyboard_Home, true, false, 9),
            new KeyboardKey("PAGE\r\nUP", LedId.Keyboard_PageUp, true, false, 9),

            new KeyboardKey("NUM\r\nLOCK", LedId.Keyboard_NumLock, true, false, 9, 14),
            new KeyboardKey("/", LedId.Keyboard_NumSlash),
            new KeyboardKey("*", LedId.Keyboard_NumAsterisk),
            new KeyboardKey("-", LedId.Keyboard_NumMinus, true, true),

            new KeyboardKey("TAB", LedId.Keyboard_Tab, true, false, 12, 7, 0, 50),
            new KeyboardKey("Q", LedId.Keyboard_Q),
            new KeyboardKey("W", LedId.Keyboard_W),
            new KeyboardKey("E", LedId.Keyboard_E),
            new KeyboardKey("R", LedId.Keyboard_R),
            new KeyboardKey("T", LedId.Keyboard_T),
            new KeyboardKey("Y", LedId.Keyboard_Y),
            new KeyboardKey("U", LedId.Keyboard_U),
            new KeyboardKey("I", LedId.Keyboard_I),
            new KeyboardKey("O", LedId.Keyboard_O),
            new KeyboardKey("P", LedId.Keyboard_P),
            new KeyboardKey("{", LedId.Keyboard_BracketLeft),
            new KeyboardKey("}", LedId.Keyboard_BracketRight),
            new KeyboardKey("\\", LedId.Keyboard_Backslash, true, false, 12, 7, 0, 49),

            new KeyboardKey("DEL", LedId.Keyboard_Delete, true, false, 9, 12),
            new KeyboardKey("END", LedId.Keyboard_End, true, false, 9),
            new KeyboardKey("PAGE\r\nDOWN", LedId.Keyboard_PageDown, true, false, 9),

            new KeyboardKey("7", LedId.Keyboard_Num7, true, false, 12, 14),
            new KeyboardKey("8", LedId.Keyboard_Num8),
            new KeyboardKey("9", LedId.Keyboard_Num9),
            new KeyboardKey("+", LedId.Keyboard_NumPlus, true, true, 12, 7, 0, 30, 69),

            new KeyboardKey("CAPS\r\nLOCK", LedId.Keyboard_CapsLock, true, false, 9, 7, 0, 60),
            new KeyboardKey("A", LedId.Keyboard_A),
            new KeyboardKey("S", LedId.Keyboard_S),
            new KeyboardKey("D", LedId.Keyboard_D),
            new KeyboardKey("F", LedId.Keyboard_F),
            new KeyboardKey("G", LedId.Keyboard_G),
            new KeyboardKey("H", LedId.Keyboard_H),
            new KeyboardKey("J", LedId.Keyboard_J),
            new KeyboardKey("K", LedId.Keyboard_K),
            new KeyboardKey("L", LedId.Keyboard_L),
            new KeyboardKey(":", LedId.Keyboard_SemicolonAndColon),
            new KeyboardKey("\"", LedId.Keyboard_ApostropheAndDoubleQuote),
            new KeyboardKey("ENTER", LedId.Keyboard_Enter, true, false, 12, 7, 0, 76),

            new KeyboardKey("4", LedId.Keyboard_Num4, true, false, 12, 130),
            new KeyboardKey("5", LedId.Keyboard_Num5),
            new KeyboardKey("6", LedId.Keyboard_Num6, true, true),

            new KeyboardKey("SHIFT", LedId.Keyboard_LeftShift, true, false, 12, 7, 0, 78),
            new KeyboardKey("Z", LedId.Keyboard_Z),
            new KeyboardKey("X", LedId.Keyboard_X),
            new KeyboardKey("C", LedId.Keyboard_C),
            new KeyboardKey("V", LedId.Keyboard_V),
            new KeyboardKey("B", LedId.Keyboard_B),
            new KeyboardKey("N", LedId.Keyboard_N),
            new KeyboardKey("M", LedId.Keyboard_M),
            new KeyboardKey("<", LedId.Keyboard_CommaAndLessThan),
            new KeyboardKey(">", LedId.Keyboard_PeriodAndBiggerThan),
            new KeyboardKey("?", LedId.Keyboard_SlashAndQuestionMark),
            new KeyboardKey("SHIFT", LedId.Keyboard_RightShift, true, false, 12, 7, 0, 95),

            new KeyboardKey("UP", LedId.Keyboard_ArrowUp, true, false, 9, 49),

            new KeyboardKey("1", LedId.Keyboard_Num1, true, false, 12, 51),
            new KeyboardKey("2", LedId.Keyboard_Num2),
            new KeyboardKey("3", LedId.Keyboard_Num3),
            new KeyboardKey("ENT", LedId.Keyboard_NumEnter, true, true, 9, 7, 0, 30, 67),

            new KeyboardKey("CTRL", LedId.Keyboard_RightCtrl, true, false, 12, 7, 0, 51),
            new KeyboardKey("WIN", LedId.Keyboard_RightGui, true, false, 12, 5, 0, 39),
            new KeyboardKey("ALT", LedId.Keyboard_RightAlt, true, false, 12, 5, 0, 42),

            new KeyboardKey("SPACE", LedId.Keyboard_Space, true, false, 12, 7, 0, 208),
            new KeyboardKey("ALT", LedId.Keyboard_LeftAlt, true, false, 12, 5, 0, 41),
            new KeyboardKey("FN", LedId.Keyboard_Function, true, false, 12, 5, 0, 41),
            new KeyboardKey("APP", LedId.Keyboard_Application, true, false, 12, 5, 0, 41),
            new KeyboardKey("CTRL", LedId.Keyboard_LeftCtrl, true, false, 12, 5, 0, 50),

            new KeyboardKey("LEFT", LedId.Keyboard_ArrowLeft, true, false, 9, 12),
            new KeyboardKey("DOWN", LedId.Keyboard_ArrowDown, true, false, 9),
            new KeyboardKey("RIGHT", LedId.Keyboard_ArrowDown, true, false, 9),

            new KeyboardKey("0", LedId.Keyboard_Num0, true, false, 12, 14, 0, 67),
            new KeyboardKey(".", LedId.Keyboard_NumPeriodAndDelete, true, true),
        };

    }
}
