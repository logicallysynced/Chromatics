#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Core;

namespace Chromatics.Views.Dialogs
{
    public partial class EVisionFlashHintDialog : Window
    {
        public EVisionFlashHintDialog()
        {
            InitializeComponent();
        }

        private void OnGotIt(object? sender, RoutedEventArgs e)
        {
            // Persist the shown-once flag so the dialog never opens
            // again on this install. Tied to the app instance, not the
            // user account - a fresh Chromatics install will pop the
            // dialog once on the next first-enable.
            try
            {
                var s = AppSettings.GetSettings();
                s.eVisionFlashHintShown = true;
                AppSettings.SaveSettings(s);
            }
            catch { /* swallow - UX nicety, not load-bearing */ }

            Close();
        }
    }
}
