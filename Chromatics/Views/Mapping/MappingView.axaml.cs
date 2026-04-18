using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

        private static readonly FilePickerFileType LayerFileType = new("Chromatics Layer Files")
        {
            Patterns = new[] { "*.chromatics3" }
        };
    }
}
