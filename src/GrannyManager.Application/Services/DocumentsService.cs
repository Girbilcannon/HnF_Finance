using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class DocumentsService
{
    private readonly ActiveCaseState _activeCaseState;

    public DocumentsService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public DocumentsLoadResult LoadDocuments()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new DocumentsLoadResult(
                false,
                "No active case is open. Create or open a case before importing documents.",
                Array.Empty<DocumentRecord>());
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var documents = repository.GetAll()
                .Where(document => document.IsActive)
                .ToList();

            return new DocumentsLoadResult(
                true,
                documents.Count == 0 ? "No documents have been imported yet." : $"{documents.Count} document(s) loaded.",
                documents);
        }
        catch (Exception ex)
        {
            return new DocumentsLoadResult(
                true,
                $"Could not load documents: {ex.Message}",
                Array.Empty<DocumentRecord>());
        }
    }

    public IReadOnlyList<string> LoadPeopleForFolders()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return new[] { "General" };

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var people = new HouseholdPeopleRepository(databasePath)
                .GetAll()
                .Where(person => person.IsActive)
                .Select(person => person.FullName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            var ordered = new List<string>();
            ordered.Add("General");

            var primaryPersonName = activeCase.PrimaryPersonName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(primaryPersonName))
            {
                ordered.Add($"Primary / Self ({primaryPersonName})");
                people.RemoveAll(name => string.Equals(name, primaryPersonName, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                ordered.Add("Primary / Self");
            }

            ordered.AddRange(people);
            return ordered;
        }
        catch
        {
            return new[] { "General", "Primary / Self" };
        }
    }

    public IReadOnlyList<DocumentConnectionOption> LoadConnectionOptions(string section)
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<DocumentConnectionOption>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);

            return section switch
            {
                "Income" => new IncomeSourcesRepository(databasePath).GetAll()
                    .Where(item => item.IsActive)
                    .Select(item => new DocumentConnectionOption { Section = "Income", Id = item.Id, Name = item.SourceName })
                    .ToList(),

                "Bills" => new BillsRepository(databasePath).GetAll()
                    .Where(item => item.IsActive)
                    .Select(item => new DocumentConnectionOption { Section = "Bills", Id = item.Id, Name = item.BillName })
                    .ToList(),

                "Debts" => new DebtsRepository(databasePath).GetAll()
                    .Where(item => item.IsActive)
                    .Select(item => new DocumentConnectionOption { Section = "Debts", Id = item.Id, Name = item.DebtName })
                    .ToList(),

                "Allowance and Savings" => new AllowanceSavingsRepository(databasePath).GetAll()
                    .Where(item => item.IsActive)
                    .Select(item => new DocumentConnectionOption { Section = "Allowance and Savings", Id = item.Id, Name = item.ItemName })
                    .ToList(),

                _ => Array.Empty<DocumentConnectionOption>()
            };
        }
        catch
        {
            return Array.Empty<DocumentConnectionOption>();
        }
    }

    public IReadOnlyList<string> LoadActiveTags()
    {
        var result = LoadDocuments();
        if (!result.HasActiveCase)
            return Array.Empty<string>();

        return result.Documents
            .SelectMany(document => SplitTags(document.Tags))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag)
            .ToList();
    }

    public bool FolderExists(DocumentImportRequest request)
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return false;

        if (string.IsNullOrWhiteSpace(request.CustomFolder))
            return false;

        var targetFolder = BuildTargetFolder(activeCase.CaseFolderPath, request.PersonName, request.Category, request.CustomFolder);
        return Directory.Exists(targetFolder);
    }

    public bool ImportDocuments(DocumentImportRequest request, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before importing documents.";
            return false;
        }

        if (request is null || request.SourceFilePaths.Count == 0)
        {
            statusMessage = "Choose at least one document to import.";
            return false;
        }

        if (request.PasswordProtectRequested && string.IsNullOrWhiteSpace(request.Password))
        {
            statusMessage = "Enter a document password before importing protected PDFs.";
            return false;
        }

        if (request.PasswordProtectRequested && !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            statusMessage = "Document password and confirmation do not match.";
            return false;
        }

        var isMerge = string.Equals(request.ImportMode, "Merge into Single PDF", StringComparison.OrdinalIgnoreCase);
        var requiresPdfProcessing = isMerge || request.PasswordProtectRequested;

        if (requiresPdfProcessing && request.SourceFilePaths.Any(path => !DocumentsPdfProcessor.IsPdf(path)))
        {
            statusMessage = "PDF merge and password protection only work with PDF files. Remove non-PDF files or turn off PDF processing.";
            return false;
        }

        if (isMerge && string.IsNullOrWhiteSpace(request.MergedFileName))
        {
            statusMessage = "Enter a merged PDF file name before importing.";
            return false;
        }

        try
        {
            var batchId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var customFolder = ResolveCustomFolder(activeCase.CaseFolderPath, request);
            var targetFolder = BuildTargetFolder(activeCase.CaseFolderPath, request.PersonName, request.Category, customFolder);
            Directory.CreateDirectory(targetFolder);

            if (isMerge)
            {
                var mergedFileName = EnsurePdfExtension(SafeFileName(request.MergedFileName));
                var storedFileName = GetUniqueFileName(targetFolder, mergedFileName);
                var targetPath = Path.Combine(targetFolder, storedFileName);

                DocumentsPdfProcessor.MergePdfs(request.SourceFilePaths, targetPath, request.PasswordProtectRequested ? request.Password : null);

                var record = new DocumentRecord
                {
                    DisplayName = Path.GetFileNameWithoutExtension(storedFileName),
                    OriginalFileName = string.Join(", ", request.SourceFilePaths.Select(Path.GetFileName)),
                    StoredFileName = storedFileName,
                    FullPath = targetPath,
                    RelativePath = Path.GetRelativePath(GetDocumentsRoot(activeCase.CaseFolderPath), targetPath),
                    PersonName = CleanFolderSegment(request.PersonName, "General"),
                    Category = CleanFolderSegment(request.Category, "Other"),
                    LinkedSection = string.IsNullOrWhiteSpace(request.LinkedSection) ? request.Category : request.LinkedSection,
                    LinkedRecordId = request.LinkedRecordId,
                    LinkedRecordName = request.LinkedRecordName,
                    CustomFolder = customFolder,
                    Tags = NormalizeTags(request.Tags),
                    Notes = request.Notes.Trim(),
                    IsMergedFile = true,
                    PasswordProtectedRequested = request.PasswordProtectRequested,
                    ImportBatchId = batchId,
                    IsActive = true
                };

                repository.Upsert(record);
                AppDataChangeNotifier.NotifyDocumentsChanged();
                statusMessage = "PDF merge import complete.";
                return true;
            }

            foreach (var sourceFilePath in request.SourceFilePaths)
            {
                if (!File.Exists(sourceFilePath))
                    continue;

                var sourceFile = new FileInfo(sourceFilePath);
                var storedFileName = GetUniqueFileName(targetFolder, sourceFile.Name);
                var targetPath = Path.Combine(targetFolder, storedFileName);

                if (request.PasswordProtectRequested && DocumentsPdfProcessor.IsPdf(sourceFile.FullName))
                    DocumentsPdfProcessor.CopyPdfWithOptionalPassword(sourceFile.FullName, targetPath, request.Password);
                else
                    File.Copy(sourceFile.FullName, targetPath, overwrite: false);

                var record = new DocumentRecord
                {
                    DisplayName = Path.GetFileNameWithoutExtension(sourceFile.Name),
                    OriginalFileName = sourceFile.Name,
                    StoredFileName = storedFileName,
                    FullPath = targetPath,
                    RelativePath = Path.GetRelativePath(GetDocumentsRoot(activeCase.CaseFolderPath), targetPath),
                    PersonName = CleanFolderSegment(request.PersonName, "General"),
                    Category = CleanFolderSegment(request.Category, "Other"),
                    LinkedSection = string.IsNullOrWhiteSpace(request.LinkedSection) ? request.Category : request.LinkedSection,
                    LinkedRecordId = request.LinkedRecordId,
                    LinkedRecordName = request.LinkedRecordName,
                    CustomFolder = customFolder,
                    Tags = NormalizeTags(request.Tags),
                    Notes = request.Notes.Trim(),
                    IsMergedFile = false,
                    PasswordProtectedRequested = request.PasswordProtectRequested && DocumentsPdfProcessor.IsPdf(sourceFile.FullName),
                    ImportBatchId = batchId,
                    IsActive = true
                };

                repository.Upsert(record);
            }

            AppDataChangeNotifier.NotifyDocumentsChanged();
            statusMessage = "Document import complete.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not import documents: {ex.Message}";
            return false;
        }
    }

    public bool SaveDocumentMetadata(DocumentRecord document, DocumentEditRequest request, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before editing document metadata.";
            return false;
        }

        if (document is null)
        {
            statusMessage = "No document was selected.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            statusMessage = "Enter a display name before saving.";
            return false;
        }

        try
        {
            var oldPath = document.FullPath;
            document.DisplayName = request.DisplayName.Trim();
            document.PersonName = CleanFolderSegment(request.PersonName, "General");
            document.Category = CleanFolderSegment(request.Category, "Other");
            document.LinkedSection = string.IsNullOrWhiteSpace(request.LinkedSection) ? document.Category : request.LinkedSection.Trim();
            document.LinkedRecordId = request.LinkedRecordId;
            document.LinkedRecordName = request.LinkedRecordName.Trim();
            document.CustomFolder = CleanFolderSegment(request.CustomFolder, string.Empty);
            document.Tags = NormalizeTags(request.Tags);
            document.Notes = request.Notes.Trim();

            var targetFolder = BuildTargetFolder(activeCase.CaseFolderPath, document.PersonName, document.Category, document.CustomFolder);
            Directory.CreateDirectory(targetFolder);

            var fileName = string.IsNullOrWhiteSpace(document.StoredFileName)
                ? Path.GetFileName(oldPath)
                : document.StoredFileName;

            var targetPath = Path.Combine(targetFolder, fileName);
            if (!string.Equals(oldPath, targetPath, StringComparison.OrdinalIgnoreCase) && File.Exists(oldPath))
            {
                targetPath = GetUniquePath(targetFolder, fileName);
                File.Move(oldPath, targetPath);
                document.StoredFileName = Path.GetFileName(targetPath);
                document.FullPath = targetPath;
                document.RelativePath = Path.GetRelativePath(GetDocumentsRoot(activeCase.CaseFolderPath), targetPath);
            }

            CreateRepository(activeCase.CaseFolderPath).Upsert(document);
            AppDataChangeNotifier.NotifyDocumentsChanged();
            statusMessage = "Document metadata saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save document metadata: {ex.Message}";
            return false;
        }
    }

    public bool RemoveDocument(long id, bool deleteFile, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing documents.";
            return false;
        }

        try
        {
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var document = repository.GetById(id);
            repository.Delete(id);

            if (deleteFile && document is not null && File.Exists(document.FullPath))
                File.Delete(document.FullPath);

            AppDataChangeNotifier.NotifyDocumentsChanged();
            statusMessage = "Document removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove document: {ex.Message}";
            return false;
        }
    }

    public bool OpenDocument(DocumentRecord document, out string statusMessage)
    {
        statusMessage = string.Empty;

        if (document is null || string.IsNullOrWhiteSpace(document.FullPath) || !File.Exists(document.FullPath))
        {
            statusMessage = "The document file could not be found.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = document.FullPath,
                UseShellExecute = true
            });

            statusMessage = "Document opened.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not open document: {ex.Message}";
            return false;
        }
    }

    public bool ShowInFileBrowser(DocumentRecord document, out string statusMessage)
    {
        statusMessage = string.Empty;

        if (document is null || string.IsNullOrWhiteSpace(document.FullPath))
        {
            statusMessage = "The document path could not be found.";
            return false;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{document.FullPath}\"",
                    UseShellExecute = true
                });
            }
            else
            {
                var folder = Path.GetDirectoryName(document.FullPath) ?? GetDocumentsRoot(_activeCaseState.ActiveCase?.CaseFolderPath ?? string.Empty);
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }

            statusMessage = "File location opened.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not show file location: {ex.Message}";
            return false;
        }
    }

    private string ResolveCustomFolder(string caseFolderPath, DocumentImportRequest request)
    {
        var customFolder = CleanFolderSegment(request.CustomFolder, string.Empty);
        if (string.IsNullOrWhiteSpace(customFolder))
            return string.Empty;

        var existingFolder = BuildTargetFolder(caseFolderPath, request.PersonName, request.Category, customFolder);
        if (!Directory.Exists(existingFolder) || request.UseExistingFolderWhenConflict)
            return customFolder;

        return $"{customFolder} - {DateTime.Now:yyyy-MM-dd}";
    }

    private static string GetDocumentsRoot(string caseFolderPath)
    {
        return Path.Combine(caseFolderPath, "Documents");
    }

    private static string BuildTargetFolder(string caseFolderPath, string personName, string category, string customFolder)
    {
        var folder = Path.Combine(
            GetDocumentsRoot(caseFolderPath),
            CleanFolderSegment(personName, "General"),
            CleanFolderSegment(category, "Other"));

        customFolder = CleanFolderSegment(customFolder, string.Empty);
        if (!string.IsNullOrWhiteSpace(customFolder))
            folder = Path.Combine(folder, customFolder);

        return folder;
    }

    private static string GetUniqueFileName(string folder, string fileName)
    {
        return Path.GetFileName(GetUniquePath(folder, fileName));
    }

    private static string GetUniquePath(string folder, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var path = Path.Combine(folder, fileName);
        var count = 1;

        while (File.Exists(path))
        {
            path = Path.Combine(folder, $"{baseName} ({count}){extension}");
            count++;
        }

        return path;
    }

    private static string EnsurePdfExtension(string fileName)
    {
        return string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{Path.GetFileNameWithoutExtension(fileName)}.pdf";
    }

    private static string SafeFileName(string fileName)
    {
        var cleaned = string.IsNullOrWhiteSpace(fileName) ? $"Merged PDF - {DateTime.Now:yyyy-MM-dd}" : fileName.Trim();

        foreach (var invalid in Path.GetInvalidFileNameChars())
            cleaned = cleaned.Replace(invalid, '-');

        return cleaned;
    }

    private static string CleanFolderSegment(string value, string fallback)
    {
        var cleaned = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        foreach (var invalid in Path.GetInvalidFileNameChars())
            cleaned = cleaned.Replace(invalid, '-');

        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }

    private static string NormalizeTags(string tags)
    {
        return string.Join(", ", SplitTags(tags));
    }

    private static IEnumerable<string> SplitTags(string tags)
    {
        return (tags ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag));
    }

    private DocumentsRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new DocumentsRepository(databasePath);
    }
}
