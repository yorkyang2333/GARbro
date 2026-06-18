using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GARbro.Avalonia.ViewModels;

namespace GARbro.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is EntryViewModel item)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.NavigateToCommand.Execute(item.FullPath);
            }
        }
    }

    private async void MenuItem_About_Click(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }
}