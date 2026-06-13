using GrannyManager.App.Themes;

namespace GrannyManager.App.Controls;

public sealed class MoneySummaryBar : UserControl
{
    private readonly Label _incomeValue;
    private readonly Label _expensesValue;
    private readonly Label _allowanceSavingsValue;
    private readonly Label _remainingValue;

    public MoneySummaryBar()
    {
        Height = 76;
        Dock = DockStyle.Top;
        BackColor = AppColors.PanelBackgroundAlt;
        Padding = new Padding(12, 10, 12, 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            BackColor = AppColors.PanelBackgroundAlt,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };

        for (int i = 0; i < 4; i++)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        AddHeader(layout, "Monthly Income", 0);
        AddHeader(layout, "Known Expenses", 1);
        AddHeader(layout, "Allowance / Savings", 2);
        AddHeader(layout, "Remaining / Deficit", 3);

        _incomeValue = AddValue(layout, "$0.00", 0, AppColors.TextPrimary);
        _expensesValue = AddValue(layout, "$0.00", 1, AppColors.TextPrimary);
        _allowanceSavingsValue = AddValue(layout, "Allowance $0.00 / Save $0.00", 2, AppColors.TextPrimary);
        _remainingValue = AddValue(layout, "$0.00", 3, AppColors.Good);

        Controls.Add(layout);
    }

    public void UpdateSummary(decimal income, decimal expenses)
    {
        UpdateSummary(income, expenses, 0m, 0m);
    }

    public void UpdateSummary(decimal income, decimal expenses, decimal allowance, decimal savings)
    {
        decimal remaining = income - expenses - allowance - savings;

        _incomeValue.Text = income.ToString("C");
        _expensesValue.Text = expenses.ToString("C");
        _allowanceSavingsValue.Text = $"Allowance {allowance:C} / Save {savings:C}";
        _remainingValue.Text = remaining.ToString("C");

        if (remaining < 0)
        {
            _remainingValue.ForeColor = AppColors.Bad;
        }
        else if (remaining < 250)
        {
            _remainingValue.ForeColor = AppColors.Warning;
        }
        else
        {
            _remainingValue.ForeColor = AppColors.Good;
        }
    }

    private static void AddHeader(TableLayoutPanel layout, string text, int column)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppFonts.HeaderSmall,
            ForeColor = AppColors.TextMuted,
            BackColor = AppColors.PanelBackgroundAlt,
            Padding = new Padding(10, 0, 4, 0)
        };
        layout.Controls.Add(label, column, 0);
    }

    private static Label AddValue(TableLayoutPanel layout, string text, int column, Color color)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AppFonts.Money,
            ForeColor = color,
            BackColor = AppColors.PanelBackgroundAlt,
            Padding = new Padding(10, 0, 4, 0)
        };
        layout.Controls.Add(label, column, 1);
        return label;
    }
}
