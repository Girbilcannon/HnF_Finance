using System;
using Avalonia.Threading;

namespace GrannyManager.App.Avalonia.Services.Security;

public sealed class CaseSecurityLockCoordinator : IDisposable
{
    private readonly ICaseSecurityLockMonitor _platformMonitor;
    private readonly SleepResumeCaseSecurityLockMonitor _sleepResumeMonitor;
    private readonly System.Action<string> _lockAction;
    private bool _disposed;

    public CaseSecurityLockCoordinator(System.Action<string> lockAction)
    {
        _lockAction = lockAction ?? throw new ArgumentNullException(nameof(lockAction));

        _platformMonitor = CaseSecurityLockMonitorFactory.CreatePlatformMonitor();
        _sleepResumeMonitor = new SleepResumeCaseSecurityLockMonitor();

        _platformMonitor.LockRequested += Monitor_LockRequested;
        _sleepResumeMonitor.LockRequested += Monitor_LockRequested;
    }

    public void Start()
    {
        _platformMonitor.Start();
        _sleepResumeMonitor.Start();
    }

    public void Stop()
    {
        _platformMonitor.Stop();
        _sleepResumeMonitor.Stop();
    }

    private void Monitor_LockRequested(object? sender, CaseSecurityLockRequestedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => _lockAction(e.Reason));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _platformMonitor.LockRequested -= Monitor_LockRequested;
        _sleepResumeMonitor.LockRequested -= Monitor_LockRequested;

        _platformMonitor.Dispose();
        _sleepResumeMonitor.Dispose();
    }
}
