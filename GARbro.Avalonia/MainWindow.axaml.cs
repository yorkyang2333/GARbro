using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GARbro.Avalonia.ViewModels;
using GARbro.Avalonia.Views;
using GameRes;

namespace GARbro.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void DataGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is EntryViewModel item)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (item.IsDirectory)
                {
                    vm.ItemDoubleClickedCommand.Execute(item);
                }
                else if (item.IsVirtual && item.Entry != null && vm.CurrentArchive != null)
                {
                    try
                    {
                        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GARbro_Temp");
                        System.IO.Directory.CreateDirectory(tempDir);
                        string destFile = System.IO.Path.Combine(tempDir, System.IO.Path.GetFileName(item.Entry.Name));
                        
                        using (var stream = vm.CurrentArchive.OpenEntry(item.Entry))
                        using (var fs = System.IO.File.Create(destFile))
                        {
                            stream.CopyTo(fs);
                        }

                        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"/select,\"{destFile}\"") { UseShellExecute = true });
                        }
                        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("open", $"-R \"{destFile}\"") { UseShellExecute = true });
                        }
                        else
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("xdg-open", $"\"{tempDir}\"") { UseShellExecute = true });
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new Views.ErrorDialog($"Error opening entry:\n{ex.Message}\n{ex.StackTrace}");
                        _ = errorDialog.ShowDialog(this);
                    }
                }
                else if (!item.IsVirtual)
                {
                    vm.ItemDoubleClickedCommand.Execute(item);
                }
            }
        }
    }

    private async void MenuItem_About_Click(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        await aboutWindow.ShowDialog(this);
    }

    private async void MenuItem_Open_Click(object? sender, RoutedEventArgs e)
    {
        var storageProvider = this.StorageProvider;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Archive",
            AllowMultiple = false
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var filePath = files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
            await vm.OpenArchive(filePath);
        }
    }

    private void MenuItem_Exit_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var selectedCount = FileGrid.SelectedItems.Cast<EntryViewModel>().Count(i => i.Entry != null);
            var menuExtract = ((ContextMenu)sender!).Items.Cast<MenuItem>().FirstOrDefault(m => m.Name == "MenuExtract");
            if (menuExtract != null)
            {
                menuExtract.IsEnabled = vm.CurrentArchive != null && selectedCount > 0;
            }
        }
    }

    private void MenuItem_Refresh_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateToCommand.Execute(vm.CurrentDirectory);
        }
    }

    private async void MenuItem_Extract_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.CurrentArchive != null)
        {
            var selectedEntries = new List<GameRes.Entry>();
            foreach (var item in FileGrid.SelectedItems.Cast<EntryViewModel>())
            {
                if (item.Entry != null)
                {
                    selectedEntries.Add(item.Entry);
                }
                else if (item.IsDirectory && item.IsVirtual && item.Name != "..")
                {
                    var prefix = item.FullPath + "/";
                    selectedEntries.AddRange(vm.CurrentArchive.Dir.Where(e => e.Name.Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
                }
            }

            if (selectedEntries.Count == 0) return;

            global::Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    var storageProvider = this.StorageProvider;
                    var folders = await storageProvider.OpenFolderPickerAsync(new global::Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Select Destination Folder",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        await Task.Delay(100); // Workaround for Avalonia macOS native dialog teardown deadlock
                        var destination = folders[0].Path.LocalPath;
                        var progressWindow = new ProgressWindow(vm.CurrentArchive, selectedEntries, destination);
                        await progressWindow.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = new Views.ErrorDialog($"Unexpected error during extraction setup:\n{ex.Message}\n{ex.StackTrace}");
                    await errorDialog.ShowDialog(this);
                }
            });
        }
    }
}