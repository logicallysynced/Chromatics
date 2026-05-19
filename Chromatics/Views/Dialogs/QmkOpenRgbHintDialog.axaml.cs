#nullable enable
using Avalonia.Controls;
using Avalonia.Interactivity;
using Chromatics.Core;

namespace Chromatics.Views.Dialogs
{
    public partial class QmkOpenRgbHintDialog : Window
    {
        public QmkOpenRgbHintDialog()
        {
            InitializeComponent();
        }

        private void OnGotIt(object? sender, RoutedEventArgs e)
        {
            // Persist the "shown once" flag so we don't pop this dialog
            // every time the user enables the QMK provider. Tied to the
            // app instance, not the user account - re-installing
            // Chromatics will show it again on the next first-enable.
            try
            {
                var s = AppSettings.GetSettings();
                s.qmkOpenRgbHintShown = true;
                AppSettings.SaveSettings(s);
            }
            catch { /* swallow - UX nicety, not load-bearing */ }

            Close();
        }
    }
}
