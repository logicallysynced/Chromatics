using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Chromatics.ViewModels;
using System.Collections.Generic;

namespace Chromatics.Views
{
    public partial class PaletteView : UserControl
    {
        public PaletteView()
        {
            InitializeComponent();
        }

        private async void OnImportClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Chromatics Color Palette",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType> { ImportablePaletteFileType, LegacyPaletteFileType },
            });

            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            if (DataContext is PaletteViewModel vm)
                vm.ImportFromPath(path);
        }

        private async void OnExportClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Chromatics Color Palette",
                SuggestedFileName = "mypalette",
                DefaultExtension = "chromatics4",
                FileTypeChoices = new List<FilePickerFileType> { PaletteFileType },
            });

            if (file == null) return;
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            if (DataContext is PaletteViewModel vm)
                vm.ExportToPath(path);
        }

        // Export uses only the current Chromatics-4 extension.
        private static readonly FilePickerFileType PaletteFileType = new("Chromatics Palette Files")
        {
            Patterns = new[] { "*.chromatics4" }
        };

        // Import accepts all known Chromatics JSON palette extensions.
        // ImportColorMappingsFromPath dispatches on extension internally.
        private static readonly FilePickerFileType ImportablePaletteFileType = new("Chromatics Palette Files")
        {
            Patterns = new[] { "*.chromatics4", "*.chromatics3", "*.chromatics2" }
        };

        // Legacy XML-format palette files from the pre-3.x era.
        private static readonly FilePickerFileType LegacyPaletteFileType = new("Legacy Palette Files")
        {
            Patterns = new[] { "*.chromatics" }
        };
    }
}
