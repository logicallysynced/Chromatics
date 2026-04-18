using Chromatics.Core;
using Chromatics.Enums;
using Chromatics.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using static Chromatics.Enums.Palette;
using DrawingColor = System.Drawing.Color;
using MediaColor = Avalonia.Media.Color;

namespace Chromatics.ViewModels
{
    public sealed class PaletteViewModel : ViewModelBase
    {
        private readonly ObservableCollection<PaletteMappingItem> _allItems = new();
        private bool _syncingColor;

        public ObservableCollection<PaletteMappingItem> Items { get; } = new();
        public ObservableCollection<CategoryOption> Categories { get; } = new();

        public RelayCommand ImportCommand { get; }
        public RelayCommand ExportCommand { get; }
        public RelayCommand UndoCommand { get; }

        private CategoryOption _selectedCategory;
        public CategoryOption SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyFilter();
                }
            }
        }

        private PaletteMappingItem _selectedItem;
        public PaletteMappingItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    UndoCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(EditorEnabled));

                    if (value != null)
                    {
                        _syncingColor = true;
                        try { EditorColor = value.Color; }
                        finally { _syncingColor = false; }
                    }
                }
            }
        }

        private MediaColor _editorColor = MediaColor.FromArgb(255, 0, 0, 0);
        public MediaColor EditorColor
        {
            get => _editorColor;
            set
            {
                if (!SetProperty(ref _editorColor, value)) return;

                if (_syncingColor || SelectedItem == null) return;
                if (SelectedItem.Color == value) return;

                SelectedItem.Color = value;
                RGBController.SaveColorPalette();
            }
        }

        public bool EditorEnabled => SelectedItem != null;

        public PaletteViewModel()
        {
            if (!RGBController.LoadColorPalette())
            {
                Logger.WriteConsole(LoggerTypes.System, "No palette file found. Creating default color palette..");
                RGBController.SaveColorPalette();
            }
            else
            {
                Logger.WriteConsole(LoggerTypes.System, "Loaded palette from palette.chromatics3");
            }

            ImportCommand = new RelayCommand(OnImport);
            ExportCommand = new RelayCommand(OnExport);
            UndoCommand = new RelayCommand(OnUndo, () => SelectedItem != null);

            BuildCategories();
            BuildItems();
            _selectedCategory = Categories.First(c => c.Value == PaletteTypes.All);
            ApplyFilter();
        }

        private void BuildCategories()
        {
            Categories.Clear();
            for (int i = 0; i <= Palette.TypeCount; i++)
            {
                var t = (PaletteTypes)i;
                var displayAttr = typeof(PaletteTypes)
                    .GetMember(t.ToString())[0]
                    .GetCustomAttribute<DisplayAttribute>();
                Categories.Add(new CategoryOption(t, displayAttr?.Name ?? t.ToString()));
            }
        }

        private void BuildItems()
        {
            var active = RGBController.GetActivePalette();
            _allItems.Clear();
            foreach (var field in typeof(PaletteColorModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType != typeof(ColorMapping)) continue;
                _allItems.Add(new PaletteMappingItem(active, field));
            }
        }

        private void ApplyFilter()
        {
            Items.Clear();
            if (_selectedCategory == null || _selectedCategory.Value == PaletteTypes.All)
            {
                foreach (var it in _allItems) Items.Add(it);
            }
            else
            {
                foreach (var it in _allItems.Where(i => i.Category == _selectedCategory.Value))
                    Items.Add(it);
            }
        }

        private void OnImport()
        {
            if (!RGBController.ImportColorPalette()) return;
            // Rebuild items against the new palette and reapply filter.
            BuildItems();
            ApplyFilter();
            SelectedItem = null;
        }

        private void OnExport()
        {
            RGBController.ExportColorPalette();
        }

        private void OnUndo()
        {
            if (SelectedItem == null) return;

            // Default colour for this mapping lives on a fresh PaletteColorModel.
            var defaults = new PaletteColorModel();
            var field = typeof(PaletteColorModel).GetField(SelectedItem.Id, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) return;

            var mapping = (ColorMapping)field.GetValue(defaults);
            var defaultColor = mapping.Color;
            var avColor = MediaColor.FromArgb(255, defaultColor.R, defaultColor.G, defaultColor.B);

            // Write-through via EditorColor so the editor, swatch, and disk all move
            // together.
            EditorColor = avColor;
        }

        public sealed class CategoryOption
        {
            public PaletteTypes Value { get; }
            public string DisplayName { get; }
            public CategoryOption(PaletteTypes value, string displayName) { Value = value; DisplayName = displayName; }
            public override string ToString() => DisplayName;
        }
    }
}
