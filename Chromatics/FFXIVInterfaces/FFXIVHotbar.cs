using System.Collections.Generic;

namespace Chromatics.FFXIVInterfaces
{
    internal class FfxivHotbar
    {
        public static void CheckJobGaugeBinds(bool toggle)
        {
            if (!toggle)
            {
                if (!Keybindtranslation.ContainsKey("NUM0"))
                {
                    Keybindtranslation.Add("NUM0", "Num0");
                }

                if (!Keybindtranslation.ContainsKey("NUM1"))
                {
                    Keybindtranslation.Add("NUM1", "Num1");
                }

                if (!Keybindtranslation.ContainsKey("NUM2"))
                {
                    Keybindtranslation.Add("NUM2", "Num2");
                }

                if (!Keybindtranslation.ContainsKey("NUM3"))
                {
                    Keybindtranslation.Add("NUM3", "Num3");
                }

                if (!Keybindtranslation.ContainsKey("NUM4"))
                {
                    Keybindtranslation.Add("NUM4", "Num4");
                }

                if (!Keybindtranslation.ContainsKey("NUM5"))
                {
                    Keybindtranslation.Add("NUM5", "Num5");
                }

                if (!Keybindtranslation.ContainsKey("NUM6"))
                {
                    Keybindtranslation.Add("NUM6", "Num6");
                }

                if (!Keybindtranslation.ContainsKey("NUM7"))
                {
                    Keybindtranslation.Add("NUM7", "Num7");
                }

                if (!Keybindtranslation.ContainsKey("NUM8"))
                {
                    Keybindtranslation.Add("NUM8", "Num8");
                }

                if (!Keybindtranslation.ContainsKey("NUM9"))
                {
                    Keybindtranslation.Add("NUM9", "Num9");
                }

                if (!Keybindtranslation.ContainsKey("NUM."))
                {
                    Keybindtranslation.Add("NUM.", "NumDecimal");
                }

                if (!Keybindtranslation.ContainsKey("NUM/"))
                {
                    Keybindtranslation.Add("NUM/", "NumDivide");
                }

                if (!Keybindtranslation.ContainsKey("NUM*"))
                {
                    Keybindtranslation.Add("NUM*", "NumMultiply");
                }

                if (!Keybindtranslation.ContainsKey("NUM-"))
                {
                    Keybindtranslation.Add("NUM-", "NumSubtract");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM0"))
                {
                    KeybindtranslationAZERTY.Add("NUM0", "Num0");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM1"))
                {
                    KeybindtranslationAZERTY.Add("NUM1", "Num1");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM2"))
                {
                    KeybindtranslationAZERTY.Add("NUM2", "Num2");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM3"))
                {
                    KeybindtranslationAZERTY.Add("NUM3", "Num3");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM4"))
                {
                    KeybindtranslationAZERTY.Add("NUM4", "Num4");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM5"))
                {
                    KeybindtranslationAZERTY.Add("NUM5", "Num5");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM6"))
                {
                    KeybindtranslationAZERTY.Add("NUM6", "Num6");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM7"))
                {
                    KeybindtranslationAZERTY.Add("NUM7", "Num7");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM8"))
                {
                    KeybindtranslationAZERTY.Add("NUM8", "Num8");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM9"))
                {
                    KeybindtranslationAZERTY.Add("NUM9", "Num9");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM."))
                {
                    KeybindtranslationAZERTY.Add("NUM.", "NumDecimal");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM/"))
                {
                    KeybindtranslationAZERTY.Add("NUM/", "NumDivide");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM*"))
                {
                    KeybindtranslationAZERTY.Add("NUM*", "NumMultiply");
                }

                if (!KeybindtranslationAZERTY.ContainsKey("NUM-"))
                {
                    KeybindtranslationAZERTY.Add("NUM-", "NumSubtract");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM0"))
                {
                    KeybindtranslationESDF.Add("NUM0", "Num0");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM1"))
                {
                    KeybindtranslationESDF.Add("NUM1", "Num1");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM2"))
                {
                    KeybindtranslationESDF.Add("NUM2", "Num2");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM3"))
                {
                    KeybindtranslationESDF.Add("NUM3", "Num3");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM4"))
                {
                    KeybindtranslationESDF.Add("NUM4", "Num4");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM5"))
                {
                    KeybindtranslationESDF.Add("NUM5", "Num5");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM6"))
                {
                    KeybindtranslationESDF.Add("NUM6", "Num6");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM7"))
                {
                    KeybindtranslationESDF.Add("NUM7", "Num7");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM8"))
                {
                    KeybindtranslationESDF.Add("NUM8", "Num8");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM9"))
                {
                    KeybindtranslationESDF.Add("NUM9", "Num9");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM."))
                {
                    KeybindtranslationESDF.Add("NUM.", "NumDecimal");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM/"))
                {
                    KeybindtranslationESDF.Add("NUM/", "NumDivide");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM*"))
                {
                    KeybindtranslationESDF.Add("NUM*", "NumMultiply");
                }

                if (!KeybindtranslationESDF.ContainsKey("NUM-"))
                {
                    KeybindtranslationESDF.Add("NUM-", "NumSubtract");
                }

            }
            else
            {
                if (Keybindtranslation.ContainsKey("NUM0"))
                {
                    Keybindtranslation.Remove("NUM0");
                }

                if (!Keybindtranslation.ContainsKey("NUM1"))
                {
                    Keybindtranslation.Remove("NUM1");
                }

                if (Keybindtranslation.ContainsKey("NUM2"))
                {
                    Keybindtranslation.Remove("NUM2");
                }

                if (Keybindtranslation.ContainsKey("NUM3"))
                {
                    Keybindtranslation.Remove("NUM3");
                }

                if (Keybindtranslation.ContainsKey("NUM4"))
                {
                    Keybindtranslation.Remove("NUM4");
                }

                if (Keybindtranslation.ContainsKey("NUM5"))
                {
                    Keybindtranslation.Remove("NUM5");
                }

                if (Keybindtranslation.ContainsKey("NUM6"))
                {
                    Keybindtranslation.Remove("NUM6");
                }

                if (Keybindtranslation.ContainsKey("NUM7"))
                {
                    Keybindtranslation.Remove("NUM7");
                }

                if (Keybindtranslation.ContainsKey("NUM8"))
                {
                    Keybindtranslation.Remove("NUM8");
                }

                if (Keybindtranslation.ContainsKey("NUM9"))
                {
                    Keybindtranslation.Remove("NUM9");
                }

                if (Keybindtranslation.ContainsKey("NUM."))
                {
                    Keybindtranslation.Remove("NUM.");
                }

                if (Keybindtranslation.ContainsKey("NUM/"))
                {
                    Keybindtranslation.Remove("NUM/");
                }

                if (Keybindtranslation.ContainsKey("NUM*"))
                {
                    Keybindtranslation.Remove("NUM*");
                }

                if (Keybindtranslation.ContainsKey("NUM-"))
                {
                    Keybindtranslation.Remove("NUM-");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM0"))
                {
                    KeybindtranslationAZERTY.Remove("NUM0");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM1"))
                {
                    KeybindtranslationAZERTY.Remove("NUM1");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM2"))
                {
                    KeybindtranslationAZERTY.Remove("NUM2");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM3"))
                {
                    KeybindtranslationAZERTY.Remove("NUM3");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM4"))
                {
                    KeybindtranslationAZERTY.Remove("NUM4");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM5"))
                {
                    KeybindtranslationAZERTY.Remove("NUM5");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM6"))
                {
                    KeybindtranslationAZERTY.Remove("NUM6");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM7"))
                {
                    KeybindtranslationAZERTY.Remove("NUM7");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM8"))
                {
                    KeybindtranslationAZERTY.Remove("NUM8");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM9"))
                {
                    KeybindtranslationAZERTY.Remove("NUM9");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM."))
                {
                    KeybindtranslationAZERTY.Remove("NUM.");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM/"))
                {
                    KeybindtranslationAZERTY.Remove("NUM/");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM*"))
                {
                    KeybindtranslationAZERTY.Remove("NUM*");
                }

                if (KeybindtranslationAZERTY.ContainsKey("NUM-"))
                {
                    KeybindtranslationAZERTY.Remove("NUM-");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM0"))
                {
                    KeybindtranslationESDF.Remove("NUM0");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM1"))
                {
                    KeybindtranslationESDF.Remove("NUM1");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM2"))
                {
                    KeybindtranslationESDF.Remove("NUM2");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM3"))
                {
                    KeybindtranslationESDF.Remove("NUM3");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM4"))
                {
                    KeybindtranslationESDF.Remove("NUM4");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM5"))
                {
                    KeybindtranslationESDF.Remove("NUM5");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM6"))
                {
                    KeybindtranslationESDF.Remove("NUM6");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM7"))
                {
                    KeybindtranslationESDF.Remove("NUM7");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM8"))
                {
                    KeybindtranslationESDF.Remove("NUM8");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM9"))
                {
                    KeybindtranslationESDF.Remove("NUM9");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM."))
                {
                    KeybindtranslationESDF.Remove("NUM.");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM/"))
                {
                    KeybindtranslationESDF.Remove("NUM/");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM*"))
                {
                    KeybindtranslationESDF.Remove("NUM*");
                }

                if (KeybindtranslationESDF.ContainsKey("NUM-"))
                {
                    KeybindtranslationESDF.Remove("NUM-");
                }
            }
        }

        public static List<string> Keybindwhitelist = new List<string>();

        public static List<string> KeybindsActive = new List<string>();

        public static List<string> KeybindsHighlights = new List<string>();
        
        public static readonly Dictionary<string, string> Keybindtranslation = new Dictionary<string, string>
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
            //{"A", "A"},
            {"B", "B"},
            {"C", "C"},
            //{"D", "D"},
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
            //{"S", "S"},
            {"T", "T"},
            {"U", "U"},
            {"V", "V"},
            //{"W", "W"},
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
            {"Esc", "Escape"},
            {"Ü",  "OemLeftBracket"},
            {"Ö",  "OemSemicolon"},
            {"Ä",  "OemApostrophe"},
            {"^",  "OemTilde"},
            {"NUM0", "Num0" },
            {"NUM1", "Num1" },
            {"NUM2", "Num2" },
            {"NUM3", "Num3" },
            {"NUM4", "Num4" },
            {"NUM5", "Num5" },
            {"NUM6", "Num6" },
            {"NUM7", "Num7" },
            {"NUM8", "Num8" },
            {"NUM9", "Num9" },
            {"NUM.", "NumDecimal" },
            {"Num/", "NumDivide" },
            {"Num*", "NumMultiply" },
            {"Num-", "NumSubtract" }
            
        };

        public static readonly Dictionary<string, string> KeybindtranslationAZERTY = new Dictionary<string, string>
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
            {"&", "D1"},
            {"é", "D2"},
            { "\"", "D3"},
            { "'", "D4"},
            { "(", "D5"},
            { "-", "D6"},
            { "è", "D7"},
            { "_", "D8"},
            { "ç", "D9"},
            { "à", "D0"},
            { "Q", "A"},
            { "B", "B"},
            { "C", "C"},
            { "D", "D"},
            { "E", "E"},
            { "F", "F"},
            { "G", "G"},
            { "H", "H"},
            { "I", "I"},
            { "J", "J"},
            { "K", "K"},
            { "L", "L"},
            { "M", "M"},
            { "N", "N"},
            { "O", "O"},
            { "P", "P"},
            { "A", "Q"},
            { "R", "R"},
            { "S", "S"},
            { "T", "T"},
            { "U", "U"},
            { "V", "V"},
            { "Z", "W"},
            { "X", "X"},
            { "Y", "Y"},
            { "W", "Z"},
            {"Tab", "Tab"},
            {"Backspace", "Backspace"},
            {"`", "OemTilde"},
            {")", "OemMinus"},
            {"=", "OemEquals"},
            {"[", "OemLeftBracket"},
            {"]", "OemRightBracket"},
            {"/", "OemSlash"},
            {";", "OemSemicolon"},
            {",", "OemComma"},
            {".", "OemPeriod"},
            {@"\", "OemBackslash"},
            //{"", "EurPound"},
            //{"", "JpnYen"},
            {"Esc", "Escape"},
            {"Ü",  "OemLeftBracket"},
            {"Ö",  "OemSemicolon"},
            {"Ä",  "OemApostrophe"},
            {"^",  "OemTilde"},
            {"²",  "OemTilde"},
            {"NUM0", "Num0" },
            {"NUM1", "Num1" },
            {"NUM2", "Num2" },
            {"NUM3", "Num3" },
            {"NUM4", "Num4" },
            {"NUM5", "Num5" },
            {"NUM6", "Num6" },
            {"NUM7", "Num7" },
            {"NUM8", "Num8" },
            {"NUM9", "Num9" },
            {"NUM.", "NumDecimal" },
            {"Num/", "NumDivide" },
            {"Num*", "NumMultiply" },
            {"Num-", "NumSubtract" }
        };

        public static readonly Dictionary<string, string> KeybindtranslationESDF = new Dictionary<string, string>
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
            //{"D", "D"},
            //{"E", "E"},
            //{"F", "F"},
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
            //{"S", "S"},
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
            {"Esc", "Escape"},
            {"Ü",  "OemLeftBracket"},
            {"Ö",  "OemSemicolon"},
            {"Ä",  "OemApostrophe"},
            {"^",  "OemTilde"},
            {"NUM0", "Num0" },
            {"NUM1", "Num1" },
            {"NUM2", "Num2" },
            {"NUM3", "Num3" },
            {"NUM4", "Num4" },
            {"NUM5", "Num5" },
            {"NUM6", "Num6" },
            {"NUM7", "Num7" },
            {"NUM8", "Num8" },
            {"NUM9", "Num9" },
            {"NUM.", "NumDecimal" },
            {"Num/", "NumDivide" },
            {"Num*", "NumMultiply" },
            {"Num-", "NumSubtract" }
        };
    }
}