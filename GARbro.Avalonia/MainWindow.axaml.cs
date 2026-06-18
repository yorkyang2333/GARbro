using Avalonia.Controls;
using Avalonia.Input;
using GARbro.Avalonia.ViewModels;

namespace GARbro.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TreeView treeView && treeView.SelectedItem is DirectoryNodeViewModel node)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.NavigateToCommand.Execute(node.FullPath);
            }
        }
    }

    private void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is EntryViewModel item)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ItemDoubleClickedCommand.Execute(item);
            }
        }
    }
}