using System.Collections.Frozen;
using System.Collections.Generic;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // QMK keycode string → RGB.NET LedId. Covers the canonical ANSI 104
    // and the common alternates (numpad, media, ISO Enter / split-keys);
    // unknown keycodes return LedId.Invalid so the layout merger falls
    // back to LedId.Custom1+i for that LED. QMK keycodes follow the
    // KC_<name> convention; the via-keyboards JSON sometimes strips
    // the prefix, so the lookup matches both forms.
    internal static class QmkKeycodeMap
    {
        public static LedId ToLedId(string keycode)
        {
            if (string.IsNullOrEmpty(keycode)) return LedId.Invalid;
            string key = keycode.Trim();
            if (key.StartsWith("KC_")) key = key.Substring(3);
            return _map.TryGetValue(key, out var id) ? id : LedId.Invalid;
        }

        // Stored as a FrozenDictionary because lookups happen during layout
        // construction (once per device + LED count) and the table never
        // changes at runtime.
        private static readonly FrozenDictionary<string, LedId> _map =
            new Dictionary<string, LedId>(System.StringComparer.OrdinalIgnoreCase)
            {
                // Letters
                ["A"] = LedId.Keyboard_A, ["B"] = LedId.Keyboard_B, ["C"] = LedId.Keyboard_C,
                ["D"] = LedId.Keyboard_D, ["E"] = LedId.Keyboard_E, ["F"] = LedId.Keyboard_F,
                ["G"] = LedId.Keyboard_G, ["H"] = LedId.Keyboard_H, ["I"] = LedId.Keyboard_I,
                ["J"] = LedId.Keyboard_J, ["K"] = LedId.Keyboard_K, ["L"] = LedId.Keyboard_L,
                ["M"] = LedId.Keyboard_M, ["N"] = LedId.Keyboard_N, ["O"] = LedId.Keyboard_O,
                ["P"] = LedId.Keyboard_P, ["Q"] = LedId.Keyboard_Q, ["R"] = LedId.Keyboard_R,
                ["S"] = LedId.Keyboard_S, ["T"] = LedId.Keyboard_T, ["U"] = LedId.Keyboard_U,
                ["V"] = LedId.Keyboard_V, ["W"] = LedId.Keyboard_W, ["X"] = LedId.Keyboard_X,
                ["Y"] = LedId.Keyboard_Y, ["Z"] = LedId.Keyboard_Z,

                // Top row digits
                ["1"] = LedId.Keyboard_1, ["2"] = LedId.Keyboard_2, ["3"] = LedId.Keyboard_3,
                ["4"] = LedId.Keyboard_4, ["5"] = LedId.Keyboard_5, ["6"] = LedId.Keyboard_6,
                ["7"] = LedId.Keyboard_7, ["8"] = LedId.Keyboard_8, ["9"] = LedId.Keyboard_9,
                ["0"] = LedId.Keyboard_0,

                // Function row
                ["F1"]  = LedId.Keyboard_F1,  ["F2"]  = LedId.Keyboard_F2,  ["F3"]  = LedId.Keyboard_F3,
                ["F4"]  = LedId.Keyboard_F4,  ["F5"]  = LedId.Keyboard_F5,  ["F6"]  = LedId.Keyboard_F6,
                ["F7"]  = LedId.Keyboard_F7,  ["F8"]  = LedId.Keyboard_F8,  ["F9"]  = LedId.Keyboard_F9,
                ["F10"] = LedId.Keyboard_F10, ["F11"] = LedId.Keyboard_F11, ["F12"] = LedId.Keyboard_F12,

                // Symbols / punctuation (US ANSI)
                ["MINS"] = LedId.Keyboard_MinusAndUnderscore,
                ["EQL"]  = LedId.Keyboard_EqualsAndPlus,
                ["LBRC"] = LedId.Keyboard_BracketLeft,
                ["RBRC"] = LedId.Keyboard_BracketRight,
                ["BSLS"] = LedId.Keyboard_Backslash,
                ["SCLN"] = LedId.Keyboard_SemicolonAndColon,
                ["QUOT"] = LedId.Keyboard_ApostropheAndDoubleQuote,
                ["COMM"] = LedId.Keyboard_CommaAndLessThan,
                ["DOT"]  = LedId.Keyboard_PeriodAndBiggerThan,
                ["SLSH"] = LedId.Keyboard_SlashAndQuestionMark,
                ["GRV"]  = LedId.Keyboard_GraveAccentAndTilde,

                // Modifiers + edit keys
                ["ESC"]  = LedId.Keyboard_Escape,
                ["TAB"]  = LedId.Keyboard_Tab,
                ["CAPS"] = LedId.Keyboard_CapsLock,
                ["LSFT"] = LedId.Keyboard_LeftShift,
                ["RSFT"] = LedId.Keyboard_RightShift,
                ["LCTL"] = LedId.Keyboard_LeftCtrl,
                ["RCTL"] = LedId.Keyboard_RightCtrl,
                ["LALT"] = LedId.Keyboard_LeftAlt,
                ["RALT"] = LedId.Keyboard_RightAlt,
                ["LGUI"] = LedId.Keyboard_LeftGui,
                ["RGUI"] = LedId.Keyboard_RightGui,
                ["ENT"]  = LedId.Keyboard_Enter,
                ["BSPC"] = LedId.Keyboard_Backspace,
                ["SPC"]  = LedId.Keyboard_Space,
                ["APP"]  = LedId.Keyboard_Application,

                // Navigation cluster
                ["INS"]  = LedId.Keyboard_Insert,
                ["DEL"]  = LedId.Keyboard_Delete,
                ["HOME"] = LedId.Keyboard_Home,
                ["END"]  = LedId.Keyboard_End,
                ["PGUP"] = LedId.Keyboard_PageUp,
                ["PGDN"] = LedId.Keyboard_PageDown,
                ["UP"]   = LedId.Keyboard_ArrowUp,
                ["DOWN"] = LedId.Keyboard_ArrowDown,
                ["LEFT"] = LedId.Keyboard_ArrowLeft,
                ["RGHT"] = LedId.Keyboard_ArrowRight,

                // System / lock keys
                ["PSCR"] = LedId.Keyboard_PrintScreen,
                ["SCRL"] = LedId.Keyboard_ScrollLock,
                ["PAUS"] = LedId.Keyboard_PauseBreak,
                ["NLCK"] = LedId.Keyboard_NumLock,

                // Numpad
                ["P0"]   = LedId.Keyboard_Num0,  ["P1"] = LedId.Keyboard_Num1,
                ["P2"]   = LedId.Keyboard_Num2,  ["P3"] = LedId.Keyboard_Num3,
                ["P4"]   = LedId.Keyboard_Num4,  ["P5"] = LedId.Keyboard_Num5,
                ["P6"]   = LedId.Keyboard_Num6,  ["P7"] = LedId.Keyboard_Num7,
                ["P8"]   = LedId.Keyboard_Num8,  ["P9"] = LedId.Keyboard_Num9,
                ["PDOT"] = LedId.Keyboard_NumPeriodAndDelete,
                ["PSLS"] = LedId.Keyboard_NumSlash,
                ["PAST"] = LedId.Keyboard_NumAsterisk,
                ["PMNS"] = LedId.Keyboard_NumMinus,
                ["PPLS"] = LedId.Keyboard_NumPlus,
                ["PENT"] = LedId.Keyboard_NumEnter,

                // Some firmwares emit ISO-only keys; map to closest ANSI siblings.
                // NUHS = ISO non-US hash (between Enter and ' on ISO layouts).
                ["NUHS"] = LedId.Keyboard_Backslash,
                // NUBS = ISO non-US backslash (next to Left Shift on ISO layouts).
                ["NUBS"] = LedId.Keyboard_NonUsBackslash,
            }.ToFrozenDictionary(System.StringComparer.OrdinalIgnoreCase);
    }
}
