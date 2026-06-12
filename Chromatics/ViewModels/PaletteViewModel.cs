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
                    // Notify the proxy properties below so the AXAML
                    // bindings re-evaluate without traversing through a
                    // null SelectedItem (which Avalonia 12 logs as a
                    // "Value is null" binding warning every selection
                    // toggle, despite FallbackValue catching the render).
                    OnPropertyChanged(nameof(SelectedItemDisplayName));
                    OnPropertyChanged(nameof(SelectedItemBrush));

                    if (value != null)
                    {
                        _syncingColor = true;
                        try { EditorColor = value.Color; }
                        finally { _syncingColor = false; }
                    }
                }
            }
        }

        // Null-safe proxy properties for AXAML. Bind to these instead of
        // SelectedItem.X — Avalonia logs binding warnings when traversing
        // through a null source, even when FallbackValue is set.
        public string SelectedItemDisplayName => _selectedItem?.DisplayName ?? string.Empty;
        public Avalonia.Media.IBrush SelectedItemBrush =>
            _selectedItem?.Brush ?? Avalonia.Media.Brushes.Transparent;

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
                Logger.WriteConsole(LoggerTypes.System, "Loaded palette from palette.chromatics4");
            }

            UndoCommand = new RelayCommand(OnUndo, () => SelectedItem != null);

            BuildCategories();
            BuildItems();
            _selectedCategory = Categories.First(c => c.Value == PaletteTypes.All);
            ApplyFilter();
        }

        // Categories with no corresponding ColorMapping entries, or whose
        // functionality has not yet been reimplemented on 4.x. Hidden from
        // the dropdown AND filtered out of the All view so the user doesn't
        // see dead entries. StatusEffects left this list when the Status
        // Inflicted effect started reading its colours.
        private static readonly PaletteTypes[] _hiddenCategories =
        {
            PaletteTypes.Abilities,
        };

        // Individual entries hidden regardless of category. Pull Countdown
        // is grouped under Notifications but the feature hasn't been
        // reimplemented yet — hide until it lands.
        private static readonly string[] _hiddenFieldNames =
        {
            "PullCountdownTick",
            "PullCountdownEmpty",
            "PullCountdownEngage",
        };

        private void BuildCategories()
        {
            Categories.Clear();
            for (int i = 0; i <= Palette.TypeCount; i++)
            {
                var t = (PaletteTypes)i;
                if (_hiddenCategories.Contains(t)) continue;
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
                if (_hiddenFieldNames.Contains(field.Name)) continue;
                _allItems.Add(new PaletteMappingItem(active, field));
            }
        }

        private void ApplyFilter()
        {
            Items.Clear();
            if (_selectedCategory == null || _selectedCategory.Value == PaletteTypes.All)
            {
                foreach (var it in _allItems.Where(i => !_hiddenCategories.Contains(i.Category)))
                    Items.Add(it);
            }
            else
            {
                foreach (var it in _allItems.Where(i => i.Category == _selectedCategory.Value))
                    Items.Add(it);
            }
        }

        public void ImportFromPath(string path)
        {
            if (!RGBController.ImportColorPalette(path)) return;
            BuildItems();
            ApplyFilter();
            SelectedItem = null;
        }

        public void ExportToPath(string path)
        {
            RGBController.ExportColorPalette(path);
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
