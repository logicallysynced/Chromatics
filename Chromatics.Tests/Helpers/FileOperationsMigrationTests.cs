using Chromatics.Helpers;
using System.IO;

namespace Chromatics.Tests.Helpers;

public class FileOperationsMigrationTests : IDisposable
{
    private readonly string _tempDir;

    public FileOperationsMigrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "chromatics-migrate-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Migrate_WithLegacyFilePresent_CreatesChromatics4AndPreservesOriginal()
    {
        var legacyPath = Path.Combine(_tempDir, "layers.chromatics3");
        File.WriteAllText(legacyPath, "{\"schemaVersion\":3,\"layers\":{}}");

        var migrated = FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);

        Assert.Contains("layers.chromatics3", migrated);
        Assert.True(File.Exists(Path.Combine(_tempDir, "layers.chromatics4")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "layers.chromatics3.migrated")));
        Assert.False(File.Exists(legacyPath));
    }

    [Fact]
    public void Migrate_WithChromatics4AlreadyPresent_DoesNotOverwrite()
    {
        var legacyPath = Path.Combine(_tempDir, "layers.chromatics3");
        var newPath    = Path.Combine(_tempDir, "layers.chromatics4");
        File.WriteAllText(legacyPath, "{\"old\":true}");
        File.WriteAllText(newPath,    "{\"new\":true}");

        var migrated = FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);

        Assert.DoesNotContain("layers.chromatics3", migrated);
        Assert.Equal("{\"new\":true}", File.ReadAllText(newPath));
        // Legacy file is still preserved, just renamed.
        Assert.True(File.Exists(Path.Combine(_tempDir, "layers.chromatics3.migrated")));
        Assert.False(File.Exists(legacyPath));
    }

    [Fact]
    public void Migrate_RunTwice_IsIdempotent()
    {
        var legacyPath = Path.Combine(_tempDir, "settings.chromatics3");
        File.WriteAllText(legacyPath, "{}");

        var first  = FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);
        var second = FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);

        Assert.Single(first);
        Assert.Empty(second);
        Assert.True(File.Exists(Path.Combine(_tempDir, "settings.chromatics4")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "settings.chromatics3.migrated")));
    }

    [Fact]
    public void Migrate_WithAllFourLegacyFilesPresent_MigratesAll()
    {
        File.WriteAllText(Path.Combine(_tempDir, "layers.chromatics3"),   "{}");
        File.WriteAllText(Path.Combine(_tempDir, "palette.chromatics3"),  "{}");
        File.WriteAllText(Path.Combine(_tempDir, "effects.chromatics3"),  "{}");
        File.WriteAllText(Path.Combine(_tempDir, "settings.chromatics3"), "{}");

        var migrated = FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);

        Assert.Equal(4, migrated.Count);
        Assert.True(File.Exists(Path.Combine(_tempDir, "layers.chromatics4")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "palette.chromatics4")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "effects.chromatics4")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "settings.chromatics4")));
    }

    [Fact]
    public void Migrate_WithNoLegacyFiles_ReturnsEmptyList()
    {
        var migrated = FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);
        Assert.Empty(migrated);
    }

    [Fact]
    public void Migrate_WithMigratedFileAlreadyPresent_DeletesDuplicateLegacy()
    {
        // A re-created .chromatics3 (e.g. user dropped an old file back in)
        // alongside a pre-existing .migrated from a prior run. The migration
        // shouldn't clobber the preserved .migrated file; it should just remove
        // the redundant .chromatics3 so we don't re-migrate on every launch.
        File.WriteAllText(Path.Combine(_tempDir, "layers.chromatics3"),          "{\"recreated\":true}");
        File.WriteAllText(Path.Combine(_tempDir, "layers.chromatics3.migrated"), "{\"original-preserved\":true}");
        File.WriteAllText(Path.Combine(_tempDir, "layers.chromatics4"),          "{\"current\":true}");

        FileOperationsHelper.MigrateLegacyChromatics3Files(_tempDir);

        Assert.False(File.Exists(Path.Combine(_tempDir, "layers.chromatics3")));
        Assert.Equal("{\"original-preserved\":true}", File.ReadAllText(Path.Combine(_tempDir, "layers.chromatics3.migrated")));
        Assert.Equal("{\"current\":true}",            File.ReadAllText(Path.Combine(_tempDir, "layers.chromatics4")));
    }
}
