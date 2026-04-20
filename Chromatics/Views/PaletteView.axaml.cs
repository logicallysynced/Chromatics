using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Chromatics.Helpers;
using Chromatics.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

            // Validate JSON-format palette files before handing off to the importer.
            // The legacy .chromatics XML format is self-describing and validated by
            // the XML deserialiser inside ImportColorMappingsFromPath, so skip it here.
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".chromatics")
            {
                var (valid, reason) = FileOperationsHelper.ValidatePaletteFile(path);
                if (!valid)
                {
                    await ShowImportErrorAsync(reason);
                    return;
                }
            }

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

        private async Task ShowImportErrorAsync(string reason)
        {
            var owner = this.FindAncestorOfType<Window>();
            if (owner == null) return;

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0),
            };

            var dialog = new Window
            {
                Title = "Unable to Import Palette",
                Width = 440,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(24, 20, 24, 20),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = reason,
                            TextWrapping = TextWrapping.Wrap,
                        },
                        okButton,
                    }
                }
            };

            okButton.Click += (_, _) => dialog.Close();
            await dialog.ShowDialog(owner);
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
