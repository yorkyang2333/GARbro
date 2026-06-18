using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GARbro.Avalonia;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
