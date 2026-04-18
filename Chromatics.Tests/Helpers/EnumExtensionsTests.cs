using System.ComponentModel.DataAnnotations;
using Chromatics.Helpers;

namespace Chromatics.Tests.Helpers;

public class EnumExtensionsTests
{
    private enum Colour
    {
        [Display(Name = "Cadet Blue")]
        CadetBlue,

        // Deliberately no Display attribute — GetDisplayName should fall back to ToString().
        Plain,
    }

    [Fact]
    public void GetDisplayName_WithDisplayAttribute_ReturnsAttributeName()
    {
        Assert.Equal("Cadet Blue", Colour.CadetBlue.GetDisplayName());
    }

    [Fact]
    public void GetDisplayName_WithoutAttribute_FallsBackToToString()
    {
        Assert.Equal("Plain", Colour.Plain.GetDisplayName());
    }

    [Fact]
    public void GetAttribute_MissingAttribute_ReturnsNull()
    {
        Assert.Null(Colour.Plain.GetAttribute<DisplayAttribute>());
    }

    [Fact]
    public void GetAttribute_PresentAttribute_Returns()
    {
        var attr = Colour.CadetBlue.GetAttribute<DisplayAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Cadet Blue", attr!.Name);
    }
}
