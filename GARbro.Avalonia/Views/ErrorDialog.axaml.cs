using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GARbro.Avalonia.Views
{
    public partial class ErrorDialog : Window
    {
        public ErrorDialog()
        {
            InitializeComponent();
        }

        public ErrorDialog(string message) : this()
        {
            this.FindControl<TextBlock>("MessageText").Text = message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
