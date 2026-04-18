using Avalonia.Controls;
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
            // Scroll on the UI thread — EntryAdded is raised from inside Append(),
            // which already runs on the UI thread via Dispatcher.Post.
            Dispatcher.UIThread.Post(() =>
            {
                ScrollHost.ScrollToEnd();
            }, DispatcherPriority.Background);
        }
    }
}
