using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameRes;

namespace GARbro.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _currentDirectory = string.Empty;

    public ObservableCollection<EntryViewModel> RightPaneItems { get; } = new();

    public MainWindowViewModel()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (Directory.Exists(desktop))
        {
            NavigateTo(desktop);
        }
        else 
        {
            NavigateTo(Directory.GetCurrentDirectory());
        }
    }

    [RelayCommand]
    public void NavigateTo(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
        
        CurrentDirectory = path;
        RightPaneItems.Clear();

        try
        {
            var parent = Directory.GetParent(path);
            if (parent != null)
            {
                RightPaneItems.Add(new EntryViewModel 
                { 
                    Name = "..", 
                    FullPath = parent.FullName,
                    IsDirectory = true,
                    IconKind = "FolderArrowUp"
                });
            }

            foreach (var dir in Directory.GetDirectories(path).OrderBy(d => d))
            {
                RightPaneItems.Add(new EntryViewModel 
                { 
                    Name = Path.GetFileName(dir), 
                    FullPath = dir,
                    IsDirectory = true,
                    IconKind = "Folder"
                });
            }

            foreach (var file in Directory.GetFiles(path).OrderBy(f => f))
            {
                string ext = Path.GetExtension(file).ToLower();
                string icon = "FileDocumentOutline";
                
                if (ext == ".xp3" || ext == ".int" || ext == ".arc" || ext == ".pck" || ext == ".dat")
                    icon = "ArchiveOutline";
                else if (ext == ".png" || ext == ".jpg" || ext == ".bmp" || ext == ".tga")
                    icon = "ImageOutline";
                else if (ext == ".ogg" || ext == ".wav" || ext == ".mp3")
                    icon = "MusicNote";

                RightPaneItems.Add(new EntryViewModel 
                { 
                    Name = Path.GetFileName(file), 
                    FullPath = file,
                    IsDirectory = false,
                    IconKind = icon,
                    Size = new FileInfo(file).Length,
                    Type = ext.TrimStart('.')
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading directory: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ItemDoubleClicked(EntryViewModel? item)
    {
        if (item == null) return;

        if (item.IsDirectory)
        {
            NavigateTo(item.FullPath);
        }
        else
        {
            OpenArchive(item.FullPath);
        }
    }

    private void OpenArchive(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        try
        {
            var arc = ArcFile.TryOpen(filePath);
            if (arc != null && arc.Dir != null)
            {
                CurrentDirectory = filePath;
                RightPaneItems.Clear();
                
                RightPaneItems.Add(new EntryViewModel 
                { 
                    Name = "..", 
                    FullPath = Path.GetDirectoryName(filePath) ?? string.Empty,
                    IsDirectory = true,
                    IconKind = "FolderArrowUp"
                });

                foreach (var entry in arc.Dir)
                {
                    string ext = Path.GetExtension(entry.Name).ToLower();
                    string icon = "FileDocumentOutline";
                    
                    if (entry.Type == "image" || ext == ".png" || ext == ".jpg" || ext == ".bmp")
                        icon = "ImageOutline";
                    else if (entry.Type == "audio" || ext == ".ogg" || ext == ".wav")
                        icon = "MusicNote";
                    else if (entry.Type == "script")
                        icon = "ScriptTextOutline";

                    RightPaneItems.Add(new EntryViewModel
                    {
                        Name = entry.Name,
                        Type = entry.Type,
                        Size = entry.Size,
                        Offset = entry.Offset,
                        IconKind = icon,
                        IsDirectory = false,
                        FullPath = "" // Not a physical file
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening archive: {ex.Message}");
        }
    }
}



public class EntryViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
    public long Offset { get; set; }
    public string IconKind { get; set; } = "FileDocumentOutline";
    public bool IsDirectory { get; set; }
    public string FullPath { get; set; } = string.Empty;
}
