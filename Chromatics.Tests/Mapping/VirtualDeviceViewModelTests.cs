using Chromatics.Enums;
using Chromatics.Models;
using Chromatics.ViewModels.Mapping;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chromatics.Tests.Mapping;

public class VirtualDeviceViewModelTests
{
    [Fact]
    public void BuildForKeyboard_Qwerty_HasAtLeastFiveRows()
    {
        var vm = VirtualDeviceViewModel.BuildForKeyboard(Guid.NewGuid(), "Kb", KeyboardLocalization.qwerty);

        var distinctY = vm.Keycaps.Select(k => k.Y).Distinct().ToList();

        // Full keyboard has Fn row + digits + 3 letter rows + spacebar row + macro/nav blocks.
        Assert.True(distinctY.Count >= 5,
            $"Expected at least 5 rows, got {distinctY.Count}");
    }

    [Fact]
    public void BuildFromKeys_LineBreak_StartsNewRowAtZeroX()
    {
        var keys = new List<KeyboardKey>
        {
            new KeyboardKey("A", LedId.Keyboard_A, linebreak: false, width: 30, height: 30),
            new KeyboardKey("B", LedId.Keyboard_B, linebreak: true,  width: 30, height: 30),
            new KeyboardKey("C", LedId.Keyboard_C, linebreak: false, width: 30, height: 30),
        };

        var vm = VirtualDeviceViewModel.BuildFromKeys(Guid.NewGuid(), "Kb", RGBDeviceType.Keyboard, keys);

        var a = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_A);
        var b = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_B);
        var c = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_C);

        Assert.Equal(a.Y, b.Y);
        Assert.True(c.Y > a.Y, "C should be on a new row below A/B");
        Assert.Equal(0, c.X);
    }

    [Fact]
    public void BuildFromKeys_LargeMarginLeft_InsertsHorizontalGap()
    {
        // Default margin_left is 7 (the implicit gutter). A value > 7 should
        // introduce extra horizontal offset before the key.
        var keys = new List<KeyboardKey>
        {
            new KeyboardKey("A", LedId.Keyboard_A, margin_left: 7,  width: 30, height: 30),
            new KeyboardKey("B", LedId.Keyboard_B, margin_left: 50, width: 30, height: 30),
        };

        var vm = VirtualDeviceViewModel.BuildFromKeys(Guid.NewGuid(), "Kb", RGBDeviceType.Keyboard, keys);

        var a = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_A);
        var b = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_B);

        // Expected X for B = A.X + A.Width + 50
        double expected = a.X + a.Width + 50;
        Assert.Equal(expected, b.X);
    }

    [Fact]
    public void BuildFromKeys_AvailableLedsFilter_SkipsMissingButPreservesRowBreaks()
    {
        var keys = new List<KeyboardKey>
        {
            new KeyboardKey("A", LedId.Keyboard_A, linebreak: false, width: 30, height: 30),
            new KeyboardKey("B", LedId.Keyboard_B, linebreak: true,  width: 30, height: 30),
            new KeyboardKey("C", LedId.Keyboard_C, linebreak: false, width: 30, height: 30),
        };

        var filter = new HashSet<LedId> { LedId.Keyboard_A, LedId.Keyboard_C };
        var vm = VirtualDeviceViewModel.BuildFromKeys(Guid.NewGuid(), "Kb", RGBDeviceType.Keyboard, keys, filter);

        Assert.Equal(2, vm.Keycaps.Count);

        var a = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_A);
        var c = vm.Keycaps.First(k => k.LedType == LedId.Keyboard_C);

        // Even though B was filtered, its line_break still moved C to a new row.
        Assert.True(c.Y > a.Y);
        Assert.Equal(0, c.X);
    }

    [Fact]
    public void BuildForKeyboard_ReportsWidthAndHeightCoveringAllKeys()
    {
        var vm = VirtualDeviceViewModel.BuildForKeyboard(Guid.NewGuid(), "Kb", KeyboardLocalization.qwerty);

        double maxRight = vm.Keycaps.Max(k => k.X + k.Width);
        double maxBottom = vm.Keycaps.Max(k => k.Y + k.Height);

        Assert.Equal(maxRight, vm.Width);
        Assert.True(vm.Height >= maxBottom - 0.001);
    }
}
