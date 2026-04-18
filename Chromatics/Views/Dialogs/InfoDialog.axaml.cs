using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chromatics.Views.Dialogs
{
    public partial class InfoDialog : Window
    {
        public InfoDialog()
        {
            InitializeComponent();
        }

        public InfoDialog(string title, string message) : this()
        {
            Title = title;
            TitleText.Text = title;
            MessageText.Text = message;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
