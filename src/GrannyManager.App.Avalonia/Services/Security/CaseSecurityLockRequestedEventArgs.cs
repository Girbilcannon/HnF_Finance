using System;
namespace GrannyManager.App.Avalonia.Services.Security;

public sealed class CaseSecurityLockRequestedEventArgs : EventArgs
{
    public CaseSecurityLockRequestedEventArgs(string reason)
    {
        Reason = string.IsNullOrWhiteSpace(reason)
            ? "Case locked for security. Reopen it from Recent Cases and enter the case PIN."
            : reason;
    }

    public string Reason { get; }
}
