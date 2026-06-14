using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class AssetsService
{
    private readonly ActiveCaseState _activeCaseState;

    public AssetsService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public AssetsLoadResult LoadAssets()
    {
        var activeCase = _activeCaseState.ActiveCase;

        if (activeCase is null || !activeCase.IsValid)
        {
            return new AssetsLoadResult(
                HasActiveCase: false,
                StatusMessage: "No active case is open. Create or open a case before adding assets.",
                Assets: Array.Empty<AssetItem>());
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var repository = new AssetsRepository(databasePath);
            var assets = repository.GetAll();

            return new AssetsLoadResult(
                HasActiveCase: true,
                StatusMessage: assets.Count == 0
                    ? "No assets have been added to this case yet."
                    : $"{assets.Count} asset record(s) loaded.",
                Assets: assets);
        }
        catch (Exception ex)
        {
            return new AssetsLoadResult(
                HasActiveCase: true,
                StatusMessage: $"Could not load assets: {ex.Message}",
                Assets: Array.Empty<AssetItem>());
        }
    }
}
