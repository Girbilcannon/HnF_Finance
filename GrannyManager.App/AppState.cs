using GrannyManager.Core.Models;

namespace GrannyManager.App;

public static class AppState
{
    private static CaseProfile? _activeCase;

    public static event EventHandler<CaseProfile?>? ActiveCaseChanged;

    public static CaseProfile? ActiveCase
    {
        get => _activeCase;
        private set
        {
            _activeCase = value;
            ActiveCaseChanged?.Invoke(null, _activeCase);
        }
    }

    public static void SetActiveCase(CaseProfile profile)
    {
        ActiveCase = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public static void ClearActiveCase()
    {
        ActiveCase = null;
    }
}
