using Avalonia.Controls;
using Avalonia.Threading;

namespace GARbro.Avalonia.Views
{
    public partial class AutoScanDialog : Window
    {
        public string CurrentScheme
        {
            set
            {
                Dispatcher.UIThread.InvokeAsync(() => {
                    CurrentSchemeTextBlock.Text = value;
                });
            }
        }

        public AutoScanDialog()
        {
            InitializeComponent();
        }
    }
}
