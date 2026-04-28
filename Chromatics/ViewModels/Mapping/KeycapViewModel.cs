using CommunityToolkit.Mvvm.ComponentModel;
using RGB.NET.Core;
using DrawingColor = System.Drawing.Color;
using IBrush = Avalonia.Media.IBrush;
using SolidColorBrush = Avalonia.Media.SolidColorBrush;
using AvColor = Avalonia.Media.Color;

namespace Chromatics.ViewModels.Mapping
{
    // One key on a virtual device. X/Y are absolute canvas positions
    // precomputed by VirtualDeviceViewModel so the view just drops these
    // into a Canvas without re-deriving row layout.
    public sealed partial class KeycapViewModel : ObservableObject
    {
        public string Label { get; }
        public LedId LedType { get; }
        public double Width { get; }
        public double Height { get; }

        // X/Y are observable so user drag updates propagate straight to the
        // Canvas.Left/Top binding on the item's ContentPresenter.
        [ObservableProperty] private double _x;
        [ObservableProperty] private double _y;

        // Grid-computed defaults, set once at construction. Used by the layout
        // reset so positions can be restored without rebuilding the whole VM.
        public double DefaultX { get; }
        public double DefaultY { get; }

        [ObservableProperty] private DrawingColor _fillColor;
        [ObservableProperty] private IBrush _fillBrush;
        [ObservableProperty] private bool _isSelected;
        [ObservableProperty] private bool _isEditing;
        [ObservableProperty] private string _editIndex;

        public KeycapViewModel(string label, LedId ledType, double x, double y, double width, double height)
            : this(label, ledType, x, y, x, y, width, height)
        {
        }

        // Grid-computed defaults must be passed in separately when a persisted
        // override has moved the keycap off its default slot — otherwise
        // DefaultX/Y end up equal to the override position and Reset becomes a
        // no-op on next app launch.
        public KeycapViewModel(string label, LedId ledType, double x, double y,
                               double defaultX, double defaultY,
                               double width, double height)
        {
            Label = label;
            LedType = ledType;
            _x = x;
            _y = y;
            DefaultX = defaultX;
            DefaultY = defaultY;
            Width = width;
            Height = height;
            _fillColor = DrawingColor.DarkGray;
            _fillBrush = new SolidColorBrush(AvColor.FromArgb(
                _fillColor.A, _fillColor.R, _fillColor.G, _fillColor.B));
        }

        partial void OnFillColorChanged(DrawingColor value)
        {
            FillBrush = new SolidColorBrush(AvColor.FromArgb(value.A, value.R, value.G, value.B));
        }
    }
}
