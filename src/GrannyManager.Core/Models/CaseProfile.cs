namespace GrannyManager.Core.Models;

public sealed class CaseProfile
{
    public string CaseId { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = string.Empty;
    public string PrimaryPersonName { get; set; } = string.Empty;
    public string CaseManagerName { get; set; } = string.Empty;
    public string CaseFolderPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastOpenedAt { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;
    public string SecurityPinHash { get; set; } = string.Empty;
    public string SecurityPinSalt { get; set; } = string.Empty;

    public bool HasSecurityPin => !string.IsNullOrWhiteSpace(SecurityPinHash) && !string.IsNullOrWhiteSpace(SecurityPinSalt);

    public bool IsValid => !string.IsNullOrWhiteSpace(DisplayName) && !string.IsNullOrWhiteSpace(CaseFolderPath);
}
