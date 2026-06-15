using System;
using System.Diagnostics;
using System.IO;

namespace GrannyManager.App.Avalonia.Services;

public static class StartupDiagnostics
{
    private static readonly object SyncRoot = new();
    private static readonly Stopwatch Stopwatch = new();

    public static string LogFilePath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "GrannyManager", "startup-diagnostics.log");
        }
    }

    public static void ResetLog()
    {
        try
        {
            lock (SyncRoot)
            {
                string? folder = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrWhiteSpace(folder))
                    Directory.CreateDirectory(folder);

                Stopwatch.Restart();

                File.WriteAllText(
                    LogFilePath,
                    "Home & Family Finance Manager startup diagnostics" + Environment.NewLine +
                    "Started: " + DateTime.Now.ToString("O") + Environment.NewLine +
                    "Machine: " + Environment.MachineName + Environment.NewLine +
                    "OS: " + Environment.OSVersion + Environment.NewLine +
                    "Process: " + Environment.ProcessPath + Environment.NewLine +
                    Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics must never prevent the app from opening.
        }
    }

    public static void Mark(string message)
    {
        try
        {
            lock (SyncRoot)
            {
                if (!Stopwatch.IsRunning)
                    Stopwatch.Start();

                string? folder = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrWhiteSpace(folder))
                    Directory.CreateDirectory(folder);

                File.AppendAllText(
                    LogFilePath,
                    DateTime.Now.ToString("O") +
                    " | +" + Stopwatch.Elapsed.TotalMilliseconds.ToString("000000.0") + " ms | " +
                    message +
                    Environment.NewLine);
            }
        }
        catch
        {
            // Diagnostics must never prevent the app from opening.
        }
    }

    public static void MarkException(string message, System.Exception exception)
    {
        Mark(message + ": " + exception);
    }
}
