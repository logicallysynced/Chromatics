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
                FileTypeFilter = new List<FilePickerFileType> { PaletteFileType, LegacyPaletteFileType },
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
                DefaultExtension = "chromatics3",
                FileTypeChoices = new List<FilePickerFileType> { PaletteFileType },
            });

            if (file == null) return;
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            if (DataContext is PaletteViewModel vm)
                vm.ExportToPath(path);
        }

        private static readonly FilePickerFileType PaletteFileType = new("Chromatics Palette Files")
        {
            Patterns = new[] { "*.chromatics3" }
        };

        private static readonly FilePickerFileType LegacyPaletteFileType = new("Legacy Palette Files")
        {
            Patterns = new[] { "*.chromatics" }
        };
    }
}
