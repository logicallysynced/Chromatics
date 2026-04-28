using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class FileOperationsHelperTests
{
    [Fact]
    public void ValidateLayerJson_EmptyString_ReturnsInvalid()
    {
        var (valid, reason) = FileOperationsHelper.ValidateLayerJson(string.Empty);

        Assert.False(valid);
        Assert.NotNull(reason);
    }

    [Fact]
    public void ValidateLayerJson_WhitespaceOnly_ReturnsInvalid()
    {
        var (valid, reason) = FileOperationsHelper.ValidateLayerJson("   ");

        Assert.False(valid);
        Assert.NotNull(reason);
    }

    [Fact]
    public void ValidateLayerJson_MalformedJson_ReturnsInvalid()
    {
        var (valid, reason) = FileOperationsHelper.ValidateLayerJson("not json {{{");

        Assert.False(valid);
        Assert.NotNull(reason);
    }

    [Fact]
    public void ValidateLayerJson_JsonArray_ReturnsInvalid()
    {
        var (valid, reason) = FileOperationsHelper.ValidateLayerJson("[1,2,3]");

        Assert.False(valid);
        Assert.NotNull(reason);
    }

    [Fact]
    public void ValidateLayerJson_NewFormatWithSchemaVersionAndLayers_ReturnsValid()
    {
        const string json = """{"schemaVersion":3,"layers":{},"deviceLayouts":{}}""";

        var (valid, _) = FileOperationsHelper.ValidateLayerJson(json);

        Assert.True(valid);
    }

    [Fact]
    public void ValidateLayerJson_LegacyFormatAllIntegerKeys_ReturnsValid()
    {
        // The legacy format is a bare ConcurrentDictionary<int, Layer> — every
        // top-level key is an integer layer ID.
        const string json = """{"1":{},"2":{}}""";

        var (valid, _) = FileOperationsHelper.ValidateLayerJson(json);

        Assert.True(valid);
    }

    [Fact]
    public void ValidateLayerJson_NonLayerFileStringKeys_ReturnsInvalid()
    {
        // A palette, settings, or arbitrary JSON file should be rejected.
        const string json = """{"version":"2.0","name":"test","data":{}}""";

        var (valid, reason) = FileOperationsHelper.ValidateLayerJson(json);

        Assert.False(valid);
        Assert.NotNull(reason);
    }
}
