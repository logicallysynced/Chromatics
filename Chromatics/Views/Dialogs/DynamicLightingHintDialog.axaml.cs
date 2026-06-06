#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Core;
using System.Diagnostics;

namespace Chromatics.Views.Dialogs
{
    public partial class DynamicLightingHintDialog : Window
    {
        public DynamicLightingHintDialog()
        {
            InitializeComponent();
        }

        // Open the Windows Settings page for Dynamic Lighting using the
        // OS deep-link URI. This jumps the user straight to the page they
        // need; no manual nav through Settings → Personalization.
        private void OnOpenSettings(object? sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:personalization-lighting",
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Best-effort; if the deep-link fails (older Windows that
                // doesn't recognise the URI, or the user is on a build
                // before Dynamic Lighting was added) we just close the
                // dialog. The hint text describes the manual nav path.
            }
        }

        private void OnGotIt(object? sender, RoutedEventArgs e)
        {
            // Persist the "shown once" flag so we don't pop this dialog
            // every time the user enables Dynamic Lighting. Tied to the
            // app instance, not the user account — re-installing
            // Chromatics will show it again on the next first-enable.
            try
            {
                var s = AppSettings.GetSettings();
                s.dynamicLightingHintShown = true;
                AppSettings.SaveSettings(s);
            }
            catch { /* swallow — UX nicety, not load-bearing */ }

            Close();
        }
    }
}
