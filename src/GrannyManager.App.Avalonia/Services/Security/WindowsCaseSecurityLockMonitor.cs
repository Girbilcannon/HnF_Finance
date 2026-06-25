using System;
using Microsoft.Win32;

namespace GrannyManager.App.Avalonia.Services.Security;

public sealed class WindowsCaseSecurityLockMonitor : ICaseSecurityLockMonitor
{
    private bool _started;

    public event EventHandler<CaseSecurityLockRequestedEventArgs>? LockRequested;

    public void Start()
    {
        if (_started)
            return;

        try
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            _started = true;
        }
        catch
        {
            _started = false;
        }
    }

    public void Stop()
    {
        if (!_started)
            return;

        try
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        }
        catch
        {
            // Security event cleanup should never block app shutdown.
        }

        _started = false;
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock ||
            e.Reason == SessionSwitchReason.SessionLogoff)
        {
            LockRequested?.Invoke(
                this,
                new CaseSecurityLockRequestedEventArgs(
                    "Case locked because Windows was locked or the user session changed. Reopen it from Recent Cases and enter the case PIN."));
        }
    }

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            LockRequested?.Invoke(
                this,
                new CaseSecurityLockRequestedEventArgs(
                    "Case locked because the computer is going to sleep or hibernate. Reopen it from Recent Cases and enter the case PIN."));
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
