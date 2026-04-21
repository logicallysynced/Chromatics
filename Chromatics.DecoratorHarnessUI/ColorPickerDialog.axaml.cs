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
        Picker.Color = initial;

        OkButton.Click += (_, _) => Close(Picker.Color);
        CancelButton.Click += (_, _) => Close(null);
    }
}
