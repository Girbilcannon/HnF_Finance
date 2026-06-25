using System;
namespace GrannyManager.App.Avalonia.Services.Security;

public sealed class PlaceholderCaseSecurityLockMonitor : ICaseSecurityLockMonitor
{
    public PlaceholderCaseSecurityLockMonitor(string note)
    {
        Note = note;
    }

    public string Note { get; }

    public event EventHandler<CaseSecurityLockRequestedEventArgs>? LockRequested;

    public void Start()
    {
        // Intentionally empty.
        // macOS/Linux session-lock hooks will plug in here later.
        // Sleep/resume protection is handled by SleepResumeCaseSecurityLockMonitor.
    }

    public void Stop()
    {
        // Intentionally empty.
    }

    public void Dispose()
    {
        Stop();
    }
}
