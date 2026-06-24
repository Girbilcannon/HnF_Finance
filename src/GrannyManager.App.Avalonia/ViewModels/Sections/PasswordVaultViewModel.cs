using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class PasswordVaultViewModel : ViewModelBase
{
    private readonly PasswordVaultService _passwordVaultService;
    private PasswordVaultData? _vaultData;
    private string _unlockedPin = string.Empty;

    public PasswordVaultViewModel(ActiveCaseState activeCaseState, PasswordVaultService passwordVaultService)
    {
        _passwordVaultService = passwordVaultService ?? throw new ArgumentNullException(nameof(passwordVaultService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => ResetForCaseChange();

        LoadStatus();
    }

    public ObservableCollection<PasswordVaultItemViewModel> Items { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUnlock))]
    private bool _hasActiveCase;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocked))]
    [NotifyPropertyChangedFor(nameof(IsUnlocked))]
    [NotifyPropertyChangedFor(nameof(CanUnlock))]
    [NotifyPropertyChangedFor(nameof(CanAddEntry))]
    [NotifyPropertyChangedFor(nameof(CanEditEntry))]
    [NotifyPropertyChangedFor(nameof(CanRemoveEntry))]
    [NotifyPropertyChangedFor(nameof(CanCopySelectedUserName))]
    [NotifyPropertyChangedFor(nameof(CanCopySelectedPassword))]
    [NotifyPropertyChangedFor(nameof(CanRevealSelectedPassword))]
    [NotifyPropertyChangedFor(nameof(CanHideSelectedPassword))]
    private bool _isVaultUnlocked;

    [ObservableProperty]
    private bool _vaultExists;

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _vaultPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedPasswordDisplay))]
    [NotifyPropertyChangedFor(nameof(CanRevealSelectedPassword))]
    [NotifyPropertyChangedFor(nameof(CanHideSelectedPassword))]
    private bool _isSelectedPasswordRevealed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedEntry))]
    [NotifyPropertyChangedFor(nameof(CanEditEntry))]
    [NotifyPropertyChangedFor(nameof(CanRemoveEntry))]
    [NotifyPropertyChangedFor(nameof(SelectedTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedUserName))]
    [NotifyPropertyChangedFor(nameof(SelectedWebsite))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedPasswordDisplay))]
    [NotifyPropertyChangedFor(nameof(HasSelectedPassword))]
    [NotifyPropertyChangedFor(nameof(CanCopySelectedUserName))]
    [NotifyPropertyChangedFor(nameof(CanCopySelectedPassword))]
    [NotifyPropertyChangedFor(nameof(CanRevealSelectedPassword))]
    [NotifyPropertyChangedFor(nameof(CanHideSelectedPassword))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedText))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedText))]
    private PasswordVaultItemViewModel? _selectedEntry;

    public bool IsLocked => !IsVaultUnlocked;
    public bool IsUnlocked => IsVaultUnlocked;
    public bool CanUnlock => HasActiveCase && !IsVaultUnlocked;
    public bool CanAddEntry => IsVaultUnlocked;
    public bool HasSelectedEntry => SelectedEntry is not null;
    public bool CanEditEntry => IsVaultUnlocked && HasSelectedEntry;
    public bool CanRemoveEntry => IsVaultUnlocked && HasSelectedEntry;

    private PasswordVaultItem? SelectedItem => SelectedEntry?.Item;

    partial void OnSelectedEntryChanged(PasswordVaultItemViewModel? value)
    {
        IsSelectedPasswordRevealed = false;
        RefreshSelectedEntryActionState();
    }

    public string SelectedTitle => SelectedItem?.Title ?? "No vault entry selected";
    public string SelectedUserName => Clean(SelectedItem?.UserName);
    public string SelectedWebsite => Clean(SelectedItem?.Website);
    public string SelectedNotes => Clean(SelectedItem?.Notes);
    public bool HasSelectedPassword => !string.IsNullOrWhiteSpace(SelectedItem?.Password);
    public string SelectedPasswordDisplay => HasSelectedPassword
        ? IsSelectedPasswordRevealed ? SelectedItem!.Password : "••••••••••••"
        : "No password saved";
    public bool CanCopySelectedUserName => IsVaultUnlocked && HasSelectedEntry && !string.IsNullOrWhiteSpace(SelectedItem?.UserName);
    public bool CanCopySelectedPassword => IsVaultUnlocked && HasSelectedPassword;
    public bool CanRevealSelectedPassword => IsVaultUnlocked && HasSelectedPassword && !IsSelectedPasswordRevealed;
    public bool CanHideSelectedPassword => IsVaultUnlocked && HasSelectedPassword && IsSelectedPasswordRevealed;
    public string SelectedUserNameForCopy => SelectedItem?.UserName ?? string.Empty;
    public string SelectedPasswordForCopy => SelectedItem?.Password ?? string.Empty;
    public string SelectedCreatedText => FormatDate(SelectedItem?.CreatedUtc);
    public string SelectedUpdatedText => FormatDate(SelectedItem?.UpdatedUtc);

    [RelayCommand]
    private void UnlockVault()
    {
        if (!HasActiveCase)
        {
            StatusMessage = "Open a case before using the password vault.";
            return;
        }

        var unlockPin = Pin;
        var result = _passwordVaultService.UnlockOrCreateVault(unlockPin);

        if (!result.Success || result.VaultData is null)
        {
            StatusMessage = result.StatusMessage;
            return;
        }

        _vaultData = result.VaultData;
        _unlockedPin = unlockPin;

        LoadItemsFromVaultData();

        IsVaultUnlocked = true;
        VaultExists = true;
        Pin = string.Empty;
        RefreshSelectedEntryActionState();
        StatusMessage = result.StatusMessage;
    }

    [RelayCommand]
    private void LockVault()
    {
        Items.Clear();
        SelectedEntry = null;
        Pin = string.Empty;
        _unlockedPin = string.Empty;
        _vaultData = null;
        IsVaultUnlocked = false;
        LoadStatus();
        RefreshSelectedEntryActionState();
        StatusMessage = "Password vault locked.";
    }

    public bool AddEntry(PasswordVaultEntryInput input)
    {
        if (!EnsureUnlockedForEdit())
            return false;

        var now = DateTime.UtcNow;
        var item = new PasswordVaultItem
        {
            Id = Guid.NewGuid(),
            Title = input.Title.Trim(),
            UserName = input.UserName.Trim(),
            Password = input.Password,
            Website = input.Website.Trim(),
            Notes = input.Notes.Trim(),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        _vaultData!.Items.Add(item);

        if (!SaveVaultData("Vault entry added."))
            return false;

        var itemViewModel = new PasswordVaultItemViewModel(item);
        Items.Add(itemViewModel);
        SelectedEntry = itemViewModel;
        RefreshSelectedEntryActionState();
        return true;
    }

    public PasswordVaultEntryInput? GetSelectedEntryInput()
    {
        if (SelectedItem is null)
            return null;

        return new PasswordVaultEntryInput(
            SelectedItem.Title,
            SelectedItem.UserName,
            SelectedItem.Password,
            SelectedItem.Website,
            SelectedItem.Notes);
    }

    public bool UpdateSelectedEntry(PasswordVaultEntryInput input)
    {
        if (!EnsureUnlockedForEdit())
            return false;

        if (SelectedItem is null || SelectedEntry is null)
        {
            StatusMessage = "Select a vault entry first.";
            return false;
        }

        SelectedItem.Title = input.Title.Trim();
        SelectedItem.UserName = input.UserName.Trim();
        SelectedItem.Password = input.Password;
        SelectedItem.Website = input.Website.Trim();
        SelectedItem.Notes = input.Notes.Trim();
        SelectedItem.UpdatedUtc = DateTime.UtcNow;

        if (!SaveVaultData("Vault entry updated."))
            return false;

        IsSelectedPasswordRevealed = false;
        SelectedEntry.Refresh();
        OnPropertyChanged(nameof(SelectedTitle));
        OnPropertyChanged(nameof(SelectedUserName));
        OnPropertyChanged(nameof(SelectedWebsite));
        OnPropertyChanged(nameof(SelectedNotes));
        OnPropertyChanged(nameof(SelectedPasswordDisplay));
        OnPropertyChanged(nameof(HasSelectedPassword));
        OnPropertyChanged(nameof(CanCopySelectedUserName));
        OnPropertyChanged(nameof(CanCopySelectedPassword));
        OnPropertyChanged(nameof(CanRevealSelectedPassword));
        OnPropertyChanged(nameof(CanHideSelectedPassword));
        OnPropertyChanged(nameof(CanEditEntry));
        OnPropertyChanged(nameof(CanRemoveEntry));
        OnPropertyChanged(nameof(SelectedCreatedText));
        OnPropertyChanged(nameof(SelectedUpdatedText));
        return true;
    }

    public bool RemoveSelectedEntry()
    {
        if (!EnsureUnlockedForEdit())
            return false;

        if (SelectedItem is null || SelectedEntry is null)
        {
            StatusMessage = "Select a vault entry first.";
            return false;
        }

        _vaultData!.Items.RemoveAll(item => item.Id == SelectedItem.Id);
        Items.Remove(SelectedEntry);
        IsSelectedPasswordRevealed = false;
        SelectedEntry = Items.FirstOrDefault();
        RefreshSelectedEntryActionState();

        return SaveVaultData("Vault entry removed.");
    }

    public bool RevealSelectedPassword(string pin)
    {
        if (!CanRevealSelectedPassword)
        {
            StatusMessage = "Select a vault entry with a saved password first.";
            return false;
        }

        if (!_passwordVaultService.VerifyActiveCasePin(pin))
        {
            StatusMessage = "That PIN did not match this case.";
            return false;
        }

        IsSelectedPasswordRevealed = true;
        RefreshSelectedEntryActionState();
        StatusMessage = "Password revealed. Use Hide Password when finished.";
        return true;
    }

    public void HideSelectedPassword()
    {
        IsSelectedPasswordRevealed = false;
        RefreshSelectedEntryActionState();
        StatusMessage = "Password hidden.";
    }

    public void SetClipboardStatus(string statusMessage)
    {
        StatusMessage = statusMessage;
    }

    private void RefreshSelectedEntryActionState()
    {
        OnPropertyChanged(nameof(HasSelectedEntry));
        OnPropertyChanged(nameof(CanEditEntry));
        OnPropertyChanged(nameof(CanRemoveEntry));
        OnPropertyChanged(nameof(HasSelectedPassword));
        OnPropertyChanged(nameof(CanCopySelectedUserName));
        OnPropertyChanged(nameof(CanCopySelectedPassword));
        OnPropertyChanged(nameof(CanRevealSelectedPassword));
        OnPropertyChanged(nameof(CanHideSelectedPassword));
        OnPropertyChanged(nameof(SelectedPasswordDisplay));
    }

    private bool EnsureUnlockedForEdit()
    {
        if (!IsVaultUnlocked || _vaultData is null || string.IsNullOrWhiteSpace(_unlockedPin))
        {
            StatusMessage = "Unlock the password vault before editing entries.";
            return false;
        }

        return true;
    }

    private bool SaveVaultData(string successMessage)
    {
        if (_vaultData is null)
        {
            StatusMessage = "No vault data is available to save.";
            return false;
        }

        var result = _passwordVaultService.SaveUnlockedVault(_unlockedPin, _vaultData);

        if (!result.Success)
        {
            StatusMessage = result.StatusMessage;
            return false;
        }

        StatusMessage = successMessage;
        return true;
    }

    private void ResetForCaseChange()
    {
        Items.Clear();
        SelectedEntry = null;
        Pin = string.Empty;
        _unlockedPin = string.Empty;
        _vaultData = null;
        IsVaultUnlocked = false;
        LoadStatus();
    }

    private void LoadStatus()
    {
        var status = _passwordVaultService.GetStatus();

        HasActiveCase = status.HasActiveCase;
        VaultExists = status.VaultExists;
        VaultPath = status.VaultPath;
        StatusMessage = status.StatusMessage;
    }

    private void LoadItemsFromVaultData()
    {
        Items.Clear();

        if (_vaultData is null)
            return;

        foreach (var item in _vaultData.Items.OrderBy(item => item.Title))
            Items.Add(new PasswordVaultItemViewModel(item));

        SelectedEntry = Items.FirstOrDefault();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string FormatDate(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "None";

        return value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
    }
}

public sealed record PasswordVaultEntryInput(
    string Title,
    string UserName,
    string Password,
    string Website,
    string Notes);

public sealed partial class PasswordVaultItemViewModel : ObservableObject
{
    public PasswordVaultItemViewModel(PasswordVaultItem item)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    public PasswordVaultItem Item { get; }

    public string Title => string.IsNullOrWhiteSpace(Item.Title) ? "Untitled entry" : Item.Title.Trim();
    public string UserName => string.IsNullOrWhiteSpace(Item.UserName) ? "No username" : Item.UserName.Trim();
    public string Website => string.IsNullOrWhiteSpace(Item.Website) ? "No website" : Item.Website.Trim();
    public string UpdatedText => Item.UpdatedUtc == default ? "Not updated yet" : $"Updated {Item.UpdatedUtc.ToLocalTime():MMM d, yyyy}";

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(Website));
        OnPropertyChanged(nameof(UpdatedText));
    }
}
