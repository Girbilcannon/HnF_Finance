namespace GrannyManager.Data.Database;

public static class CaseDatabaseLocator
{
    public static string? TryFindActiveCaseFolder()
    {
        // Security rule: pages may only read a case after AppState.ActiveCase is explicitly set.
        // Never auto-discover the newest/recent case folder, because that can expose data before a user opens/unlocks a case.
        return null;
    }

    public static string GetDatabasePathForCaseFolder(string caseFolder)
    {
        if (string.IsNullOrWhiteSpace(caseFolder))
            throw new ArgumentException("Case folder is required.", nameof(caseFolder));

        Directory.CreateDirectory(caseFolder);
        return Path.Combine(caseFolder, "data.db");
    }
}
