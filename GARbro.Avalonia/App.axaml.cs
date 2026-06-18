using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GameRes;
using GARbro.Avalonia.ViewModels;

namespace GARbro.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize GARbro formats
        try
        {
            var formatsDatPath = Path.Combine(FormatCatalog.Instance.DataDirectory, "Formats.dat");
            if (File.Exists(formatsDatPath))
            {
                using var file = File.OpenRead(formatsDatPath);
                FormatCatalog.Instance.DeserializeScheme(file);
            }
        }
        catch
        {
            // Ignore deserialization errors
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}