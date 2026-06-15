namespace GrannyManager.Core.Models;

public sealed class PasswordVaultData
{
    public int Version { get; set; } = 1;
    public List<PasswordVaultItem> Items { get; set; } = new();
}
