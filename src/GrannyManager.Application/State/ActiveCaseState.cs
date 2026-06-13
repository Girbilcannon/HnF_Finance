using GrannyManager.Core.Models;

namespace GrannyManager.Application.State;

public sealed class ActiveCaseState
{
    private CaseProfile? _activeCase;

    public event EventHandler<CaseProfile?>? ActiveCaseChanged;

    public CaseProfile? ActiveCase
    {
        get => _activeCase;
        private set
        {
            if (ReferenceEquals(_activeCase, value))
                return;

            _activeCase = value;
            ActiveCaseChanged?.Invoke(this, _activeCase);
        }
    }

    public bool HasActiveCase => ActiveCase is not null && ActiveCase.IsValid;

    public void SetActiveCase(CaseProfile profile)
    {
        ActiveCase = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public void ClearActiveCase()
    {
        ActiveCase = null;
    }
}
