using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GrannyManager.App.Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
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
        private string _currentSection = "Dashboard";

        public string CurrentPageTitle => CurrentSection switch
        {
            "Household" => "Household",
            "Income" => "Income",
            "Bills" => "Bills",
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
            "Income" => "Track income sources, payment frequency, tax status, and monthly income estimates.",
            "Bills" => "Track recurring bills, payment responsibility, due dates, and spending obligations.",
            "AllowanceSavings" => "Track spending allowance and planned savings as separate monthly buckets.",
            "Assets" => "Track vehicles, property, accounts, investments, crypto, and other valuable assets.",
            "Debts" => "Track debts, payment status, balances, and priority payoff information.",
            "Documents" => "Import, categorize, and find important documents attached to the current case.",
            "PasswordVault" => "Store sensitive account credentials and recovery notes in the protected vault.",
            _ => "Avalonia shell is running. The summary bar stays visible while sections change below it."
        };

        public string CurrentPageCardTitle => CurrentSection switch
        {
            "Household" => "Household Placeholder",
            "Income" => "Income Placeholder",
            "Bills" => "Bills Placeholder",
            "AllowanceSavings" => "Allowance / Savings Placeholder",
            "Assets" => "Assets Placeholder",
            "Debts" => "Debts Placeholder",
            "Documents" => "Documents Placeholder",
            "PasswordVault" => "Password Vault Placeholder",
            _ => "v0.10.0 Migration Status"
        };

        public string CurrentPageBody => CurrentSection switch
        {
            "Household" => "This will become the Avalonia replacement for the People page, renamed to Household in the navigation.",
            "Income" => "This section will connect to the existing income models, repositories, and monthly calculation logic.",
            "Bills" => "This section will replace the WinForms Bills page with a cleaner list/profile/edit workflow.",
            "AllowanceSavings" => "This section will manage allowance and savings entries while the top summary bar remains visible.",
            "Assets" => "This section will host the new Assets workflow for vehicles, property, accounts, investments, and valuables.",
            "Debts" => "This section will track outstanding debts and repayment priorities.",
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
