using Avalonia.Controls;
using Avalonia.Media;

namespace Chromatics.DecoratorHarnessUI;

public partial class ColorPickerDialog : Window
{
    public ColorPickerDialog()
    {
        InitializeComponent();
    }

    public ColorPickerDialog(Color initial, string label) : this()
    {
        Title = $"Pick Color — {label}";

        // Avalonia 12's ColorView only derives its HSV state from Color
        // once the control's template is applied. Setting Color in the
        // ctor (before Loaded) leaves the spectrum slider thumb desynced
        // from the displayed colour, and the drift compounds on repeat
        // opens. Defer to Loaded so the template is in place first.
        Picker.Loaded += (_, _) => Picker.Color = initial;

        OkButton.Click += (_, _) => Close(Picker.Color);
        CancelButton.Click += (_, _) => Close(null);
    }
}
