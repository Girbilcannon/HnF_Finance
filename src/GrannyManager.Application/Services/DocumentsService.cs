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
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding documents.",
                Documents: Array.Empty<DocumentRecord>());
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            EnsureCaseDocumentsFolder(activeCase.CaseFolderPath);

            var repository = new DocumentsRepository(databasePath);
            var documents = repository.GetAll();

            return new DocumentsLoadResult(
                HasActiveCase: true,
                StatusMessage: documents.Count == 0
                    ? "No documents have been added to this case yet."
                    : $"{documents.Count} document record(s) loaded.",
                Documents: documents);
        }
        catch (Exception ex)
        {
            return new DocumentsLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load documents: {ex.Message}",
                Documents: Array.Empty<DocumentRecord>());
        }
    }

    public static string EnsureCaseDocumentsFolder(string caseFolderPath)
    {
        var folder = Path.Combine(caseFolderPath, "documents");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
