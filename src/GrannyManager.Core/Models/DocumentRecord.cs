using System;

namespace GrannyManager.Core.Models;

public sealed class DocumentRecord
{
    public long Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string PersonName { get; set; } = "General";
    public string Category { get; set; } = "Other";
    public string LinkedSection { get; set; } = "Other";
    public long LinkedRecordId { get; set; }
    public string LinkedRecordName { get; set; } = string.Empty;
    public string CustomFolder { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsMergedFile { get; set; }
    public bool PasswordProtectedRequested { get; set; }
    public string ImportBatchId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime ImportedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public string TagDisplay => string.IsNullOrWhiteSpace(Tags) ? "None" : Tags.Trim();
    public string LinkedDisplay => string.IsNullOrWhiteSpace(LinkedRecordName) ? LinkedSection : $"{LinkedSection}: {LinkedRecordName}";
    public string FolderDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CustomFolder))
                return $"{PersonName} / {Category}";

            return $"{PersonName} / {Category} / {CustomFolder}";
        }
    }
}
