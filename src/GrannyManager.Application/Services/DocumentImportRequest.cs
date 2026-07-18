namespace GrannyManager.Application.Services;

public sealed class DocumentImportRequest
{
    public IReadOnlyList<string> SourceFilePaths { get; init; } = Array.Empty<string>();
    public string ImportMode { get; init; } = "Import Individually";
    public string MergedFileName { get; init; } = string.Empty;
    public string PersonName { get; init; } = "General";
    public string Category { get; init; } = "Other";
    public string LinkedSection { get; init; } = "Other";
    public long LinkedRecordId { get; init; }
    public string LinkedRecordName { get; init; } = string.Empty;
    public string CustomFolder { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public bool PasswordProtectRequested { get; init; }
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public bool UseExistingFolderWhenConflict { get; init; } = true;
}

public sealed class DocumentEditRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public string PersonName { get; init; } = "General";
    public string Category { get; init; } = "Other";
    public string LinkedSection { get; init; } = "Other";
    public long LinkedRecordId { get; init; }
    public string LinkedRecordName { get; init; } = string.Empty;
    public string CustomFolder { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

public sealed class DocumentConnectionOption
{
    public string Section { get; init; } = "Other";
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public override string ToString()
    {
        return Name;
    }
}
