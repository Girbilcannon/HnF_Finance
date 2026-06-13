using GrannyManager.App.Pages;

namespace GrannyManager.App.Navigation;

public sealed class PageFactory
{
    private readonly Dictionary<AppPageKey, UserControl> _pageCache = new();

    public UserControl GetPage(AppPageKey key)
    {
        if (_pageCache.TryGetValue(key, out UserControl? cachedPage))
            return cachedPage;

        UserControl page = key switch
        {
            AppPageKey.Dashboard => new DashboardPage(),
            AppPageKey.CaseSetup => new CaseSetupPage(),
            AppPageKey.FinanceWizard => new FinanceWizardPage(),
            AppPageKey.People => new PeoplePage(),
            AppPageKey.Income => new IncomePage(),
            AppPageKey.Bills => new BillsPage(),
            AppPageKey.AllowanceSavings => new AllowanceSavingsPage(),
            AppPageKey.Assets => new AssetsPage(),
            AppPageKey.Debts => new DebtsPage(),
            AppPageKey.Documents => new DocumentsPage(),
            AppPageKey.Vault => new VaultPage(),
            _ => new DashboardPage()
        };

        page.Dock = DockStyle.Fill;
        _pageCache[key] = page;
        return page;
    }
}
