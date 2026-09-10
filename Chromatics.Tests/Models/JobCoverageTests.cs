using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chromatics.Models;
using Sharlayan.Core.Enums;

namespace Chromatics.Tests.Models;

public class JobCoverageTests
{
    // Base classes upgrade into a job and are shown in that job's colours, so
    // they carry no palette entry of their own. Unknown is the enum's sentinel
    // for "no job read yet" and is never painted.
    private static readonly HashSet<string> NotPainted =
        ["GLD", "PGL", "MRD", "LNC", "ARC", "CNJ", "THM", "ACN", "ROG", "Unknown"];

    private static IEnumerable<string> Jobs()
        => Enum.GetNames<Actor.Job>().Where(name => !NotPainted.Contains(name));

    [Fact]
    public void EveryJobHasBaseAndHighlightColours()
    {
        // Guards the job-addition checklist: a new job in Sharlayan without
        // palette entries here paints from whatever the switch falls through
        // to, which is silent and easy to miss (BST arrived in 7.56 this way).
        var fields = typeof(PaletteColorModel)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => f.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = Jobs()
            .Where(job => !fields.Contains($"Job{job}Base") || !fields.Contains($"Job{job}Highlight"))
            .ToList();

        Assert.True(missing.Count == 0, "Missing: " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryJobColourRoundTripsThroughTheLegacyPaletteFile()
    {
        // Legacy palette files map by name over reflection, so a job whose
        // entry exists on the model but not here loads back as its default and
        // quietly discards what the user picked.
        var dtoProperties = typeof(LegacyColorMappings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = Jobs()
            .Where(job => !dtoProperties.Contains($"ColorMappingJob{job}Base")
                       || !dtoProperties.Contains($"ColorMappingJob{job}Highlight"))
            .ToList();

        Assert.True(missing.Count == 0, "Missing: " + string.Join(", ", missing));
    }

    [Fact]
    public void BeastmasterIsRegistered()
    {
        Assert.Contains("BST", Enum.GetNames<Actor.Job>());
        Assert.NotNull(new PaletteColorModel().JobBSTBase);
        Assert.NotNull(new PaletteColorModel().JobBSTHighlight);
    }
}
