using System;
namespace GrannyManager.App.Avalonia.Services.Security;

public interface ICaseSecurityLockMonitor : IDisposable
{
    event EventHandler<CaseSecurityLockRequestedEventArgs>? LockRequested;

    void Start();

    void Stop();
}
