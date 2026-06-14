using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.App.Avalonia.ViewModels.Sections;

namespace GrannyManager.App.Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ActiveCaseState _activeCaseState;

        public MainWindowViewModel()
        {
            _activeCaseState = new ActiveCaseState();

            var householdService = new HouseholdService(_activeCaseState);
            var incomeService = new IncomeService(_activeCaseState);
            var billsService = new BillsService(_activeCaseState);
            var allowanceSavingsService = new AllowanceSavingsService(_activeCaseState);
            var assetsService = new AssetsService(_activeCaseState);
            var debtsService = new DebtsService(_activeCaseState);
            var documentsService = new DocumentsService(_activeCaseState);

            Household = new HouseholdViewModel(_activeCaseState, householdService);
            Income = new IncomeViewModel(_activeCaseState, incomeService);
            Bills = new BillsViewModel(_activeCaseState, billsService);
            AllowanceSavings = new AllowanceSavingsViewModel(_activeCaseState, allowanceSavingsService);
            Assets = new AssetsViewModel(_activeCaseState, assetsService);
            Debts = new DebtsViewModel(_activeCaseState, debtsService);
            Documents = new DocumentsViewModel(_activeCaseState, documentsService);
        }

        public HouseholdViewModel Household { get; }

        public IncomeViewModel Income { get; }

        public BillsViewModel Bills { get; }

        public AllowanceSavingsViewModel AllowanceSavings { get; }

        public AssetsViewModel Assets { get; }

        public DebtsViewModel Debts { get; }

        public DocumentsViewModel Documents { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentPageTitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageSubtitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageCardTitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageBody))]
        [NotifyPropertyChangedFor(nameof(IsDashboardSelected))]
        [NotifyPropertyChangedFor(nameof(IsHouseholdSelected))]
        [NotifyPropertyChangedFor(nameof(IsIncomeSelected))]
        [NotifyPropertyChangedFor(nameof(IsBillsSelected))]
        [NotifyPropertyChangedFor(nameof(IsAllowanceSavingsSelected))]
        [NotifyPropertyChangedFor(nameof(IsAssetsSelected))]
        [NotifyPropertyChangedFor(nameof(IsDebtsSelected))]
        [NotifyPropertyChangedFor(nameof(IsDocumentsSelected))]
        [NotifyPropertyChangedFor(nameof(IsPasswordVaultSelected))]
        [NotifyPropertyChangedFor(nameof(IsGenericPlaceholderVisible))]
        private string _currentSection = "Dashboard";

        public string CurrentPageTitle => CurrentSection switch
        {
            "Household" => "Household",
            "Income" => "Income Sources",
            "Bills" => "Bills / Spending",
            "AllowanceSavings" => "Allowance / Savings",
            "Assets" => "Assets",
            "Debts" => "Debts",
            "Documents" => "Documents",
            "PasswordVault" => "Password Vault",
            _ => "Dashboard"
        };

        public string CurrentPageSubtitle => CurrentSection switch
        {
            "Household" => "Manage household members, relationships, roles, notes, and profile details.",
            "Income" => "Track recurring and irregular income, then normalize each source into a monthly estimate.",
            "Bills" => "Track recurring bills, spending estimates, payment responsibility, and receipt-based fuel/grocery averages.",
            "AllowanceSavings" => "Track spending allowance and planned savings as separate monthly buckets.",
            "Assets" => "Track vehicles, property, accounts, investments, crypto, and other valuable assets.",
            "Debts" => "Track debts, payment status, balances, and priority payoff information.",
            "Documents" => "Import, categorize, and find important documents attached to the current case.",
            "PasswordVault" => "Store sensitive account credentials and recovery notes in the protected vault.",
            _ => "Avalonia shell is running. The summary bar stays visible while sections change below it."
        };

        public string CurrentPageCardTitle => CurrentSection switch
        {
            "Assets" => "Assets Placeholder",
            "Documents" => "Documents Placeholder",
            "PasswordVault" => "Password Vault Placeholder",
            _ => "v0.10.9 Migration Status"
        };

        public string CurrentPageBody => CurrentSection switch
        {
            "Assets" => "This section will host the new Assets workflow for vehicles, property, accounts, investments, and valuables.",
            "Documents" => "This section will manage imported documents, categories, and search integration.",
            "PasswordVault" => "This section will later connect to the secure credential vault.",
            _ => "The new tri-platform Avalonia shell is replacing the original WinForms frame. Sidebar navigation now updates this content area while keeping the summary bar fixed at the top."
        };

        public bool IsDashboardSelected => CurrentSection == "Dashboard";
        public bool IsHouseholdSelected => CurrentSection == "Household";
        public bool IsIncomeSelected => CurrentSection == "Income";
        public bool IsBillsSelected => CurrentSection == "Bills";
        public bool IsAllowanceSavingsSelected => CurrentSection == "AllowanceSavings";
        public bool IsAssetsSelected => CurrentSection == "Assets";
        public bool IsDebtsSelected => CurrentSection == "Debts";
        public bool IsDocumentsSelected => CurrentSection == "Documents";
        public bool IsPasswordVaultSelected => CurrentSection == "PasswordVault";

        public bool IsGenericPlaceholderVisible => CurrentSection != "Household" && CurrentSection != "Income" && CurrentSection != "Bills" && CurrentSection != "AllowanceSavings" && CurrentSection != "Assets" && CurrentSection != "Debts" && CurrentSection != "Documents";

        [RelayCommand]
        private void Navigate(string section)
        {
            if (!string.IsNullOrWhiteSpace(section))
            {
                CurrentSection = section;
            }
        }
    }
}
