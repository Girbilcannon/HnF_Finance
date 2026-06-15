using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class PasswordVaultViewModel : ViewModelBase
{
    private readonly PasswordVaultService _passwordVaultService;

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
    private bool _isVaultUnlocked;

    [ObservableProperty]
    private bool _vaultExists;

    [ObservableProperty]
    private string _pin = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _vaultPath = string.Empty;

    public bool IsLocked => !IsVaultUnlocked;
    public bool IsUnlocked => IsVaultUnlocked;
    public bool CanUnlock => HasActiveCase && !IsVaultUnlocked;

    [RelayCommand]
    private void UnlockVault()
    {
        if (!HasActiveCase)
        {
            StatusMessage = "Open a case before using the password vault.";
            return;
        }

        var result = _passwordVaultService.UnlockOrCreateVault(Pin);

        if (!result.Success || result.VaultData is null)
        {
            StatusMessage = result.StatusMessage;
            return;
        }

        Items.Clear();

        foreach (var item in result.VaultData.Items)
            Items.Add(new PasswordVaultItemViewModel(item));

        IsVaultUnlocked = true;
        VaultExists = true;
        Pin = string.Empty;
        StatusMessage = result.StatusMessage;
    }

    [RelayCommand]
    private void LockVault()
    {
        Items.Clear();
        Pin = string.Empty;
        IsVaultUnlocked = false;
        LoadStatus();
        StatusMessage = "Password vault locked.";
    }

    private void ResetForCaseChange()
    {
        Items.Clear();
        Pin = string.Empty;
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
}

public sealed class PasswordVaultItemViewModel
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
}
