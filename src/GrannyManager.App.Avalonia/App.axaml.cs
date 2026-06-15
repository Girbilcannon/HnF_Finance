using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GrannyManager.App.Avalonia.Services;
using GrannyManager.App.Avalonia.ViewModels;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia;

public partial class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        StartupDiagnostics.Mark("App.Initialize entered. Loading App.axaml.");
        AvaloniaXamlLoader.Load(this);
        StartupDiagnostics.Mark("App.Initialize completed.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        StartupDiagnostics.Mark("OnFrameworkInitializationCompleted entered.");

        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StartupDiagnostics.Mark("Classic desktop lifetime detected. Creating splash window.");

            var splash = new SplashWindow();
            desktop.MainWindow = splash;

            StartupDiagnostics.Mark("SplashWindow constructed. Calling Show().");
            splash.Show();
            StartupDiagnostics.Mark("SplashWindow.Show returned.");

            _ = ContinueStartupAsync(desktop, splash);
        }
        else
        {
            StartupDiagnostics.Mark("ApplicationLifetime was not classic desktop.");
        }

        StartupDiagnostics.Mark("Calling base.OnFrameworkInitializationCompleted.");
        base.OnFrameworkInitializationCompleted();
        StartupDiagnostics.Mark("OnFrameworkInitializationCompleted exited.");
    }

    private static async Task ContinueStartupAsync(IClassicDesktopStyleApplicationLifetime desktop, SplashWindow splash)
    {
        StartupDiagnostics.Mark("ContinueStartupAsync started.");

        await ShowStatusAsync(splash, "Loading App.axaml...");
        await ShowStatusAsync(splash, "Loading application shell...");
        await ShowStatusAsync(splash, "Preparing active case state...");
        await ShowStatusAsync(splash, "Loading dashboard...");
        await ShowStatusAsync(splash, "Loading recent cases...");
        await ShowStatusAsync(splash, "Preparing household section...");
        await ShowStatusAsync(splash, "Preparing income section...");
        await ShowStatusAsync(splash, "Preparing bills and spending section...");
        await ShowStatusAsync(splash, "Preparing allowance and savings section...");
        await ShowStatusAsync(splash, "Preparing assets section...");
        await ShowStatusAsync(splash, "Preparing debts section...");
        await ShowStatusAsync(splash, "Preparing documents section...");
        await ShowStatusAsync(splash, "Creating main window...");

        StartupDiagnostics.Mark("MainWindow construction starting.");
        var mainWindow = new MainWindow();
        StartupDiagnostics.Mark("MainWindow construction completed.");

        StartupDiagnostics.Mark("MainWindowViewModel construction starting.");
        mainWindow.DataContext = new MainWindowViewModel();
        StartupDiagnostics.Mark("MainWindowViewModel construction completed.");

        await ShowStatusAsync(splash, "Opening Home & Family Finance Manager...");

        StartupDiagnostics.Mark("Assigning desktop.MainWindow to MainWindow.");
        desktop.MainWindow = mainWindow;

        StartupDiagnostics.Mark("MainWindow.Show starting.");
        mainWindow.Show();
        StartupDiagnostics.Mark("MainWindow.Show returned.");

        await Task.Delay(150);

        StartupDiagnostics.Mark("Closing SplashWindow.");
        splash.Close();
        StartupDiagnostics.Mark("SplashWindow closed. Startup handoff complete.");
    }

    private static async Task ShowStatusAsync(SplashWindow splash, string status)
    {
        StartupDiagnostics.Mark("Splash status: " + status);
        splash.SetStatus(status);
        await Task.Delay(90);
    }
}
