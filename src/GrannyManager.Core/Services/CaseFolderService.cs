using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GrannyManager.Core.Models;

namespace GrannyManager.Core.Services;

public sealed class CaseFolderService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string GetDefaultCaseRoot()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "GrannyManager Cases");
    }

    public CaseProfile CreateCase(string displayName, string primaryPersonName, string rootFolder, string? securityPin = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A case name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(rootFolder))
        {
            rootFolder = GetDefaultCaseRoot();
        }

        Directory.CreateDirectory(rootFolder);

        string safeFolderName = MakeSafeFolderName(displayName.Trim());
        string caseFolder = GetUniqueFolderPath(Path.Combine(rootFolder, safeFolderName));
        Directory.CreateDirectory(caseFolder);

        Directory.CreateDirectory(Path.Combine(caseFolder, "documents"));
        Directory.CreateDirectory(Path.Combine(caseFolder, "imports"));
        Directory.CreateDirectory(Path.Combine(caseFolder, "exports"));
        Directory.CreateDirectory(Path.Combine(caseFolder, "backups"));

        string nowReadable = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.WriteAllText(Path.Combine(caseFolder, "documents", "README.txt"),
            "Put copied case documents here. Later the app will attach and tag documents directly.\r\nCreated: " + nowReadable + "\r\n");
        File.WriteAllText(Path.Combine(caseFolder, "imports", "README.txt"),
            "Put imported bank/statement files here.\r\nCreated: " + nowReadable + "\r\n");
        File.WriteAllText(Path.Combine(caseFolder, "exports", "README.txt"),
            "App reports and exports will go here.\r\nCreated: " + nowReadable + "\r\n");
        File.WriteAllText(Path.Combine(caseFolder, "backups", "README.txt"),
            "Manual/automatic backups will go here.\r\nCreated: " + nowReadable + "\r\n");

        var profile = new CaseProfile
        {
            DisplayName = displayName.Trim(),
            PrimaryPersonName = primaryPersonName.Trim(),
            CaseFolderPath = caseFolder,
            CreatedAt = DateTime.Now,
            LastOpenedAt = DateTime.Now
        };

        if (!string.IsNullOrWhiteSpace(securityPin))
        {
            SetSecurityPin(profile, securityPin);
        }

        SaveCase(profile);
        return profile;
    }

    public void SetSecurityPin(CaseProfile profile, string pin)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        ValidatePin(pin);
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        profile.SecurityPinSalt = Convert.ToBase64String(saltBytes);
        profile.SecurityPinHash = HashPin(pin, saltBytes);
    }

    public bool VerifySecurityPin(CaseProfile profile, string pin)
    {
        if (profile is null)
        {
            return false;
        }

        if (!profile.HasSecurityPin)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(pin))
        {
            return false;
        }

        try
        {
            byte[] saltBytes = Convert.FromBase64String(profile.SecurityPinSalt);
            string hash = HashPin(pin.Trim(), saltBytes);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash),
                Encoding.UTF8.GetBytes(profile.SecurityPinHash));
        }
        catch
        {
            return false;
        }
    }

    private static void ValidatePin(string pin)
    {
        if (pin is null || pin.Trim().Length != 4 || pin.Trim().Any(c => !char.IsDigit(c)))
        {
            throw new ArgumentException("The case security PIN must be exactly 4 digits.", nameof(pin));
        }
    }

    private static string HashPin(string pin, byte[] saltBytes)
    {
        using var sha = SHA256.Create();
        byte[] pinBytes = Encoding.UTF8.GetBytes(pin.Trim());
        byte[] combined = new byte[saltBytes.Length + pinBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(pinBytes, 0, combined, saltBytes.Length, pinBytes.Length);
        return Convert.ToBase64String(sha.ComputeHash(combined));
    }

    public CaseProfile LoadCaseFromFile(string caseFilePath)
    {
        if (string.IsNullOrWhiteSpace(caseFilePath))
        {
            throw new ArgumentException("A case file path is required.", nameof(caseFilePath));
        }

        if (!File.Exists(caseFilePath))
        {
            throw new FileNotFoundException("The selected case file could not be found.", caseFilePath);
        }

        string json = File.ReadAllText(caseFilePath);
        CaseProfile? profile = JsonSerializer.Deserialize<CaseProfile>(json, JsonOptions);

        if (profile is null || !profile.IsValid)
        {
            throw new InvalidOperationException("The selected file is not a valid Granny Manager case file.");
        }

        profile.CaseFolderPath = Path.GetDirectoryName(caseFilePath) ?? profile.CaseFolderPath;
        profile.LastOpenedAt = DateTime.Now;
        SaveCase(profile);
        return profile;
    }

    public void SaveCase(CaseProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (string.IsNullOrWhiteSpace(profile.CaseFolderPath))
        {
            throw new InvalidOperationException("The case folder path is missing.");
        }

        Directory.CreateDirectory(profile.CaseFolderPath);
        string caseFilePath = GetCaseFilePath(profile);
        string json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(caseFilePath, json);
    }

    public string GetCaseFilePath(CaseProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        return Path.Combine(profile.CaseFolderPath, GetCaseFileName(profile.DisplayName));
    }

    public string GetCaseFilePath(string caseFolderPath, string displayName)
    {
        return Path.Combine(caseFolderPath, GetCaseFileName(displayName));
    }

    public string GetCaseFileName(string displayName)
    {
        string safeName = MakeSafeFileNameBase(displayName);
        return safeName + ".gmcase";
    }

    private static string MakeSafeFolderName(string input)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string cleaned = new(input.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "New Case" : cleaned;
    }

    private static string MakeSafeFileNameBase(string input)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string cleaned = new(input.Trim().Select(c =>
        {
            if (invalidChars.Contains(c))
            {
                return '_';
            }

            return char.IsWhiteSpace(c) ? '_' : c;
        }).ToArray());

        while (cleaned.Contains("__", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("__", "_", StringComparison.Ordinal);
        }

        cleaned = cleaned.Trim('_');
        return string.IsNullOrWhiteSpace(cleaned) ? "New_Case" : cleaned;
    }

    private static string GetUniqueFolderPath(string requestedPath)
    {
        if (!Directory.Exists(requestedPath))
        {
            return requestedPath;
        }

        for (int i = 2; i < 1000; i++)
        {
            string candidate = requestedPath + " " + i;
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return requestedPath + " " + DateTime.Now.ToString("yyyyMMddHHmmss");
    }
}
