using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.Core.Services;

namespace GrannyManager.App.Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ActiveCaseState _activeCaseState;
        private readonly FinanceTotalsService _financeTotalsService;

        public MainWindowViewModel()
        {
            _activeCaseState = new ActiveCaseState();

            var caseFolderService = new CaseFolderService();
            var recentCasesService = new RecentCasesService();
            var householdService = new HouseholdService(_activeCaseState);
            var incomeService = new IncomeService(_activeCaseState);
            var billsService = new BillsService(_activeCaseState);
            var allowanceSavingsService = new AllowanceSavingsService(_activeCaseState);
            var assetsService = new AssetsService(_activeCaseState);
            var debtsService = new DebtsService(_activeCaseState);
            var documentsService = new DocumentsService(_activeCaseState);
            var passwordVaultService = new PasswordVaultService(_activeCaseState, caseFolderService);

            _financeTotalsService = new FinanceTotalsService(_activeCaseState);

            Dashboard = new DashboardViewModel(_activeCaseState, caseFolderService, recentCasesService);
            Household = new HouseholdViewModel(_activeCaseState, householdService);
            Income = new IncomeViewModel(_activeCaseState, incomeService);
            Bills = new BillsViewModel(_activeCaseState, billsService);
            AllowanceSavings = new AllowanceSavingsViewModel(_activeCaseState, allowanceSavingsService);
            Assets = new AssetsViewModel(_activeCaseState, assetsService);
            Debts = new DebtsViewModel(_activeCaseState, debtsService);
            Documents = new DocumentsViewModel(_activeCaseState, documentsService);
            PasswordVault = new PasswordVaultViewModel(_activeCaseState, passwordVaultService);

            _activeCaseState.ActiveCaseChanged += (_, _) => RefreshTotals();

            AppDataChangeNotifier.IncomeSourcesChanged += (_, _) => RefreshTotals();
            AppDataChangeNotifier.BillsChanged += (_, _) => RefreshTotals();
            AppDataChangeNotifier.AllowanceSavingsChanged += (_, _) => RefreshTotals();
            AppDataChangeNotifier.AssetsChanged += (_, _) => RefreshTotals();
            AppDataChangeNotifier.DebtsChanged += (_, _) => RefreshTotals();
            AppDataChangeNotifier.HouseholdChanged += (_, _) => RefreshTotals();
            AppDataChangeNotifier.DocumentsChanged += (_, _) => RefreshTotals();

            RefreshTotals();
        }

        public DashboardViewModel Dashboard { get; }
        public HouseholdViewModel Household { get; }
        public IncomeViewModel Income { get; }
        public BillsViewModel Bills { get; }
        public AllowanceSavingsViewModel AllowanceSavings { get; }
        public AssetsViewModel Assets { get; }
        public DebtsViewModel Debts { get; }
        public DocumentsViewModel Documents { get; }
        public PasswordVaultViewModel PasswordVault { get; }

        [ObservableProperty]
        private string _monthlyIncomeText = "$0.00";

        [ObservableProperty]
        private string _monthlyBillsText = "$0.00";

        [ObservableProperty]
        private string _monthlyAllowanceSavingsText = "$0.00 / $0.00";

        [ObservableProperty]
        private string _remainingDeficitText = "$0.00";

        [ObservableProperty]
        private string _remainingDeficitBrush = "#D9534F";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentPageTitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageSubtitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageCardTitle))]
        [NotifyPropertyChangedFor(nameof(CurrentPageBody))]
        [NotifyPropertyChangedFor(nameof(IsDashboardSelected))]
        [NotifyPropertyChangedFor(nameof(IsNotDashboardSelected))]
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
            _ => string.Empty
        };

        public string CurrentPageCardTitle => CurrentSection switch
        {
            "Documents" => "Documents",
            "PasswordVault" => "Password Vault Placeholder",
            _ => "Dashboard"
        };

        public string CurrentPageBody => CurrentSection switch
        {
            "Documents" => "Import, link, tag, organize, and open case documents.",
            "PasswordVault" => "This section will later connect to the secure credential vault.",
            _ => "Create or open a case to begin."
        };

        public bool IsDashboardSelected => CurrentSection == "Dashboard";
        public bool IsNotDashboardSelected => !IsDashboardSelected;
        public bool IsHouseholdSelected => CurrentSection == "Household";
        public bool IsIncomeSelected => CurrentSection == "Income";
        public bool IsBillsSelected => CurrentSection == "Bills";
        public bool IsAllowanceSavingsSelected => CurrentSection == "AllowanceSavings";
        public bool IsAssetsSelected => CurrentSection == "Assets";
        public bool IsDebtsSelected => CurrentSection == "Debts";
        public bool IsDocumentsSelected => CurrentSection == "Documents";
        public bool IsPasswordVaultSelected => CurrentSection == "PasswordVault";

        public bool IsGenericPlaceholderVisible =>
            CurrentSection != "Dashboard" &&
            CurrentSection != "Household" &&
            CurrentSection != "Income" &&
            CurrentSection != "Bills" &&
            CurrentSection != "AllowanceSavings" &&
            CurrentSection != "Assets" &&
            CurrentSection != "Debts" &&
            CurrentSection != "Documents" &&
            CurrentSection != "PasswordVault";

        public void SecureLockActiveCase(string reason)
        {
            if (!_activeCaseState.HasActiveCase)
                return;

            _activeCaseState.ClearActiveCase();
            CurrentSection = "Dashboard";
            Dashboard.StatusMessage = string.IsNullOrWhiteSpace(reason)
                ? "Case locked for security. Reopen it from Recent Cases and enter the case PIN."
                : reason;

            RefreshTotals();
        }

        [RelayCommand]
        private void Help()
        {
            CurrentSection = "Dashboard";
            Dashboard.HelpCommand.Execute(null);
        }

        [RelayCommand]
        private void Navigate(string section)
        {
            if (!string.IsNullOrWhiteSpace(section))
                CurrentSection = section;

            RefreshTotals();
        }

        private void RefreshTotals()
        {
            var totals = _financeTotalsService.LoadTotals();
            MonthlyIncomeText = totals.MonthlyIncomeText;
            MonthlyBillsText = totals.MonthlyBillsText;
            MonthlyAllowanceSavingsText = totals.MonthlyAllowanceSavingsText;
            RemainingDeficitText = totals.RemainingDeficitText;
            RemainingDeficitBrush = totals.RemainingDeficitBrush;
        }
    }
}
