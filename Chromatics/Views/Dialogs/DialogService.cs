using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Chromatics.Views.Dialogs
{
    public static class DialogService
    {
        public static Task<bool> ConfirmAsync(string title, string message, string ok = "OK", string cancel = "Cancel")
        {
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dlg = new ConfirmDialog(title, message, ok, cancel);
                await ShowWithOptionalOwnerAsync(dlg, GetMainWindow());
                return dlg.Result;
            });
        }

        public static Task ShowAsync(string title, string message)
        {
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dlg = new InfoDialog(title, message);
                await ShowWithOptionalOwnerAsync(dlg, GetMainWindow());
            });
        }

        // Shows a dialog modal to `owner` when the owner is alive and
        // visible, falling back to a stand-alone Show() + TaskCompletionSource
        // otherwise. ShowDialog throws InvalidOperationException("Cannot show
        // window with non-visible owner.") if the owner is hidden — which
        // happens whenever the user minimised to tray between the call site
        // initiating an async operation and the dialog actually opening
        // (CHROMATICS-16, was Sentry'd from OnCheckUpdatesClick). Treat
        // hidden the same as missing so every DialogService caller and any
        // future external caller gets the same safe semantics.
        public static Task ShowWithOptionalOwnerAsync(Window dialog, Window owner)
        {
            if (owner != null && owner.IsVisible)
                return dialog.ShowDialog(owner);

            var tcs = new TaskCompletionSource<bool>();
            dialog.Closed += (_, _) => tcs.TrySetResult(true);
            dialog.Show();
            return tcs.Task;
        }

        private static Window GetMainWindow()
        {
            return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }
    }
}
