using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Repositories;

namespace GrannyManager.App.Services;

public sealed class CaseSearchResult
{
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public AppPageKey PageKey { get; init; }
    public long RecordId { get; init; }
}

public sealed class CaseSearchService
{
    public IReadOnlyList<CaseSearchResult> Search(CaseProfile? activeCase, string query)
    {
        if (activeCase is null || string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<CaseSearchResult>();
        }

        var databasePath = Path.Combine(activeCase.CaseFolderPath, "data.db");
        if (!System.IO.File.Exists(databasePath))
        {
            return Array.Empty<CaseSearchResult>();
        }

        var results = new List<CaseSearchResult>();
        var q = query.Trim();

        TryAddPeople(results, databasePath, q);
        TryAddIncome(results, databasePath, q);
        TryAddBills(results, databasePath, q);
        TryAddAllowanceSavings(results, databasePath, q);
        TryAddAssets(results, databasePath, q);
        TryAddDebts(results, databasePath, q);
        TryAddDocuments(results, databasePath, q);
        TryAddCredentials(results, databasePath, q);

        return results
            .OrderBy(r => CategoryOrder(r.Category))
            .ThenBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase)
            .Take(80)
            .ToList();
    }

    private static void TryAddPeople(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var person in new HouseholdPeopleRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Name", person.FullName),
                    ("Relationship", person.Relationship),
                    ("Role", person.Role),
                    ("Contribution", person.LinkedIncomeSourceName),
                    ("Notes", person.Notes));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "People",
                    Title = string.IsNullOrWhiteSpace(person.FullName) ? "Unnamed person" : person.FullName,
                    Context = context,
                    PageKey = AppPageKey.People,
                    RecordId = person.Id
                });
            }
        }
        catch { }
    }

    private static void TryAddIncome(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new IncomeSourcesRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Source", item.SourceName),
                    ("Type", item.IncomeType),
                    ("Frequency", item.Frequency),
                    ("Deposit", item.DepositDisplayText),
                    ("Expected", item.ExpectedDayOrDate),
                    ("Notes", item.Notes));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "Income Sources",
                    Title = item.SourceName,
                    Context = $"{context} · {item.MonthlyEquivalentText}/mo",
                    PageKey = AppPageKey.Income,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }

    private static void TryAddBills(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new BillsRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Bill", item.BillName),
                    ("Category", item.Category),
                    ("Frequency", item.Frequency),
                    ("Paid by", item.PaidBy),
                    ("Owner", item.ResponsibilityOwner),
                    ("Priority", item.Priority),
                    ("Notes", item.Notes));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "Bills / Spending",
                    Title = item.BillName,
                    Context = $"{context} · {item.MonthlyEquivalentText}/mo",
                    PageKey = AppPageKey.Bills,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }

    private static void TryAddAllowanceSavings(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new AllowanceSavingsRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Name", item.ItemName),
                    ("Type", item.ItemType),
                    ("Frequency", item.Frequency),
                    ("Where", string.IsNullOrWhiteSpace(item.LinkedBankAssetName) ? item.WhereStored : item.LinkedBankAssetName),
                    ("Notes", item.Notes));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "Allowance / Savings",
                    Title = item.ItemName,
                    Context = $"{context} · {item.GetMonthlyEquivalent():C2}/mo",
                    PageKey = AppPageKey.AllowanceSavings,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }

    private static void TryAddAssets(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new AssetsRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Asset", item.AssetName),
                    ("Type", item.AssetType),
                    ("Owner", item.Owner),
                    ("Status", item.Status),
                    ("Location", item.LocationOrInstitution),
                    ("Vehicle", $"{item.VehicleYear} {item.VehicleMake} {item.VehicleModel} {item.VehicleVin} {item.VehiclePlate}"),
                    ("Property", $"{item.PropertyType} {item.PropertyAddress} {item.Occupants}"),
                    ("Bank", $"{item.InstitutionName} {item.AccountNickname}"),
                    ("Item", $"{item.ValuableDescription} {item.SerialOrIdentifier} {item.StorageLocation}"),
                    ("Bill", item.LinkedBillName),
                    ("Income", item.LinkedIncomeSourceName),
                    ("Notes", item.Notes));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "Assets",
                    Title = item.AssetName,
                    Context = $"{context} · {item.AssetType}",
                    PageKey = AppPageKey.Assets,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }

    private static void TryAddDebts(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new DebtsRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Debt", item.DebtName),
                    ("Type", item.DebtType),
                    ("Creditor", item.CreditorCollector),
                    ("Owner", item.ResponsibilityOwner),
                    ("Paid by", item.PaidBy),
                    ("Bill", item.LinkedBillName),
                    ("Status", item.Status),
                    ("Priority", item.Priority),
                    ("Notes", item.Notes));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "Debts",
                    Title = item.DebtName,
                    Context = $"{context} · Balance: {item.BalanceText}",
                    PageKey = AppPageKey.Debts,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }

    private static void TryAddDocuments(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new DocumentsRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Title", item.Title),
                    ("Category", item.Category),
                    ("Tags", item.Tags),
                    ("File", item.FileNameDisplay),
                    ("Linked", item.LinkedDisplay),
                    ("Notes", item.Notes));

                if (context is null)
                    continue;

                var important = item.IsImportant ? " · Important" : string.Empty;
                results.Add(new CaseSearchResult
                {
                    Category = "Documents",
                    Title = item.Title,
                    Context = $"{context}{important}",
                    PageKey = AppPageKey.Documents,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }


    private static void TryAddCredentials(List<CaseSearchResult> results, string databasePath, string query)
    {
        try
        {
            foreach (var item in new CredentialVaultRepository(databasePath).GetAll())
            {
                var context = FirstMatchingContext(query,
                    ("Account", item.AccountName),
                    ("Website", item.WebsiteUrl),
                    ("Linked", item.LinkedDisplay));

                if (context is null)
                    continue;

                results.Add(new CaseSearchResult
                {
                    Category = "Credential Vault",
                    Title = item.AccountName,
                    Context = context,
                    PageKey = AppPageKey.Vault,
                    RecordId = item.Id
                });
            }
        }
        catch { }
    }

    private static string? FirstMatchingContext(string query, params (string Label, string? Value)[] fields)
    {
        foreach (var (label, value) in fields)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (value.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                return $"{label}: {TrimContext(value, query)}";
        }

        return null;
    }

    private static string TrimContext(string value, string query)
    {
        value = value.Replace("\r", " ").Replace("\n", " ").Trim();
        if (value.Length <= 110)
            return value;

        var index = value.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        if (index < 0)
            return value[..107] + "...";

        var start = Math.Max(0, index - 35);
        var length = Math.Min(value.Length - start, 105);
        var snippet = value.Substring(start, length).Trim();
        if (start > 0)
            snippet = "..." + snippet;
        if (start + length < value.Length)
            snippet += "...";
        return snippet;
    }

    private static int CategoryOrder(string category)
    {
        return category switch
        {
            "People" => 0,
            "Income Sources" => 1,
            "Bills / Spending" => 2,
            "Allowance / Savings" => 3,
            "Assets" => 4,
            "Debts" => 5,
            "Documents" => 6,
            "Credential Vault" => 7,
            _ => 99
        };
    }
}
