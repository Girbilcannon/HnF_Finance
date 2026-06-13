using System.Security.Cryptography;
using System.Text;
using GrannyManager.Core.Models;

namespace GrannyManager.Security.Crypto;

public static class CredentialVaultCrypto
{
    private const string Prefix = "v1";

    public static string Encrypt(CaseProfile? profile, string? plainText)
    {
        plainText ??= string.Empty;
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        byte[] key = BuildKey(profile);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipher = new byte[plainBytes.Length];
        byte[] tag = new byte[16];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        return string.Join(':', Prefix, Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(cipher));
    }

    public static string Decrypt(CaseProfile? profile, string? encryptedText)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
            return string.Empty;

        if (!encryptedText.StartsWith(Prefix + ":", StringComparison.Ordinal))
            return encryptedText;

        try
        {
            string[] parts = encryptedText.Split(':');
            if (parts.Length != 4)
                return string.Empty;

            byte[] key = BuildKey(profile);
            byte[] nonce = Convert.FromBase64String(parts[1]);
            byte[] tag = Convert.FromBase64String(parts[2]);
            byte[] cipher = Convert.FromBase64String(parts[3]);
            byte[] plainBytes = new byte[cipher.Length];

            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, cipher, tag, plainBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte[] BuildKey(CaseProfile? profile)
    {
        string material = profile is null
            ? "GrannyManagerCredentialVaultFallback"
            : string.Join('|', "GrannyManagerCredentialVault", profile.CaseId, profile.SecurityPinSalt, profile.SecurityPinHash);

        return SHA256.HashData(Encoding.UTF8.GetBytes(material));
    }
}
