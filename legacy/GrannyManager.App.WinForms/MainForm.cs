using GrannyManager.App.Controls;
using GrannyManager.App.Navigation;
using GrannyManager.App.Services;
using GrannyManager.App.Themes;
using GrannyManager.Core.Models;
using System.Diagnostics;

namespace GrannyManager.App;

public sealed class MainForm : Form
{
    private readonly Panel _pageHost;
    private readonly SidebarNav _sidebar;
    private readonly MoneySummaryBar _summaryBar;
    private readonly NavigationService _navigation;
    private readonly FinanceSummaryService _financeSummaryService = new();
    private readonly System.Windows.Forms.Timer _summaryRefreshTimer;
    private TextBox? _searchTextBox;
    private Label? _statusLabel;
    private Button? _setupWizardButton;
    private Panel? _searchResultsPanel;
    private readonly CaseSearchService _caseSearchService = new();

    public MainForm()
    {
        Text = "Home & Family Finance Manager";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 720);
        Size = new Size(1300, 840);
        BackColor = AppColors.AppBackground;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = AppColors.AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        Controls.Add(root);

        _searchResultsPanel = BuildSearchResultsPanel();
        Controls.Add(_searchResultsPanel);
        _searchResultsPanel.BringToFront();

        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);

        _summaryBar = new MoneySummaryBar();
        _summaryBar.UpdateSummary(0m, 0m);
        root.Controls.Add(_summaryBar, 0, 1);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.AppBackground
        };
        root.Controls.Add(body, 0, 2);

        _sidebar = new SidebarNav();
        _sidebar.NavigationRequested += (_, key) => _navigation.NavigateTo(key);
        body.Controls.Add(_sidebar);

        _pageHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.AppBackground,
            Padding = new Padding(14)
        };
        body.Controls.Add(_pageHost);
        _pageHost.BringToFront();

        _navigation = new NavigationService(_pageHost, new PageFactory());
        _navigation.Navigated += (_, key) =>
        {
            _sidebar.SetSelected(key);
            RefreshMoneySummary();
        };
        AppState.ActiveCaseChanged += AppState_ActiveCaseChanged;

        _summaryRefreshTimer = new System.Windows.Forms.Timer
        {
            Interval = 1500
        };
        _summaryRefreshTimer.Tick += (_, _) => RefreshMoneySummary();

        Load += (_, _) =>
        {
            UpdateActiveCaseStatus(AppState.ActiveCase);
            RefreshMoneySummary();
            _summaryRefreshTimer.Start();
            _navigation.NavigateTo(AppPageKey.Dashboard);
        };
    }

    private const string SearchPlaceholderText = "Search for documents, names, keywords, etc.";

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.HeaderBackground,
            Padding = new Padding(0)
        };

        _searchTextBox = new TextBox
        {
            Width = 430,
            Height = 30,
            Location = new Point(28, 14),
            AutoSize = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = AppColors.SearchBoxBackground,
            ForeColor = AppColors.TextSubtle,
            Font = AppFonts.SearchBox,
            Text = SearchPlaceholderText,
            Margin = new Padding(0)
        };
        _searchTextBox.KeyDown += SearchTextBox_KeyDown;
        _searchTextBox.TextChanged += SearchTextBox_TextChanged;
        _searchTextBox.GotFocus += SearchTextBox_GotFocus;
        _searchTextBox.LostFocus += SearchTextBox_LostFocus;

        var searchButton = new Button
        {
            Width = 96,
            Height = 30,
            Location = new Point(466, 14),
            Text = "Search",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppColors.SearchButtonBackground,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.SearchButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        searchButton.FlatAppearance.BorderColor = AppColors.Border;
        searchButton.FlatAppearance.MouseOverBackColor = AppColors.SidebarButtonHover;
        searchButton.FlatAppearance.MouseDownBackColor = AppColors.SidebarButtonSelected;
        searchButton.Click += (_, _) => ShowSearchResultsFromCurrentText(force: true);

        _setupWizardButton = new Button
        {
            Width = 170,
            Height = 30,
            Location = new Point(574, 14),
            Text = "Start Finance Setup",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(38, 138, 76),
            ForeColor = Color.White,
            Font = AppFonts.SearchButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        _setupWizardButton.FlatAppearance.BorderColor = Color.FromArgb(72, 190, 116);
        _setupWizardButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 160, 88);
        _setupWizardButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(27, 105, 58);
        _setupWizardButton.Click += (_, _) => _navigation.NavigateTo(AppPageKey.FinanceWizard);

        var helpButton = new Button
        {
            Width = 34,
            Height = 30,
            Location = new Point(754, 14),
            Text = "?",
            FlatStyle = FlatStyle.Flat,
            BackColor = AppColors.SearchButtonBackground,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.SearchButton,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        helpButton.FlatAppearance.BorderColor = AppColors.Border;
        helpButton.FlatAppearance.MouseOverBackColor = AppColors.SidebarButtonHover;
        helpButton.FlatAppearance.MouseDownBackColor = AppColors.SidebarButtonSelected;
        helpButton.Click += (_, _) => OpenLocalHelp();

        _statusLabel = new Label
        {
            Text = "No case open · Build v0.9.23",
            AutoSize = false,
            Dock = DockStyle.Right,
            Width = 360,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.HeaderSmall,
            Padding = new Padding(0, 0, 20, 0)
        };

        header.Controls.Add(_statusLabel);
        header.Controls.Add(_searchTextBox);
        header.Controls.Add(searchButton);
        header.Controls.Add(_setupWizardButton);
        header.Controls.Add(helpButton);
        return header;
    }

    private Panel BuildSearchResultsPanel()
    {
        var panel = new Panel
        {
            Visible = false,
            Location = new Point(28, 50),
            Size = new Size(700, 430),
            BackColor = AppColors.PanelBackground,
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        panel.LostFocus += (_, _) => { };
        return panel;
    }


    private void AppState_ActiveCaseChanged(object? sender, CaseProfile? activeCase)
    {
        UpdateActiveCaseStatus(activeCase);
        HideSearchResults();
        RefreshMoneySummary();
    }


    private void OpenLocalHelp()
    {
        try
        {
            string helpPath = Path.Combine(AppContext.BaseDirectory, "Help", "index.html");
            if (!System.IO.File.Exists(helpPath))
            {
                MessageBox.Show(
                    "The local help files were not found. They should be in the Help folder next to the application.",
                    "Help not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = helpPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not open help", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateActiveCaseStatus(CaseProfile? activeCase)
    {
        if (_statusLabel is null)
        {
            return;
        }

        _statusLabel.Text = activeCase is null
            ? "No case open · Build v0.9.23"
            : $"Active case: {activeCase.DisplayName} · Build v0.9.23";

        if (_setupWizardButton is not null)
        {
            _setupWizardButton.Text = activeCase is null ? "Start Finance Setup" : "Continue Setup";
        }
    }

    private void RefreshMoneySummary()
    {
        if (_summaryBar is null)
        {
            return;
        }

        CaseMoneySummary summary = _financeSummaryService.BuildSummary(AppState.ActiveCase);
        _summaryBar.UpdateSummary(
            summary.MonthlyIncome,
            summary.MonthlyExpenses,
            summary.MonthlyAllowance,
            summary.MonthlySavings);
    }

    private void SearchTextBox_GotFocus(object? sender, EventArgs e)
    {
        if (_searchTextBox?.Text == SearchPlaceholderText)
        {
            _searchTextBox.Text = string.Empty;
            _searchTextBox.ForeColor = AppColors.TextPrimary;
        }
    }

    private void SearchTextBox_LostFocus(object? sender, EventArgs e)
    {
        if (_searchTextBox is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_searchTextBox.Text))
        {
            _searchTextBox.Text = SearchPlaceholderText;
            _searchTextBox.ForeColor = AppColors.TextSubtle;
        }
    }

    private void SearchTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_searchTextBox is null)
        {
            return;
        }

        if (!_searchTextBox.Focused || _searchTextBox.Text == SearchPlaceholderText)
        {
            return;
        }

        ShowSearchResultsFromCurrentText(force: false);
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            HideSearchResults();
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        ShowSearchResultsFromCurrentText(force: true);
    }

    private void ShowSearchResultsFromCurrentText(bool force)
    {
        var query = _searchTextBox?.Text.Trim() ?? string.Empty;

        if (query == SearchPlaceholderText)
        {
            query = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            if (force && _statusLabel is not null)
            {
                _statusLabel.Text = "Search ready · Enter a keyword, document tag, name, bill, or asset";
            }

            HideSearchResults();
            return;
        }

        if (!force && query.Length < 2)
        {
            HideSearchResults();
            return;
        }

        ShowSearchResults(query);
    }

    private void ShowSearchResults(string query)
    {
        if (_searchResultsPanel is null)
        {
            return;
        }

        _searchResultsPanel.SuspendLayout();
        _searchResultsPanel.Controls.Clear();

        if (AppState.ActiveCase is null)
        {
            AddSearchMessage(_searchResultsPanel, "Open a case first", "Search works inside the active case database.");
            ShowSearchPanel();
            _searchResultsPanel.ResumeLayout();
            return;
        }

        var results = _caseSearchService.Search(AppState.ActiveCase, query)
            .GroupBy(result => result.Category)
            .ToList();

        if (results.Count == 0)
        {
            AddSearchMessage(_searchResultsPanel, "No matches found", $"Nothing matched \"{query}\" in this case yet.");
            ShowSearchPanel();
            _searchResultsPanel.ResumeLayout();
            return;
        }

        var scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(10)
        };
        _searchResultsPanel.Controls.Add(scrollHost);

        var y = 8;
        foreach (var group in results)
        {
            var section = new Label
            {
                Text = group.Key,
                Location = new Point(10, y),
                Size = new Size(_searchResultsPanel.Width - 44, 22),
                ForeColor = AppColors.TextPrimary,
                Font = AppFonts.BodyBold,
                BackColor = AppColors.PanelBackground
            };
            scrollHost.Controls.Add(section);
            y += 26;

            foreach (var result in group.Take(8))
            {
                var row = BuildSearchResultRow(result, query, _searchResultsPanel.Width - 48, y);
                scrollHost.Controls.Add(row);
                y += row.Height + 6;
            }

            y += 8;
        }

        ShowSearchPanel();
        _searchResultsPanel.ResumeLayout();
    }

    private Panel BuildSearchResultRow(CaseSearchResult result, string query, int width, int y)
    {
        var row = new Panel
        {
            Location = new Point(10, y),
            Size = new Size(width, 56),
            BackColor = AppColors.SearchButtonBackground,
            Cursor = Cursors.Hand,
            Tag = result
        };

        row.Paint += (_, e) =>
        {
            using var pen = new Pen(AppColors.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
        };
        row.MouseEnter += (_, _) => row.BackColor = AppColors.SidebarButtonHover;
        row.MouseLeave += (_, _) => row.BackColor = AppColors.SearchButtonBackground;
        row.Click += (_, _) => OpenSearchResult(result);

        var title = BuildHighlightBox(result.Title, query, new Point(8, 5), new Size(width - 16, 22), AppFonts.BodyBold);
        var context = BuildHighlightBox(result.Context, query, new Point(8, 29), new Size(width - 16, 22), AppFonts.Body);

        title.Click += (_, _) => OpenSearchResult(result);
        context.Click += (_, _) => OpenSearchResult(result);
        row.Controls.Add(title);
        row.Controls.Add(context);
        return row;
    }

    private RichTextBox BuildHighlightBox(string text, string query, Point location, Size size, Font font)
    {
        var box = new RichTextBox
        {
            Text = text,
            Location = location,
            Size = size,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = AppColors.SearchButtonBackground,
            ForeColor = AppColors.TextPrimary,
            Font = font,
            ScrollBars = RichTextBoxScrollBars.None,
            TabStop = false,
            Cursor = Cursors.Hand
        };

        box.GotFocus += (_, _) => _searchTextBox?.Focus();
        ApplySearchHighlight(box, query);
        return box;
    }

    private static void ApplySearchHighlight(RichTextBox box, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var start = 0;
        while (start < box.TextLength)
        {
            var index = box.Text.IndexOf(query, start, StringComparison.CurrentCultureIgnoreCase);
            if (index < 0)
            {
                break;
            }

            box.Select(index, query.Length);
            box.SelectionBackColor = Color.FromArgb(100, 86, 26);
            box.SelectionColor = Color.White;
            start = index + query.Length;
        }

        box.Select(0, 0);
    }

    private void AddSearchMessage(Panel panel, string title, string detail)
    {
        panel.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(14, 14),
            Size = new Size(panel.Width - 28, 24),
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.BodyBold
        });
        panel.Controls.Add(new Label
        {
            Text = detail,
            Location = new Point(14, 42),
            Size = new Size(panel.Width - 28, 44),
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.Body
        });
    }

    private void OpenSearchResult(CaseSearchResult result)
    {
        HideSearchResults();
        _searchTextBox?.SelectAll();
        _navigation.NavigateTo(result.PageKey);

        var openedExactRecord = false;
        if (_navigation.CurrentPage is ISearchResultPage searchResultPage && result.RecordId > 0)
        {
            openedExactRecord = searchResultPage.OpenSearchResult(result.RecordId);
        }

        if (_statusLabel is not null)
        {
            _statusLabel.Text = openedExactRecord
                ? $"Opened {result.Category}: {result.Title}"
                : $"Opened {result.Category}";
        }
    }

    private void ShowSearchPanel()
    {
        if (_searchResultsPanel is null)
        {
            return;
        }

        _searchResultsPanel.Visible = true;
        _searchResultsPanel.BringToFront();
    }

    private void HideSearchResults()
    {
        if (_searchResultsPanel is null)
        {
            return;
        }

        _searchResultsPanel.Visible = false;
        _searchResultsPanel.Controls.Clear();
    }
}
