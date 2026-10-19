using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Chromatics.Helpers
{
    /// <summary>
    /// Reads the modifier names out of a hotbar keybind hint.
    /// </summary>
    /// <remarks>
    /// The hint comes from the game, so the names arrive in the client's own
    /// language: a German client says "Strg" where an English one says "Ctrl".
    /// Matching the English spellings alone meant every modified keybind was
    /// invisible outside an English client.
    ///
    /// The spellings below are the game's own, read out of the Addon sheet
    /// rows 7915 (Ctrl), 7916 (Alt) and 7917 (Shift) in every client language:
    ///
    ///     en / ja / chs / tc / ko   Ctrl   Alt   Shift
    ///     de                        Strg   Alt   Umsch
    ///     fr                        Ctrl   Alt   Maj.
    ///
    /// There is no Spanish client, so a Spanish Chromatics install reads
    /// whichever of these its game is running.
    ///
    /// Anything this cannot place is returned through <c>unrecognised</c> so the
    /// caller can name it in the console tab, which turns a silent miss into one
    /// line a user can report and one entry to add here.
    /// </remarks>
    public static class HotbarModifierParser
    {
        // Lower-cased, width-folded and stripped of punctuation before lookup,
        // so "Maj." arrives here as "maj" and needs no entry of its own.
        private static readonly HashSet<string> CtrlNames = new(StringComparer.Ordinal)
        {
            "ctrl",       // en, fr, ja, chs, tc, ko
            "strg",       // de
            "ctl",        // tolerated variants
            "control",
            "steuerung",
        };

        private static readonly HashSet<string> AltNames = new(StringComparer.Ordinal)
        {
            "alt",        // every client
            "altgr",
            "alt gr",
        };

        private static readonly HashSet<string> ShiftNames = new(StringComparer.Ordinal)
        {
            "shift",      // en, ja, chs, tc, ko
            "umsch",      // de - the sheet abbreviates it, not "Umschalt"
            "maj",        // fr, from "Maj."
            "umschalt",   // tolerated variants
            "umschalttaste",
            "majuscule",
        };

        /// <summary>
        /// Resolves which of Ctrl / Alt / Shift a keybind's modifier names refer to.
        /// </summary>
        /// <param name="modifiers">The modifier names as the game rendered them.</param>
        /// <param name="unrecognised">Names that matched nothing, for diagnostics.</param>
        public static (bool Ctrl, bool Alt, bool Shift) Parse(
            IEnumerable<string> modifiers, out IReadOnlyList<string> unrecognised)
        {
            bool ctrl = false, alt = false, shift = false;
            List<string> unknown = null;

            foreach (var raw in modifiers ?? Enumerable.Empty<string>())
            {
                var name = Normalise(raw);
                if (name.Length == 0) continue;

                if (CtrlNames.Contains(name)) ctrl = true;
                else if (AltNames.Contains(name)) alt = true;
                else if (ShiftNames.Contains(name)) shift = true;
                else (unknown ??= new List<string>()).Add(raw);
            }

            unrecognised = unknown ?? (IReadOnlyList<string>)Array.Empty<string>();
            return (ctrl, alt, shift);
        }

        private static string Normalise(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            // A Japanese client can render the name in full-width characters, so
            // "Ｃｔｒｌ" has to fold onto "ctrl" before the lookup. FormKC does
            // that fold; the trailing trim drops the period some clients append.
            var folded = value.Normalize(NormalizationForm.FormKC).Trim();

            var builder = new StringBuilder(folded.Length);
            foreach (var c in folded)
            {
                if (char.IsLetterOrDigit(c) || c == ' ') builder.Append(c);
            }

            return builder.ToString().Trim().ToLower(CultureInfo.InvariantCulture);
        }
    }
}
