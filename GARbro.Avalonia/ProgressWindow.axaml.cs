using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GameRes;

namespace GARbro.Avalonia;

public partial class ProgressWindow : Window
{
    private CancellationTokenSource _cts = new();

    public ProgressWindow()
    {
        InitializeComponent();
    }

    public async void StartExtraction(ArcFile archive, List<Entry> entries, string destinationPath)
    {
        try
        {
            await Task.Run(() =>
            {
                int total = entries.Count;
                int current = 0;

                foreach (var entry in entries)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    if (entry == null) continue;

                    Dispatcher.UIThread.Post(() =>
                    {
                        StatusText.Text = $"Extracting {entry.Name}...";
                    });

                    try
                    {
                        using var stream = archive.OpenEntry(entry);
                        string destFile = Path.Combine(destinationPath, entry.Name);
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                        using var fs = File.Create(destFile);
                        stream.CopyTo(fs);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to extract {entry.Name}: {ex.Message}");
                    }

                    current++;
                    Dispatcher.UIThread.Post(() =>
                    {
                        ExtractionProgress.Value = (current * 100.0) / total;
                    });
                }
            });
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
