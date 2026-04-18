using Avalonia.Media;
using Chromatics.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using static Chromatics.Enums.Palette;
using DrawingColor = System.Drawing.Color;
using MediaColor = Avalonia.Media.Color;

namespace Chromatics.ViewModels
{
    public sealed class PaletteMappingItem : ViewModelBase
    {
        private readonly PaletteColorModel _palette;
        private readonly FieldInfo _field;
        private MediaColor _color;
        private IBrush _brush;

        public string Id => _field.Name;
        public string DisplayName { get; }
        public PaletteTypes Category { get; }
        public string CategoryName { get; }

        public MediaColor Color
        {
            get => _color;
            set
            {
                if (SetProperty(ref _color, value))
                {
                    Brush = new SolidColorBrush(value);
                    var mapping = (ColorMapping)_field.GetValue(_palette);
                    mapping.Color = DrawingColor.FromArgb(value.A, value.R, value.G, value.B);
                    _field.SetValue(_palette, mapping);
                }
            }
        }

        public IBrush Brush
        {
            get => _brush;
            private set => SetProperty(ref _brush, value);
        }

        public PaletteMappingItem(PaletteColorModel palette, FieldInfo field)
        {
            _palette = palette;
            _field = field;

            var mapping = (ColorMapping)field.GetValue(palette);
            var name = mapping.Name;
            var displayAttr = typeof(PaletteTypes)
                .GetMember(mapping.Type.ToString())[0]
                .GetCustomAttribute<DisplayAttribute>();

            DisplayName = name;
            Category = mapping.Type;
            CategoryName = displayAttr?.Name ?? mapping.Type.ToString();

            // Force alpha to 255 for display — the original WinForms cell style did
            // the same. Stored alpha still round-trips through ColorMapping.Color.
            var c = mapping.Color;
            _color = MediaColor.FromArgb(255, c.R, c.G, c.B);
            _brush = new SolidColorBrush(_color);
        }
    }
}
