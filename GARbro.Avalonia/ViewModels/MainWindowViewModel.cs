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
        if (path != CurrentDirectory) CommitNavigationState();
        
        CurrentDirectory = path;
        RightPaneItems.Clear();
        NavigateUpCommand.NotifyCanExecuteChanged();

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

    private bool CanNavigateUp()
    {
        if (CurrentArchive != null) return true;
        if (string.IsNullOrEmpty(CurrentDirectory)) return false;
        try { return Directory.GetParent(CurrentDirectory) != null; } catch { return false; }
    }

    [RelayCommand(CanExecute = nameof(CanNavigateUp))]
    private void NavigateUp()
    {
        CommitNavigationState();
        if (CurrentArchive != null)
        {
            if (CurrentVirtualPath != "")
            {
                int lastSlash = CurrentVirtualPath.LastIndexOf('/');
                if (lastSlash >= 0)
                    CurrentVirtualPath = CurrentVirtualPath.Substring(0, lastSlash);
                else
                    CurrentVirtualPath = "";
                RenderVirtualDirectory();
            }
            else
            {
                CurrentArchive.Dispose();
                CurrentArchive = null;
                CurrentVirtualPath = "";
                NavigateTo(Path.GetDirectoryName(CurrentDirectory) ?? string.Empty);
            }
        }
        else
        {
            NavigateTo(Path.GetDirectoryName(CurrentDirectory) ?? string.Empty);
        }
    }

    [RelayCommand]
    private async Task ItemDoubleClicked(EntryViewModel? item)
    {
        if (item == null) return;

        if (item.IsDirectory)
        {
            if (item.IsVirtual)
            {
                CommitNavigationState();
                if (item.Name == "..")
                {
                    int lastSlash = CurrentVirtualPath.LastIndexOf('/');
                    if (lastSlash >= 0)
                        CurrentVirtualPath = CurrentVirtualPath.Substring(0, lastSlash);
                    else
                        CurrentVirtualPath = "";
                    RenderVirtualDirectory();
                }
                else
                {
                    CurrentVirtualPath = item.FullPath;
                    RenderVirtualDirectory();
                }
            }
            else
            {
                if (CurrentArchive != null && item.Name == "..")
                {
                    CurrentArchive.Dispose();
                    CurrentArchive = null;
                    CurrentVirtualPath = "";
                }
                NavigateTo(item.FullPath);
            }
        }
        else
        {
            if (!item.IsVirtual)
            {
                await OpenArchive(item.FullPath);
            }
            else
            {
                Console.WriteLine($"Selected file inside archive: {item.FullPath}");
            }
        }
    }

    [ObservableProperty]
    private ArcFile? _currentArchive;

    [ObservableProperty]
    private string _currentVirtualPath = string.Empty;

    private Views.AutoScanDialog? _autoScanDialog;

    private void RenderVirtualDirectory()
    {
        if (CurrentArchive == null || CurrentArchive.Dir == null) return;

        RightPaneItems.Clear();

        RightPaneItems.Add(new EntryViewModel 
        { 
            Name = "..", 
            FullPath = CurrentVirtualPath == "" ? (Path.GetDirectoryName(CurrentDirectory) ?? string.Empty) : "",
            IsDirectory = true,
            IsVirtual = CurrentVirtualPath != "",
            IconKind = "FolderArrowUp"
        });

        string prefix = CurrentVirtualPath == "" ? "" : CurrentVirtualPath + "/";
        var dirs = new System.Collections.Generic.HashSet<string>();
        
        foreach (var entry in CurrentArchive.Dir)
        {
            string entryName = entry.Name.Replace('\\', '/');
            if (!entryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string relativePath = entryName.Substring(prefix.Length);
            int slashIndex = relativePath.IndexOf('/');

            if (slashIndex >= 0)
            {
                string dirName = relativePath.Substring(0, slashIndex);
                if (dirs.Add(dirName))
                {
                    RightPaneItems.Add(new EntryViewModel
                    {
                        Name = dirName,
                        FullPath = prefix + dirName,
                        IsDirectory = true,
                        IsVirtual = true,
                        IconKind = "Folder"
                    });
                }
            }
            else
            {
                string ext = Path.GetExtension(entryName).ToLower();
                string icon = "FileDocumentOutline";
                
                if (entry.Type == "image" || ext == ".png" || ext == ".jpg" || ext == ".bmp")
                    icon = "ImageOutline";
                else if (entry.Type == "audio" || ext == ".ogg" || ext == ".wav")
                    icon = "MusicNote";
                else if (entry.Type == "script")
                    icon = "ScriptTextOutline";

                RightPaneItems.Add(new EntryViewModel
                {
                    Name = relativePath,
                    Type = string.IsNullOrEmpty(entry.Type) ? (GameRes.FormatCatalog.Instance.GetTypeFromName(entry.Name) ?? "") : entry.Type,
                    Size = entry.Size,
                    Offset = entry.Offset,
                    IconKind = icon,
                    IsDirectory = false,
                    IsVirtual = true,
                    FullPath = entryName,
                    Entry = entry
                });
            }
        }

        var items = RightPaneItems.ToList();
        var upDir = items.FirstOrDefault(i => i.Name == "..");
        if (upDir != null) items.Remove(upDir);

        items = items.OrderByDescending(i => i.IsDirectory)
                     .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                     .ToList();

        RightPaneItems.Clear();
        if (upDir != null) RightPaneItems.Add(upDir);
        foreach (var item in items)
        {
            RightPaneItems.Add(item);
        }
    }

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
                if (CurrentDirectory != filePath) CommitNavigationState();
                CurrentArchive = arc;
                CurrentDirectory = filePath;
                CurrentVirtualPath = "";
                NavigateUpCommand.NotifyCanExecuteChanged();
                RenderVirtualDirectory();
            }
            else
            {
                var errorMsg = GameRes.FormatCatalog.Instance.LastError?.ToString() ?? "ArcFile.TryOpen returned null.";
                Console.WriteLine($"Failed to open archive:\n{errorMsg}");
                
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    if (_autoScanDialog != null)
                    {
                        _autoScanDialog.Close();
                        _autoScanDialog = null;
                    }
                    var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                    if (desktop?.MainWindow != null)
                    {
                        var errorDialog = new Views.ErrorDialog($"Failed to open archive.\nError: {GameRes.FormatCatalog.Instance.LastError?.Message ?? "ArcFile.TryOpen returned null."}");
                        errorDialog.ShowDialog(desktop.MainWindow);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening archive: {ex.ToString()}");
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                if (_autoScanDialog != null)
                {
                    _autoScanDialog.Close();
                    _autoScanDialog = null;
                }
                var desktop = global::Avalonia.Application.Current?.ApplicationLifetime as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
                if (desktop?.MainWindow != null)
                {
                    var errorDialog = new Views.ErrorDialog($"Exception occurred while opening archive.\nError: {ex.Message}");
                    errorDialog.ShowDialog(desktop.MainWindow);
                }
            });
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
                    
                    Console.WriteLine($"[OnParametersRequest] desktop: {desktop != null}, mainWindow: {mainWindow != null}");

                    if (mainWindow != null)
                    {
                        Console.WriteLine($"[OnParametersRequest] Showing dialog...");
                        var dialog = new Views.ManualSchemeDialog();
                        dialog.NoticeText = e.Notice;
                        var res = await dialog.ShowDialog<GameRes.ResourceOptions>(mainWindow);
                        if (res != null)
                        {
                            Console.WriteLine($"[OnParametersRequest] Dialog returned result");
                            options = res;
                            result = true;

                            if (options is GameRes.Formats.KiriKiri.Xp3Options xp3Options && xp3Options.Scheme is GameRes.Formats.KiriKiri.AutoDetectCrypt)
                            {
                                Console.WriteLine($"[OnParametersRequest] Showing AutoScanDialog...");
                                _autoScanDialog = new Views.AutoScanDialog();
                                _autoScanDialog.Show(mainWindow);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[OnParametersRequest] Dialog returned null (cancelled)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OnParametersRequest] Exception: {ex}");
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
    public class NavigationState
    {
        public string PhysicalPath { get; set; } = string.Empty;
        public string VirtualPath { get; set; } = string.Empty;
    }

    private System.Collections.Generic.Stack<NavigationState> _backStack = new();
    private System.Collections.Generic.Stack<NavigationState> _forwardStack = new();
    private bool _isNavigatingHistory = false;

    private void CommitNavigationState()
    {
        if (_isNavigatingHistory) return;
        if (string.IsNullOrEmpty(CurrentDirectory)) return;

        var currentState = new NavigationState { PhysicalPath = this.CurrentDirectory, VirtualPath = this.CurrentVirtualPath };
        
        if (_backStack.Count == 0 || (_backStack.Peek().PhysicalPath != currentState.PhysicalPath || _backStack.Peek().VirtualPath != currentState.VirtualPath))
        {
            _backStack.Push(currentState);
            _forwardStack.Clear();
            NavigateBackCommand.NotifyCanExecuteChanged();
            NavigateForwardCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanNavigateBack() => _backStack.Count > 0;
    
    [RelayCommand(CanExecute = nameof(CanNavigateBack))]
    private async Task NavigateBack()
    {
        if (_backStack.Count == 0) return;

        _forwardStack.Push(new NavigationState { PhysicalPath = CurrentDirectory, VirtualPath = CurrentVirtualPath });
        var target = _backStack.Pop();
        
        _isNavigatingHistory = true;
        try
        {
            await ApplyNavigationState(target);
        }
        finally
        {
            _isNavigatingHistory = false;
            NavigateBackCommand.NotifyCanExecuteChanged();
            NavigateForwardCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanNavigateForward() => _forwardStack.Count > 0;
    
    [RelayCommand(CanExecute = nameof(CanNavigateForward))]
    private async Task NavigateForward()
    {
        if (_forwardStack.Count == 0) return;

        _backStack.Push(new NavigationState { PhysicalPath = CurrentDirectory, VirtualPath = CurrentVirtualPath });
        var target = _forwardStack.Pop();
        
        _isNavigatingHistory = true;
        try
        {
            await ApplyNavigationState(target);
        }
        finally
        {
            _isNavigatingHistory = false;
            NavigateBackCommand.NotifyCanExecuteChanged();
            NavigateForwardCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task ApplyNavigationState(NavigationState state)
    {
        if (state.PhysicalPath != CurrentDirectory || (CurrentArchive == null && state.VirtualPath != ""))
        {
            if (File.Exists(state.PhysicalPath))
            {
                await OpenArchive(state.PhysicalPath);
                CurrentVirtualPath = state.VirtualPath;
                RenderVirtualDirectory();
            }
            else
            {
                if (CurrentArchive != null)
                {
                    CurrentArchive.Dispose();
                    CurrentArchive = null;
                }
                CurrentVirtualPath = "";
                NavigateTo(state.PhysicalPath);
            }
        }
        else
        {
            CurrentVirtualPath = state.VirtualPath;
            RenderVirtualDirectory();
        }
    }
}

public class EntryViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long? Size { get; set; }
    
    public string DisplaySize 
    { 
        get 
        {
            if (!Size.HasValue) return "";
            if (Size.Value < 1024) return Size.Value + " B";
            if (Size.Value < 1024 * 1024) return (Size.Value / 1024) + " KB";
            if (Size.Value < 1024 * 1024 * 1024) return (Size.Value / (1024 * 1024)) + " MB";
            return (Size.Value / (1024 * 1024 * 1024)) + " GB";
        }
    }

    public long Offset { get; set; }
    public string IconKind { get; set; } = "FileDocumentOutline";
    public bool IsDirectory { get; set; }
    public bool IsVirtual { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public GameRes.Entry? Entry { get; set; }
}
