using System;
using Avalonia;
using GrannyManager.App.Avalonia.Services;

namespace GrannyManager.App.Avalonia
{
    internal sealed class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            StartupDiagnostics.ResetLog();
            StartupDiagnostics.Mark("Program.Main entered.");

            try
            {
                StartupDiagnostics.Mark("BuildAvaloniaApp starting.");
                var appBuilder = BuildAvaloniaApp();
                StartupDiagnostics.Mark("BuildAvaloniaApp completed. Starting classic desktop lifetime.");

                appBuilder.StartWithClassicDesktopLifetime(args);

                StartupDiagnostics.Mark("Classic desktop lifetime exited.");
            }
            catch (System.Exception ex)
            {
                StartupDiagnostics.MarkException("Fatal startup exception", ex);
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            StartupDiagnostics.Mark("AppBuilder.Configure starting.");

            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .LogToTrace();

            StartupDiagnostics.Mark("AppBuilder.Configure completed.");
            return builder;
        }
    }
}
