using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class AllowanceSavingsViewModel : ViewModelBase
{
    private readonly AllowanceSavingsService _allowanceSavingsService;

    public AllowanceSavingsViewModel(ActiveCaseState activeCaseState, AllowanceSavingsService allowanceSavingsService)
    {
        _allowanceSavingsService = allowanceSavingsService ?? throw new ArgumentNullException(nameof(allowanceSavingsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadItems();

        AppDataChangeNotifier.AllowanceSavingsChanged += (_, _) => LoadItems();

        LoadItems();
    }

    public ObservableCollection<AllowanceSavingsRowViewModel> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
    [NotifyPropertyChangedFor(nameof(CanEditItem))]
    [NotifyPropertyChangedFor(nameof(CanRemoveItem))]
    [NotifyPropertyChangedFor(nameof(SelectedItemName))]
    [NotifyPropertyChangedFor(nameof(SelectedType))]
    [NotifyPropertyChangedFor(nameof(SelectedStatus))]
    [NotifyPropertyChangedFor(nameof(SelectedAmount))]
    [NotifyPropertyChangedFor(nameof(SelectedFrequency))]
    [NotifyPropertyChangedFor(nameof(SelectedMonthlyEquivalent))]
    [NotifyPropertyChangedFor(nameof(SelectedWhereStored))]
    [NotifyPropertyChangedFor(nameof(SelectedStorageMethod))]
    [NotifyPropertyChangedFor(nameof(SelectedLinkedBankAsset))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private AllowanceSavingsRowViewModel? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddItem))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedItem => SelectedItem is not null;
    public bool CanAddItem => HasActiveCase;
    public bool CanEditItem => HasActiveCase && HasSelectedItem;
    public bool CanRemoveItem => HasActiveCase && HasSelectedItem;

    public string SelectedItemName => SelectedItem?.Item.ItemName ?? "No item selected";
    public string SelectedType => Clean(SelectedItem?.Item.ItemType);
    public string SelectedStatus => SelectedItem?.Item.IsActive == true ? "Active" : "Inactive";
    public string SelectedAmount => SelectedItem?.Item.AmountText ?? "$0.00";
    public string SelectedFrequency => Clean(SelectedItem?.Item.Frequency);
    public string SelectedMonthlyEquivalent => SelectedItem?.Item.MonthlyEquivalentText ?? "$0.00";
    public string SelectedWhereStored => Clean(SelectedItem?.Item.WhereStored);
    public string SelectedStorageMethod => Clean(SelectedItem?.Item.StorageMethod);
    public string SelectedLinkedBankAsset
    {
        get
        {
            var item = SelectedItem?.Item;
            if (item is null || item.LinkedBankAssetId <= 0)
                return "None";

            return string.IsNullOrWhiteSpace(item.LinkedBankAssetName)
                ? "Linked bank account"
                : item.LinkedBankAssetName.Trim();
        }
    }

    public string SelectedNotes => Clean(SelectedItem?.Item.Notes);
    public string SelectedCreatedUtc => FormatDate(SelectedItem?.Item.CreatedUtc);
    public string SelectedUpdatedUtc => FormatDate(SelectedItem?.Item.UpdatedUtc);

    partial void OnSelectedItemChanged(AllowanceSavingsRowViewModel? oldValue, AllowanceSavingsRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public void RefreshFromNavigation()
    {
        LoadItems();
    }

    public AllowanceSavingsItem CreateBlankItem()
    {
        return new AllowanceSavingsItem
        {
            ItemType = "Allowance",
            Frequency = "Monthly",
            StorageMethod = "Cash / Envelope",
            IsActive = true
        };
    }

    public AllowanceSavingsItem? CreateEditableCopyOfSelectedItem()
    {
        var item = SelectedItem?.Item;
        if (item is null)
            return null;

        return new AllowanceSavingsItem
        {
            Id = item.Id,
            ItemName = item.ItemName,
            ItemType = item.ItemType,
            Amount = item.Amount,
            Frequency = item.Frequency,
            WhereStored = item.WhereStored,
            StorageMethod = item.StorageMethod,
            LinkedBankAssetId = item.LinkedBankAssetId,
            LinkedBankAssetName = item.LinkedBankAssetName,
            IsActive = item.IsActive,
            Notes = item.Notes,
            CreatedUtc = item.CreatedUtc,
            UpdatedUtc = item.UpdatedUtc
        };
    }

    public bool SaveItem(AllowanceSavingsItem item)
    {
        if (!_allowanceSavingsService.SaveItem(item, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadItems();
        SelectedItem = Items.FirstOrDefault(row => row.Item.Id == item.Id) ?? Items.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedItem()
    {
        var selectedId = SelectedItem?.Item.Id ?? 0;
        if (!_allowanceSavingsService.DeleteItem(selectedId, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadItems();
        StatusMessage = message;
        return true;
    }

    private void LoadItems()
    {
        var selectedId = SelectedItem?.Item.Id ?? 0;
        var result = _allowanceSavingsService.LoadItems();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Items.Clear();

        var index = 0;
        foreach (var item in result.Items)
        {
            Items.Add(new AllowanceSavingsRowViewModel(item, index));
            index++;
        }

        SelectedItem = Items.FirstOrDefault(row => row.Item.Id == selectedId) ?? Items.FirstOrDefault();
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string FormatDate(DateTime? value) => value is null || value.Value == default ? "Not saved" : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}

public sealed partial class AllowanceSavingsRowViewModel : ObservableObject
{
    public AllowanceSavingsRowViewModel(AllowanceSavingsItem item, int index)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Index = index;
    }

    public AllowanceSavingsItem Item { get; }
    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string ItemName => string.IsNullOrWhiteSpace(Item.ItemName) ? "Unnamed Item" : Item.ItemName.Trim();
    public string ItemType => string.IsNullOrWhiteSpace(Item.ItemType) ? "Allowance" : Item.ItemType.Trim();
    public string MonthlyEquivalent => Item.MonthlyEquivalentText;
    public string WhereStored => string.IsNullOrWhiteSpace(Item.WhereStored) ? "None" : Item.WhereStored.Trim();
    public bool IsInactive => !Item.IsActive;
    public string NameForeground => IsInactive ? "#7D8795" : "White";
    public string DetailForeground => IsInactive ? "#707A88" : "#C8D4E2";
}
