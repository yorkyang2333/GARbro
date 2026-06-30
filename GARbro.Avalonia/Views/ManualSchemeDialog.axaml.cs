using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GameRes;
using GameRes.Formats.KiriKiri;

namespace GARbro.Avalonia.Views
{
    public partial class ManualSchemeDialog : Window
    {
        public string NoticeText
        {
            get => NoticeTextBlock.Text;
            set => NoticeTextBlock.Text = value;
        }

        public ManualSchemeDialog()
        {
            InitializeComponent();

            var schemes = Xp3Opener.KnownSchemes.Keys.OrderBy(k => k).ToList();
            SchemeComboBox.ItemsSource = schemes;
            if (schemes.Count > 0)
                SchemeComboBox.SelectedIndex = 0;
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            var options = new Xp3Options { Scheme = new AutoDetectCrypt() };
            Close(options);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedSchemeName = SchemeComboBox.SelectedItem as string;
            var scheme = selectedSchemeName != null ? Xp3Opener.GetScheme(selectedSchemeName) : null;
            var options = new Xp3Options { Scheme = scheme };
            Close(options);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
