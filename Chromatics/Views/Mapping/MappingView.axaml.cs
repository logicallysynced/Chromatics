using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Chromatics.Helpers;
using Chromatics.Layers;
using Chromatics.ViewModels.Mapping;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chromatics.Views.Mapping
{
    public partial class MappingView : UserControl
    {
        public MappingView()
        {
            InitializeComponent();
        }

        private async void OnImportClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Chromatics Layers",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType> { LayerFileType }
            });

            if (files.Count == 0) return;
            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            var owner = this.FindAncestorOfType<Window>() ?? topLevel as Window;
            if (owner == null) return;

            // Validate the file before showing the confirmation dialog so the
            // user doesn't have to dismiss two dialogs for a bad file.
            var (valid, reason) = await Task.Run(() => FileOperationsHelper.ValidateLayerFile(path));
            if (!valid)
            {
                await ShowMessageDialog(owner,
                    "Invalid File",
                    $"The selected file cannot be imported.\n\n{reason}",
                    isError: true);
                return;
            }

            // Confirm before overwriting — import is destructive.
            bool confirmed = await ShowConfirmDialog(owner,
                "Import Layers",
                "Importing will overwrite all current layer mappings for every device.\n\nThis cannot be undone. Continue?");
            if (!confirmed) return;

            await Task.Run(() => MappingLayers.ImportMappingsFromPath(path));

            if (DataContext is MappingViewModel vm)
                vm.ReloadAfterImport();
        }

        private async void OnExportClick(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Chromatics Layers",
                SuggestedFileName = "layers",
                DefaultExtension = "chromatics3",
                FileTypeChoices = new List<FilePickerFileType> { LayerFileType }
            });

            if (file == null) return;
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path)) return;

            await Task.Run(() => MappingLayers.ExportMappingsToPath(path));
        }

        private static async Task<bool> ShowConfirmDialog(Window owner, string title, string message)
        {
            var result = false;

            var dialog = new Window
            {
                Title = title,
                Width = 400,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.Height,
            };

            var yesBtn = new Button { Content = "Continue", MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            var noBtn  = new Button { Content = "Cancel",   MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };

            yesBtn.Click += (_, _) => { result = true;  dialog.Close(); };
            noBtn.Click  += (_, _) => { result = false; dialog.Close(); };

            dialog.Content = new StackPanel
            {
                Margin  = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing             = 8,
                        Children            = { noBtn, yesBtn },
                    }
                }
            };

            await dialog.ShowDialog(owner);
            return result;
        }

        private static async Task ShowMessageDialog(Window owner, string title, string message, bool isError = false)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.Height,
            };

            var okBtn = new Button { Content = "OK", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            okBtn.Click += (_, _) => dialog.Close();

            dialog.Content = new StackPanel
            {
                Margin  = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children            = { okBtn },
                    }
                }
            };

            await dialog.ShowDialog(owner);
        }

        private static readonly FilePickerFileType LayerFileType = new("Chromatics Layer Files")
        {
            Patterns = new[] { "*.chromatics3" }
        };
    }
}
