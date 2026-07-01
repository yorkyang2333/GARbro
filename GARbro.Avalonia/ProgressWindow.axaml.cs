using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GARbro.Avalonia.Views;
using GameRes;

namespace GARbro.Avalonia;

public partial class ProgressWindow : Window
{
    private CancellationTokenSource _cts = new();

    private ArcFile _archive;
    private List<Entry> _entries;
    private string _destinationPath = string.Empty;

    public ProgressWindow()
    {
        InitializeComponent();
    }

    public ProgressWindow(ArcFile archive, List<Entry> entries, string destinationPath)
    {
        InitializeComponent();
        _archive = archive;
        _entries = entries;
        _destinationPath = destinationPath;
        this.Opened += ProgressWindow_Opened;
    }

    private async void ProgressWindow_Opened(object? sender, EventArgs e)
    {
        try
        {
            var extractionErrors = await Task.Run(() =>
            {
                int total = _entries.Count;
                int current = 0;
                List<string> errors = new List<string>();

                int lastUpdate = 0;

                foreach (var entry in _entries)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    if (entry == null) continue;

                    if (current - lastUpdate > 10 || current == total - 1 || current == 0)
                    {
                        lastUpdate = current;
                        Dispatcher.UIThread.Post(() =>
                        {
                            StatusText.Text = $"Extracting {entry.Name}...";
                        });
                    }

                    try
                    {
                        using var stream = _archive.OpenEntry(entry);
                        string normalizedName = entry.Name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
                        string destFile = Path.Combine(_destinationPath, normalizedName);
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                        using var fs = File.Create(destFile);
                        stream.CopyTo(fs);
                    }
                    catch (Exception ex)
                    {
                        if (errors.Count < 20)
                            errors.Add($"Failed to extract {entry.Name}: {ex.Message}");
                        else if (errors.Count == 20)
                            errors.Add("... and more errors.");
                    }

                    current++;
                    if (current - lastUpdate == 0 || current == total)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ExtractionProgress.Value = (current * 100.0) / total;
                        });
                    }
                }
                return errors;
            });

            if (extractionErrors.Count > 0)
            {
                var errorDialog = new ErrorDialog("Extraction Errors:\n" + string.Join("\n", extractionErrors));
                await errorDialog.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            Close();
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        CancelButton.IsEnabled = false;
        StatusText.Text = "Cancelling...";
    }
}
