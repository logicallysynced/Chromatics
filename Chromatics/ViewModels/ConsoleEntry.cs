using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DrawingColor = System.Drawing.Color;

namespace Chromatics.ViewModels
{
    public sealed partial class ConsoleEntry : ObservableObject
    {
        public string Message { get; }

        // Kept so we can re-resolve the brush when the user flips themes.
        public DrawingColor RawColor { get; }

        [ObservableProperty]
        private IBrush _foreground;

        public ConsoleEntry(string message, DrawingColor rawColor, IBrush foreground)
        {
            Message    = message;
            RawColor   = rawColor;
            _foreground = foreground;
        }
    }
}
