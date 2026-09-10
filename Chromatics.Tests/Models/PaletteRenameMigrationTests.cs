using System.Drawing;
using Chromatics.Models;
using Newtonsoft.Json;

namespace Chromatics.Tests.Models;

public class PaletteRenameMigrationTests
{
    // Palette files are keyed on the field name, so renaming a field drops
    // whatever the user had saved under the old one. The two weather fields
    // renamed to their plural in-game spellings carry their old values across
    // through setter-only aliases.
    private const string LegacyPalette = """
    {
      "version": "3",
      "WeatherFirestormBase":        { "Name": "Firestorm (Base)",        "Type": 11, "Color": "Magenta" },
      "WeatherFirestormHighlight":   { "Name": "Firestorm (Highlight)",   "Type": 11, "Color": "Teal" },
      "WeatherUmbralDuststormBase":  { "Name": "Umbral Duststorm (Base)", "Type": 11, "Color": "Navy" }
    }
    """;

    [Fact]
    public void ColoursSavedUnderTheOldWeatherKeysSurviveTheRename()
    {
        var palette = JsonConvert.DeserializeObject<PaletteColorModel>(LegacyPalette);

        Assert.NotNull(palette);
        Assert.Equal(Color.Magenta.ToArgb(), palette.WeatherFirestormsBase.Color.ToArgb());
        Assert.Equal(Color.Teal.ToArgb(), palette.WeatherFirestormsHighlight.Color.ToArgb());
        Assert.Equal(Color.Navy.ToArgb(), palette.WeatherUmbralDuststormsBase.Color.ToArgb());
    }

    [Fact]
    public void AnUntouchedEntryKeepsItsDefault()
    {
        var palette = JsonConvert.DeserializeObject<PaletteColorModel>(LegacyPalette);

        // Absent from the legacy file, so it must fall back to the initialiser
        // rather than being nulled by the partial deserialise.
        Assert.NotNull(palette!.WeatherUmbralDuststormsHighlight);
        Assert.Equal(Color.SandyBrown.ToArgb(), palette.WeatherUmbralDuststormsHighlight.Color.ToArgb());
    }

    [Fact]
    public void SavingDropsTheOldKeys()
    {
        // The aliases are setter-only, so a re-saved palette carries the new
        // spelling alone and the legacy keys retire.
        var json = JsonConvert.SerializeObject(new PaletteColorModel());

        Assert.Contains("WeatherFirestormsBase", json);
        Assert.DoesNotContain("\"WeatherFirestormBase\"", json);
        Assert.DoesNotContain("\"WeatherUmbralDuststormBase\"", json);
    }
}
