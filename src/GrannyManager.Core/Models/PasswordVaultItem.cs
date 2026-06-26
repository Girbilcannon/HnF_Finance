namespace GrannyManager.Core.Models;

public sealed class PasswordVaultItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Legacy field from the first vault entry pass. Existing vault files may still contain this.
    // PasswordVaultService migrates it into PublicNotes after unlock.
    public string Notes { get; set; } = string.Empty;

    public string PublicNotes { get; set; } = string.Empty;
    public string SecureNotes { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
