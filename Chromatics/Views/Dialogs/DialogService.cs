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
                var owner = GetMainWindow();
                if (owner != null)
                {
                    await dlg.ShowDialog(owner);
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    dlg.Closed += (_, _) => tcs.TrySetResult(true);
                    dlg.Show();
                    await tcs.Task;
                }
                return dlg.Result;
            });
        }

        public static Task ShowAsync(string title, string message)
        {
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dlg = new InfoDialog(title, message);
                var owner = GetMainWindow();
                if (owner != null)
                {
                    await dlg.ShowDialog(owner);
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    dlg.Closed += (_, _) => tcs.TrySetResult(true);
                    dlg.Show();
                    await tcs.Task;
                }
            });
        }

        private static Window GetMainWindow()
        {
            return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        }
    }
}
