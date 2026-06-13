namespace GrannyManager.Security.Crypto;

public sealed class ClipboardService
{
    // Placeholder for the first shell build.
    // Clipboard access belongs in the WinForms App layer because System.Windows.Forms.Clipboard is UI-specific.
    public TimeSpan DefaultClearDelay { get; set; } = TimeSpan.FromSeconds(45);
}
