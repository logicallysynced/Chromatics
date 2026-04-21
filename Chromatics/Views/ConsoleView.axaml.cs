using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Chromatics.ViewModels;
using System;

namespace Chromatics.Views
{
    public partial class ConsoleView : UserControl
    {
        private ConsoleViewModel _vm;

        public ConsoleView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            DetachedFromVisualTree += OnDetached;
            CopyAllButton.Click += OnCopyAllClicked;
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (_vm != null)
            {
                _vm.EntryAdded -= OnEntryAdded;
            }

            _vm = DataContext as ConsoleViewModel;

            if (_vm != null)
            {
                _vm.EntryAdded += OnEntryAdded;
            }
        }

        private void OnDetached(object sender, EventArgs e)
        {
            if (_vm != null)
            {
                _vm.EntryAdded -= OnEntryAdded;
                _vm = null;
            }
        }

        private void OnEntryAdded(object sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ScrollHost.ScrollToEnd();
            }, DispatcherPriority.Background);
        }

        private async void OnCopyAllClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_vm == null) return;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;
            await clipboard.SetTextAsync(_vm.GetAllText());
        }
    }
}
