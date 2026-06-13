namespace GrannyManager.Core.Models;

public sealed class RecentCaseInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public string CaseFilePath { get; set; } = string.Empty;
    public DateTime LastOpenedAt { get; set; } = DateTime.Now;
}
