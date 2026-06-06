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

        // HID keyboard usage IDs → RGB.NET LedId. OpenRGB-QMK's GetLedInfo
        // returns the layer-0 keycode at byte +6 of each 7-byte record; the
        // low byte of a QMK basic keycode is the HID usage ID, so we can
        // map LEDs to semantic LedIds without needing matrix-coord guesses
        // or the bundled keymap JSON. Unknown / Keychron-vendor keycodes
        // (0xA0+ for FN, brightness, RGB cycle, etc.) return LedId.Invalid
        // and the caller falls back to Custom1+i.
        public static LedId FromKeycodeByte(byte hidUsage)
        {
            return _keycodeMap.TryGetValue(hidUsage, out var id) ? id : LedId.Invalid;
        }

        private static readonly FrozenDictionary<byte, LedId> _keycodeMap =
            new Dictionary<byte, LedId>
            {
                // 0x04-0x1D = A-Z
                [0x04] = LedId.Keyboard_A, [0x05] = LedId.Keyboard_B, [0x06] = LedId.Keyboard_C,
                [0x07] = LedId.Keyboard_D, [0x08] = LedId.Keyboard_E, [0x09] = LedId.Keyboard_F,
                [0x0A] = LedId.Keyboard_G, [0x0B] = LedId.Keyboard_H, [0x0C] = LedId.Keyboard_I,
                [0x0D] = LedId.Keyboard_J, [0x0E] = LedId.Keyboard_K, [0x0F] = LedId.Keyboard_L,
                [0x10] = LedId.Keyboard_M, [0x11] = LedId.Keyboard_N, [0x12] = LedId.Keyboard_O,
                [0x13] = LedId.Keyboard_P, [0x14] = LedId.Keyboard_Q, [0x15] = LedId.Keyboard_R,
                [0x16] = LedId.Keyboard_S, [0x17] = LedId.Keyboard_T, [0x18] = LedId.Keyboard_U,
                [0x19] = LedId.Keyboard_V, [0x1A] = LedId.Keyboard_W, [0x1B] = LedId.Keyboard_X,
                [0x1C] = LedId.Keyboard_Y, [0x1D] = LedId.Keyboard_Z,

                // 0x1E-0x27 = 1, 2, 3, 4, 5, 6, 7, 8, 9, 0
                [0x1E] = LedId.Keyboard_1, [0x1F] = LedId.Keyboard_2, [0x20] = LedId.Keyboard_3,
                [0x21] = LedId.Keyboard_4, [0x22] = LedId.Keyboard_5, [0x23] = LedId.Keyboard_6,
                [0x24] = LedId.Keyboard_7, [0x25] = LedId.Keyboard_8, [0x26] = LedId.Keyboard_9,
                [0x27] = LedId.Keyboard_0,

                // 0x28-0x38 = Enter, Esc, Backspace, Tab, Space, -, =, [, ], \, NUHS, ;, ', `, ,, ., /
                [0x28] = LedId.Keyboard_Enter,
                [0x29] = LedId.Keyboard_Escape,
                [0x2A] = LedId.Keyboard_Backspace,
                [0x2B] = LedId.Keyboard_Tab,
                [0x2C] = LedId.Keyboard_Space,
                [0x2D] = LedId.Keyboard_MinusAndUnderscore,
                [0x2E] = LedId.Keyboard_EqualsAndPlus,
                [0x2F] = LedId.Keyboard_BracketLeft,
                [0x30] = LedId.Keyboard_BracketRight,
                [0x31] = LedId.Keyboard_Backslash,
                [0x32] = LedId.Keyboard_Backslash, // NonUsHash → closest ANSI sibling
                [0x33] = LedId.Keyboard_SemicolonAndColon,
                [0x34] = LedId.Keyboard_ApostropheAndDoubleQuote,
                [0x35] = LedId.Keyboard_GraveAccentAndTilde,
                [0x36] = LedId.Keyboard_CommaAndLessThan,
                [0x37] = LedId.Keyboard_PeriodAndBiggerThan,
                [0x38] = LedId.Keyboard_SlashAndQuestionMark,

                // 0x39 = Caps Lock, 0x3A-0x45 = F1-F12
                [0x39] = LedId.Keyboard_CapsLock,
                [0x3A] = LedId.Keyboard_F1,  [0x3B] = LedId.Keyboard_F2,  [0x3C] = LedId.Keyboard_F3,
                [0x3D] = LedId.Keyboard_F4,  [0x3E] = LedId.Keyboard_F5,  [0x3F] = LedId.Keyboard_F6,
                [0x40] = LedId.Keyboard_F7,  [0x41] = LedId.Keyboard_F8,  [0x42] = LedId.Keyboard_F9,
                [0x43] = LedId.Keyboard_F10, [0x44] = LedId.Keyboard_F11, [0x45] = LedId.Keyboard_F12,

                // 0x46-0x4E = PrintScreen, Scroll Lock, Pause, Insert, Home, PageUp, Delete, End, PageDown
                [0x46] = LedId.Keyboard_PrintScreen,
                [0x47] = LedId.Keyboard_ScrollLock,
                [0x48] = LedId.Keyboard_PauseBreak,
                [0x49] = LedId.Keyboard_Insert,
                [0x4A] = LedId.Keyboard_Home,
                [0x4B] = LedId.Keyboard_PageUp,
                [0x4C] = LedId.Keyboard_Delete,
                [0x4D] = LedId.Keyboard_End,
                [0x4E] = LedId.Keyboard_PageDown,

                // 0x4F-0x52 = Right, Left, Down, Up arrows
                [0x4F] = LedId.Keyboard_ArrowRight,
                [0x50] = LedId.Keyboard_ArrowLeft,
                [0x51] = LedId.Keyboard_ArrowDown,
                [0x52] = LedId.Keyboard_ArrowUp,

                // 0x53-0x63 = NumLock, Numpad /, *, -, +, Enter, 1-9, 0, .
                [0x53] = LedId.Keyboard_NumLock,
                [0x54] = LedId.Keyboard_NumSlash,
                [0x55] = LedId.Keyboard_NumAsterisk,
                [0x56] = LedId.Keyboard_NumMinus,
                [0x57] = LedId.Keyboard_NumPlus,
                [0x58] = LedId.Keyboard_NumEnter,
                [0x59] = LedId.Keyboard_Num1, [0x5A] = LedId.Keyboard_Num2, [0x5B] = LedId.Keyboard_Num3,
                [0x5C] = LedId.Keyboard_Num4, [0x5D] = LedId.Keyboard_Num5, [0x5E] = LedId.Keyboard_Num6,
                [0x5F] = LedId.Keyboard_Num7, [0x60] = LedId.Keyboard_Num8, [0x61] = LedId.Keyboard_Num9,
                [0x62] = LedId.Keyboard_Num0,
                [0x63] = LedId.Keyboard_NumPeriodAndDelete,

                // 0x64 = ISO non-US backslash, 0x65 = Application/Menu
                [0x64] = LedId.Keyboard_NonUsBackslash,
                [0x65] = LedId.Keyboard_Application,

                // 0xE0-0xE7 = Left/Right Ctrl/Shift/Alt/GUI
                [0xE0] = LedId.Keyboard_LeftCtrl,
                [0xE1] = LedId.Keyboard_LeftShift,
                [0xE2] = LedId.Keyboard_LeftAlt,
                [0xE3] = LedId.Keyboard_LeftGui,
                [0xE4] = LedId.Keyboard_RightCtrl,
                [0xE5] = LedId.Keyboard_RightShift,
                [0xE6] = LedId.Keyboard_RightAlt,
                [0xE7] = LedId.Keyboard_RightGui,

                // Note: keycodes 0x00..0x0B overlap Keychron's QK_KB custom
                // range (KC_MAC_MISSION_CONTROL, KC_LOPTN, KC_LCMMD, etc.)
                // whose enum order varies per board, so they're resolved
                // position-aware in QmkRawHidRGBDeviceProvider's
                // ResolveKeycodePositionAware rather than here. The standard
                // map below covers HID usage IDs only.

                // Consumer (Mac F-row) keycodes — Keychron Mac base layer
                // populates F1-F12 with brightness/media keys (KC_BRID,
                // KC_MCTRL, KC_MPRV, KC_MUTE, etc.) whose low byte falls
                // into the 0xA8..0xC2 consumer range. The firmware exposes
                // keymaps[0] only, so these are what GetLedInfo reports for
                // the F-row regardless of which OS-base layer the user is
                // on. Map them back to Keyboard_F1..F12 so Chromatics's
                // keyboard layers paint the F-row positions. Two slots
                // (F5/F6 = UG_VALD/UG_VALU on Keychron, low byte 0x28/0x27)
                // collide with KC_ENTER/KC_0 and fall through to Custom_*
                // via the dedupe step in BuildLayoutFromKeycodes.
                [0xBE] = LedId.Keyboard_F1,   // KC_BRIGHTNESS_DOWN
                [0xBD] = LedId.Keyboard_F2,   // KC_BRIGHTNESS_UP
                [0xC1] = LedId.Keyboard_F3,   // KC_MISSION_CONTROL (KC_MCTRL)
                [0xC2] = LedId.Keyboard_F4,   // KC_LAUNCHPAD (KC_LNPAD)
                [0xAC] = LedId.Keyboard_F7,   // KC_MEDIA_PREV_TRACK (KC_MPRV)
                [0xAE] = LedId.Keyboard_F8,   // KC_MEDIA_PLAY_PAUSE (KC_MPLY)
                [0xAB] = LedId.Keyboard_F9,   // KC_MEDIA_NEXT_TRACK (KC_MNXT)
                [0xA8] = LedId.Keyboard_F10,  // KC_AUDIO_MUTE (KC_MUTE)
                [0xAA] = LedId.Keyboard_F11,  // KC_AUDIO_VOL_DOWN (KC_VOLD)
                [0xA9] = LedId.Keyboard_F12,  // KC_AUDIO_VOL_UP (KC_VOLU)
            }.ToFrozenDictionary();

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
