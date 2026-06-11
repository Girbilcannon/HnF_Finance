using GrannyManager.App;

namespace GrannyManager.App.Navigation;

public sealed class NavigationService
{
    private readonly Panel _hostPanel;
    private readonly PageFactory _pageFactory;
    private IAppPage? _currentPage;

    public event EventHandler<AppPageKey>? Navigated;

    public NavigationService(Panel hostPanel, PageFactory pageFactory)
    {
        _hostPanel = hostPanel ?? throw new ArgumentNullException(nameof(hostPanel));
        _pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
    }

    public AppPageKey? CurrentPageKey => _currentPage?.PageKey;

    public IAppPage? CurrentPage => _currentPage;

    public void NavigateTo(AppPageKey key)
    {
        if (RequiresOpenCase(key) && AppState.ActiveCase is null)
        {
            key = AppPageKey.Dashboard;
            MessageBox.Show(
                "Open or create a case before viewing case details.",
                "No case open",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        if (_currentPage is not null && !_currentPage.CanNavigateAway())
            return;

        UserControl page = _pageFactory.GetPage(key);
        if (page is not IAppPage appPage)
            throw new InvalidOperationException($"Page {page.GetType().Name} does not implement IAppPage.");

        _hostPanel.SuspendLayout();
        _hostPanel.Controls.Clear();
        _hostPanel.Controls.Add(page);
        _hostPanel.ResumeLayout();

        _currentPage = appPage;
        appPage.OnNavigatedTo();
        Navigated?.Invoke(this, key);
    }

    private static bool RequiresOpenCase(AppPageKey key)
    {
        return key is not AppPageKey.Dashboard
            and not AppPageKey.CaseSetup
            and not AppPageKey.FinanceWizard;
    }
}
