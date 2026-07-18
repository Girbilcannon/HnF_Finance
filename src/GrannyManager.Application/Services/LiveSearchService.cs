using System;
using System.Collections.Generic;
using System.Linq;
using GrannyManager.Application.State;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public sealed class LiveSearchService
{
    private readonly ActiveCaseState _activeCaseState;

    public LiveSearchService(ActiveCaseState activeCaseState)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
    }

    public IReadOnlyList<LiveSearchResult> Search(string query)
    {
        query = (query ?? string.Empty).Trim();

        if (query.Length < 2)
            return Array.Empty<LiveSearchResult>();

        var activeCase = _activeCaseState.ActiveCase;
        if (activeCase is null || !activeCase.IsValid)
            return Array.Empty<LiveSearchResult>();

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var results = new List<LiveSearchResult>();

            AddHouseholdResults(results, databasePath, query);
            AddIncomeResults(results, databasePath, query);
            AddBillResults(results, databasePath, query);
            AddAllowanceSavingsResults(results, databasePath, query);
            AddAssetResults(results, databasePath, query);
            AddDebtResults(results, databasePath, query);
            AddDocumentResults(results, databasePath, query);

            return results
                .OrderBy(result => result.Section)
                .ThenBy(result => result.Title)
                .Take(40)
                .ToList();
        }
        catch
        {
            return Array.Empty<LiveSearchResult>();
        }
    }

    private static void AddHouseholdResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var person in new HouseholdPeopleRepository(databasePath).GetAll())
        {
            if (!Matches(query, person.FullName, person.Relationship, person.Role, person.Notes))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Household",
                Title = person.FullName,
                Detail = $"{person.Relationship} • {person.Role}",
                NavigateSection = "Household",
                TargetId = person.Id
            });
        }
    }

    private static void AddIncomeResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var item in new IncomeSourcesRepository(databasePath).GetAll())
        {
            if (!Matches(query, item.SourceName, item.IncomeType, item.LinkedHouseholdPersonName, item.DepositDestination, item.Notes))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Income",
                Title = item.SourceName,
                Detail = $"{item.IncomeType} • {item.MonthlyEquivalentText}",
                NavigateSection = "Income",
                TargetId = item.Id
            });
        }
    }

    private static void AddBillResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var item in new BillsRepository(databasePath).GetAll())
        {
            if (!Matches(query, item.BillName, item.Category, item.PaymentMethod, item.PaidBy, item.ResponsibilityOwner, item.LinkedBankAssetName, item.LinkedDebtName, item.Notes))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Bills",
                Title = item.BillName,
                Detail = $"{item.Category} • {item.MonthlyEquivalentText}",
                NavigateSection = "Bills",
                TargetId = item.Id
            });
        }
    }

    private static void AddAllowanceSavingsResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var item in new AllowanceSavingsRepository(databasePath).GetAll())
        {
            if (!Matches(query, item.ItemName, item.ItemType, item.WhereStored, item.StorageMethod, item.LinkedBankAssetName, item.Notes))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Allowance / Savings",
                Title = item.ItemName,
                Detail = $"{item.ItemType} • {item.MonthlyEquivalentText}",
                NavigateSection = "AllowanceSavings",
                TargetId = item.Id
            });
        }
    }

    private static void AddAssetResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var item in new AssetsRepository(databasePath).GetAll())
        {
            if (!Matches(query, item.DisplayName, item.AssetType, item.InstitutionName, item.AccountType, item.AccountLastFour, item.Notes))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Assets",
                Title = item.DisplayName,
                Detail = $"{item.AssetType} • {item.EstimatedValueText}",
                NavigateSection = "Assets",
                TargetId = item.Id
            });
        }
    }

    private static void AddDebtResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var item in new DebtsRepository(databasePath).GetAll())
        {
            if (!Matches(query, item.DebtName, item.DebtType, item.CreditorCollector, item.Status, item.PaidBy, item.ResponsibilityOwner, item.LinkedBillName, item.Notes))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Debts",
                Title = item.DebtName,
                Detail = $"{item.DebtType} • {item.BalanceText}",
                NavigateSection = "Debts",
                TargetId = item.Id
            });
        }
    }

    private static void AddDocumentResults(List<LiveSearchResult> results, string databasePath, string query)
    {
        foreach (var item in new DocumentsRepository(databasePath).GetAll())
        {
            if (!Matches(query, item.DisplayName, item.OriginalFileName, item.PersonName, item.Category, item.LinkedRecordName, item.Tags, item.Notes, item.RelativePath))
                continue;

            results.Add(new LiveSearchResult
            {
                Section = "Documents",
                Title = item.DisplayName,
                Detail = $"{item.FolderDisplay} • {item.TagDisplay}",
                NavigateSection = "Documents",
                TargetId = item.Id
            });
        }
    }

    private static bool Matches(string query, params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                value.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
