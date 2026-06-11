using GrannyManager.App.Navigation;
using GrannyManager.App.Services;
using GrannyManager.App.Themes;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;

namespace GrannyManager.App.Pages;

public sealed class DashboardPage : UserControl, IAppPage
{
    private readonly RecentCasesService _recentCasesService = new();
    private readonly CaseFolderService _caseFolderService = new();
    private readonly FinanceSummaryService _financeSummaryService = new();

    private Label _activeCaseLabel = null!;
    private ListBox _recentCasesListBox = null!;
    private ListBox _importantDocumentsListBox = null!;
    private Label _monthlyIncomeValue = null!;
    private Label _knownExpensesValue = null!;
    private Label _allowanceValue = null!;
    private Label _savingsValue = null!;
    private Label _remainingValue = null!;
    private Label _pastDueValue = null!;
    private Label _householdValue = null!;
    private Label _contributorsValue = null!;
    private Label _householdContributionValue = null!;
    private Label _billsValue = null!;
    private Label _reserveItemsValue = null!;

    public AppPageKey PageKey => AppPageKey.Dashboard;
    public string PageTitle => "Dashboard";

    public DashboardPage()
    {
        InitializeLayout();
        Load += (_, _) => RefreshDashboard();
        AppState.ActiveCaseChanged += (_, _) => RefreshDashboard();
    }

    public void OnNavigatedTo()
    {
        RefreshDashboard();
    }

    public bool CanNavigateAway() => true;

    private void InitializeLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = AppColors.AppBackground;

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(30, 28, 30, 24),
            AutoScroll = true
        };
        Controls.Add(content);

        var title = new Label
        {
            Text = "Dashboard",
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.PageTitle,
            AutoSize = true,
            Location = new Point(30, 24)
        };
        content.Controls.Add(title);


        _activeCaseLabel = new Label
        {
            Text = "No active case is open yet.",
            ForeColor = AppColors.Warning,
            Font = AppFonts.BodyBold,
            AutoSize = false,
            Location = new Point(32, 72),
            Size = new Size(900, 48)
        };
        content.Controls.Add(_activeCaseLabel);

        int statsTop = 136;
        int left = 32;
        int right = 528;
        int rightTop = 104;
        int rightWidth = 430;

        AddSectionTitle(content, "Monthly picture", left, statsTop);
        _monthlyIncomeValue = AddStatRow(content, "Monthly income", "$0.00", left, statsTop + 34);
        _householdContributionValue = AddStatRow(content, "Linked household income", "$0.00", left, statsTop + 64);
        _knownExpensesValue = AddStatRow(content, "Known expenses", "$0.00", left, statsTop + 94);
        _allowanceValue = AddStatRow(content, "Allowance", "$0.00", left, statsTop + 124);
        _savingsValue = AddStatRow(content, "Savings", "$0.00", left, statsTop + 154);
        _remainingValue = AddStatRow(content, "Remaining / deficit", "$0.00", left, statsTop + 184, AppColors.Good);
        _pastDueValue = AddStatRow(content, "Past due total", "$0.00", left, statsTop + 214, AppColors.Warning);

        AddSectionTitle(content, "Case pressure", left, statsTop + 280);
        _householdValue = AddStatRow(content, "Household members", "0", left, statsTop + 314);
        _contributorsValue = AddStatRow(content, "Contributors", "0", left, statsTop + 344);
        _billsValue = AddStatRow(content, "Active bills / spending", "0", left, statsTop + 374);
        _reserveItemsValue = AddStatRow(content, "Allowance / savings items", "0", left, statsTop + 404);

        AddSectionTitle(content, "Recent cases", right, rightTop);
        _recentCasesListBox = new ListBox
        {
            Location = new Point(right, rightTop + 34),
            Size = new Size(rightWidth, 120),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            BackColor = AppColors.SearchBoxBackground,
            ForeColor = AppColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AppFonts.Body,
            IntegralHeight = false
        };
        _recentCasesListBox.DoubleClick += (_, _) => OpenSelectedRecentCase();
        _recentCasesListBox.MouseDown += RecentCasesListBox_MouseDown;
        _recentCasesListBox.ContextMenuStrip = CreateRecentCasesContextMenu();
        content.Controls.Add(_recentCasesListBox);

        var openButton = CreateButton("Open Selected Recent Case");
        openButton.Location = new Point(right, rightTop + 166);
        openButton.Size = new Size(240, 38);
        openButton.Click += (_, _) => OpenSelectedRecentCase();
        content.Controls.Add(openButton);

        AddSectionTitle(content, "Important documents", right, rightTop + 224);
        _importantDocumentsListBox = new ListBox
        {
            Location = new Point(right, rightTop + 258),
            Size = new Size(rightWidth, 125),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            BackColor = AppColors.SearchBoxBackground,
            ForeColor = AppColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AppFonts.Body,
            IntegralHeight = false
        };
        _importantDocumentsListBox.DoubleClick += (_, _) => OpenSelectedImportantDocument();
        content.Controls.Add(_importantDocumentsListBox);

        var openDocumentButton = CreateButton("Open Selected Document");
        openDocumentButton.Location = new Point(right, rightTop + 395);
        openDocumentButton.Size = new Size(220, 38);
        openDocumentButton.Click += (_, _) => OpenSelectedImportantDocument();
        content.Controls.Add(openDocumentButton);

    }

    private static void AddSectionTitle(Control parent, string text, int x, int y)
    {
        var label = new Label
        {
            Text = text,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.BodyBold,
            AutoSize = true,
            Location = new Point(x, y)
        };
        parent.Controls.Add(label);
    }

    private static Label AddStatRow(Control parent, string labelText, string valueText, int x, int y, Color? valueColor = null)
    {
        var label = new Label
        {
            Text = labelText + ":",
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.BodyBold,
            AutoSize = false,
            Location = new Point(x, y),
            Size = new Size(210, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };
        parent.Controls.Add(label);

        var value = new Label
        {
            Text = valueText,
            ForeColor = valueColor ?? AppColors.TextPrimary,
            Font = AppFonts.BodyBold,
            AutoSize = false,
            Location = new Point(x + 220, y),
            Size = new Size(260, 24),
            TextAlign = ContentAlignment.MiddleLeft
        };
        parent.Controls.Add(value);
        return value;
    }

    private Button CreateButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Height = 38,
            BackColor = AppColors.SearchButtonBackground,
            ForeColor = AppColors.TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = AppFonts.BodyBold,
            Margin = new Padding(0, 10, 0, 0)
        };
        button.FlatAppearance.BorderColor = AppColors.Border;
        button.FlatAppearance.MouseOverBackColor = AppColors.SidebarButtonHover;
        return button;
    }

    private void RefreshDashboard()
    {
        LoadRecentCases();
        LoadImportantDocuments();
        UpdateActiveCaseLabel(AppState.ActiveCase);
        UpdateMoneyStats();
    }

    private void LoadRecentCases()
    {
        _recentCasesListBox.Items.Clear();

        foreach (RecentCaseInfo item in _recentCasesService.LoadRecentCases())
            _recentCasesListBox.Items.Add(new RecentCaseListItem(item.DisplayName, item.CaseFilePath, item.LastOpenedAt));

        if (_recentCasesListBox.Items.Count == 0)
            _recentCasesListBox.Items.Add(new RecentCaseListItem("No recent cases yet", string.Empty, DateTime.MinValue, isPlaceholder: true));
    }

    private void LoadImportantDocuments()
    {
        _importantDocumentsListBox.Items.Clear();

        var activeCase = AppState.ActiveCase;
        if (activeCase is null || string.IsNullOrWhiteSpace(activeCase.CaseFolderPath))
        {
            _importantDocumentsListBox.Items.Add(new ImportantDocumentListItem("No active case open", string.Empty, string.Empty, isPlaceholder: true));
            return;
        }

        try
        {
            string databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var importantDocuments = new DocumentsRepository(databasePath)
                .GetAll()
                .Where(document => document.IsActive && document.IsImportant)
                .Take(8);

            foreach (DocumentRecord document in importantDocuments)
                _importantDocumentsListBox.Items.Add(new ImportantDocumentListItem(document.Title, document.Category, document.StoredFilePath));
        }
        catch
        {
            _importantDocumentsListBox.Items.Add(new ImportantDocumentListItem("Could not load important documents", string.Empty, string.Empty, isPlaceholder: true));
            return;
        }

        if (_importantDocumentsListBox.Items.Count == 0)
            _importantDocumentsListBox.Items.Add(new ImportantDocumentListItem("No important documents flagged yet", string.Empty, string.Empty, isPlaceholder: true));
    }

    private ContextMenuStrip CreateRecentCasesContextMenu()
    {
        var menu = new ContextMenuStrip();
        var removeItem = new ToolStripMenuItem("Remove from list");
        removeItem.Click += (_, _) => RemoveSelectedRecentCase();
        menu.Items.Add(removeItem);
        menu.Opening += (_, e) =>
        {
            e.Cancel = _recentCasesListBox.SelectedItem is not RecentCaseListItem selected || string.IsNullOrWhiteSpace(selected.CaseFilePath);
        };
        return menu;
    }

    private void RecentCasesListBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
            return;

        int index = _recentCasesListBox.IndexFromPoint(e.Location);
        if (index >= 0)
            _recentCasesListBox.SelectedIndex = index;
    }

    private void RemoveSelectedRecentCase()
    {
        if (_recentCasesListBox.SelectedItem is not RecentCaseListItem selected || string.IsNullOrWhiteSpace(selected.CaseFilePath))
            return;

        _recentCasesService.RemoveByPath(selected.CaseFilePath);
        LoadRecentCases();
    }

    private void OpenSelectedRecentCase()
    {
        if (_recentCasesListBox.SelectedItem is not RecentCaseListItem selected || string.IsNullOrWhiteSpace(selected.CaseFilePath))
            return;

        try
        {
            CaseProfile profile = _caseFolderService.LoadCaseFromFile(selected.CaseFilePath);
            if (!CasePinPrompt.VerifyCasePin(this, profile, _caseFolderService))
            {
                return;
            }

            AppState.SetActiveCase(profile);
            _recentCasesService.AddOrUpdate(profile);
            RefreshDashboard();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not open case", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenSelectedImportantDocument()
    {
        if (_importantDocumentsListBox.SelectedItem is not ImportantDocumentListItem selected || selected.IsPlaceholder || string.IsNullOrWhiteSpace(selected.StoredFilePath))
            return;

        if (!System.IO.File.Exists(selected.StoredFilePath))
        {
            MessageBox.Show("The copied document file could not be found.", "File missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(selected.StoredFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not open document", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateActiveCaseLabel(CaseProfile? activeCase)
    {
        if (activeCase is null)
        {
            _activeCaseLabel.Text = "No active case is open yet.";
            _activeCaseLabel.ForeColor = AppColors.Warning;
            return;
        }

        _activeCaseLabel.Text = "Active case: " + activeCase.DisplayName + "\r\n" + activeCase.CaseFolderPath;
        _activeCaseLabel.ForeColor = AppColors.Good;
    }

    private void UpdateMoneyStats()
    {
        CaseMoneySummary summary = _financeSummaryService.BuildSummary(AppState.ActiveCase);

        _monthlyIncomeValue.Text = summary.MonthlyIncome.ToString("C");
        _householdContributionValue.Text = summary.HouseholdContributionMonthly.ToString("C");
        _knownExpensesValue.Text = summary.MonthlyExpenses.ToString("C");
        _allowanceValue.Text = summary.MonthlyAllowance.ToString("C");
        _savingsValue.Text = summary.MonthlySavings.ToString("C");
        _remainingValue.Text = summary.Remaining.ToString("C");
        _pastDueValue.Text = summary.PastDue.ToString("C");

        _remainingValue.ForeColor = summary.Remaining < 0m
            ? AppColors.Bad
            : summary.Remaining < 250m ? AppColors.Warning : AppColors.Good;

        _householdValue.Text = summary.HouseholdMemberCount.ToString();
        _contributorsValue.Text = summary.ContributorCount.ToString();
        _billsValue.Text = summary.ActiveBillCount.ToString();
        _reserveItemsValue.Text = $"{summary.ActiveReserveCount} ({summary.AllowanceCount} allowance / {summary.SavingsCount} savings)";
    }

    private string GetHouseholdCountText()
    {
        var activeCase = AppState.ActiveCase;
        if (activeCase is null || string.IsNullOrWhiteSpace(activeCase.CaseFolderPath))
            return "0";

        try
        {
            string databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var people = new HouseholdPeopleRepository(databasePath).GetAll();
            int household = people.Count(person => person.LivesInHousehold);
            return household.ToString();
        }
        catch
        {
            return "0";
        }
    }

    private string GetContributorCountText()
    {
        var activeCase = AppState.ActiveCase;
        if (activeCase is null || string.IsNullOrWhiteSpace(activeCase.CaseFolderPath))
            return "0";

        try
        {
            string databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(activeCase.CaseFolderPath);
            var people = new HouseholdPeopleRepository(databasePath).GetAll();
            int contributors = people.Count(person => person.LinkedIncomeSourceId > 0 || person.PaysRent);
            return contributors.ToString();
        }
        catch
        {
            return "0";
        }
    }

    private sealed class ImportantDocumentListItem
    {
        public ImportantDocumentListItem(string title, string category, string storedFilePath, bool isPlaceholder = false)
        {
            Title = title;
            Category = category;
            StoredFilePath = storedFilePath;
            IsPlaceholder = isPlaceholder;
        }

        public string Title { get; }
        public string Category { get; }
        public string StoredFilePath { get; }
        public bool IsPlaceholder { get; }

        public override string ToString()
        {
            if (IsPlaceholder)
                return Title;

            return string.IsNullOrWhiteSpace(Category)
                ? Title
                : Title + "  ·  " + Category;
        }
    }

    private sealed class RecentCaseListItem
    {
        private readonly bool _isPlaceholder;

        public RecentCaseListItem(string displayName, string caseFilePath, DateTime lastOpenedAt, bool isPlaceholder = false)
        {
            DisplayName = displayName;
            CaseFilePath = caseFilePath;
            LastOpenedAt = lastOpenedAt;
            _isPlaceholder = isPlaceholder;
        }

        public string DisplayName { get; }
        public string CaseFilePath { get; }
        public DateTime LastOpenedAt { get; }

        public override string ToString()
        {
            if (_isPlaceholder)
                return DisplayName;

            return DisplayName + "  ·  " + LastOpenedAt.ToString("g");
        }
    }
}
