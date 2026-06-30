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
            var msgText = this.FindControl<TextBlock>("MessageText");
            if (msgText != null)
            {
                msgText.Text = message;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
