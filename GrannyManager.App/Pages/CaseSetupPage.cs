using GrannyManager.App.Navigation;
using GrannyManager.App.Services;
using GrannyManager.App.Themes;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;

namespace GrannyManager.App.Pages;

public sealed class CaseSetupPage : BasePage
{
    private const int BodyTopOffset = 108;

    private readonly CaseFolderService _caseFolderService = new();
    private readonly RecentCasesService _recentCasesService = new();

    private readonly TextBox _caseNameTextBox;
    private readonly TextBox _primaryPersonTextBox;
    private readonly TextBox _rootFolderTextBox;
    private readonly Label _activeCaseLabel;
    private readonly Button _changePinButton;
    private readonly ListBox _recentCasesListBox;

    public CaseSetupPage()
        : base(AppPageKey.CaseSetup, "Case Setup", "Create or open a local case folder. This is the foundation for documents, imports, exports, backups, and later database storage.")
    {
        var layout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Location = new Point(0, BodyTopOffset),
            Size = new Size(ContentPanel.ClientSize.Width, Math.Max(100, ContentPanel.ClientSize.Height - BodyTopOffset)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = AppColors.PanelBackground
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        ContentPanel.Controls.Add(layout);

        var leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(0, 0, 20, 0)
        };

        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppColors.PanelBackgroundAlt,
            Padding = new Padding(18)
        };

        layout.Controls.Add(leftPanel, 0, 0);
        layout.Controls.Add(rightPanel, 1, 0);

        var leftStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        leftPanel.Controls.Add(leftStack);
        leftStack.Resize += (_, _) => ResizeFlowChildren(leftStack);

        _activeCaseLabel = CreateBodyLabel("No active case is open yet.", 54, AppColors.Warning, AppFonts.BodyBold);
        leftStack.Controls.Add(_activeCaseLabel);

        _changePinButton = CreateActionButton("Change Case PIN");
        _changePinButton.Margin = new Padding(0, 0, 0, 14);
        _changePinButton.Click += (_, _) => ChangeActiveCasePin();
        leftStack.Controls.Add(_changePinButton);

        _rootFolderTextBox = CreateTextBox(_caseFolderService.GetDefaultCaseRoot());
        leftStack.Controls.Add(CreateFieldPanel("Case root folder", _rootFolderTextBox, CreateBrowseRootButton()));

        _primaryPersonTextBox = CreateTextBox(string.Empty);
        leftStack.Controls.Add(CreateFieldPanel("Primary person being helped", _primaryPersonTextBox));

        _caseNameTextBox = CreateTextBox(string.Empty);
        leftStack.Controls.Add(CreateFieldPanel("Case name / project name", _caseNameTextBox));

        var createButton = CreateActionButton("Create New Case");
        createButton.Margin = new Padding(0, 12, 0, 8);
        createButton.Click += (_, _) => CreateNewCase();
        leftStack.Controls.Add(createButton);

        var openButton = CreateActionButton("Open Existing .gmcase File");
        openButton.Click += (_, _) => OpenExistingCase();
        leftStack.Controls.Add(openButton);

        ResizeFlowChildren(leftStack);

        var recentTitle = CreateBodyLabel("Recent cases", 34, AppColors.TextPrimary, AppFonts.BodyBold);
        rightPanel.Controls.Add(recentTitle);
        recentTitle.Dock = DockStyle.Top;

        _recentCasesListBox = new ListBox
        {
            Dock = DockStyle.Top,
            Height = 420,
            BackColor = AppColors.SearchBoxBackground,
            ForeColor = AppColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AppFonts.Body,
            IntegralHeight = false
        };
        _recentCasesListBox.DoubleClick += (_, _) => OpenSelectedRecentCase();
        _recentCasesListBox.MouseDown += RecentCasesListBox_MouseDown;
        _recentCasesListBox.ContextMenuStrip = CreateRecentCasesContextMenu();
        rightPanel.Controls.Add(_recentCasesListBox);
        _recentCasesListBox.BringToFront();

        var recentOpenButton = CreateActionButton("Open Selected Recent Case");
        recentOpenButton.Click += (_, _) => OpenSelectedRecentCase();
        rightPanel.Controls.Add(recentOpenButton);
        recentOpenButton.Dock = DockStyle.Top;
        recentOpenButton.Margin = new Padding(0, 10, 0, 0);

        LoadRecentCases();
        AppState.ActiveCaseChanged += (_, activeCase) => UpdateActiveCaseLabel(activeCase);
        UpdateActiveCaseLabel(AppState.ActiveCase);
    }

    public override void OnNavigatedTo()
    {
        LoadRecentCases();
        UpdateActiveCaseLabel(AppState.ActiveCase);
    }

    private void CreateNewCase()
    {
        try
        {
            string primaryPerson = _primaryPersonTextBox.Text.Trim();
            string caseName = _caseNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(caseName) && !string.IsNullOrWhiteSpace(primaryPerson))
            {
                caseName = primaryPerson;
                _caseNameTextBox.Text = caseName;
            }

            if (string.IsNullOrWhiteSpace(caseName))
            {
                MessageBox.Show("Enter a case name first. A simple name like 'Mary Smith' or 'General Finances' is fine.", "Case name needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _caseNameTextBox.Focus();
                return;
            }

            if (!CasePinPrompt.TryPromptForNewPin(this, out string securityPin))
            {
                return;
            }

            CaseProfile profile = _caseFolderService.CreateCase(caseName, primaryPerson, _rootFolderTextBox.Text.Trim(), securityPin);
            AppState.SetActiveCase(profile);
            _recentCasesService.AddOrUpdate(profile);
            LoadRecentCases();

            MessageBox.Show(
                "Case created successfully.\r\n\r\n" + profile.CaseFolderPath,
                "Case created",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not create case", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenExistingCase()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open Granny Manager Case",
            Filter = "Granny Manager case (*.gmcase)|*.gmcase|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        OpenCaseFile(dialog.FileName);
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
        if (_recentCasesListBox.SelectedItem is not RecentCaseListItem selected)
        {
            MessageBox.Show("Select a recent case first.", "No recent case selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(selected.CaseFilePath))
        {
            return;
        }

        OpenCaseFile(selected.CaseFilePath);
    }

    private void OpenCaseFile(string caseFilePath)
    {
        try
        {
            CaseProfile profile = _caseFolderService.LoadCaseFromFile(caseFilePath);
            if (!CasePinPrompt.VerifyCasePin(this, profile, _caseFolderService))
            {
                return;
            }

            AppState.SetActiveCase(profile);
            _recentCasesService.AddOrUpdate(profile);
            LoadRecentCases();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not open case", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Button CreateBrowseRootButton()
    {
        var button = CreateSmallButton("Browse");
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose where Granny Manager case folders should be created",
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(_rootFolderTextBox.Text) ? _rootFolderTextBox.Text : _caseFolderService.GetDefaultCaseRoot()
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _rootFolderTextBox.Text = dialog.SelectedPath;
            }
        };
        return button;
    }

    private void LoadRecentCases()
    {
        _recentCasesListBox.Items.Clear();

        foreach (RecentCaseInfo item in _recentCasesService.LoadRecentCases())
        {
            _recentCasesListBox.Items.Add(new RecentCaseListItem(item.DisplayName, item.CaseFilePath, item.LastOpenedAt));
        }

        if (_recentCasesListBox.Items.Count == 0)
        {
            _recentCasesListBox.Items.Add(new RecentCaseListItem("No recent cases yet", string.Empty, DateTime.MinValue, isPlaceholder: true));
        }
    }

    private void UpdateActiveCaseLabel(CaseProfile? activeCase)
    {
        if (activeCase is null)
        {
            _activeCaseLabel.Text = "No active case is open yet.";
            _activeCaseLabel.ForeColor = AppColors.Warning;
            _changePinButton.Enabled = false;
            return;
        }

        _activeCaseLabel.Text = "Active case: " + activeCase.DisplayName + "\r\n" + activeCase.CaseFolderPath;
        _activeCaseLabel.ForeColor = AppColors.Good;
        _changePinButton.Enabled = true;
    }

    private void ChangeActiveCasePin()
    {
        var activeCase = AppState.ActiveCase;
        if (activeCase is null)
        {
            MessageBox.Show("Open a case before changing its PIN.", "No case open", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (CasePinPrompt.TryChangePin(this, activeCase, _caseFolderService))
        {
            _recentCasesService.AddOrUpdate(activeCase);
            LoadRecentCases();
        }
    }

    private static TextBox CreateTextBox(string text)
    {
        return new TextBox
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Text = text,
            BackColor = AppColors.SearchBoxBackground,
            ForeColor = AppColors.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AppFonts.Body,
            Margin = new Padding(0)
        };
    }

    private static Button CreateActionButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Height = 38,
            Width = 260,
            Margin = new Padding(0, 0, 0, 8),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppColors.SearchButtonBackground,
            ForeColor = AppColors.TextPrimary,
            Font = AppFonts.BodyBold,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = AppColors.Border;
        button.FlatAppearance.MouseOverBackColor = AppColors.SidebarButtonHover;
        button.FlatAppearance.MouseDownBackColor = AppColors.SidebarButtonSelected;
        return button;
    }

    private static Button CreateSmallButton(string text)
    {
        var button = CreateActionButton(text);
        button.Width = 86;
        button.Height = 26;
        button.Margin = new Padding(8, 0, 0, 0);
        return button;
    }

    private static Label CreateBodyLabel(string text, int height, Color color, Font font)
    {
        return new Label
        {
            Text = text,
            Width = 500,
            Height = height,
            ForeColor = color,
            Font = font,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 0, 0, 4),
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private static Panel CreateFieldPanel(string labelText, TextBox textBox, Button? sideButton = null)
    {
        var fieldPanel = new Panel
        {
            Width = 500,
            Height = 58,
            BackColor = AppColors.PanelBackground,
            Margin = new Padding(0, 0, 0, 10)
        };

        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            Height = 21,
            ForeColor = AppColors.TextMuted,
            Font = AppFonts.Body,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(0, 0, 0, 2)
        };

        var inputHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 27,
            BackColor = AppColors.PanelBackground,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        if (sideButton is not null)
        {
            sideButton.Dock = DockStyle.Right;
            inputHost.Controls.Add(sideButton);
        }

        inputHost.Controls.Add(textBox);
        textBox.BringToFront();

        fieldPanel.Controls.Add(inputHost);
        fieldPanel.Controls.Add(label);
        return fieldPanel;
    }

    private static void ResizeFlowChildren(FlowLayoutPanel flow)
    {
        int width = Math.Max(240, flow.ClientSize.Width - 24);

        foreach (Control child in flow.Controls)
        {
            if (child is Button button && button.Text != "Browse")
            {
                child.Width = Math.Min(300, width);
            }
            else
            {
                child.Width = width;
            }
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
            {
                return DisplayName;
            }

            return DisplayName + "  ·  " + LastOpenedAt.ToString("g");
        }
    }
}
