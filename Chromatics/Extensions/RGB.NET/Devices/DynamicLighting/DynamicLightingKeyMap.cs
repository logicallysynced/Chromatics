using RGB.NET.Core;
using System.Collections.Generic;
using Windows.System;

namespace Chromatics.Extensions.RGB.NET.Devices.DynamicLighting
{
    // Static map from RGB.NET LedId.Keyboard_* values to the Windows
    // VirtualKey codes that LampArray.GetIndicesForKey() resolves
    // against. Lets the device's InitializeLayout produce semantic
    // LedId mappings for keyboards without any per-OEM lookup table.
    //
    // The OS owns the mapping from VirtualKey → physical-lamp-index
    // (it's part of the LampArray firmware response to
    // LampAttributesRequestReport). We just have to declare which
    // VirtualKey corresponds to each LedId in our own keyboard
    // vocabulary and let the OS resolve the rest.
    internal static class DynamicLightingKeyMap
    {
        // Ordered list of (LedId, VirtualKey) pairs covering the standard
        // ANSI 104 layout plus media keys. Iterated by InitializeLayout
        // in the order presented; first-match wins for any duplicate
        // VirtualKey resolution.
        public static readonly IReadOnlyList<(LedId LedId, VirtualKey VirtualKey)> LedIdToVirtualKey = new (LedId, VirtualKey)[]
        {
            // Function row
            (LedId.Keyboard_Escape, VirtualKey.Escape),
            (LedId.Keyboard_F1,  VirtualKey.F1),
            (LedId.Keyboard_F2,  VirtualKey.F2),
            (LedId.Keyboard_F3,  VirtualKey.F3),
            (LedId.Keyboard_F4,  VirtualKey.F4),
            (LedId.Keyboard_F5,  VirtualKey.F5),
            (LedId.Keyboard_F6,  VirtualKey.F6),
            (LedId.Keyboard_F7,  VirtualKey.F7),
            (LedId.Keyboard_F8,  VirtualKey.F8),
            (LedId.Keyboard_F9,  VirtualKey.F9),
            (LedId.Keyboard_F10, VirtualKey.F10),
            (LedId.Keyboard_F11, VirtualKey.F11),
            (LedId.Keyboard_F12, VirtualKey.F12),
            (LedId.Keyboard_PrintScreen, VirtualKey.Print),
            (LedId.Keyboard_ScrollLock, VirtualKey.Scroll),
            (LedId.Keyboard_PauseBreak, VirtualKey.Pause),

            // Number row
            (LedId.Keyboard_GraveAccentAndTilde, (VirtualKey)0xC0), // VK_OEM_3
            (LedId.Keyboard_1, VirtualKey.Number1),
            (LedId.Keyboard_2, VirtualKey.Number2),
            (LedId.Keyboard_3, VirtualKey.Number3),
            (LedId.Keyboard_4, VirtualKey.Number4),
            (LedId.Keyboard_5, VirtualKey.Number5),
            (LedId.Keyboard_6, VirtualKey.Number6),
            (LedId.Keyboard_7, VirtualKey.Number7),
            (LedId.Keyboard_8, VirtualKey.Number8),
            (LedId.Keyboard_9, VirtualKey.Number9),
            (LedId.Keyboard_0, VirtualKey.Number0),
            (LedId.Keyboard_MinusAndUnderscore, (VirtualKey)0xBD),  // VK_OEM_MINUS
            (LedId.Keyboard_EqualsAndPlus, (VirtualKey)0xBB),       // VK_OEM_PLUS
            (LedId.Keyboard_Backspace, VirtualKey.Back),

            // Top alpha row
            (LedId.Keyboard_Tab, VirtualKey.Tab),
            (LedId.Keyboard_Q, VirtualKey.Q),
            (LedId.Keyboard_W, VirtualKey.W),
            (LedId.Keyboard_E, VirtualKey.E),
            (LedId.Keyboard_R, VirtualKey.R),
            (LedId.Keyboard_T, VirtualKey.T),
            (LedId.Keyboard_Y, VirtualKey.Y),
            (LedId.Keyboard_U, VirtualKey.U),
            (LedId.Keyboard_I, VirtualKey.I),
            (LedId.Keyboard_O, VirtualKey.O),
            (LedId.Keyboard_P, VirtualKey.P),
            (LedId.Keyboard_BracketLeft, (VirtualKey)0xDB),         // VK_OEM_4
            (LedId.Keyboard_BracketRight, (VirtualKey)0xDD),        // VK_OEM_6
            (LedId.Keyboard_Backslash, (VirtualKey)0xDC),           // VK_OEM_5

            // Home alpha row
            (LedId.Keyboard_CapsLock, VirtualKey.CapitalLock),
            (LedId.Keyboard_A, VirtualKey.A),
            (LedId.Keyboard_S, VirtualKey.S),
            (LedId.Keyboard_D, VirtualKey.D),
            (LedId.Keyboard_F, VirtualKey.F),
            (LedId.Keyboard_G, VirtualKey.G),
            (LedId.Keyboard_H, VirtualKey.H),
            (LedId.Keyboard_J, VirtualKey.J),
            (LedId.Keyboard_K, VirtualKey.K),
            (LedId.Keyboard_L, VirtualKey.L),
            (LedId.Keyboard_SemicolonAndColon, (VirtualKey)0xBA),   // VK_OEM_1
            (LedId.Keyboard_ApostropheAndDoubleQuote, (VirtualKey)0xDE), // VK_OEM_7
            (LedId.Keyboard_Enter, VirtualKey.Enter),

            // Bottom alpha row
            (LedId.Keyboard_LeftShift, VirtualKey.LeftShift),
            (LedId.Keyboard_Z, VirtualKey.Z),
            (LedId.Keyboard_X, VirtualKey.X),
            (LedId.Keyboard_C, VirtualKey.C),
            (LedId.Keyboard_V, VirtualKey.V),
            (LedId.Keyboard_B, VirtualKey.B),
            (LedId.Keyboard_N, VirtualKey.N),
            (LedId.Keyboard_M, VirtualKey.M),
            (LedId.Keyboard_CommaAndLessThan, (VirtualKey)0xBC),    // VK_OEM_COMMA
            (LedId.Keyboard_PeriodAndBiggerThan, (VirtualKey)0xBE), // VK_OEM_PERIOD
            (LedId.Keyboard_SlashAndQuestionMark, (VirtualKey)0xBF), // VK_OEM_2
            (LedId.Keyboard_RightShift, VirtualKey.RightShift),

            // Bottom row modifiers
            (LedId.Keyboard_LeftCtrl, VirtualKey.LeftControl),
            (LedId.Keyboard_LeftGui, VirtualKey.LeftWindows),
            (LedId.Keyboard_LeftAlt, VirtualKey.LeftMenu),
            (LedId.Keyboard_Space, VirtualKey.Space),
            (LedId.Keyboard_RightAlt, VirtualKey.RightMenu),
            (LedId.Keyboard_RightGui, VirtualKey.RightWindows),
            (LedId.Keyboard_Application, VirtualKey.Application),
            (LedId.Keyboard_RightCtrl, VirtualKey.RightControl),

            // Nav cluster
            (LedId.Keyboard_Insert, VirtualKey.Insert),
            (LedId.Keyboard_Home, VirtualKey.Home),
            (LedId.Keyboard_PageUp, VirtualKey.PageUp),
            (LedId.Keyboard_Delete, VirtualKey.Delete),
            (LedId.Keyboard_End, VirtualKey.End),
            (LedId.Keyboard_PageDown, VirtualKey.PageDown),
            (LedId.Keyboard_ArrowUp, VirtualKey.Up),
            (LedId.Keyboard_ArrowLeft, VirtualKey.Left),
            (LedId.Keyboard_ArrowDown, VirtualKey.Down),
            (LedId.Keyboard_ArrowRight, VirtualKey.Right),

            // Numpad
            (LedId.Keyboard_NumLock, VirtualKey.NumberKeyLock),
            (LedId.Keyboard_NumSlash, VirtualKey.Divide),
            (LedId.Keyboard_NumAsterisk, VirtualKey.Multiply),
            (LedId.Keyboard_NumMinus, VirtualKey.Subtract),
            (LedId.Keyboard_NumPlus, VirtualKey.Add),
            (LedId.Keyboard_NumEnter, VirtualKey.Enter),
            (LedId.Keyboard_Num0, VirtualKey.NumberPad0),
            (LedId.Keyboard_Num1, VirtualKey.NumberPad1),
            (LedId.Keyboard_Num2, VirtualKey.NumberPad2),
            (LedId.Keyboard_Num3, VirtualKey.NumberPad3),
            (LedId.Keyboard_Num4, VirtualKey.NumberPad4),
            (LedId.Keyboard_Num5, VirtualKey.NumberPad5),
            (LedId.Keyboard_Num6, VirtualKey.NumberPad6),
            (LedId.Keyboard_Num7, VirtualKey.NumberPad7),
            (LedId.Keyboard_Num8, VirtualKey.NumberPad8),
            (LedId.Keyboard_Num9, VirtualKey.NumberPad9),
            (LedId.Keyboard_NumPeriodAndDelete, VirtualKey.Decimal),
        };
    }
}
