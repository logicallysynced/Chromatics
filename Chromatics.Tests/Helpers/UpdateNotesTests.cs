using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

// Pins the release-notes trimming behind the update dialog. Notes embedded
// by older publishes start with their own version heading and carry every
// older changelog section; the dialog adds its own heading per release, so
// rendering them raw doubled the version line and repeated old sections.
public class UpdateNotesTests
{
    [Fact]
    public void OldFormatNotes_ReduceToOwnBullets()
    {
        var notes = "## 4.3.26\n\n- New feature one.\n- Fix two.\n\n## 4.3.24\n\n- Old bullet.\n";

        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.26");

        Assert.Equal("- New feature one.\n- Fix two.", trimmed.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NewFormatNotes_BulletsOnly_PassThroughUnchanged()
    {
        var notes = "- New feature one.\n- Fix two.";

        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.26");

        Assert.Equal(notes, trimmed.Replace("\r\n", "\n"));
    }

    [Theory]
    [InlineData("## 4.3.26.0\n\n- Bullet.")]
    [InlineData("## [4.3.26]\n\n- Bullet.")]
    [InlineData("## v4.3.26\n\n- Bullet.")]
    [InlineData("## 4.3.26 - 2026-07-07\n\n- Bullet.")]
    public void HeadingVariants_AreAllStripped(string notes)
    {
        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.26");

        Assert.Equal("- Bullet.", trimmed);
    }

    [Fact]
    public void DifferentVersionHeading_IsNotStripped_ButLaterSectionsAreCut()
    {
        // A mislabelled asset should not lose its first line; only the
        // trailing sections get cut.
        var notes = "- Bullet without heading.\n\n## 4.3.20\n\n- Old.";

        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.26");

        Assert.Equal("- Bullet without heading.", trimmed.Replace("\r\n", "\n"));
    }

    [Fact]
    public void EmptyOrWhitespaceNotes_ReturnEmpty()
    {
        Assert.Equal(string.Empty, UpdateService.TrimNotesToOwnSection("", "4.3.26"));
        Assert.Equal(string.Empty, UpdateService.TrimNotesToOwnSection("   ", "4.3.26"));
        Assert.Equal(string.Empty, UpdateService.TrimNotesToOwnSection(null!, "4.3.26"));
    }

    [Fact]
    public void VersionPrefix_DoesNotStripALongerVersionsHeading()
    {
        // "4.3.2" is a numeric prefix of "4.3.26" - the heading must
        // survive so another release's bullets are never adopted.
        var notes = "## 4.3.26\n\n- Bullet from 4.3.26.";

        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.2");

        Assert.StartsWith("## 4.3.26", trimmed);
        Assert.Contains("- Bullet from 4.3.26.", trimmed);
    }

    [Fact]
    public void WrongLeadingHeading_KeepsItsSection_InsteadOfWipingToEmpty()
    {
        // A mislabelled feed asset: the heading names a different version.
        // The entry must keep that heading and its bullets; only sections
        // after it get cut.
        var notes = "## 4.3.30\n\n- Bullet A.\n\n## 4.3.29\n\n- Bullet B.";

        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.29");

        Assert.StartsWith("## 4.3.30", trimmed.Replace("\r\n", "\n"));
        Assert.Contains("- Bullet A.", trimmed);
        Assert.DoesNotContain("- Bullet B.", trimmed);
    }

    [Fact]
    public void LeadingHorizontalRule_IsStripped()
    {
        var notes = "---\n\n- Bullet.";

        var trimmed = UpdateService.TrimNotesToOwnSection(notes, "4.3.26");

        Assert.Equal("- Bullet.", trimmed);
    }
}
