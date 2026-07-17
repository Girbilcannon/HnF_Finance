using System;

namespace GrannyManager.Application.Services;

public static class AppDataChangeNotifier
{
    public static event EventHandler? IncomeSourcesChanged;
    public static event EventHandler? HouseholdChanged;
    public static event EventHandler? BillsChanged;
    public static event EventHandler? AllowanceSavingsChanged;

    public static void NotifyIncomeSourcesChanged()
    {
        IncomeSourcesChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifyHouseholdChanged()
    {
        HouseholdChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifyBillsChanged()
    {
        BillsChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void NotifyAllowanceSavingsChanged()
    {
        AllowanceSavingsChanged?.Invoke(null, EventArgs.Empty);
    }
}
