using System.Text.Json;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;

namespace GrannyManager.App.Services;

public sealed class RecentCasesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private string AppSettingsFolder
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "GrannyManager");
        }
    }

    private string RecentCasesFilePath => Path.Combine(AppSettingsFolder, "recent-cases.json");

    public IReadOnlyList<RecentCaseInfo> LoadRecentCases()
    {
        try
        {
            if (!File.Exists(RecentCasesFilePath))
            {
                return Array.Empty<RecentCaseInfo>();
            }

            string json = File.ReadAllText(RecentCasesFilePath);
            List<RecentCaseInfo>? items = JsonSerializer.Deserialize<List<RecentCaseInfo>>(json, JsonOptions);
            return (items ?? new List<RecentCaseInfo>())
                .Where(item => !string.IsNullOrWhiteSpace(item.CaseFilePath))
                .OrderByDescending(item => item.LastOpenedAt)
                .Take(10)
                .ToList();
        }
        catch
        {
            return Array.Empty<RecentCaseInfo>();
        }
    }

    public void AddOrUpdate(CaseProfile profile)
    {
        if (profile is null || string.IsNullOrWhiteSpace(profile.CaseFolderPath))
        {
            return;
        }

        Directory.CreateDirectory(AppSettingsFolder);

        var caseFolderService = new CaseFolderService();
        string caseFilePath = caseFolderService.GetCaseFilePath(profile);
        List<RecentCaseInfo> items = LoadRecentCases().ToList();
        items.RemoveAll(item => string.Equals(item.CaseFilePath, caseFilePath, StringComparison.OrdinalIgnoreCase));
        items.Insert(0, new RecentCaseInfo
        {
            DisplayName = profile.DisplayName,
            CaseFilePath = caseFilePath,
            LastOpenedAt = DateTime.Now
        });

        items = items.Take(10).ToList();
        string json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(RecentCasesFilePath, json);
    }
    public void RemoveByPath(string caseFilePath)
    {
        if (string.IsNullOrWhiteSpace(caseFilePath))
            return;

        Directory.CreateDirectory(AppSettingsFolder);

        List<RecentCaseInfo> items = LoadRecentCases().ToList();
        int removed = items.RemoveAll(item => string.Equals(item.CaseFilePath, caseFilePath, StringComparison.OrdinalIgnoreCase));

        if (removed <= 0)
            return;

        string json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(RecentCasesFilePath, json);
    }

}
