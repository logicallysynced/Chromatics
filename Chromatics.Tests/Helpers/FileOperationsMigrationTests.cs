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

    [Fact]
    public void RelocateToAppData_MovesKnownFilesAndLeavesSourceEmpty()
    {
        var src = Path.Combine(_tempDir, "exe");
        var dst = Path.Combine(_tempDir, "appdata");
        Directory.CreateDirectory(src);

        File.WriteAllText(Path.Combine(src, "layers.chromatics4"),   "L");
        File.WriteAllText(Path.Combine(src, "palette.chromatics4"),  "P");
        File.WriteAllText(Path.Combine(src, "effects.chromatics4"),  "E");
        File.WriteAllText(Path.Combine(src, "settings.chromatics4"), "S");

        var moved = FileOperationsHelper.MigrateExeDirDataToAppData(src, dst);

        Assert.Equal(4, moved.Count);
        Assert.True(File.Exists(Path.Combine(dst, "layers.chromatics4")));
        Assert.Equal("L", File.ReadAllText(Path.Combine(dst, "layers.chromatics4")));
        Assert.False(File.Exists(Path.Combine(src, "layers.chromatics4")));
    }

    [Fact]
    public void RelocateToAppData_WhenTargetAlreadyExists_LeavesSourceUntouched()
    {
        var src = Path.Combine(_tempDir, "exe");
        var dst = Path.Combine(_tempDir, "appdata");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);

        File.WriteAllText(Path.Combine(src, "settings.chromatics4"), "OLD");
        File.WriteAllText(Path.Combine(dst, "settings.chromatics4"), "NEW");

        var moved = FileOperationsHelper.MigrateExeDirDataToAppData(src, dst);

        Assert.Empty(moved);
        Assert.Equal("NEW", File.ReadAllText(Path.Combine(dst, "settings.chromatics4")));
        Assert.Equal("OLD", File.ReadAllText(Path.Combine(src, "settings.chromatics4")));
    }

    [Fact]
    public void RelocateToAppData_IncludesLegacyAndBackupFiles()
    {
        var src = Path.Combine(_tempDir, "exe");
        var dst = Path.Combine(_tempDir, "appdata");
        Directory.CreateDirectory(src);

        File.WriteAllText(Path.Combine(src, "layers.chromatics3"),                   "legacy");
        File.WriteAllText(Path.Combine(src, "palette.chromatics3.migrated"),         "preserved");
        File.WriteAllText(Path.Combine(src, "backup_layers_20250101_120000.chromatics4"), "backup");

        var moved = FileOperationsHelper.MigrateExeDirDataToAppData(src, dst);

        Assert.Contains("layers.chromatics3", moved);
        Assert.Contains("palette.chromatics3.migrated", moved);
        Assert.Contains("backup_layers_20250101_120000.chromatics4", moved);
        Assert.True(File.Exists(Path.Combine(dst, "backup_layers_20250101_120000.chromatics4")));
    }

    [Fact]
    public void RelocateToAppData_WhenSourceEqualsTarget_IsNoOp()
    {
        File.WriteAllText(Path.Combine(_tempDir, "layers.chromatics4"), "L");

        var moved = FileOperationsHelper.MigrateExeDirDataToAppData(_tempDir, _tempDir);

        Assert.Empty(moved);
        Assert.True(File.Exists(Path.Combine(_tempDir, "layers.chromatics4")));
    }

    [Fact]
    public void RelocateToAppData_WithNoKnownFiles_ReturnsEmptyList()
    {
        var src = Path.Combine(_tempDir, "exe");
        var dst = Path.Combine(_tempDir, "appdata");
        Directory.CreateDirectory(src);

        var moved = FileOperationsHelper.MigrateExeDirDataToAppData(src, dst);

        Assert.Empty(moved);
    }
}
