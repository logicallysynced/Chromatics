using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Chromatics.Tests.Localization;

public class LocaleCompletenessTests
{
    public static IEnumerable<object[]> NonEnglishLocales => new[]
    {
        new object[] { "de" },
        new object[] { "es" },
        new object[] { "fr" },
        new object[] { "ja" },
        new object[] { "ko" },
        new object[] { "zh_CN" },
    };

    [Theory]
    [MemberData(nameof(NonEnglishLocales))]
    public void LocaleFile_ContainsAllEnglishKeys(string locale)
    {
        var localeDir = GetLocaleDirectory();
        var enPath = Path.Combine(localeDir, "en.json");
        var targetPath = Path.Combine(localeDir, $"{locale}.json");

        Assert.True(File.Exists(enPath), $"Master en.json not found at {enPath}");
        Assert.True(File.Exists(targetPath), $"{locale}.json not found at {targetPath}");

        var enKeys = LoadKeys(enPath);
        var targetKeys = LoadKeys(targetPath);

        var missing = enKeys.Except(targetKeys).OrderBy(k => k).ToList();

        Assert.True(missing.Count == 0,
            $"{locale}.json is missing {missing.Count} key(s) from en.json. " +
            $"Run `python translate.py --update` from the repo root to regenerate.\n" +
            "First missing keys:\n  " +
            string.Join("\n  ", missing.Take(10)) +
            (missing.Count > 10 ? $"\n  ...and {missing.Count - 10} more" : ""));
    }

    private static HashSet<string> LoadKeys(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return new HashSet<string>(doc.RootElement.EnumerateObject().Select(p => p.Name));
    }

    // [CallerFilePath] resolves at compile time to the absolute path of this
    // source file, which lets the test locate the locale/ directory in the
    // sibling project regardless of which build config the binary came from.
    private static string GetLocaleDirectory([CallerFilePath] string sourceFile = "")
    {
        var testDir = Path.GetDirectoryName(sourceFile)!;
        var testsRoot = Path.GetDirectoryName(testDir)!;
        var repoRoot = Path.GetDirectoryName(testsRoot)!;
        return Path.Combine(repoRoot, "Chromatics", "locale");
    }
}
