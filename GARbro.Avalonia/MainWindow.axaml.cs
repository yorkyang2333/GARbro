using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GARbro.Avalonia.ViewModels;
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
                    vm.NavigateToCommand.Execute(item.FullPath);
                }
                else if (item.Entry != null && vm.CurrentArchive != null)
                {
                    try
                    {
                        if (item.Entry.Type == "image")
                        {
                            var stream = vm.CurrentArchive.OpenEntry(item.Entry);
                            var binStream = new GameRes.BinaryStream(stream, item.Entry.Name);
                            var image = ImageFormat.Read(binStream);
                            if (image != null)
                            {
                                var viewer = new ImageViewerWindow();
                                viewer.LoadImage(item.Entry, image);
                                await viewer.ShowDialog(this);
                            }
                        }
                        else if (item.Entry.Type == "audio")
                        {
                            var stream = vm.CurrentArchive.OpenEntry(item.Entry);
                            var binStream = new GameRes.BinaryStream(stream, item.Entry.Name);
                            var audio = AudioFormat.Read(binStream);
                            if (audio != null)
                            {
                                var player = new AudioPlayerWindow();
                                player.LoadAudio(item.Entry, audio);
                                await player.ShowDialog(this);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening entry: {ex.Message}");
                    }
                }
                else
                {
                    _ = vm.OpenArchive(item.FullPath);
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
            var selectedItems = FileGrid.SelectedItems.Cast<EntryViewModel>().Where(i => i.Entry != null).ToList();
            if (selectedItems.Count == 0) return;

            var storageProvider = this.StorageProvider;
            var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Destination Folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var destination = folders[0].Path.LocalPath;
                var progressWindow = new ProgressWindow();
                progressWindow.StartExtraction(vm.CurrentArchive, selectedItems.Select(i => i.Entry!).ToList(), destination);
                await progressWindow.ShowDialog(this);
            }
        }
    }
}