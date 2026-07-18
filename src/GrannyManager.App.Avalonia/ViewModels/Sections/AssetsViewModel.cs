using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class AssetsViewModel : ViewModelBase
{
    private readonly AssetsService _assetsService;

    public AssetsViewModel(ActiveCaseState activeCaseState, AssetsService assetsService)
    {
        _assetsService = assetsService ?? throw new ArgumentNullException(nameof(assetsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadAssets();

        AppDataChangeNotifier.AssetsChanged += (_, _) => LoadAssets();

        LoadAssets();
    }

    public ObservableCollection<AssetRowViewModel> Assets { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAsset))]
    [NotifyPropertyChangedFor(nameof(CanEditAsset))]
    [NotifyPropertyChangedFor(nameof(CanRemoveAsset))]
    [NotifyPropertyChangedFor(nameof(SelectedAssetName))]
    [NotifyPropertyChangedFor(nameof(SelectedAssetType))]
    [NotifyPropertyChangedFor(nameof(SelectedEstimatedValue))]
    [NotifyPropertyChangedFor(nameof(SelectedInstitution))]
    [NotifyPropertyChangedFor(nameof(SelectedAccountType))]
    [NotifyPropertyChangedFor(nameof(SelectedAccountLastFour))]
    [NotifyPropertyChangedFor(nameof(SelectedStatus))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private AssetRowViewModel? _selectedAsset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddAsset))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedAsset => SelectedAsset is not null;
    public bool CanAddAsset => HasActiveCase;
    public bool CanEditAsset => HasActiveCase && HasSelectedAsset;
    public bool CanRemoveAsset => HasActiveCase && HasSelectedAsset;

    public string SelectedAssetName => SelectedAsset?.Asset.AssetName ?? "No asset selected";
    public string SelectedAssetType => Clean(SelectedAsset?.Asset.AssetType);
    public string SelectedEstimatedValue => SelectedAsset?.Asset.EstimatedValueText ?? "$0.00";
    public string SelectedInstitution => Clean(SelectedAsset?.Asset.InstitutionName);
    public string SelectedAccountType => Clean(SelectedAsset?.Asset.AccountType);
    public string SelectedAccountLastFour => Clean(SelectedAsset?.Asset.AccountLastFour);
    public string SelectedStatus => SelectedAsset?.Asset.IsActive == true ? "Active" : "Inactive";
    public string SelectedNotes => Clean(SelectedAsset?.Asset.Notes);
    public string SelectedCreatedUtc => FormatDate(SelectedAsset?.Asset.CreatedUtc);
    public string SelectedUpdatedUtc => FormatDate(SelectedAsset?.Asset.UpdatedUtc);

    partial void OnSelectedAssetChanged(AssetRowViewModel? oldValue, AssetRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public void RefreshFromNavigation() => LoadAssets();

    public AssetItem CreateBlankAsset()
    {
        return new AssetItem
        {
            AssetType = "Bank Account",
            AccountType = "Checking",
            IsActive = true
        };
    }

    public AssetItem? CreateEditableCopyOfSelectedAsset()
    {
        var asset = SelectedAsset?.Asset;
        if (asset is null)
            return null;

        return new AssetItem
        {
            Id = asset.Id,
            AssetName = asset.AssetName,
            AssetType = asset.AssetType,
            EstimatedValue = asset.EstimatedValue,
            InstitutionName = asset.InstitutionName,
            AccountType = asset.AccountType,
            AccountLastFour = asset.AccountLastFour,
            LinkedIncomeSourceName = asset.LinkedIncomeSourceName,
            LinkedBillName = asset.LinkedBillName,
            IsActive = asset.IsActive,
            Notes = asset.Notes,
            CreatedUtc = asset.CreatedUtc,
            UpdatedUtc = asset.UpdatedUtc
        };
    }

    public bool SaveAsset(AssetItem asset)
    {
        if (!_assetsService.SaveAsset(asset, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadAssets();
        SelectedAsset = Assets.FirstOrDefault(row => row.Asset.Id == asset.Id) ?? Assets.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedAsset()
    {
        var selectedId = SelectedAsset?.Asset.Id ?? 0;
        if (!_assetsService.DeleteAsset(selectedId, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadAssets();
        StatusMessage = message;
        return true;
    }

    private void LoadAssets()
    {
        var selectedId = SelectedAsset?.Asset.Id ?? 0;
        var result = _assetsService.LoadAssets();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Assets.Clear();
        var index = 0;
        foreach (var asset in result.Assets)
            Assets.Add(new AssetRowViewModel(asset, index++));

        SelectedAsset = Assets.FirstOrDefault(row => row.Asset.Id == selectedId) ?? Assets.FirstOrDefault();
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string FormatDate(DateTime? value) => value is null || value.Value == default ? "Not saved" : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}

public sealed partial class AssetRowViewModel : ObservableObject
{
    public AssetRowViewModel(AssetItem asset, int index)
    {
        Asset = asset ?? throw new ArgumentNullException(nameof(asset));
        Index = index;
    }

    public AssetItem Asset { get; }
    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string AssetName => Asset.DisplayName;
    public string AssetType => string.IsNullOrWhiteSpace(Asset.AssetType) ? "Other" : Asset.AssetType.Trim();
    public string EstimatedValue => Asset.EstimatedValueText;
    public string Institution => string.IsNullOrWhiteSpace(Asset.InstitutionName) ? "None" : Asset.InstitutionName.Trim();
    public bool IsInactive => !Asset.IsActive;
    public string NameForeground => IsInactive ? "#7D8795" : "White";
    public string DetailForeground => IsInactive ? "#707A88" : "#C8D4E2";
}
