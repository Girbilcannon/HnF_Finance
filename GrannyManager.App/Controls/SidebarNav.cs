using GrannyManager.App.Navigation;
using GrannyManager.App.Themes;

namespace GrannyManager.App.Controls;

public sealed class SidebarNav : UserControl
{
    private readonly Dictionary<AppPageKey, Button> _buttons = new();

    public event EventHandler<AppPageKey>? NavigationRequested;

    public SidebarNav()
    {
        Dock = DockStyle.Left;
        Width = 215;
        BackColor = AppColors.SidebarBackground;
        Padding = new Padding(10, 12, 10, 10);

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppColors.SidebarBackground
        };

        Controls.Add(stack);

        AddButton(stack, AppPageKey.Dashboard, "Dashboard");
        AddButton(stack, AppPageKey.CaseSetup, "Case Setup");
        AddSpacer(stack);
        AddButton(stack, AppPageKey.People, "People / Household");
        AddButton(stack, AppPageKey.Income, "Income Sources");
        AddButton(stack, AppPageKey.Bills, "Bills / Spending");
        AddButton(stack, AppPageKey.AllowanceSavings, "Allowance / Savings");
        AddButton(stack, AppPageKey.Assets, "Assets");
        AddButton(stack, AppPageKey.Debts, "Debts");
        AddButton(stack, AppPageKey.Documents, "Documents");
        AddButton(stack, AppPageKey.Vault, "Credential Vault");
    }

    public void SetSelected(AppPageKey key)
    {
        foreach ((AppPageKey buttonKey, Button button) in _buttons)
        {
            bool selected = buttonKey == key;
            button.BackColor = selected ? AppColors.SidebarButtonSelected : AppColors.SidebarButton;
            button.Font = selected ? AppFonts.SidebarSelected : AppFonts.Sidebar;
        }
    }

    private void AddButton(FlowLayoutPanel stack, AppPageKey key, string text)
    {
        var button = new Button
        {
            Text = text,
            Tag = key,
            Width = 188,
            Height = 42,
            Margin = new Padding(0, 0, 0, 7),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppColors.SidebarButton,
            ForeColor = AppColors.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = AppFonts.Sidebar,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = AppColors.Border;
        button.MouseEnter += (_, _) =>
        {
            if (button.Tag is AppPageKey pageKey && !_buttons.TryGetValue(pageKey, out _)) return;
            if (button.BackColor != AppColors.SidebarButtonSelected)
                button.BackColor = AppColors.SidebarButtonHover;
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.BackColor != AppColors.SidebarButtonSelected)
                button.BackColor = AppColors.SidebarButton;
        };
        button.Click += (_, _) => NavigationRequested?.Invoke(this, key);

        _buttons[key] = button;
        stack.Controls.Add(button);
    }

    private static void AddSpacer(FlowLayoutPanel stack)
    {
        stack.Controls.Add(new Panel
        {
            Width = 188,
            Height = 8,
            Margin = new Padding(0),
            BackColor = AppColors.SidebarBackground
        });
    }
}
