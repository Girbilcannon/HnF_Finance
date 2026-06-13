namespace GrannyManager.App.Navigation;

public interface IAppPage
{
    AppPageKey PageKey { get; }
    string PageTitle { get; }
    void OnNavigatedTo();
    bool CanNavigateAway();
}
