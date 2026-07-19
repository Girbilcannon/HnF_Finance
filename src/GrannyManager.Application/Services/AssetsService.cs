using System;
using System.Collections.Generic;
using System.Linq;
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
            var repository = CreateRepository(activeCase.CaseFolderPath);
            var assets = repository.GetAll()
                .OrderBy(asset => asset.IsActive ? 0 : 1)
                .ThenBy(asset => asset.AssetType, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(asset => asset.AssetName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new AssetsLoadResult(
                HasActiveCase: true,
                StatusMessage: assets.Count == 0 ? "No assets have been added to this case yet." : $"{assets.Count} asset(s) loaded.",
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

    public IReadOnlyList<AssetItem> LoadBankAccounts()
    {
        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<AssetItem>();

        try
        {
            return CreateRepository(activeCase.CaseFolderPath).GetBankAccounts();
        }
        catch
        {
            return Array.Empty<AssetItem>();
        }
    }

    public bool SaveAsset(AssetItem asset, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before saving assets.";
            return false;
        }

        if (asset is null)
        {
            statusMessage = "No asset was provided.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(asset.AssetName))
        {
            statusMessage = "Enter an asset name before saving.";
            return false;
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            new AssetsRepository(databasePath).Upsert(asset);
            SavingsBankAccountSyncService.Sync(databasePath);
            AppDataChangeNotifier.NotifyAssetsChanged();
            AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
            statusMessage = "Asset saved.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not save asset: {ex.Message}";
            return false;
        }
    }

    public bool DeleteAsset(long id, out string statusMessage)
    {
        statusMessage = string.Empty;

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
        {
            statusMessage = "Open a case before removing assets.";
            return false;
        }

        if (id <= 0)
        {
            statusMessage = "Select an asset before removing.";
            return false;
        }

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            new AssetsRepository(databasePath).Delete(id);
            SavingsBankAccountSyncService.Sync(databasePath);
            AppDataChangeNotifier.NotifyAssetsChanged();
            AppDataChangeNotifier.NotifyAllowanceSavingsChanged();
            statusMessage = "Asset removed.";
            return true;
        }
        catch (Exception ex)
        {
            statusMessage = $"Could not remove asset: {ex.Message}";
            return false;
        }
    }

    private static AssetsRepository CreateRepository(string caseFolderPath)
    {
        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(caseFolderPath);
        return new AssetsRepository(databasePath);
    }
}
