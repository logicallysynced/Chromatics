using CommunityToolkit.Mvvm.ComponentModel;

namespace Chromatics.DecoratorHarnessUI;

public partial class ColorSlot : ObservableObject
{
    [ObservableProperty] private AvColor _color;
    [ObservableProperty] private int _index;

    public Avalonia.Media.SolidColorBrush Brush => new(Color);
    public string Label => $"Color {Index}";

    partial void OnColorChanged(AvColor value) => OnPropertyChanged(nameof(Brush));
    partial void OnIndexChanged(int value) => OnPropertyChanged(nameof(Label));
}
