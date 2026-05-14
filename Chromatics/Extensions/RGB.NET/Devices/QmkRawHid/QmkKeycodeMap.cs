using System.Collections.Frozen;
using System.Collections.Generic;
using RGB.NET.Core;

namespace Chromatics.Extensions.RGB.NET.Devices.QmkRawHid
{
    // QMK label / keycode string → RGB.NET LedId. QMK boards label their keys
    // inconsistently across the keyboards/ tree — some entries use the QMK
    // keycode short form ("ESC", "BSPC"), some the full prefix ("KC_ESC"),
    // and many use human-readable legends ("Esc", "Backspace", "Page Up").
    // This map covers all three styles for the canonical ANSI 104 plus
    // numpad and ISO-only keys; lookup is case-insensitive and the "KC_"
    // prefix is stripped if present so all conventions resolve.
    //
    // Unknown labels return LedId.Invalid so the layout merger falls back
    // to LedId.Custom1+i for that LED.
    internal static class QmkKeycodeMap
    {
        public static LedId ToLedId(string label)
        {
            if (string.IsNullOrEmpty(label)) return LedId.Invalid;
            string key = label.Trim();
            if (key.StartsWith("KC_")) key = key.Substring(3);
            return _map.TryGetValue(key, out var id) ? id : LedId.Invalid;
        }

        // Stored as a FrozenDictionary because lookups happen during layout
        // construction (once per device + LED count) and the table never
        // changes at runtime. Case-insensitive — covers "Esc"/"ESC"/"esc".
        private static readonly FrozenDictionary<string, LedId> _map =
            new Dictionary<string, LedId>(System.StringComparer.OrdinalIgnoreCase)
            {
                // ── Letters ──────────────────────────────────────────
                ["A"] = LedId.Keyboard_A, ["B"] = LedId.Keyboard_B, ["C"] = LedId.Keyboard_C,
                ["D"] = LedId.Keyboard_D, ["E"] = LedId.Keyboard_E, ["F"] = LedId.Keyboard_F,
                ["G"] = LedId.Keyboard_G, ["H"] = LedId.Keyboard_H, ["I"] = LedId.Keyboard_I,
                ["J"] = LedId.Keyboard_J, ["K"] = LedId.Keyboard_K, ["L"] = LedId.Keyboard_L,
                ["M"] = LedId.Keyboard_M, ["N"] = LedId.Keyboard_N, ["O"] = LedId.Keyboard_O,
                ["P"] = LedId.Keyboard_P, ["Q"] = LedId.Keyboard_Q, ["R"] = LedId.Keyboard_R,
                ["S"] = LedId.Keyboard_S, ["T"] = LedId.Keyboard_T, ["U"] = LedId.Keyboard_U,
                ["V"] = LedId.Keyboard_V, ["W"] = LedId.Keyboard_W, ["X"] = LedId.Keyboard_X,
                ["Y"] = LedId.Keyboard_Y, ["Z"] = LedId.Keyboard_Z,

                // ── Top-row digits ───────────────────────────────────
                ["1"] = LedId.Keyboard_1, ["2"] = LedId.Keyboard_2, ["3"] = LedId.Keyboard_3,
                ["4"] = LedId.Keyboard_4, ["5"] = LedId.Keyboard_5, ["6"] = LedId.Keyboard_6,
                ["7"] = LedId.Keyboard_7, ["8"] = LedId.Keyboard_8, ["9"] = LedId.Keyboard_9,
                ["0"] = LedId.Keyboard_0,

                // ── Function row ─────────────────────────────────────
                ["F1"]  = LedId.Keyboard_F1,  ["F2"]  = LedId.Keyboard_F2,  ["F3"]  = LedId.Keyboard_F3,
                ["F4"]  = LedId.Keyboard_F4,  ["F5"]  = LedId.Keyboard_F5,  ["F6"]  = LedId.Keyboard_F6,
                ["F7"]  = LedId.Keyboard_F7,  ["F8"]  = LedId.Keyboard_F8,  ["F9"]  = LedId.Keyboard_F9,
                ["F10"] = LedId.Keyboard_F10, ["F11"] = LedId.Keyboard_F11, ["F12"] = LedId.Keyboard_F12,
                // F13+ exists on the QMK side (full-size + macropad layouts) but
                // RGB.NET's LedId enum tops out at F12; entries above that drop
                // to LedId.Custom_* via the merge step's fallback.

                // ── Punctuation: QMK keycode forms ───────────────────
                ["MINS"]  = LedId.Keyboard_MinusAndUnderscore,
                ["EQL"]   = LedId.Keyboard_EqualsAndPlus,
                ["LBRC"]  = LedId.Keyboard_BracketLeft,
                ["RBRC"]  = LedId.Keyboard_BracketRight,
                ["BSLS"]  = LedId.Keyboard_Backslash,
                ["SCLN"]  = LedId.Keyboard_SemicolonAndColon,
                ["QUOT"]  = LedId.Keyboard_ApostropheAndDoubleQuote,
                ["COMM"]  = LedId.Keyboard_CommaAndLessThan,
                ["DOT"]   = LedId.Keyboard_PeriodAndBiggerThan,
                ["SLSH"]  = LedId.Keyboard_SlashAndQuestionMark,
                ["GRV"]   = LedId.Keyboard_GraveAccentAndTilde,

                // ── Punctuation: human-legend forms ──────────────────
                ["-"]  = LedId.Keyboard_MinusAndUnderscore,
                ["="]  = LedId.Keyboard_EqualsAndPlus,
                ["["]  = LedId.Keyboard_BracketLeft,
                ["]"]  = LedId.Keyboard_BracketRight,
                ["\\"] = LedId.Keyboard_Backslash,
                [";"]  = LedId.Keyboard_SemicolonAndColon,
                ["'"]  = LedId.Keyboard_ApostropheAndDoubleQuote,
                [","]  = LedId.Keyboard_CommaAndLessThan,
                ["."]  = LedId.Keyboard_PeriodAndBiggerThan,
                ["/"]  = LedId.Keyboard_SlashAndQuestionMark,
                ["`"]  = LedId.Keyboard_GraveAccentAndTilde,

                // ── Modifiers + edit: short + long forms ─────────────
                ["ESC"]            = LedId.Keyboard_Escape,
                ["Escape"]         = LedId.Keyboard_Escape,
                ["TAB"]            = LedId.Keyboard_Tab,
                ["Tab"]            = LedId.Keyboard_Tab,
                ["CAPS"]           = LedId.Keyboard_CapsLock,
                ["Caps"]           = LedId.Keyboard_CapsLock,
                ["Caps Lock"]      = LedId.Keyboard_CapsLock,
                ["CapsLock"]       = LedId.Keyboard_CapsLock,
                ["LSFT"]           = LedId.Keyboard_LeftShift,
                ["LShift"]         = LedId.Keyboard_LeftShift,
                ["Left Shift"]     = LedId.Keyboard_LeftShift,
                ["Shift"]          = LedId.Keyboard_LeftShift, // ambiguous label; first occurrence wins, second falls back to Custom_*
                ["RSFT"]           = LedId.Keyboard_RightShift,
                ["RShift"]         = LedId.Keyboard_RightShift,
                ["Right Shift"]    = LedId.Keyboard_RightShift,
                ["LCTL"]           = LedId.Keyboard_LeftCtrl,
                ["LCtrl"]          = LedId.Keyboard_LeftCtrl,
                ["Left Ctrl"]      = LedId.Keyboard_LeftCtrl,
                ["Ctrl"]           = LedId.Keyboard_LeftCtrl,
                ["Control"]        = LedId.Keyboard_LeftCtrl,
                ["RCTL"]           = LedId.Keyboard_RightCtrl,
                ["RCtrl"]          = LedId.Keyboard_RightCtrl,
                ["Right Ctrl"]     = LedId.Keyboard_RightCtrl,
                ["LALT"]           = LedId.Keyboard_LeftAlt,
                ["LAlt"]           = LedId.Keyboard_LeftAlt,
                ["Left Alt"]       = LedId.Keyboard_LeftAlt,
                ["Alt"]            = LedId.Keyboard_LeftAlt,
                ["RALT"]           = LedId.Keyboard_RightAlt,
                ["RAlt"]           = LedId.Keyboard_RightAlt,
                ["Right Alt"]      = LedId.Keyboard_RightAlt,
                ["AltGr"]          = LedId.Keyboard_RightAlt,
                ["LGUI"]           = LedId.Keyboard_LeftGui,
                ["LGui"]           = LedId.Keyboard_LeftGui,
                ["Left GUI"]       = LedId.Keyboard_LeftGui,
                ["Left Win"]       = LedId.Keyboard_LeftGui,
                ["Win"]            = LedId.Keyboard_LeftGui,
                ["Windows"]        = LedId.Keyboard_LeftGui,
                ["Cmd"]            = LedId.Keyboard_LeftGui,
                ["RGUI"]           = LedId.Keyboard_RightGui,
                ["RGui"]           = LedId.Keyboard_RightGui,
                ["Right GUI"]      = LedId.Keyboard_RightGui,
                ["Right Win"]      = LedId.Keyboard_RightGui,
                ["ENT"]            = LedId.Keyboard_Enter,
                ["ENTER"]          = LedId.Keyboard_Enter,
                ["Enter"]          = LedId.Keyboard_Enter,
                ["Return"]         = LedId.Keyboard_Enter,
                ["BSPC"]           = LedId.Keyboard_Backspace,
                ["Backspace"]      = LedId.Keyboard_Backspace,
                ["BkSp"]           = LedId.Keyboard_Backspace,
                ["SPC"]            = LedId.Keyboard_Space,
                ["Space"]          = LedId.Keyboard_Space,
                ["APP"]            = LedId.Keyboard_Application,
                ["Menu"]           = LedId.Keyboard_Application,
                ["App"]            = LedId.Keyboard_Application,

                // ── Navigation cluster ───────────────────────────────
                ["INS"]      = LedId.Keyboard_Insert,
                ["Ins"]      = LedId.Keyboard_Insert,
                ["Insert"]   = LedId.Keyboard_Insert,
                ["DEL"]      = LedId.Keyboard_Delete,
                ["Del"]      = LedId.Keyboard_Delete,
                ["Delete"]   = LedId.Keyboard_Delete,
                ["HOME"]     = LedId.Keyboard_Home,
                ["Home"]     = LedId.Keyboard_Home,
                ["END"]      = LedId.Keyboard_End,
                ["End"]      = LedId.Keyboard_End,
                ["PGUP"]     = LedId.Keyboard_PageUp,
                ["PgUp"]     = LedId.Keyboard_PageUp,
                ["Page Up"]  = LedId.Keyboard_PageUp,
                ["PageUp"]   = LedId.Keyboard_PageUp,
                ["PGDN"]     = LedId.Keyboard_PageDown,
                ["PgDn"]     = LedId.Keyboard_PageDown,
                ["Page Down"] = LedId.Keyboard_PageDown,
                ["PageDown"] = LedId.Keyboard_PageDown,
                ["UP"]       = LedId.Keyboard_ArrowUp,
                ["Up"]       = LedId.Keyboard_ArrowUp,
                ["↑"]        = LedId.Keyboard_ArrowUp,
                ["DOWN"]     = LedId.Keyboard_ArrowDown,
                ["Down"]     = LedId.Keyboard_ArrowDown,
                ["↓"]        = LedId.Keyboard_ArrowDown,
                ["LEFT"]     = LedId.Keyboard_ArrowLeft,
                ["Left"]     = LedId.Keyboard_ArrowLeft,
                ["←"]        = LedId.Keyboard_ArrowLeft,
                ["RGHT"]     = LedId.Keyboard_ArrowRight,
                ["Right"]    = LedId.Keyboard_ArrowRight,
                ["→"]        = LedId.Keyboard_ArrowRight,

                // ── System / locks ───────────────────────────────────
                ["PSCR"]        = LedId.Keyboard_PrintScreen,
                ["PrtSc"]       = LedId.Keyboard_PrintScreen,
                ["PrintScreen"] = LedId.Keyboard_PrintScreen,
                ["Print Screen"] = LedId.Keyboard_PrintScreen,
                ["SCRL"]        = LedId.Keyboard_ScrollLock,
                ["ScrLk"]       = LedId.Keyboard_ScrollLock,
                ["Scroll Lock"] = LedId.Keyboard_ScrollLock,
                ["ScrollLock"]  = LedId.Keyboard_ScrollLock,
                ["PAUS"]        = LedId.Keyboard_PauseBreak,
                ["Pause"]       = LedId.Keyboard_PauseBreak,
                ["Break"]       = LedId.Keyboard_PauseBreak,
                ["Pause/Break"] = LedId.Keyboard_PauseBreak,
                ["NLCK"]        = LedId.Keyboard_NumLock,
                ["NumLk"]       = LedId.Keyboard_NumLock,
                ["Num Lock"]    = LedId.Keyboard_NumLock,
                ["NumLock"]     = LedId.Keyboard_NumLock,

                // ── Numpad: QMK keycode forms (P0..P9, PDOT etc.) ────
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

                // ── Numpad: human-legend forms (KP_0..KP_9, KP_Plus, etc.) ──
                ["KP_0"] = LedId.Keyboard_Num0, ["KP_1"] = LedId.Keyboard_Num1,
                ["KP_2"] = LedId.Keyboard_Num2, ["KP_3"] = LedId.Keyboard_Num3,
                ["KP_4"] = LedId.Keyboard_Num4, ["KP_5"] = LedId.Keyboard_Num5,
                ["KP_6"] = LedId.Keyboard_Num6, ["KP_7"] = LedId.Keyboard_Num7,
                ["KP_8"] = LedId.Keyboard_Num8, ["KP_9"] = LedId.Keyboard_Num9,
                ["KP_."]      = LedId.Keyboard_NumPeriodAndDelete,
                ["KP_/"]      = LedId.Keyboard_NumSlash,
                ["KP_*"]      = LedId.Keyboard_NumAsterisk,
                ["KP_-"]      = LedId.Keyboard_NumMinus,
                ["KP_+"]      = LedId.Keyboard_NumPlus,
                ["KP_Enter"]  = LedId.Keyboard_NumEnter,

                // ── ISO-only keys: map to closest ANSI siblings ──────
                // NUHS = ISO non-US hash (between Enter and ' on ISO layouts).
                ["NUHS"] = LedId.Keyboard_Backslash,
                // NUBS = ISO non-US backslash (next to Left Shift on ISO).
                ["NUBS"] = LedId.Keyboard_NonUsBackslash,
            }.ToFrozenDictionary(System.StringComparer.OrdinalIgnoreCase);
    }
}
