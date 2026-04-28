using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chromatics.Views.Dialogs
{
    public partial class ConfirmDialog : Window
    {
        public bool Result { get; private set; }

        public ConfirmDialog()
        {
            InitializeComponent();
        }

        public ConfirmDialog(string title, string message, string ok = "OK", string cancel = "Cancel") : this()
        {
            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
            OkButton.Content = ok;
            CancelButton.Content = cancel;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }
    }
}
