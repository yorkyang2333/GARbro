using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        GameRes.FormatCatalog.Instance.ParametersRequest += OnParametersRequest;

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
    private async Task ItemDoubleClicked(EntryViewModel? item)
    {
        if (item == null) return;

        if (item.IsDirectory)
        {
            NavigateTo(item.FullPath);
        }
        else
        {
            await OpenArchive(item.FullPath);
        }
    }

    [ObservableProperty]
    private ArcFile? _currentArchive;

    private Views.AutoScanDialog? _autoScanDialog;

    public async Task OpenArchive(string filePath)
    {
        Console.WriteLine($"[OpenArchive] Called with filePath: {filePath}");
        if (string.IsNullOrEmpty(filePath)) return;
        
        try
        {
            var formatsCount = GameRes.FormatCatalog.Instance.ArcFormats.Count();
            Console.WriteLine($"Trying to open {filePath}, available formats: {formatsCount}");
            
            // Run TryOpen on a background thread to allow OnParametersRequest to block for UI
            var arc = await Task.Run(() => {
                GameRes.Formats.KiriKiri.Xp3Opener.AutoDetectProgress = (scheme) => {
                    if (_autoScanDialog != null)
                    {
                        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                            if (_autoScanDialog != null)
                                _autoScanDialog.CurrentScheme = scheme;
                        });
                    }
                };

                return ArcFile.TryOpen(filePath);
            });
            
            // Close the auto scan dialog if it was shown
            if (_autoScanDialog != null)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    _autoScanDialog.Close();
                    _autoScanDialog = null;
                });
            }

            if (arc != null && arc.Dir != null)
            {
                Console.WriteLine($"Successfully opened archive with {arc.Dir.Count()} items.");
                CurrentArchive = arc;
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
                        FullPath = "", // Not a physical file
                        Entry = entry
                    });
                }
            }
            else
            {
                Console.WriteLine($"Failed to open archive: ArcFile.TryOpen returned null.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening archive: {ex.ToString()}");
            if (_autoScanDialog != null)
            {
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    _autoScanDialog.Close();
                    _autoScanDialog = null;
                });
            }
        }
    }

    private void OnParametersRequest(object sender, GameRes.ParametersRequestEventArgs e)
    {
        var format = sender as GameRes.IResource;
        if (format == null) return;
        
        if (format.Tag == "XP3")
        {
            bool result = false;
            GameRes.ResourceOptions? options = null;
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            global::Avalonia.Threading.Dispatcher.UIThread.Post(async () => 
            {
                try
                {
                    var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                    var mainWindow = desktop?.MainWindow;

                    if (mainWindow != null)
                    {
                        var dialog = new Views.ManualSchemeDialog();
                        dialog.NoticeText = e.Notice;
                        var res = await dialog.ShowDialog<GameRes.ResourceOptions>(mainWindow);
                        if (res != null)
                        {
                            options = res;
                            result = true;

                            if (options is GameRes.Formats.KiriKiri.Xp3Options xp3Options && xp3Options.Scheme is GameRes.Formats.KiriKiri.AutoDetectCrypt)
                            {
                                _autoScanDialog = new Views.AutoScanDialog();
                                _autoScanDialog.Show(mainWindow);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText("garbro_error.txt", "Dialog exception: " + ex.ToString() + "\n");
                }
                finally
                {
                    tcs.SetResult(true);
                }
            });
            tcs.Task.GetAwaiter().GetResult();

            if (result)
            {
                e.Options = options;
                e.InputResult = true;
            }
            else
            {
                e.Options = format.GetDefaultOptions();
                e.InputResult = false;
            }
        }
        else
        {
            e.Options = format.GetDefaultOptions();
            e.InputResult = true;
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
    public GameRes.Entry? Entry { get; set; }
}
