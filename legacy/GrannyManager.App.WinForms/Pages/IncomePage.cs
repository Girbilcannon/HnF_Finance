using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class IncomePage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel = Color.FromArgb(16, 34, 55);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);

    private readonly BindingList<IncomeSource> _incomeSources = new();

    private IncomeSourcesRepository? _repository;
    private string? _caseFolder;
    private IncomeSource? _selectedIncome;

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addIncomeButton = null!;
    private Button _viewProfileButton = null!;
    private Button _editSelectedButton = null!;

    private Panel _profilePanel = null!;
    private Label _profileTitleLabel = null!;
    private FlowLayoutPanel _profileDetailsPanel = null!;
    private Button _profileBackButton = null!;
    private Button _profileEditButton = null!;
    private Button _profileRemoveButton = null!;
    private Button _profileExportButton = null!;

    private Panel _editPanel = null!;
    private Label _editTitleLabel = null!;
    private TextBox _sourceNameTextBox = null!;
    private ComboBox _incomeTypeComboBox = null!;
    private Label _amountLabel = null!;
    private TextBox _amountTextBox = null!;
    private CheckBox _taxesWithheldCheckBox = null!;
    private ComboBox _frequencyComboBox = null!;
    private TextBox _expectedDayTextBox = null!;
    private ComboBox _depositMethodComboBox = null!;
    private Label _bankAccountLabel = null!;
    private ComboBox _bankAccountComboBox = null!;
    private readonly List<BankAssetOption> _bankAccountOptions = new();
    private CheckBox _activeCheckBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public IncomePage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
    }

    public AppPageKey PageKey => AppPageKey.Income;
    public string PageTitle => "Income Sources";

    public void OnNavigatedTo()
    {
        InitializeForActiveCase();
    }

    public bool CanNavigateAway() => true;

    private void BuildUi()
    {
        Controls.Clear();

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            Padding = new Padding(28)
        };
        Controls.Add(root);

        var title = new Label
        {
            Text = "Income Sources",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Track recurring and irregular income, then normalize each source into a monthly estimate.",
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(subtitle);
        subtitle.BringToFront();

        _caseStatusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(_caseStatusLabel);
        _caseStatusLabel.BringToFront();

        _statsLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            ForeColor = Good,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(_statsLabel);
        _statsLabel.BringToFront();

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            Padding = new Padding(0, 12, 0, 0)
        };
        root.Controls.Add(_contentHost);
        _contentHost.BringToFront();

        BuildListPanel();
        BuildProfilePanel();
        BuildEditPanel();
        ShowListPanel();
    }

    private void BuildListPanel()
    {
        _listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            Padding = new Padding(0)
        };
        _contentHost.Controls.Add(_listPanel);

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back,
            Padding = new Padding(0, 8, 0, 0)
        };
        _listPanel.Controls.Add(buttonRow);

        _addIncomeButton = CreateButton("Add Income Source", 0, 0, 150);
        _viewProfileButton = CreateButton("View Profile", 0, 0, 120);
        _editSelectedButton = CreateButton("Edit Selected", 0, 0, 125);
        buttonRow.Controls.Add(_addIncomeButton);
        buttonRow.Controls.Add(_viewProfileButton);
        buttonRow.Controls.Add(_editSelectedButton);

        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Border,
            Padding = new Padding(1)
        };
        _listPanel.Controls.Add(gridHost);
        gridHost.BringToFront();

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Back,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Border,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars = ScrollBars.Both,
            DataSource = _incomeSources
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.DefaultCellStyle.BackColor = Back;
        _grid.DefaultCellStyle.ForeColor = TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(43, 70, 105);
        _grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(13, 29, 48);
        _grid.RowTemplate.Height = 30;

        AddFillTextColumn("SourceName", "Source", 36f);
        AddFillTextColumn("IncomeType", "Type", 26f);
        AddTextColumn("Amount", "Amount", 110, "C2");
        AddFillTextColumn("Frequency", "Frequency", 22f);
        AddTextColumn("MonthlyEquivalent", "Monthly Est.", 120, "C2");

        _grid.SelectionChanged += (_, _) => UpdateSelectionFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowSelectedProfile();
        ListPageGridHelper.AttachRightClickRemove(_grid, UpdateSelectionFromGrid, RemoveSelectedIncome);

        gridHost.Controls.Add(_grid);

        _addIncomeButton.Click += (_, _) => BeginAddIncome();
        _viewProfileButton.Click += (_, _) => ShowSelectedProfile();
        _editSelectedButton.Click += (_, _) => BeginEditSelectedIncome();
    }

    private void BuildProfilePanel()
    {
        _profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            AutoScroll = true,
            Visible = false
        };
        _contentHost.Controls.Add(_profilePanel);
        _profilePanel.Resize += (_, _) => UpdateProfileContentWidth();

        var buttonRow = new FlowLayoutPanel
        {
            Location = new Point(0, 0),
            Size = new Size(700, 46),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back
        };

        _profileBackButton = CreateButton("Back to List", 0, 0, 120);
        _profileEditButton = CreateButton("Edit", 0, 0, 90);
        _profileRemoveButton = CreateButton("Remove", 0, 0, 100);
        _profileExportButton = CreateButton("Export PDF", 0, 0, 120);
        buttonRow.Controls.Add(_profileBackButton);
        buttonRow.Controls.Add(_profileEditButton);
        buttonRow.Controls.Add(_profileRemoveButton);
        buttonRow.Controls.Add(_profileExportButton);
        _profilePanel.Controls.Add(buttonRow);

        _profileTitleLabel = new Label
        {
            Location = new Point(0, 58),
            Size = new Size(720, 40),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _profilePanel.Controls.Add(_profileTitleLabel);

        _profileDetailsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 112),
            Size = new Size(760, 760),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Back
        };
        _profilePanel.Controls.Add(_profileDetailsPanel);

        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileEditButton.Click += (_, _) => BeginEditSelectedIncome();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedIncome();
        _profileExportButton.Click += (_, _) => ExportCurrentIncomePdf();
    }

    private void BuildEditPanel()
    {
        _editPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            AutoScroll = true,
            Visible = false
        };
        _contentHost.Controls.Add(_editPanel);

        _editTitleLabel = new Label
        {
            Text = "Add Income Source",
            Location = new Point(0, 0),
            Size = new Size(700, 42),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _editPanel.Controls.Add(_editTitleLabel);

        _sourceNameTextBox = CreateTextBox(_editPanel, "Source name", 58, 420);

        var incomeTypeLabel = new Label
        {
            Text = "Income type",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 116)
        };
        _editPanel.Controls.Add(incomeTypeLabel);

        _incomeTypeComboBox = new ComboBox
        {
            Location = new Point(22, 138),
            Size = new Size(300, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        _incomeTypeComboBox.Items.AddRange(new object[]
        {
            "Social Security",
            "Pension",
            "Survivor Benefits",
            "Disability",
            "Employment / Wages",
            "Family Contribution",
            "Rental Income",
            "Retirement Account",
            "Settlement / Lump Sum",
            "Other"
        });
        _incomeTypeComboBox.SelectedItem = "Other";
        _editPanel.Controls.Add(_incomeTypeComboBox);

        _taxesWithheldCheckBox = CreateCheckBox("Taxes withheld", 184);
        _editPanel.Controls.Add(_taxesWithheldCheckBox);
        _taxesWithheldCheckBox.CheckedChanged += (_, _) => UpdateAmountLabel();

        _amountLabel = new Label
        {
            Text = "Gross Pay",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 222)
        };
        _editPanel.Controls.Add(_amountLabel);

        _amountTextBox = new TextBox
        {
            Location = new Point(22, 244),
            Size = new Size(180, 26),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        _editPanel.Controls.Add(_amountTextBox);

        var frequencyLabel = new Label
        {
            Text = "Frequency",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 300)
        };
        _editPanel.Controls.Add(frequencyLabel);

        _frequencyComboBox = new ComboBox
        {
            Location = new Point(22, 322),
            Size = new Size(220, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        _frequencyComboBox.Items.AddRange(new object[]
        {
            "Weekly",
            "Every 2 weeks",
            "Twice monthly",
            "Monthly",
            "Quarterly",
            "Yearly",
            "One-time / irregular"
        });
        _frequencyComboBox.SelectedItem = "Monthly";
        _editPanel.Controls.Add(_frequencyComboBox);

        _expectedDayTextBox = CreateTextBox(_editPanel, "Expected day/date", 368, 300);

        var depositMethodLabel = new Label
        {
            Text = "Deposit method / destination",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 426)
        };
        _editPanel.Controls.Add(depositMethodLabel);

        _depositMethodComboBox = new ComboBox
        {
            Location = new Point(22, 448),
            Size = new Size(260, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        _depositMethodComboBox.Items.AddRange(new object[]
        {
            "Cash",
            "Check",
            "Select Bank Account",
            "Add Bank Account"
        });
        _depositMethodComboBox.SelectedItem = "Cash";
        _editPanel.Controls.Add(_depositMethodComboBox);

        _bankAccountLabel = new Label
        {
            Text = "Bank account",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 494),
            Visible = false
        };
        _editPanel.Controls.Add(_bankAccountLabel);

        _bankAccountComboBox = new ComboBox
        {
            Location = new Point(22, 516),
            Size = new Size(420, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Visible = false
        };
        _editPanel.Controls.Add(_bankAccountComboBox);

        _activeCheckBox = CreateCheckBox("Active income source", 574);
        _activeCheckBox.Checked = true;
        _editPanel.Controls.Add(_activeCheckBox);

        _notesTextBox = CreateTextBox(_editPanel, "Notes", 616, 650, multiline: true);

        _saveButton = CreateButton("Save Income Source", 22, 738, 170);
        _cancelButton = CreateButton("Cancel", 205, 738, 110);
        _editPanel.Controls.Add(_saveButton);
        _editPanel.Controls.Add(_cancelButton);

        _depositMethodComboBox.SelectedIndexChanged += (_, _) => HandleDepositMethodChanged();

        _saveButton.Click += (_, _) => SaveIncomeFromEditor();
        _cancelButton.Click += (_, _) => CancelEdit();
    }

    private void UpdateAmountLabel()
    {
        if (_amountLabel is null || _taxesWithheldCheckBox is null)
            return;

        _amountLabel.Text = _taxesWithheldCheckBox.Checked
            ? "Payment Amount (After Taxes)"
            : "Gross Pay";
    }

    private void InitializeForActiveCase()
    {
        var activeCase = AppState.ActiveCase;
        _caseFolder = activeCase?.CaseFolderPath;

        if (string.IsNullOrWhiteSpace(_caseFolder))
            _caseFolder = CaseDatabaseLocator.TryFindActiveCaseFolder();

        if (string.IsNullOrWhiteSpace(_caseFolder))
        {
            _repository = null;
            _incomeSources.Clear();
            _caseStatusLabel.Text = "No active case is open. Open or create a case before adding income sources.";
            _caseStatusLabel.ForeColor = Danger;
            UpdateStats();
            return;
        }

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
        _repository = new IncomeSourcesRepository(databasePath);
        ReloadBankAccountOptions();
        _caseStatusLabel.Text = "Active case folder: " + _caseFolder;
        _caseStatusLabel.ForeColor = TextMuted;
        ReloadIncome();
    }

    private void ReloadIncome()
    {
        _incomeSources.Clear();

        if (_repository is null)
        {
            UpdateStats();
            return;
        }

        foreach (var source in _repository.GetAll())
            _incomeSources.Add(source);

        UpdateStats();
        ListPageGridHelper.ApplyInactiveRowStyles(_grid, item => item is IncomeSource source && source.IsActive);
        UpdateSelectionFromGrid();
    }

    private void UpdateStats()
    {
        var activeSources = _incomeSources.Count(i => i.IsActive);
        var monthlyTotal = _incomeSources.Sum(i => i.MonthlyEquivalent);
        _statsLabel.Text = $"Income sources: {_incomeSources.Count}     Active: {activeSources}     Estimated monthly income: {monthlyTotal:C2}";
    }

    private void UpdateSelectionFromGrid()
    {
        _selectedIncome = _grid.CurrentRow?.DataBoundItem as IncomeSource;
        var hasSelection = _selectedIncome is not null;
        _viewProfileButton.Enabled = hasSelection;
        _editSelectedButton.Enabled = hasSelection;
    }

    private void BeginAddIncome()
    {
        if (_repository is null)
        {
            MessageBox.Show("Open or create a case first.", "No active case", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _selectedIncome = new IncomeSource { IsActive = true, Frequency = "Monthly" };
        LoadEditor(_selectedIncome, isNew: true);
        ShowEditPanel();
    }

    private void BeginEditSelectedIncome()
    {
        if (_selectedIncome is null)
        {
            MessageBox.Show("Select an income source first.", "No income selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        LoadEditor(_selectedIncome, isNew: false);
        ShowEditPanel();
    }

    private void ShowSelectedProfile()
    {
        if (_selectedIncome is null)
        {
            MessageBox.Show("Select an income source first.", "No income selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _profileTitleLabel.Text = _selectedIncome.SourceName;
        PopulateProfileDetails(_selectedIncome);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var source = _incomeSources.FirstOrDefault(i => i.Id == recordId);
        if (source is null && _repository is not null)
            source = _repository.GetAll().FirstOrDefault(i => i.Id == recordId);

        if (source is null)
            return false;

        _selectedIncome = source;
        SelectGridRowByIncomeId(recordId);
        _profileTitleLabel.Text = source.SourceName;
        PopulateProfileDetails(source);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByIncomeId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is IncomeSource source && source.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void LoadEditor(IncomeSource source, bool isNew)
    {
        _editTitleLabel.Text = isNew ? "Add Income Source" : "Edit Income Source";
        _sourceNameTextBox.Text = source.SourceName;
        _incomeTypeComboBox.SelectedItem = string.IsNullOrWhiteSpace(source.IncomeType) ? "Other" : source.IncomeType;
        if (_incomeTypeComboBox.SelectedItem is null)
            _incomeTypeComboBox.SelectedItem = "Other";
        _taxesWithheldCheckBox.Checked = source.TaxesWithheld;
        UpdateAmountLabel();
        _amountTextBox.Text = source.Amount == 0m ? string.Empty : source.Amount.ToString("0.##", CultureInfo.CurrentCulture);
        _frequencyComboBox.SelectedItem = string.IsNullOrWhiteSpace(source.Frequency) ? "Monthly" : source.Frequency;
        if (_frequencyComboBox.SelectedItem is null)
            _frequencyComboBox.SelectedItem = "Monthly";
        _expectedDayTextBox.Text = source.ExpectedDayOrDate;
        ReloadBankAccountOptions();
        var depositMethod = string.IsNullOrWhiteSpace(source.DepositMethod)
            ? GuessDepositMethodFromLegacyValue(source.DepositedToAccount)
            : source.DepositMethod;

        if (depositMethod == "Bank Account")
            depositMethod = "Select Bank Account";

        if (_depositMethodComboBox.Items.Contains(depositMethod))
            _depositMethodComboBox.SelectedItem = depositMethod;
        else
            _depositMethodComboBox.SelectedItem = "Cash";

        SelectBankAccountOption(source.LinkedBankAssetId, source.LinkedBankAssetName);
        UpdateDepositDestinationVisibility();
        _activeCheckBox.Checked = source.IsActive;
        _notesTextBox.Text = source.Notes;
    }


    private void HandleDepositMethodChanged()
    {
        var method = _depositMethodComboBox.SelectedItem?.ToString() ?? "Cash";

        if (method == "Add Bank Account")
        {
            var newBank = CreateBankAccountFromIncomeDialog();

            if (newBank is not null)
            {
                ReloadBankAccountOptions();
                _depositMethodComboBox.SelectedItem = "Select Bank Account";
                SelectBankAccountOption(newBank.Id, newBank.DisplayName);
            }
            else
            {
                _depositMethodComboBox.SelectedItem = _bankAccountComboBox.Items.Count > 0 ? "Select Bank Account" : "Cash";
            }
        }

        UpdateDepositDestinationVisibility();
    }

    private void UpdateDepositDestinationVisibility()
    {
        var showBankPicker = string.Equals(_depositMethodComboBox.SelectedItem?.ToString(), "Select Bank Account", StringComparison.OrdinalIgnoreCase);

        _bankAccountLabel.Visible = showBankPicker;
        _bankAccountComboBox.Visible = showBankPicker;

        ReflowDepositDestinationSection(showBankPicker);
    }

    private void ReflowDepositDestinationSection(bool showBankPicker)
    {
        // Keep conditional controls directly under the deposit method selector.
        // Do not use a fixed absolute Y value here; small earlier layout changes can
        // otherwise leave a large blank gap before the bank account picker.
        const int left = 22;
        var nextY = _depositMethodComboBox.Bottom + 14;

        if (showBankPicker)
        {
            _bankAccountLabel.Location = new Point(left, nextY);
            _bankAccountComboBox.Location = new Point(left, nextY + 22);
            _bankAccountLabel.BringToFront();
            _bankAccountComboBox.BringToFront();
            nextY = _bankAccountComboBox.Bottom + 38;
        }

        _activeCheckBox.Location = new Point(left, nextY);

        nextY = _activeCheckBox.Bottom + 24;

        var notesLabel = FindEditLabel("Notes");
        if (notesLabel is not null)
            notesLabel.Location = new Point(left, nextY);

        _notesTextBox.Location = new Point(left, nextY + 22);

        nextY = _notesTextBox.Bottom + 14;

        _saveButton.Location = new Point(left, nextY);
        _cancelButton.Location = new Point(205, nextY);
    }

    private Label? FindEditLabel(string text)
    {
        return _editPanel.Controls
            .OfType<Label>()
            .FirstOrDefault(label => string.Equals(label.Text, text, StringComparison.OrdinalIgnoreCase));
    }

    private static string GuessDepositMethodFromLegacyValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Cash";

        var trimmed = value.Trim();

        if (string.Equals(trimmed, "Cash", StringComparison.OrdinalIgnoreCase))
            return "Cash";

        if (string.Equals(trimmed, "Check", StringComparison.OrdinalIgnoreCase))
            return "Check";

        return "Select Bank Account";
    }

    private void ReloadBankAccountOptions()
    {
        _bankAccountOptions.Clear();

        if (_bankAccountComboBox is not null)
            _bankAccountComboBox.Items.Clear();

        if (string.IsNullOrWhiteSpace(_caseFolder))
            return;

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
            DatabaseInitializer.EnsureCreated(databasePath);

            using var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT Id, AssetName, InstitutionName, AccountNickname, CurrentBalanceValue
FROM Assets
WHERE AssetType = 'Bank'
ORDER BY AssetName COLLATE NOCASE;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                var assetName = reader.GetString(1);
                var institution = reader.GetString(2);
                var nickname = reader.GetString(3);
                var balance = Convert.ToDecimal(reader.GetDouble(4));

                var pieces = new List<string>();
                if (!string.IsNullOrWhiteSpace(assetName))
                    pieces.Add(assetName.Trim());

                var detailPieces = new List<string>();
                if (!string.IsNullOrWhiteSpace(institution))
                    detailPieces.Add(institution.Trim());
                if (!string.IsNullOrWhiteSpace(nickname))
                    detailPieces.Add(nickname.Trim());

                var display = pieces.Count > 0 ? pieces[0] : "Bank Account";
                if (detailPieces.Count > 0)
                    display += " (" + string.Join(" - ", detailPieces) + ")";

                _bankAccountOptions.Add(new BankAssetOption(id, display, balance));
            }
        }
        catch
        {
            // Keep income editing available even if bank assets cannot be loaded.
        }

        if (_bankAccountComboBox is not null)
        {
            foreach (var option in _bankAccountOptions)
                _bankAccountComboBox.Items.Add(option);

            if (_bankAccountComboBox.Items.Count > 0 && _bankAccountComboBox.SelectedIndex < 0)
                _bankAccountComboBox.SelectedIndex = 0;
        }
    }

    private void SelectBankAccountOption(long bankAssetId, string? bankAssetName)
    {
        if (_bankAccountComboBox is null)
            return;

        for (var i = 0; i < _bankAccountComboBox.Items.Count; i++)
        {
            if (_bankAccountComboBox.Items[i] is BankAssetOption option)
            {
                if (bankAssetId > 0 && option.Id == bankAssetId)
                {
                    _bankAccountComboBox.SelectedIndex = i;
                    return;
                }

                if (bankAssetId <= 0 &&
                    !string.IsNullOrWhiteSpace(bankAssetName) &&
                    string.Equals(option.DisplayName, bankAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    _bankAccountComboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        if (_bankAccountComboBox.Items.Count > 0 && _bankAccountComboBox.SelectedIndex < 0)
            _bankAccountComboBox.SelectedIndex = 0;
    }

    private BankAssetOption? CreateBankAccountFromIncomeDialog()
    {
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return null;

        using var dialog = new Form
        {
            Text = "Create Bank Account Asset",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(500, 390),
            MinimumSize = new Size(500, 390),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Back,
            ForeColor = TextPrimary
        };

        var title = new Label
        {
            Text = "Create Bank Account",
            Location = new Point(22, 18),
            Size = new Size(430, 32),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold)
        };
        dialog.Controls.Add(title);

        var nameBox = CreateDialogTextBox(dialog, "Account / asset name", 72, 420);
        var institutionBox = CreateDialogTextBox(dialog, "Bank / institution", 132, 420);
        var nicknameBox = CreateDialogTextBox(dialog, "Account nickname", 192, 420);
        var balanceBox = CreateDialogTextBox(dialog, "Current balance / value (optional)", 252, 180);

        var saveButton = CreateButton("Create Bank Account", 22, 320, 170);
        var cancelButton = CreateButton("Cancel", 205, 320, 110);
        dialog.Controls.Add(saveButton);
        dialog.Controls.Add(cancelButton);

        saveButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                MessageBox.Show(dialog, "Enter an account / asset name first.", "Name needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                nameBox.Focus();
                return;
            }

            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        cancelButton.Click += (_, _) =>
        {
            dialog.DialogResult = DialogResult.Cancel;
            dialog.Close();
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return null;

        decimal.TryParse(balanceBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out var balance);

        try
        {
            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
            DatabaseInitializer.EnsureCreated(databasePath);

            using var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO Assets
(AssetName, AssetType, Owner, EstimatedValue, Status, LocationOrInstitution,
 InstitutionName, AccountNickname, CurrentBalanceValue, IsActive, Notes, CreatedUtc, UpdatedUtc)
VALUES
($AssetName, 'Bank', '', $EstimatedValue, 'Active / Open', $InstitutionName,
 $InstitutionName, $AccountNickname, $CurrentBalanceValue, 1, '', $CreatedUtc, $UpdatedUtc);
SELECT last_insert_rowid();";

            var now = DateTime.UtcNow.ToString("O");
            command.Parameters.AddWithValue("$AssetName", nameBox.Text.Trim());
            command.Parameters.AddWithValue("$EstimatedValue", balance);
            command.Parameters.AddWithValue("$InstitutionName", institutionBox.Text.Trim());
            command.Parameters.AddWithValue("$AccountNickname", nicknameBox.Text.Trim());
            command.Parameters.AddWithValue("$CurrentBalanceValue", balance);
            command.Parameters.AddWithValue("$CreatedUtc", now);
            command.Parameters.AddWithValue("$UpdatedUtc", now);

            var id = Convert.ToInt64(command.ExecuteScalar());
            var display = nameBox.Text.Trim();
            var detailPieces = new List<string>();
            if (!string.IsNullOrWhiteSpace(institutionBox.Text))
                detailPieces.Add(institutionBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(nicknameBox.Text))
                detailPieces.Add(nicknameBox.Text.Trim());
            if (detailPieces.Count > 0)
                display += " (" + string.Join(" - ", detailPieces) + ")";

            return new BankAssetOption(id, display, balance);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not create bank account asset:\n{ex.Message}", "Create failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    private TextBox CreateDialogTextBox(Control parent, string labelText, int y, int width)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, y)
        };

        var textBox = new TextBox
        {
            Location = new Point(22, y + 22),
            Size = new Size(width, 26),
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };

        parent.Controls.Add(label);
        parent.Controls.Add(textBox);
        return textBox;
    }

    private void SaveIncomeFromEditor()
    {
        if (_repository is null || _selectedIncome is null)
            return;

        var sourceName = _sourceNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            MessageBox.Show("Enter a source name first.", "Source name needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _sourceNameTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(_amountTextBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount))
        {
            MessageBox.Show("Enter a valid amount.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _amountTextBox.Focus();
            return;
        }

        _selectedIncome.SourceName = sourceName;
        _selectedIncome.IncomeType = _incomeTypeComboBox.SelectedItem?.ToString() ?? "Other";
        _selectedIncome.Amount = amount;
        _selectedIncome.TaxesWithheld = _taxesWithheldCheckBox.Checked;
        _selectedIncome.Frequency = _frequencyComboBox.SelectedItem?.ToString() ?? "Monthly";
        _selectedIncome.ExpectedDayOrDate = _expectedDayTextBox.Text.Trim();

        var depositMethod = _depositMethodComboBox.SelectedItem?.ToString() ?? "Cash";
        _selectedIncome.DepositMethod = depositMethod;
        _selectedIncome.LinkedBankAssetId = 0;
        _selectedIncome.LinkedBankAssetName = string.Empty;

        if (depositMethod == "Select Bank Account")
        {
            if (_bankAccountComboBox.SelectedItem is not BankAssetOption selectedBank || selectedBank.Id <= 0)
            {
                MessageBox.Show("Select the bank account this income is deposited into.", "Bank account needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _bankAccountComboBox.Focus();
                return;
            }

            _selectedIncome.LinkedBankAssetId = selectedBank.Id;
            _selectedIncome.LinkedBankAssetName = selectedBank.DisplayName;
            _selectedIncome.DepositedToAccount = selectedBank.DisplayName;
        }
        else
        {
            _selectedIncome.DepositedToAccount = depositMethod;
        }

        _selectedIncome.IsActive = _activeCheckBox.Checked;
        _selectedIncome.Notes = _notesTextBox.Text.Trim();

        _repository.Upsert(_selectedIncome);
        ReloadIncome();
        ShowListPanel();
    }

    private void RemoveSelectedIncome()
    {
        if (_repository is null || _selectedIncome is null || _selectedIncome.Id <= 0)
            return;

        var result = MessageBox.Show(
            $"Remove {_selectedIncome.SourceName}?\n\nThis deletes this income source from the current case.",
            "Remove income source",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedIncome.Id);
        _selectedIncome = null;
        ReloadIncome();
        ShowListPanel();
    }

    private void CancelEdit()
    {
        if (_selectedIncome is not null && _selectedIncome.Id > 0)
            ShowSelectedProfile();
        else
            ShowListPanel();
    }

    private void ExportCurrentIncomePdf()
    {
        if (_selectedIncome is null || _selectedIncome.Id <= 0)
            return;

        var exportFolder = GetDefaultExportFolder();

        using var dialog = new SaveFileDialog
        {
            Title = "Export Income Source Profile PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(_selectedIncome.SourceName) + "_Income_Profile.pdf",
            InitialDirectory = exportFolder,
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WriteIncomeProfile(dialog.FileName, _selectedIncome, BuildProfileText(_selectedIncome));
            MessageBox.Show("Income profile PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not export PDF:\n{ex.Message}", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private string GetDefaultExportFolder()
    {
        var fallbackFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (string.IsNullOrWhiteSpace(_caseFolder))
            return fallbackFolder;

        try
        {
            var exportFolder = Path.Combine(_caseFolder, "exports");
            Directory.CreateDirectory(exportFolder);
            return exportFolder;
        }
        catch
        {
            return fallbackFolder;
        }
    }

    private void ShowListPanel()
    {
        _listPanel.Visible = true;
        _profilePanel.Visible = false;
        _editPanel.Visible = false;
        _listPanel.BringToFront();
    }

    private void ShowProfilePanel()
    {
        _listPanel.Visible = false;
        _profilePanel.Visible = true;
        _editPanel.Visible = false;
        _profilePanel.BringToFront();
        _profilePanel.AutoScrollPosition = Point.Empty;
        UpdateProfileContentWidth();
    }

    private void ShowEditPanel()
    {
        _listPanel.Visible = false;
        _profilePanel.Visible = false;
        _editPanel.Visible = true;
        _editPanel.BringToFront();
        UpdateDepositDestinationVisibility();
        _editPanel.AutoScrollPosition = Point.Empty;
    }

    private void PopulateProfileDetails(IncomeSource source)
    {
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Basic Information");
        AddProfileRow("Source", source.SourceName);
        AddProfileRow("Type", source.IncomeType);
        AddProfileRow("Active", YesNo(source.IsActive));

        AddProfileSpacer();
        AddProfileSection("Payment Details");
        AddProfileRow("Tax handling", source.TaxHandlingText);
        AddProfileRow(source.AmountLabel, source.Amount.ToString("C2"));
        AddProfileRow("Frequency", source.Frequency);
        AddProfileRow("Monthly equivalent", source.MonthlyEquivalent.ToString("C2"));
        AddProfileRow("Expected day/date", source.ExpectedDayOrDate);
        AddProfileRow("Deposit destination", source.DepositDisplayText);

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(source.Notes) ? "None" : source.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", source.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", source.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

        _profileDetailsPanel.ResumeLayout();
        UpdateProfileContentWidth();
    }

    private void UpdateProfileContentWidth()
    {
        if (_profilePanel is null || _profileDetailsPanel is null)
            return;

        var usableWidth = Math.Max(400, _profilePanel.ClientSize.Width - 24);
        _profileDetailsPanel.Width = usableWidth;

        foreach (Control control in _profileDetailsPanel.Controls)
            control.Width = usableWidth;
    }

    private void AddProfileSection(string text)
    {
        _profileDetailsPanel.Controls.Add(new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = false,
            Width = Math.Max(400, _profileDetailsPanel.ClientSize.Width - 26),
            Height = 30,
            Margin = new Padding(0, 0, 0, 6)
        });
    }

    private void AddProfileRow(string label, string? value)
    {
        var row = new Label
        {
            Text = string.Empty,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            AutoSize = false,
            Width = Math.Max(400, _profileDetailsPanel.ClientSize.Width - 26),
            Height = 24,
            Margin = new Padding(0, 0, 0, 2)
        };

        row.Paint += (_, e) =>
        {
            using var bold = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            using var regular = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            var labelText = label + ": ";
            TextRenderer.DrawText(e.Graphics, labelText, bold, new Point(0, 1), TextPrimary);
            var labelSize = TextRenderer.MeasureText(labelText, bold);
            TextRenderer.DrawText(e.Graphics, CleanProfileValue(value), regular, new Point(labelSize.Width - 4, 1), TextPrimary);
        };

        _profileDetailsPanel.Controls.Add(row);
    }

    private void AddProfileParagraph(string text)
    {
        var width = Math.Max(400, _profileDetailsPanel.ClientSize.Width - 26);
        using var font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        var measured = TextRenderer.MeasureText(
            text,
            font,
            new Size(Math.Max(300, width - 8), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        _profileDetailsPanel.Controls.Add(new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            AutoSize = false,
            Width = width,
            Height = Math.Max(46, measured.Height + 12),
            Margin = new Padding(0, 0, 0, 2)
        });
    }

    private void AddProfileSpacer()
    {
        _profileDetailsPanel.Controls.Add(new Label
        {
            Text = string.Empty,
            AutoSize = false,
            Width = 1,
            Height = 14,
            Margin = new Padding(0)
        });
    }

    private static string CleanProfileValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string BuildProfileText(IncomeSource source)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Source: {source.SourceName}");
        builder.AppendLine($"Type: {source.IncomeType}");
        builder.AppendLine($"Active: {YesNo(source.IsActive)}");
        builder.AppendLine();
        builder.AppendLine("Payment Details");
        builder.AppendLine($"Tax handling: {source.TaxHandlingText}");
        builder.AppendLine($"{source.AmountLabel}: {source.Amount:C2}");
        builder.AppendLine($"Frequency: {source.Frequency}");
        builder.AppendLine($"Monthly equivalent: {source.MonthlyEquivalent:C2}");
        builder.AppendLine($"Expected day/date: {source.ExpectedDayOrDate}");
        builder.AppendLine($"Deposit destination: {source.DepositDisplayText}");
        builder.AppendLine();
        builder.AppendLine("Notes");
        builder.AppendLine(string.IsNullOrWhiteSpace(source.Notes) ? "None" : source.Notes);
        builder.AppendLine();
        builder.AppendLine($"Created UTC: {source.CreatedUtc:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Updated UTC: {source.UpdatedUtc:yyyy-MM-dd HH:mm}");
        return builder.ToString();
    }

    private static string YesNo(bool value) => value ? "Yes" : "No";

    private void AddFillTextColumn(string property, string header, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            FillWeight = fillWeight,
            MinimumWidth = 90,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
    }

    private void AddTextColumn(string property, string header, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            Width = width,
            MinimumWidth = width,
            SortMode = DataGridViewColumnSortMode.Automatic
        };

        if (!string.IsNullOrWhiteSpace(format))
            column.DefaultCellStyle.Format = format;

        _grid.Columns.Add(column);
    }

    private TextBox CreateTextBox(Control parent, string labelText, int y, int width, bool multiline = false)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, y)
        };

        var textBox = new TextBox
        {
            Location = new Point(22, y + 22),
            Size = new Size(width, multiline ? 90 : 26),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };

        parent.Controls.Add(label);
        parent.Controls.Add(textBox);
        return textBox;
    }

    private CheckBox CreateCheckBox(string text, int y)
    {
        return new CheckBox
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, y),
            FlatStyle = FlatStyle.Flat
        };
    }

    private Button CreateButton(string text, int x, int y, int width)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 38),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Panel2,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "IncomeSource" : safe.Trim();
    }


    private sealed class BankAssetOption
    {
        public BankAssetOption(long id, string displayName, decimal currentBalance)
        {
            Id = id;
            DisplayName = displayName;
            CurrentBalance = currentBalance;
        }

        public long Id { get; }
        public string DisplayName { get; }
        public decimal CurrentBalance { get; }

        public override string ToString() => DisplayName;
    }

    private static class MinimalPdfWriter
    {
        public static void WriteIncomeProfile(string path, IncomeSource source, string profileText)
        {
            var lines = new List<PdfLine>
            {
                new(source.SourceName, "Title"),
                new($"Exported: {DateTime.Now:yyyy-MM-dd h:mm tt}", "Muted", 8),

                new("Basic Information", "Section"),
                Row("Source", source.SourceName),
                Row("Type", source.IncomeType),
                Row("Active", YesNo(source.IsActive)),

                new("Payment Details", "Section"),
                Row("Tax handling", source.TaxHandlingText),
                Row(source.AmountLabel, source.Amount.ToString("C2")),
                Row("Frequency", source.Frequency),
                Row("Monthly equivalent", source.MonthlyEquivalent.ToString("C2")),
                Row("Expected day/date", source.ExpectedDayOrDate),
                Row("Deposit destination", source.DepositDisplayText),

                new("Notes", "Section"),
                new(string.IsNullOrWhiteSpace(source.Notes) ? "None" : source.Notes, "Paragraph", 4),

                new("Record Info", "Section"),
                Row("Created UTC", source.CreatedUtc.ToString("yyyy-MM-dd HH:mm")),
                Row("Updated UTC", source.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"))
            };

            var content = BuildPdfContent(lines, "Income Source Profile");
            WritePdf(path, content);
        }

        private static PdfLine Row(string label, string? value) => new($"{label}||{Clean(value)}", "Row");

        private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();

        private static string YesNo(bool value) => value ? "Yes" : "No";

        private sealed record PdfLine(string Text, string Style, int ExtraGapAfter = 0);

        private static string BuildPdfContent(IReadOnlyList<PdfLine> lines, string documentTitle)
        {
            var builder = new StringBuilder();

            // Print-friendly white page with black text and light gray separator lines.
            // No dark backgrounds/colors here so exported PDFs are easy to print.
            DrawText(builder, "Family Finance & Trust Manager", 42, 754, "F2", 18, "0 0 0");
            DrawText(builder, documentTitle, 42, 730, "F1", 12, "0.28 0.28 0.28");
            builder.AppendLine("0.72 0.72 0.72 rg");
            builder.AppendLine("42 712 528 1.2 re f");

            var y = 674;
            foreach (var line in lines)
            {
                if (y < 58)
                    break;

                switch (line.Style)
                {
                    case "Title":
                        DrawText(builder, line.Text, 42, y, "F2", 20, "0 0 0");
                        y -= 30 + line.ExtraGapAfter;
                        break;

                    case "Section":
                        y -= 10;
                        builder.AppendLine("0.72 0.72 0.72 rg");
                        builder.AppendLine($"42 {y - 5} 528 1.2 re f");
                        DrawText(builder, line.Text, 42, y + 6, "F2", 13, "0 0 0");
                        y -= 24 + line.ExtraGapAfter;
                        break;

                    case "Row":
                        {
                            var parts = line.Text.Split(new[] { "||" }, 2, StringSplitOptions.None);
                            var label = parts.Length > 0 ? parts[0] : string.Empty;
                            var value = parts.Length > 1 ? parts[1] : string.Empty;
                            DrawText(builder, label + ":", 42, y, "F2", 10.5, "0 0 0");
                            foreach (var wrapped in Wrap(value, 62))
                            {
                                DrawText(builder, wrapped, 202, y, "F1", 10.5, "0 0 0");
                                y -= 15;
                                if (y < 58)
                                    break;
                            }
                            y -= 3 + line.ExtraGapAfter;
                            break;
                        }

                    case "Paragraph":
                        foreach (var wrapped in Wrap(line.Text, 92))
                        {
                            DrawText(builder, wrapped, 42, y, "F1", 10.5, "0 0 0");
                            y -= 15;
                            if (y < 58)
                                break;
                        }
                        y -= 4 + line.ExtraGapAfter;
                        break;

                    case "Muted":
                        DrawText(builder, line.Text, 42, y, "F1", 9, "0.35 0.35 0.35");
                        y -= 14 + line.ExtraGapAfter;
                        break;
                }
            }

            return builder.ToString();
        }

        private static void DrawText(StringBuilder builder, string text, int x, int y, string fontKey, double size, string rgb)
        {
            builder.AppendLine("BT");
            builder.AppendLine($"{rgb} rg");
            builder.AppendLine($"/{fontKey} {size.ToString("0.###", CultureInfo.InvariantCulture)} Tf");
            builder.AppendLine($"{x} {y} Td");
            builder.Append('(').Append(EscapePdfText(text)).AppendLine(") Tj");
            builder.AppendLine("ET");
        }

        private static IEnumerable<string> Wrap(string? value, int maxChars)
        {
            value = string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
            value = value.Replace("\r", string.Empty);

            foreach (var paragraph in value.Split('\n'))
            {
                var remaining = paragraph.Trim();
                if (remaining.Length == 0)
                {
                    yield return string.Empty;
                    continue;
                }

                while (remaining.Length > maxChars)
                {
                    var cut = remaining.LastIndexOf(' ', Math.Min(maxChars, remaining.Length - 1));
                    if (cut < 24)
                        cut = Math.Min(maxChars, remaining.Length);

                    yield return remaining[..cut].Trim();
                    remaining = remaining[cut..].Trim();
                }

                yield return remaining;
            }
        }

        private static void WritePdf(string path, string content)
        {
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
                $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"
            };

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.ASCII);

            writer.WriteLine("%PDF-1.4");
            var offsets = new List<long> { 0 };

            for (var i = 0; i < objects.Count; i++)
            {
                writer.Flush();
                offsets.Add(stream.Position);
                writer.WriteLine($"{i + 1} 0 obj");
                writer.WriteLine(objects[i]);
                writer.WriteLine("endobj");
            }

            writer.Flush();
            var xrefOffset = stream.Position;
            writer.WriteLine("xref");
            writer.WriteLine($"0 {objects.Count + 1}");
            writer.WriteLine("0000000000 65535 f ");

            for (var i = 1; i < offsets.Count; i++)
                writer.WriteLine($"{offsets[i]:0000000000} 00000 n ");

            writer.WriteLine("trailer");
            writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefOffset);
            writer.WriteLine("%%EOF");
        }

        private static string EscapePdfText(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }
    }
}
