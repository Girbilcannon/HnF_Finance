using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GrannyManager.App.Avalonia.ViewModels;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia;

public partial class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}