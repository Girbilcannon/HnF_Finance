using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly ActiveCaseState _activeCaseState;
    private readonly CaseFolderService _caseFolderService;
    private readonly RecentCasesService _recentCasesService;
    private CaseProfile? _pendingPinProfile;

    public DashboardViewModel(ActiveCaseState activeCaseState, CaseFolderService caseFolderService, RecentCasesService recentCasesService)
    {
        _activeCaseState = activeCaseState ?? throw new ArgumentNullException(nameof(activeCaseState));
        _caseFolderService = caseFolderService ?? throw new ArgumentNullException(nameof(caseFolderService));
        _recentCasesService = recentCasesService ?? throw new ArgumentNullException(nameof(recentCasesService));
        DefaultCaseRootFolder = _caseFolderService.GetDefaultCaseRoot();
        _activeCaseState.ActiveCaseChanged += (_, activeCase) => UpdateActiveCase(activeCase);
        LoadRecentCases();
        UpdateActiveCase(_activeCaseState.ActiveCase);
    }

    public ObservableCollection<RecentCaseListItemViewModel> RecentCases { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedRecentCase))]
    private RecentCaseListItemViewModel? _selectedRecentCase;

    [ObservableProperty] private string _statusMessage = "Ready. Start a new case or open a recent case to begin.";
    [ObservableProperty] private string _activeCaseTitle = "No active case is open";
    [ObservableProperty] private string _activeCasePath = "Create or open a case to enable the finance sections.";
    [ObservableProperty] private string _activeCasePrimaryPerson = "None";
    [ObservableProperty] private string _welcomeText = "Welcome!";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoActiveCase))]
    private bool _hasActiveCase;

    public string DefaultCaseRootFolder { get; }
    public bool HasNoActiveCase => !HasActiveCase;
    public bool HasSelectedRecentCase => SelectedRecentCase is not null && !SelectedRecentCase.IsPlaceholder;

    public bool TryCreateCase(NewCaseRequest request, out string message)
    {
        message = string.Empty;
        try
        {
            if (request is null) { message = "Case information is missing."; return false; }
            var caseName = request.CaseName.Trim();
            var primaryPerson = request.PrimaryPersonName.Trim();
            var managerName = request.CaseManagerName.Trim();
            var rootFolder = request.CaseRootFolder.Trim();
            var pin = request.SecurityPin.Trim();
            var confirmPin = request.ConfirmSecurityPin.Trim();
            if (string.IsNullOrWhiteSpace(caseName) && !string.IsNullOrWhiteSpace(primaryPerson)) caseName = primaryPerson;
            if (string.IsNullOrWhiteSpace(caseName)) { message = "Enter a case name first."; return false; }
            if (string.IsNullOrWhiteSpace(primaryPerson)) { message = "Enter the name of the person this case is for."; return false; }
            if (string.IsNullOrWhiteSpace(managerName)) { message = "Enter the name of the person managing this case."; return false; }
            if (pin.Length != 4 || pin.Any(c => !char.IsDigit(c))) { message = "Security PIN must be exactly 4 digits."; return false; }
            if (!string.Equals(pin, confirmPin, StringComparison.Ordinal)) { message = "Security PIN and verify PIN do not match."; return false; }

            var profile = _caseFolderService.CreateCase(caseName, primaryPerson, rootFolder, pin, managerName);
            _activeCaseState.SetActiveCase(profile);
            _recentCasesService.AddOrUpdate(profile);
            LoadRecentCases();
            StatusMessage = $"Case created and opened: {profile.DisplayName}";
            message = StatusMessage;
            return true;
        }
        catch (Exception ex)
        {
            message = $"Could not create case: {ex.Message}";
            StatusMessage = message;
            return false;
        }
    }

    public bool TryOpenCaseFile(string caseFilePath, out bool requiresPin)
    {
        requiresPin = false;

        try
        {
            if (string.IsNullOrWhiteSpace(caseFilePath))
            {
                StatusMessage = "The selected case does not have a valid file path.";
                return false;
            }

            var profile = _caseFolderService.LoadCaseFromFile(caseFilePath);

            if (profile.HasSecurityPin)
            {
                _pendingPinProfile = profile;
                requiresPin = true;
                StatusMessage = "Enter the case PIN to unlock this case.";
                return false;
            }

            SetOpenedCase(profile);
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open case: {ex.Message}";
            return false;
        }
    }

    public bool CompletePinUnlock(string pin, out string message)
    {
        message = string.Empty;

        if (_pendingPinProfile is null)
        {
            message = "No PIN-protected case is waiting to be unlocked.";
            StatusMessage = message;
            return false;
        }

        if (!_caseFolderService.VerifySecurityPin(_pendingPinProfile, pin))
        {
            message = "That PIN did not match this case.";
            StatusMessage = message;
            return false;
        }

        SetOpenedCase(_pendingPinProfile);
        _pendingPinProfile = null;
        message = StatusMessage;
        return true;
    }

    public void CancelPinUnlock()
    {
        _pendingPinProfile = null;
        StatusMessage = "Case unlock cancelled.";
    }

    private void SetOpenedCase(CaseProfile profile)
    {
        _activeCaseState.SetActiveCase(profile);
        _recentCasesService.AddOrUpdate(profile);
        LoadRecentCases();
        StatusMessage = $"Opened case: {profile.DisplayName}";
    }

    public void OpenCaseFile(string caseFilePath)
    {
        TryOpenCaseFile(caseFilePath, out _);
    }

    [RelayCommand]
    private void OpenSelectedRecentCase()
    {
        if (SelectedRecentCase is null || SelectedRecentCase.IsPlaceholder) { StatusMessage = "Select a recent case first."; return; }
        OpenCaseFile(SelectedRecentCase.CaseFilePath);
    }
    [RelayCommand]
    private void RemoveSelectedRecentCase()
    {
        if (SelectedRecentCase is null || SelectedRecentCase.IsPlaceholder) { StatusMessage = "Select a recent case first."; return; }
        _recentCasesService.RemoveByPath(SelectedRecentCase.CaseFilePath);
        LoadRecentCases();
        StatusMessage = "Recent case removed from the list.";
    }
    [RelayCommand] private void ReloadRecentCases() { LoadRecentCases(); StatusMessage = "Recent cases refreshed."; }
    [RelayCommand] private void StartWizard() { StatusMessage = "Case Setup Wizard will be wired in the next dashboard pass."; }
    [RelayCommand] private void MoreCaseDetails() { StatusMessage = HasActiveCase ? "More Case Details will open a dedicated case summary/details page later." : "Open a case before viewing more case details."; }
    [RelayCommand] private void Help() { StatusMessage = "Help will open guided setup and section explanations in a later pass. For now, start by creating or opening a case."; }

    private void LoadRecentCases()
    {
        RecentCases.Clear();
        foreach (var item in _recentCasesService.LoadRecentCases()) RecentCases.Add(new RecentCaseListItemViewModel(item));
        if (RecentCases.Count == 0) RecentCases.Add(RecentCaseListItemViewModel.Placeholder());
        SelectedRecentCase = RecentCases.FirstOrDefault(item => !item.IsPlaceholder) ?? RecentCases.FirstOrDefault();
        MarkCurrentCase();
        MarkCurrentCase();
    }

    private void UpdateActiveCase(CaseProfile? activeCase)
    {
        if (activeCase is null)
        {
            HasActiveCase = false;
            WelcomeText = "Welcome!";
            ActiveCaseTitle = "No active case is open";
            ActiveCasePath = "Create or open a case to enable the finance sections.";
            ActiveCasePrimaryPerson = "None";
            return;
        }
        HasActiveCase = true;
        WelcomeText = "Welcome " + GetFirstName(activeCase.CaseManagerName) + "!";
        ActiveCaseTitle = "Active case: " + activeCase.DisplayName;
        ActiveCasePath = activeCase.CaseFolderPath;
        ActiveCasePrimaryPerson = string.IsNullOrWhiteSpace(activeCase.PrimaryPersonName) ? "None" : activeCase.PrimaryPersonName.Trim();
    }


    private void MarkCurrentCase()
    {
        var activeCase = _activeCaseState.ActiveCase;
        var caseFolderPath = activeCase?.CaseFolderPath ?? string.Empty;

        foreach (var item in RecentCases)
        {
            item.IsCurrentCase = !item.IsPlaceholder
                && !string.IsNullOrWhiteSpace(caseFolderPath)
                && item.CaseFilePath.Contains(caseFolderPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string GetFirstName(string value) => string.IsNullOrWhiteSpace(value) ? "there" : (value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there");
}

public sealed class NewCaseRequest
{
    public string PrimaryPersonName { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public string CaseManagerName { get; set; } = string.Empty;
    public string CaseRootFolder { get; set; } = string.Empty;
    public string SecurityPin { get; set; } = string.Empty;
    public string ConfirmSecurityPin { get; set; } = string.Empty;
}

public sealed partial class RecentCaseListItemViewModel : ObservableObject
{
    public RecentCaseListItemViewModel(RecentCaseInfo recentCase)
    {
        DisplayName = string.IsNullOrWhiteSpace(recentCase.DisplayName) ? "Unnamed case" : recentCase.DisplayName.Trim();
        CaseFilePath = recentCase.CaseFilePath;
        CaseManagerName = recentCase.CaseManagerName;
        LastOpenedAt = recentCase.LastOpenedAt;
        IsPlaceholder = false;
    }
    private RecentCaseListItemViewModel()
    {
        DisplayName = "No recent cases yet";
        CaseFilePath = string.Empty;
        CaseManagerName = string.Empty;
        LastOpenedAt = DateTime.MinValue;
        IsPlaceholder = true;
    }
    public string DisplayName { get; }
    public string CaseFilePath { get; }
    public string CaseManagerName { get; }
    public DateTime LastOpenedAt { get; }
    public bool IsPlaceholder { get; }
    public string LastOpenedText => IsPlaceholder ? "Create a case to get started" : LastOpenedAt.ToString("yyyy-MM-dd h:mm tt");
    public string CaseFilePathDisplay => string.IsNullOrWhiteSpace(CaseFilePath) ? "None" : CaseFilePath;
    public string ManagedByText => string.IsNullOrWhiteSpace(CaseManagerName) ? "Managed by: Not entered" : "Managed by: " + CaseManagerName.Trim();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBackground))]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    [NotifyPropertyChangedFor(nameof(CardBorderThickness))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBackground))]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    [NotifyPropertyChangedFor(nameof(CardBorderThickness))]
    private bool _isCurrentCase;

    public string CardBackground => IsCurrentCase ? "#173A5A" : "Transparent";
    public string CardBorderBrush => IsCurrentCase ? "#4F7FA8" : "#26384F";
    public double CardBorderThickness => IsCurrentCase ? 1 : 1;

    public static RecentCaseListItemViewModel Placeholder() => new RecentCaseListItemViewModel();
}
