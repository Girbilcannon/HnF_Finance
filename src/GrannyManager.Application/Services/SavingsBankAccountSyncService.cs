using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GrannyManager.Core.Models;
using GrannyManager.Data.Repositories;

namespace GrannyManager.Application.Services;

public static class SavingsBankAccountSyncService
{
    private const string AutoLinkNote = "Auto-linked savings bank account. Amount is calculated from income deposits into this savings account.";

    public static bool Sync(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            return false;

        var changed = false;

        var assetsRepository = new AssetsRepository(databasePath);
        var incomeRepository = new IncomeSourcesRepository(databasePath);
        var allowanceSavingsRepository = new AllowanceSavingsRepository(databasePath);

        var assets = assetsRepository.GetAll().ToList();
        var incomeSources = incomeRepository.GetAll().Where(source => source.IsActive).ToList();
        var allowanceSavingsItems = allowanceSavingsRepository.GetAll().ToList();

        var savingsBankAccounts = assets
            .Where(IsSavingsBankAccount)
            .ToList();

        foreach (var account in savingsBankAccounts)
        {
            var monthlySavingsDeposit = CalculateMonthlySavingsDeposit(account, incomeSources);
            var existing = allowanceSavingsItems
                .Where(item => item.LinkedBankAssetId == account.Id)
                .Where(item => item.IsSavings)
                .FirstOrDefault(IsAutoLinkedSavingsRecord);

            if (existing is null)
            {
                existing = new AllowanceSavingsItem
                {
                    ItemName = BuildSavingsItemName(account),
                    ItemType = "Savings",
                    Frequency = "Monthly",
                    StorageMethod = "Select Bank Account",
                    WhereStored = account.DisplayName,
                    LinkedBankAssetId = account.Id,
                    LinkedBankAssetName = account.DisplayName,
                    IsActive = account.IsActive,
                    Notes = AutoLinkNote
                };
            }

            var newName = BuildSavingsItemName(account);
            var newLinkedName = account.DisplayName;
            var newWhereStored = account.DisplayName;
            var newAmount = monthlySavingsDeposit;
            var newIsActive = account.IsActive;

            if (!StringEquals(existing.ItemName, newName) ||
                !StringEquals(existing.ItemType, "Savings") ||
                !StringEquals(existing.Frequency, "Monthly") ||
                !StringEquals(existing.StorageMethod, "Select Bank Account") ||
                !StringEquals(existing.WhereStored, newWhereStored) ||
                !StringEquals(existing.LinkedBankAssetName, newLinkedName) ||
                existing.LinkedBankAssetId != account.Id ||
                existing.Amount != newAmount ||
                existing.IsActive != newIsActive ||
                !IsAutoLinkedSavingsRecord(existing))
            {
                existing.ItemName = newName;
                existing.ItemType = "Savings";
                existing.Amount = newAmount;
                existing.Frequency = "Monthly";
                existing.StorageMethod = "Select Bank Account";
                existing.WhereStored = newWhereStored;
                existing.LinkedBankAssetId = account.Id;
                existing.LinkedBankAssetName = newLinkedName;
                existing.IsActive = newIsActive;
                existing.Notes = AutoLinkNote;
                allowanceSavingsRepository.Upsert(existing);
                changed = true;
            }
        }

        var activeSavingsIds = savingsBankAccounts.Select(account => account.Id).ToHashSet();

        foreach (var item in allowanceSavingsItems.Where(item =>
                     item.LinkedBankAssetId > 0 &&
                     item.IsSavings &&
                     IsAutoLinkedSavingsRecord(item)))
        {
            if (activeSavingsIds.Contains(item.LinkedBankAssetId))
                continue;

            if (item.IsActive || item.Amount != 0m)
            {
                item.IsActive = false;
                item.Amount = 0m;
                allowanceSavingsRepository.Upsert(item);
                changed = true;
            }
        }

        return changed;
    }

    public static bool IsAutoLinkedSavingsRecord(AllowanceSavingsItem item)
    {
        return item is not null &&
               item.IsSavings &&
               item.Notes.Contains("Auto-linked savings bank account", StringComparison.OrdinalIgnoreCase);
    }

    public static decimal CalculateMonthlySavingsDeposit(AssetItem savingsAccount, IEnumerable<IncomeSource> activeIncomeSources)
    {
        var total = 0m;

        foreach (var source in activeIncomeSources)
        {
            var destination = source.DepositDestination?.Trim() ?? string.Empty;

            if (StringEquals(destination, "Select Bank Account") &&
                source.LinkedBankAssetId == savingsAccount.Id)
            {
                total += IncomeSource.CalculateMonthlyEquivalent(source.Amount, source.Frequency, source.IsActive);
                continue;
            }

            if (StringEquals(destination, "Select Multiple Bank Accounts"))
            {
                var splitAmount = TryFindSplitAmountForAccount(source.LinkedBankAssetName, savingsAccount);
                if (splitAmount > 0m)
                    total += IncomeSource.CalculateMonthlyEquivalent(splitAmount, source.Frequency, source.IsActive);
            }
        }

        return total;
    }

    private static decimal TryFindSplitAmountForAccount(string linkedBankAssetName, AssetItem account)
    {
        if (string.IsNullOrWhiteSpace(linkedBankAssetName))
            return 0m;

        var parts = linkedBankAssetName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var colonIndex = part.LastIndexOf(':');
            if (colonIndex < 0)
                continue;

            var name = part[..colonIndex].Trim();
            var amountText = part[(colonIndex + 1)..].Trim();

            if (!MatchesAccountName(name, account))
                continue;

            amountText = amountText.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
            if (decimal.TryParse(amountText, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out var currentParsed))
                return currentParsed;

            if (decimal.TryParse(amountText, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var invariantParsed))
                return invariantParsed;
        }

        return 0m;
    }

    private static bool IsSavingsBankAccount(AssetItem asset)
    {
        return asset.IsBankAccount &&
               asset.IsActive &&
               string.Equals(asset.AccountType?.Trim(), "Savings", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAccountName(string name, AssetItem account)
    {
        return StringEquals(name, account.AssetName) ||
               StringEquals(name, account.DisplayName) ||
               (!string.IsNullOrWhiteSpace(account.AccountLastFour) && name.Contains(account.AccountLastFour.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildSavingsItemName(AssetItem account)
    {
        return $"Savings - {account.DisplayName}";
    }

    private static bool StringEquals(string? left, string? right)
    {
        return string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
