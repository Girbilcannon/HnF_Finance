using System;
using System.Runtime.InteropServices;

namespace GrannyManager.App.Avalonia.Services.Security;

public static class CaseSecurityLockMonitorFactory
{
    public static ICaseSecurityLockMonitor CreatePlatformMonitor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new WindowsCaseSecurityLockMonitor();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new PlaceholderCaseSecurityLockMonitor("macOS session-lock monitor has not been implemented yet.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new PlaceholderCaseSecurityLockMonitor("Linux session-lock monitor has not been implemented yet.");

        return new PlaceholderCaseSecurityLockMonitor("No platform-specific session-lock monitor is available for this OS.");
    }
}
