#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Core;
using Chromatics.Helpers;
using Chromatics.Localization;
using Chromatics.ViewModels;
using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chromatics.Views.Dialogs
{
    public partial class CopyLayersDialog : Window
    {
        private readonly CopyLayersDialogViewModel _vm;

        public bool Applied { get; private set; }

        public CopyLayersDialog() : this(new Dictionary<Guid, IRGBDevice>(), Guid.Empty) { }

        public CopyLayersDialog(IReadOnlyDictionary<Guid, IRGBDevice> connectedDevices, Guid initialSourceGuid)
        {
            InitializeComponent();
            _vm = new CopyLayersDialogViewModel(connectedDevices, initialSourceGuid);
            DataContext = _vm;
        }

        private void OnCancel(object? sender, RoutedEventArgs e)
        {
            Applied = false;
            Close();
        }

        private async void OnCopy(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedSource == null || _vm.SelectedDestination == null) return;
            try
            {
                var plans = _vm.BuildPlans();
                if (plans.Count == 0)
                {
                    await DialogService.ShowAsync(
                        LocalizationService.Instance["Nothing to copy"],
                        LocalizationService.Instance["Select at least one layer to copy."]);
                    return;
                }

                var result = LayerCopier.Apply(
                    _vm.SelectedSource.DeviceId,
                    _vm.SelectedDestination.DeviceId,
                    _vm.SelectedDestination.DeviceType,
                    plans);

                string template = LocalizationService.Instance["Copied {0} layer(s) to {1}. {2} replaced an existing layer. {3} source LED(s) had no destination mapping and were skipped."];
                string body = string.Format(template,
                    result.LayersAdded + result.LayersReplaced,
                    _vm.SelectedDestination.Name,
                    result.LayersReplaced,
                    result.LedMappingsDropped);
                await DialogService.ShowAsync(LocalizationService.Instance["Copy complete"], body);
                Applied = true;
            }
            catch (Exception ex)
            {
                string template = LocalizationService.Instance["Copy failed: {0}"];
                await DialogService.ShowAsync(LocalizationService.Instance["Copy failed"], string.Format(template, ex.Message));
                Applied = false;
            }
            finally
            {
                Close();
            }
        }
    }
}
