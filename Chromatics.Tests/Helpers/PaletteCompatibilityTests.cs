using Chromatics.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chromatics.Tests.Helpers;

public class PaletteCompatibilityTests
{
    // palette.chromatics4 written by an older build lacks every field added
    // since. Deserialisation must fall back to the C# field initialisers for
    // the missing members so new palette entries appear (with defaults) the
    // first time the user runs a newer build.
    [Fact]
    public void Deserialize_FileMissingNewFields_FallsBackToInitialisers()
    {
        var json = JObject.FromObject(new PaletteColorModel());
        json.Remove(nameof(PaletteColorModel.CastingSuccess));
        json.Remove(nameof(PaletteColorModel.FocusTargetHpClaimed));
        json.Remove(nameof(PaletteColorModel.Doom));
        json["version"] = "2";

        var loaded = JsonConvert.DeserializeObject<PaletteColorModel>(json.ToString());

        Assert.NotNull(loaded);
        Assert.NotNull(loaded.CastingSuccess);
        Assert.Equal("Casting Success", loaded.CastingSuccess.Name);
        Assert.Equal(System.Drawing.Color.White.ToArgb(), loaded.CastingSuccess.Color.ToArgb());
        Assert.NotNull(loaded.FocusTargetHpClaimed);
        Assert.NotNull(loaded.Doom);
        Assert.Equal("2", loaded.version);
    }

    // A file written by a newer build can carry members this build doesn't
    // know. Loading must not throw and known members must still apply -
    // that's the forward-compatibility half of the palette versioning
    // contract.
    [Fact]
    public void Deserialize_FileWithUnknownFields_IgnoresThem()
    {
        var json = JObject.FromObject(new PaletteColorModel());
        json["version"] = "99";
        json["SomeFutureEntry"] = JObject.FromObject(new { Name = "Future", Type = 7, Color = "Red" });

        var loaded = JsonConvert.DeserializeObject<PaletteColorModel>(json.ToString());

        Assert.NotNull(loaded);
        Assert.Equal("99", loaded.version);
        Assert.Equal("Bleeding", loaded.Bleed.Name);
    }

    // Every StatusEffects display name doubles as the StatusNameEnglish
    // match key for the Status Inflicted effect, so duplicates would make
    // the colour lookup ambiguous.
    [Fact]
    public void StatusEffectEntries_HaveUniqueNames()
    {
        var palette = new PaletteColorModel();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in typeof(PaletteColorModel).GetFields())
        {
            if (field.FieldType != typeof(ColorMapping)) continue;
            var mapping = (ColorMapping)field.GetValue(palette)!;
            if (mapping.Type != Chromatics.Enums.Palette.PaletteTypes.StatusEffects) continue;
            Assert.True(names.Add(mapping.Name), $"Duplicate status palette name: {mapping.Name}");
        }

        Assert.True(names.Count > 1000, $"Expected the full detrimental catalogue, found {names.Count}");
    }
}
