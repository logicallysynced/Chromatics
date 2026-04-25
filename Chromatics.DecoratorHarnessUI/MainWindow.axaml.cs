using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml;

namespace Chromatics.DecoratorHarnessUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        _vm.CopyToClipboard = async text =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(text);
        };
        DataContext = _vm;
        Closed += (_, _) => _vm.Dispose();
    }

    private async void OnColorSwatchTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { Tag: string tag }) return;
        e.Handled = true;

        if (tag == "Base")
        {
            var dialog = new ColorPickerDialog(_vm.ColorBase, tag);
            var result = await dialog.ShowDialog<Color?>(this);
            if (result is { } chosen) _vm.ColorBase = chosen;
        }
        else if (tag == "AsdwHighlight")
        {
            var dialog = new ColorPickerDialog(_vm.AsdwHighlightColor, "ASDW Highlight");
            var result = await dialog.ShowDialog<Color?>(this);
            if (result is { } chosen) _vm.AsdwHighlightColor = chosen;
        }
    }

    private async void OnColorSlotTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { DataContext: ColorSlot slot }) return;
        e.Handled = true;

        var dialog = new ColorPickerDialog(slot.Color, slot.Label);
        var result = await dialog.ShowDialog<Color?>(this);
        if (result is { } chosen) slot.Color = chosen;
    }

    private void OnAddColorClicked(object? sender, RoutedEventArgs e)
    {
        _vm.AddColorCommand.Execute(null);
    }

    private void OnRemoveColorClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ColorSlot slot })
            _vm.RemoveColorCommand.Execute(slot);
    }
}
