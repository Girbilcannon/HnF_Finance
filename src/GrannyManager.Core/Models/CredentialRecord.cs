namespace GrannyManager.Core.Models;

public sealed class CredentialRecord
{
    public long Id { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string EncryptedUsername { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public string EncryptedRecoveryInfo { get; set; } = string.Empty;
    public string EncryptedSecurityNotes { get; set; } = string.Empty;
    public string LinkedRecordType { get; set; } = string.Empty;
    public long LinkedRecordId { get; set; }
    public string LinkedRecordName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string LinkedDisplay => string.IsNullOrWhiteSpace(LinkedRecordType)
        ? "Not linked"
        : LinkedRecordId > 0 && !string.IsNullOrWhiteSpace(LinkedRecordName)
            ? $"{LinkedRecordType}: {LinkedRecordName}"
            : LinkedRecordType;

    public string StatusText => IsActive ? "Active" : "Inactive";
}
