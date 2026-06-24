using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;

namespace GrannyManager.Application.Services;

public sealed class PasswordVaultService
{
    private const int Version = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int KeyDerivationIterations = 210_000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ActiveCaseState _activeCaseState;
    private readonly CaseFolderService _caseFolderService;

    public PasswordVaultService(ActiveCaseState activeCaseState, CaseFolderService caseFolderService)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
        _caseFolderService = caseFolderService ?? throw new ArgumentNullException(nameof(caseFolderService));
    }

    public bool VerifyActiveCasePin(string pin)
    {
        var activeCase = _activeCaseState.ActiveCase;
        return activeCase is not null && activeCase.IsValid && _caseFolderService.VerifySecurityPin(activeCase, pin);
    }

    public PasswordVaultStatus GetStatus()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new PasswordVaultStatus(
                HasActiveCase: false,
                VaultExists: false,
                VaultPath: string.Empty,
                StatusMessage: "No active case is open. Create or open a case before using the password vault.");
        }

        string vaultPath = GetVaultPath(activeCase.CaseFolderPath);

        return new PasswordVaultStatus(
            HasActiveCase: true,
            VaultExists: File.Exists(vaultPath),
            VaultPath: vaultPath,
            StatusMessage: File.Exists(vaultPath)
                ? "Password vault is locked."
                : "Password vault has not been created for this case yet.");
    }

    public PasswordVaultUnlockResult UnlockOrCreateVault(string pin)
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return PasswordVaultUnlockResult.Fail("No active case is open.");
        }

        if (string.IsNullOrWhiteSpace(pin))
        {
            return PasswordVaultUnlockResult.Fail("Enter the 4-digit case PIN.");
        }

        if (!_caseFolderService.VerifySecurityPin(activeCase, pin))
        {
            return PasswordVaultUnlockResult.Fail("That PIN did not match this case.");
        }

        try
        {
            string vaultPath = GetVaultPath(activeCase.CaseFolderPath);

            if (!File.Exists(vaultPath))
            {
                var created = new PasswordVaultData();
                SaveVault(activeCase.CaseFolderPath, pin, created);

                return PasswordVaultUnlockResult.Ok(
                    created,
                    "Encrypted vault created and unlocked.");
            }

            var data = LoadVault(activeCase.CaseFolderPath, pin);

            return PasswordVaultUnlockResult.Ok(
                data,
                data.Items.Count == 0
                    ? "Vault unlocked. No password entries have been added yet."
                    : $"Vault unlocked. {data.Items.Count} password entr{(data.Items.Count == 1 ? "y" : "ies")} loaded.");
        }
        catch (CryptographicException)
        {
            return PasswordVaultUnlockResult.Fail("Could not decrypt the vault. Check the PIN and try again.");
        }
        catch (Exception ex)
        {
            return PasswordVaultUnlockResult.Fail($"Could not unlock vault: {ex.Message}");
        }
    }

    public PasswordVaultSaveResult SaveUnlockedVault(string pin, PasswordVaultData vaultData)
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return PasswordVaultSaveResult.Fail("No active case is open.");
        }

        if (vaultData is null)
        {
            return PasswordVaultSaveResult.Fail("No vault data is available to save.");
        }

        if (!_caseFolderService.VerifySecurityPin(activeCase, pin))
        {
            return PasswordVaultSaveResult.Fail("Could not save vault because the case PIN was not verified.");
        }

        try
        {
            SaveVault(activeCase.CaseFolderPath, pin, vaultData);
            return PasswordVaultSaveResult.Ok("Encrypted vault saved.");
        }
        catch (Exception ex)
        {
            return PasswordVaultSaveResult.Fail($"Could not save vault: {ex.Message}");
        }
    }

    public static string EnsureVaultFolder(string caseFolderPath)
    {
        string folder = Path.Combine(caseFolderPath, "vault");
        Directory.CreateDirectory(folder);
        return folder;
    }

    public static string GetVaultPath(string caseFolderPath)
    {
        return Path.Combine(EnsureVaultFolder(caseFolderPath), "password-vault.hnfvault");
    }

    private static PasswordVaultData LoadVault(string caseFolderPath, string pin)
    {
        string vaultPath = GetVaultPath(caseFolderPath);
        string json = File.ReadAllText(vaultPath);
        var envelope = JsonSerializer.Deserialize<PasswordVaultEnvelope>(json)
            ?? throw new InvalidOperationException("The vault file could not be read.");

        byte[] salt = Convert.FromBase64String(envelope.Salt);
        byte[] nonce = Convert.FromBase64String(envelope.Nonce);
        byte[] tag = Convert.FromBase64String(envelope.Tag);
        byte[] cipherText = Convert.FromBase64String(envelope.CipherText);
        byte[] key = DeriveKey(pin, salt);

        byte[] plainText = new byte[cipherText.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainText);

        string dataJson = Encoding.UTF8.GetString(plainText);
        return JsonSerializer.Deserialize<PasswordVaultData>(dataJson)
            ?? new PasswordVaultData();
    }

    private static void SaveVault(string caseFolderPath, string pin, PasswordVaultData data)
    {
        string vaultPath = GetVaultPath(caseFolderPath);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] key = DeriveKey(pin, salt);

        byte[] plainText = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, JsonOptions));
        byte[] cipherText = new byte[plainText.Length];
        byte[] tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainText, cipherText, tag);

        var envelope = new PasswordVaultEnvelope
        {
            Version = Version,
            KeyDerivation = "PBKDF2-SHA256",
            Iterations = KeyDerivationIterations,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            CipherText = Convert.ToBase64String(cipherText)
        };

        File.WriteAllText(vaultPath, JsonSerializer.Serialize(envelope, JsonOptions));
    }

    private static byte[] DeriveKey(string pin, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password: pin.Trim(),
            salt: salt,
            iterations: KeyDerivationIterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: KeySize);
    }

    private sealed class PasswordVaultEnvelope
    {
        public int Version { get; set; } = 1;
        public string KeyDerivation { get; set; } = "PBKDF2-SHA256";
        public int Iterations { get; set; } = KeyDerivationIterations;
        public string Salt { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string CipherText { get; set; } = string.Empty;
    }
}

public sealed record PasswordVaultStatus(
    bool HasActiveCase,
    bool VaultExists,
    string VaultPath,
    string StatusMessage);

public sealed record PasswordVaultUnlockResult(
    bool Success,
    string StatusMessage,
    PasswordVaultData? VaultData)
{
    public static PasswordVaultUnlockResult Fail(string statusMessage)
    {
        return new PasswordVaultUnlockResult(false, statusMessage, null);
    }

    public static PasswordVaultUnlockResult Ok(PasswordVaultData vaultData, string statusMessage)
    {
        return new PasswordVaultUnlockResult(true, statusMessage, vaultData);
    }
}


public sealed record PasswordVaultSaveResult(
    bool Success,
    string StatusMessage)
{
    public static PasswordVaultSaveResult Fail(string statusMessage)
    {
        return new PasswordVaultSaveResult(false, statusMessage);
    }

    public static PasswordVaultSaveResult Ok(string statusMessage)
    {
        return new PasswordVaultSaveResult(true, statusMessage);
    }
}
