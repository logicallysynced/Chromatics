using System;
using System.Linq;
using System.Reflection;
using Chromatics.Models;

namespace Chromatics.Tests.Models;

public class WeatherCoverageTests
{
    // ReactiveWeatherProcessor.GetWeatherColor builds the palette field name
    // from the game's weather name verbatim:
    //     "Weather" + name.Replace(" ", "") + "Base"
    // A field spelled even slightly differently never resolves, and the weather
    // silently paints as Unknown instead. Firestorms, Umbral Duststorms and the
    // three 7.5x additions all landed that way, which is invisible without a
    // test because nothing throws.
    //
    // Names below are taken from the game's Weather sheet.
    private static readonly string[] GameWeather =
    [
        "Clear Skies", "Fair Skies", "Clouds", "Fog", "Rain", "Showers",
        "Wind", "Gales", "Snow", "Blizzards", "Thunder", "Thunderstorms",
        "Dust Storms", "Sandstorms", "Heat Waves", "Umbral Wind",
        "Umbral Static", "Everlasting Light",
        // Endwalker and Dawntrail zones.
        // "Astromagnetic Storms" is deliberately absent: its palette field is
        // singular, so the generic lookup misses it, but its own switch case
        // assigns WeatherAstromagneticStormBase in both branches and never
        // consults the generic path. Renaming the field to match would reset
        // the colour every user has already saved under the old name, for no
        // change in what they see.
        "Atmospheric Phantasms", "Illusory Disturbances",
        "Gravitational Flux", "Meteor Showers", "Sporing Mist",
        "Annealing Winds", "Glass Storms", "Bubble Bloom",
        // Plural spellings that previously fell through to Unknown.
        "Firestorms", "Umbral Duststorms",
        // Added in the 7.5x patches.
        "Lyrical Catharsis", "Auroral Flares", "Floracane",
    ];

    private static string FieldFor(string weather, string suffix)
        => "Weather" + weather.Replace(" ", "") + suffix;

    [Fact]
    public void EveryWeatherResolvesToAPaletteColour()
    {
        var fields = typeof(PaletteColorModel)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => f.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = GameWeather
            .Where(w => !fields.Contains(FieldFor(w, "Base")))
            .ToList();

        Assert.True(missing.Count == 0,
            "Weather with no palette colour, so it paints as Unknown: " + string.Join(", ", missing));
    }

    [Fact]
    public void WeatherColoursAreRegisteredUnderTheReactiveWeatherCategory()
    {
        var palette = new PaletteColorModel();

        foreach (var weather in GameWeather)
        {
            var field = typeof(PaletteColorModel)
                .GetField(FieldFor(weather, "Base"), BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(field);

            var mapping = (ColorMapping)field!.GetValue(palette)!;
            Assert.Equal(Chromatics.Enums.Palette.PaletteTypes.ReactiveWeather, mapping.Type);
        }
    }
}
