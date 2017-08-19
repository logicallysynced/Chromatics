using System.Collections.Generic;

namespace Chromatics.FFXIVInterfaces
{
    internal class FFXIVHotbar
    {
        public static List<string> keybindwhitelist = new List<string>();

        public static readonly Dictionary<string, string> keybindtranslation = new Dictionary<string, string>
        {
            //Keys
            {"1", "D1"},
            {"2", "D2"},
            {"3", "D3"},
            {"4", "D4"},
            {"5", "D5"},
            {"6", "D6"},
            {"7", "D7"},
            {"8", "D8"},
            {"9", "D9"},
            {"0", "D0"},
            {"A", "A"},
            {"B", "B"},
            {"C", "C"},
            {"D", "D"},
            {"E", "E"},
            {"F", "F"},
            {"G", "G"},
            {"H", "H"},
            {"I", "I"},
            {"J", "J"},
            {"K", "K"},
            {"L", "L"},
            {"M", "M"},
            {"N", "N"},
            {"O", "O"},
            {"P", "P"},
            {"Q", "Q"},
            {"R", "R"},
            {"S", "S"},
            {"T", "T"},
            {"U", "U"},
            {"V", "V"},
            {"W", "W"},
            {"X", "X"},
            {"Y", "Y"},
            {"Z", "Z"},
            {"Tab", "Tab"},
            {"Backspace", "Backspace"},
            {"`", "OemTilde"},
            {"-", "OemMinus"},
            {"=", "OemEquals"},
            {"[", "OemLeftBracket"},
            {"]", "OemRightBracket"},
            {"/", "OemSlash"},
            {";", "OemSemicolon"},
            {"'", "OemApostrophe"},
            {",", "OemComma"},
            {".", "OemPeriod"},
            {@"\", "OemBackslash"},
            //{"", "EurPound"},
            //{"", "JpnYen"},
            {"Esc", "Escape"}
        };
    }
}