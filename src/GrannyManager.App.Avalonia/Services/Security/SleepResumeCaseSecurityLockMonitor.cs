using System;
using Avalonia.Threading;

namespace GrannyManager.App.Avalonia.Services.Security;

public sealed class SleepResumeCaseSecurityLockMonitor : ICaseSecurityLockMonitor
{
    private readonly DispatcherTimer _timer = new();
    private DateTime _lastTickUtc = DateTime.UtcNow;
    private bool _started;

    public event EventHandler<CaseSecurityLockRequestedEventArgs>? LockRequested;

    public SleepResumeCaseSecurityLockMonitor()
    {
        _timer.Interval = TimeSpan.FromSeconds(15);
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _lastTickUtc = DateTime.UtcNow;
        _timer.Start();
    }

    public void Stop()
    {
        if (!_started)
            return;

        _started = false;
        _timer.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var nowUtc = DateTime.UtcNow;
        var tickGap = nowUtc - _lastTickUtc;
        _lastTickUtc = nowUtc;

        if (tickGap > TimeSpan.FromMinutes(2))
        {
            LockRequested?.Invoke(
                this,
                new CaseSecurityLockRequestedEventArgs(
                    "Case locked because the computer was asleep, hibernated, or suspended. Reopen it from Recent Cases and enter the case PIN."));
        }
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= Timer_Tick;
    }
}
