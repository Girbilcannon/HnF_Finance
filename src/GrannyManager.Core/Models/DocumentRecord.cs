namespace GrannyManager.Core.Models;

public sealed class DocumentRecord
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFilePath { get; set; } = string.Empty;
    public string SourceFilePath { get; set; } = string.Empty;
    public string LinkedRecordType { get; set; } = "None";
    public long LinkedRecordId { get; set; }
    public string LinkedRecordName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsImportant { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string FileNameDisplay => string.IsNullOrWhiteSpace(OriginalFileName) ? Path.GetFileName(StoredFilePath) : OriginalFileName;
    public string LinkedDisplay => string.Equals(LinkedRecordType, "None", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(LinkedRecordName)
        ? "None"
        : $"{LinkedRecordType}: {LinkedRecordName}";
    public string ImportantText => IsImportant ? "Yes" : "No";
    public string StatusText => IsActive ? "Active" : "Inactive";
}
