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

        LoadAssets();
    }

    public ObservableCollection<AssetListItemViewModel> Assets { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedAsset))]
    [NotifyPropertyChangedFor(nameof(SelectedAssetName))]
    [NotifyPropertyChangedFor(nameof(SelectedAssetType))]
    [NotifyPropertyChangedFor(nameof(SelectedOwner))]
    [NotifyPropertyChangedFor(nameof(SelectedEstimatedValue))]
    [NotifyPropertyChangedFor(nameof(SelectedStatus))]
    [NotifyPropertyChangedFor(nameof(SelectedActive))]
    [NotifyPropertyChangedFor(nameof(SelectedLocationOrInstitution))]
    [NotifyPropertyChangedFor(nameof(IsVehicleSelected))]
    [NotifyPropertyChangedFor(nameof(IsPropertySelected))]
    [NotifyPropertyChangedFor(nameof(IsBankSelected))]
    [NotifyPropertyChangedFor(nameof(IsValuableItemSelected))]
    [NotifyPropertyChangedFor(nameof(IsOtherSelected))]
    [NotifyPropertyChangedFor(nameof(SelectedVehicleYear))]
    [NotifyPropertyChangedFor(nameof(SelectedVehicleMake))]
    [NotifyPropertyChangedFor(nameof(SelectedVehicleModel))]
    [NotifyPropertyChangedFor(nameof(SelectedVehicleVin))]
    [NotifyPropertyChangedFor(nameof(SelectedVehiclePlate))]
    [NotifyPropertyChangedFor(nameof(SelectedRegistrationStatus))]
    [NotifyPropertyChangedFor(nameof(SelectedRegistrationDueDate))]
    [NotifyPropertyChangedFor(nameof(SelectedMileage))]
    [NotifyPropertyChangedFor(nameof(SelectedMpg))]
    [NotifyPropertyChangedFor(nameof(SelectedPrimaryDriver))]
    [NotifyPropertyChangedFor(nameof(SelectedPropertyType))]
    [NotifyPropertyChangedFor(nameof(SelectedPropertyAddress))]
    [NotifyPropertyChangedFor(nameof(SelectedOccupants))]
    [NotifyPropertyChangedFor(nameof(SelectedHoaOrManagement))]
    [NotifyPropertyChangedFor(nameof(SelectedInstitutionName))]
    [NotifyPropertyChangedFor(nameof(SelectedAccountNickname))]
    [NotifyPropertyChangedFor(nameof(SelectedCurrentBalance))]
    [NotifyPropertyChangedFor(nameof(SelectedValuableDescription))]
    [NotifyPropertyChangedFor(nameof(SelectedSerialOrIdentifier))]
    [NotifyPropertyChangedFor(nameof(SelectedStorageLocation))]
    [NotifyPropertyChangedFor(nameof(SelectedOtherDetails))]
    [NotifyPropertyChangedFor(nameof(SelectedLinkedBill))]
    [NotifyPropertyChangedFor(nameof(SelectedLinkedIncome))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private AssetListItemViewModel? _selectedAsset;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    public bool HasSelectedAsset => SelectedAsset is not null;

    private AssetItem? Asset => SelectedAsset?.Asset;

    public string SelectedAssetName => Asset?.AssetName ?? "No asset selected";
    public string SelectedAssetType => Clean(Asset?.AssetType);
    public string SelectedOwner => Clean(Asset?.Owner);
    public string SelectedEstimatedValue => Asset?.ValueDisplay ?? "Unknown";
    public string SelectedStatus => Clean(Asset?.Status);
    public string SelectedActive => YesNo(Asset?.IsActive);
    public string SelectedLocationOrInstitution => Clean(Asset?.LocationOrInstitution);

    public bool IsVehicleSelected => IsType("Vehicle");
    public bool IsPropertySelected => IsType("Property");
    public bool IsBankSelected => IsType("Bank");
    public bool IsValuableItemSelected => IsType("Valuable Item");
    public bool IsOtherSelected => HasSelectedAsset && !IsVehicleSelected && !IsPropertySelected && !IsBankSelected && !IsValuableItemSelected;

    public string SelectedVehicleYear => Clean(Asset?.VehicleYear);
    public string SelectedVehicleMake => Clean(Asset?.VehicleMake);
    public string SelectedVehicleModel => Clean(Asset?.VehicleModel);
    public string SelectedVehicleVin => Clean(Asset?.VehicleVin);
    public string SelectedVehiclePlate => Clean(Asset?.VehiclePlate);
    public string SelectedRegistrationStatus => Clean(Asset?.RegistrationStatus);
    public string SelectedRegistrationDueDate => Clean(Asset?.RegistrationDueDate);
    public string SelectedMileage => Asset?.MileageDisplay ?? "Not entered";
    public string SelectedMpg => Asset?.MpgDisplay ?? "Not entered";
    public string SelectedPrimaryDriver => Clean(Asset?.PrimaryDriver);

    public string SelectedPropertyType => Clean(Asset?.PropertyType);
    public string SelectedPropertyAddress => Clean(Asset?.PropertyAddress);
    public string SelectedOccupants => Clean(Asset?.Occupants);
    public string SelectedHoaOrManagement => Clean(Asset?.HoaOrManagement);

    public string SelectedInstitutionName => Clean(Asset?.InstitutionName);
    public string SelectedAccountNickname => Clean(Asset?.AccountNickname);
    public string SelectedCurrentBalance => Asset?.CurrentBalanceDisplay ?? "Not entered";

    public string SelectedValuableDescription => Clean(Asset?.ValuableDescription);
    public string SelectedSerialOrIdentifier => Clean(Asset?.SerialOrIdentifier);
    public string SelectedStorageLocation => Clean(Asset?.StorageLocation);

    public string SelectedOtherDetails => Clean(Asset?.OtherDetails);

    public string SelectedLinkedBill => GetLinkedBillDisplay(Asset);
    public string SelectedLinkedIncome => GetLinkedIncomeDisplay(Asset);

    public string SelectedNotes => Clean(Asset?.Notes);
    public string SelectedCreatedUtc => FormatUtc(Asset?.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(Asset?.UpdatedUtc);

    partial void OnSelectedAssetChanged(AssetListItemViewModel? oldValue, AssetListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    private void LoadAssets()
    {
        var result = _assetsService.LoadAssets();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Assets.Clear();

        var index = 0;
        foreach (var asset in result.Assets)
        {
            Assets.Add(new AssetListItemViewModel(asset, index));
            index++;
        }

        SelectedAsset = Assets.FirstOrDefault();
    }

    private bool IsType(string type)
    {
        return string.Equals(Asset?.AssetType?.Trim(), type, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLinkedBillDisplay(AssetItem? asset)
    {
        if (asset is null)
            return "None";

        if (asset.RecurringCostHandling.Equals("Paid Off", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(asset.DatePaidOff)
                ? "Paid off"
                : $"Paid off on {asset.DatePaidOff}";

        if (asset.LinkedBillId > 0)
            return string.IsNullOrWhiteSpace(asset.LinkedBillName)
                ? "Linked bill / expense"
                : asset.LinkedBillName.Trim();

        return string.IsNullOrWhiteSpace(asset.RecurringCostHandling) || asset.RecurringCostHandling.Equals("Not Applicable", StringComparison.OrdinalIgnoreCase)
            ? "None"
            : asset.RecurringCostHandling.Trim();
    }

    private static string GetLinkedIncomeDisplay(AssetItem? asset)
    {
        if (asset is null)
            return "None";

        if (asset.LinkedIncomeSourceId > 0)
            return string.IsNullOrWhiteSpace(asset.LinkedIncomeSourceName)
                ? "Linked income source"
                : asset.LinkedIncomeSourceName.Trim();

        return string.IsNullOrWhiteSpace(asset.IncomeHandling) || asset.IncomeHandling.Equals("No Income", StringComparison.OrdinalIgnoreCase)
            ? "None"
            : asset.IncomeHandling.Trim();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string YesNo(bool? value)
    {
        return value == true ? "Yes" : "No";
    }

    private static string FormatUtc(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "Not saved";

        return value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

public sealed partial class AssetListItemViewModel : ObservableObject
{
    public AssetListItemViewModel(AssetItem asset, int index)
    {
        Asset = asset ?? throw new ArgumentNullException(nameof(asset));
        Index = index;
    }

    public AssetItem Asset { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(MutedForeground))]
    private bool _isSelected;

    public string AssetName => string.IsNullOrWhiteSpace(Asset.AssetName) ? "Unnamed Asset" : Asset.AssetName.Trim();
    public string AssetType => string.IsNullOrWhiteSpace(Asset.AssetType) ? "Other" : Asset.AssetType.Trim();
    public string ValueOrBalance => GetValueOrBalance(Asset);
    public string Status => string.IsNullOrWhiteSpace(Asset.Status) ? "None" : Asset.Status.Trim();

    public bool IsInactive => !Asset.IsActive;

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

    private static string GetValueOrBalance(AssetItem asset)
    {
        if (string.Equals(asset.AssetType, "Bank", StringComparison.OrdinalIgnoreCase))
            return asset.CurrentBalanceDisplay;

        return asset.ValueDisplay;
    }
}
