using System;
using System.Linq;
using System.Collections.ObjectModel;
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

        LoadItems();
    }

    public ObservableCollection<AllowanceSavingsListItemViewModel> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItem))]
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
    private AllowanceSavingsListItemViewModel? _selectedItem;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    public bool HasSelectedItem => SelectedItem is not null;

    public string SelectedItemName => SelectedItem?.Item.ItemName ?? "No item selected";
    public string SelectedType => Clean(SelectedItem?.Item.ItemType);
    public string SelectedStatus => SelectedItem?.Item.IsActive == true ? "Active" : "Inactive";
    public string SelectedAmount => FormatMoney(SelectedItem?.Item.Amount);
    public string SelectedFrequency => Clean(SelectedItem?.Item.Frequency);
    public string SelectedMonthlyEquivalent => FormatMoney(SelectedItem?.Item.GetMonthlyEquivalent());
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
                ? "Linked bank asset"
                : item.LinkedBankAssetName.Trim();
        }
    }

    public string SelectedNotes => Clean(SelectedItem?.Item.Notes);
    public string SelectedCreatedUtc => FormatUtc(SelectedItem?.Item.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(SelectedItem?.Item.UpdatedUtc);

    partial void OnSelectedItemChanged(AllowanceSavingsListItemViewModel? oldValue, AllowanceSavingsListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    private void LoadItems()
    {
        var result = _allowanceSavingsService.LoadItems();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Items.Clear();

        var index = 0;
        foreach (var item in result.Items)
        {
            Items.Add(new AllowanceSavingsListItemViewModel(item, index));
            index++;
        }

        SelectedItem = Items.FirstOrDefault();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string FormatMoney(decimal? value)
    {
        return (value ?? 0m).ToString("C2");
    }

    private static string FormatUtc(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "Not saved";

        return value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

public sealed partial class AllowanceSavingsListItemViewModel : ObservableObject
{
    public AllowanceSavingsListItemViewModel(AllowanceSavingsItem item, int index)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Index = index;
    }

    public AllowanceSavingsItem Item { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(MutedForeground))]
    private bool _isSelected;

    public string ItemName => string.IsNullOrWhiteSpace(Item.ItemName) ? "Unnamed Item" : Item.ItemName.Trim();
    public string ItemType => string.IsNullOrWhiteSpace(Item.ItemType) ? "Allowance" : Item.ItemType.Trim();
    public string MonthlyEquivalent => Item.GetMonthlyEquivalent().ToString("C2");
    public string WhereStored => string.IsNullOrWhiteSpace(Item.WhereStored) ? "None" : Item.WhereStored.Trim();

    public bool IsInactive => !Item.IsActive;

    public string RowBackground
    {
        get
        {
            if (IsSelected)
                return "#2A6FA8";

            if (IsInactive)
                return "#1A1F29";

            return Index % 2 == 0 ? "#122238" : "#0F1B2A";
        }
    }

    public string RowForeground
    {
        get
        {
            if (IsSelected)
                return "White";

            return IsInactive ? "#7D8795" : "#DDE7F3";
        }
    }

    public string MutedForeground => IsSelected ? "White" : IsInactive ? "#707A88" : "#C8D4E2";
}
