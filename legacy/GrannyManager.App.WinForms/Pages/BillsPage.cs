using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class BillsPage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);

    private readonly BindingList<Bill> _bills = new();
    private readonly BindingList<BillReceipt> _receiptItems = new();

    private BillsRepository? _repository;
    private BillReceiptsRepository? _receiptRepository;
    private string? _caseFolder;
    private Bill? _selectedBill;

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addBillButton = null!;
    private Button _viewProfileButton = null!;
    private Button _editSelectedButton = null!;
    private Button _fuelCalculatorButton = null!;
    private Button _groceryCalculatorButton = null!;

    private Panel _profilePanel = null!;
    private Label _profileTitleLabel = null!;
    private FlowLayoutPanel _profileDetailsPanel = null!;
    private Button _profileBackButton = null!;
    private Button _profileEditButton = null!;
    private Button _profileRemoveButton = null!;
    private Button _profileExportButton = null!;

    private Panel _editPanel = null!;
    private Label _editTitleLabel = null!;
    private TextBox _billNameTextBox = null!;
    private ComboBox _categoryComboBox = null!;
    private TextBox _amountTextBox = null!;
    private ComboBox _frequencyComboBox = null!;
    private TextBox _dueDateTextBox = null!;
    private CheckBox _autopayCheckBox = null!;
    private ComboBox _paidByComboBox = null!;
    private Label _paidByOtherLabel = null!;
    private TextBox _paidByOtherTextBox = null!;
    private ComboBox _responsibilityOwnerComboBox = null!;
    private Label _responsibilityOwnerOtherLabel = null!;
    private TextBox _responsibilityOwnerOtherTextBox = null!;
    private TextBox _pastDueAmountTextBox = null!;
    private ComboBox _priorityComboBox = null!;
    private CheckBox _activeCheckBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    private Panel _calculatorPanel = null!;
    private Label _calculatorTitleLabel = null!;
    private Label _calculatorStatsLabel = null!;
    private DataGridView _receiptGrid = null!;
    private DateTimePicker _receiptDatePicker = null!;
    private TextBox _receiptAmountTextBox = null!;
    private Button _addReceiptButton = null!;
    private Button _removeReceiptButton = null!;
    private Button _applyCalculatorBillButton = null!;
    private Button _backToBillsFromCalculatorButton = null!;
    private string _activeCalculatorType = "Fuel";

    public BillsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
    }

    public AppPageKey PageKey => AppPageKey.Bills;
    public string PageTitle => "Bills / Spending";

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
            Text = "Bills / Spending",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Track regular bills, shared responsibilities, past-due amounts, and normalized monthly cost.",
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
        BuildCalculatorPanel();
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

        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Back,
            Padding = new Padding(0, 8, 0, 0)
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        _listPanel.Controls.Add(buttonRow);

        var leftButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back,
            Margin = new Padding(0)
        };
        var rightButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Back,
            Margin = new Padding(0)
        };
        buttonRow.Controls.Add(leftButtons, 0, 0);
        buttonRow.Controls.Add(rightButtons, 1, 0);

        _addBillButton = CreateButton("Add Bill / Expense", 0, 0, 150);
        _viewProfileButton = CreateButton("View Profile", 0, 0, 120);
        _editSelectedButton = CreateButton("Edit Selected", 0, 0, 125);
        leftButtons.Controls.Add(_addBillButton);
        leftButtons.Controls.Add(_viewProfileButton);
        leftButtons.Controls.Add(_editSelectedButton);

        _groceryCalculatorButton = CreateGreenButton("Grocery Bill", 0, 0, 130);
        _fuelCalculatorButton = CreateGreenButton("Fuel Bill", 0, 0, 110);
        rightButtons.Controls.Add(_groceryCalculatorButton);
        rightButtons.Controls.Add(_fuelCalculatorButton);

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
            DataSource = _bills
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

        AddFillTextColumn("BillName", "Bill / Expense", 34f);
        AddFillTextColumn("Category", "Category", 24f);
        AddTextColumn("Amount", "Amount", 100, "C2");
        AddFillTextColumn("Frequency", "Frequency", 20f);
        AddTextColumn("MonthlyEquivalent", "Monthly Est.", 120, "C2");

        _grid.SelectionChanged += (_, _) => UpdateSelectionFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowSelectedProfile();
        ListPageGridHelper.AttachRightClickRemove(_grid, UpdateSelectionFromGrid, RemoveSelectedBill);

        gridHost.Controls.Add(_grid);

        _addBillButton.Click += (_, _) => BeginAddBill();
        _viewProfileButton.Click += (_, _) => ShowSelectedProfile();
        _editSelectedButton.Click += (_, _) => BeginEditSelectedBill();
        _fuelCalculatorButton.Click += (_, _) => ShowReceiptCalculator("Fuel");
        _groceryCalculatorButton.Click += (_, _) => ShowReceiptCalculator("Grocery");
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
        _profileEditButton.Click += (_, _) => BeginEditSelectedBill();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedBill();
        _profileExportButton.Click += (_, _) => ExportCurrentBillPdf();
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
            Text = "Add Bill / Expense",
            Location = new Point(0, 0),
            Size = new Size(700, 42),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _editPanel.Controls.Add(_editTitleLabel);

        var fieldsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 54),
            Size = new Size(760, 900),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Back,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _editPanel.Controls.Add(fieldsPanel);

        _billNameTextBox = CreateTextBoxField(fieldsPanel, "Bill / expense name", 420);

        _categoryComboBox = CreateComboBoxField(fieldsPanel, "Category", 300, new[]
        {
            "Housing",
            "Utilities",
            "Vehicle",
            "Insurance",
            "Debt Payment",
            "Medical",
            "Food / Groceries",
            "Phone / Internet",
            "Taxes / Government",
            "Family Support",
            "Uncommon Spending",
            "Other"
        }, "Other");

        _amountTextBox = CreateTextBoxField(fieldsPanel, "Amount", 180);

        _frequencyComboBox = CreateComboBoxField(fieldsPanel, "Frequency", 220, new[]
        {
            "Weekly",
            "Every 2 weeks",
            "Twice monthly",
            "Monthly",
            "Quarterly",
            "Yearly",
            "One-time / irregular"
        }, "Monthly");

        _dueDateTextBox = CreateTextBoxField(fieldsPanel, "Due date / expected date", 300);
        _autopayCheckBox = CreateCheckBoxField(fieldsPanel, "Autopay");

        _paidByComboBox = CreatePersonChoiceComboBoxField(fieldsPanel, "Who Pays This?", 340);
        (_paidByOtherLabel, _paidByOtherTextBox) = CreateOtherNameField(fieldsPanel, "Outside payer name", 340);

        _responsibilityOwnerComboBox = CreatePersonChoiceComboBoxField(fieldsPanel, "Responsibility / Owner", 340);
        (_responsibilityOwnerOtherLabel, _responsibilityOwnerOtherTextBox) = CreateOtherNameField(fieldsPanel, "Outside responsible party name", 420);

        _pastDueAmountTextBox = CreateTextBoxField(fieldsPanel, "Past due amount", 180);

        _priorityComboBox = CreateComboBoxField(fieldsPanel, "Priority", 220, new[]
        {
            "Critical",
            "High",
            "Normal",
            "Low",
            "Unknown"
        }, "Normal");

        _activeCheckBox = CreateCheckBoxField(fieldsPanel, "Active bill / expense");
        _activeCheckBox.Checked = true;

        _notesTextBox = CreateTextBoxField(fieldsPanel, "Notes", 650, multiline: true);

        var buttonRow = new FlowLayoutPanel
        {
            Width = 700,
            Height = 46,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back,
            Margin = new Padding(22, 4, 0, 16)
        };
        _saveButton = CreateButton("Save Bill / Expense", 0, 0, 170);
        _cancelButton = CreateButton("Cancel", 0, 0, 110);
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_cancelButton);
        fieldsPanel.Controls.Add(buttonRow);

        _paidByComboBox.SelectedIndexChanged += (_, _) => UpdatePersonChoiceOtherVisibility(_paidByComboBox, _paidByOtherLabel, _paidByOtherTextBox);
        _responsibilityOwnerComboBox.SelectedIndexChanged += (_, _) => UpdatePersonChoiceOtherVisibility(_responsibilityOwnerComboBox, _responsibilityOwnerOtherLabel, _responsibilityOwnerOtherTextBox);

        RefreshPersonChoiceDropdowns();

        _saveButton.Click += (_, _) => SaveBillFromEditor();
        _cancelButton.Click += (_, _) => CancelEdit();
    }

    private void BuildCalculatorPanel()
    {
        _calculatorPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            AutoScroll = true,
            Visible = false
        };
        _contentHost.Controls.Add(_calculatorPanel);

        _calculatorTitleLabel = new Label
        {
            Location = new Point(0, 0),
            Size = new Size(760, 38),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _calculatorPanel.Controls.Add(_calculatorTitleLabel);

        _calculatorStatsLabel = new Label
        {
            Location = new Point(0, 46),
            Size = new Size(900, 32),
            ForeColor = Good,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _calculatorPanel.Controls.Add(_calculatorStatsLabel);

        var topRow = new FlowLayoutPanel
        {
            Location = new Point(0, 88),
            Size = new Size(860, 46),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back
        };
        _calculatorPanel.Controls.Add(topRow);

        _backToBillsFromCalculatorButton = CreateButton("Back to Bills", 0, 0, 120);
        _addReceiptButton = CreateGreenButton("Add Receipt", 0, 0, 120);
        _removeReceiptButton = CreateButton("Remove Selected Receipts", 0, 0, 190);
        topRow.Controls.Add(_backToBillsFromCalculatorButton);
        topRow.Controls.Add(_addReceiptButton);
        topRow.Controls.Add(_removeReceiptButton);

        var gridHost = new Panel
        {
            Location = new Point(0, 148),
            Size = new Size(620, 360),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Border,
            Padding = new Padding(1)
        };
        _calculatorPanel.Controls.Add(gridHost);

        _receiptGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            MultiSelect = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            BackgroundColor = Back,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Border,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars = ScrollBars.Both,
            DataSource = _receiptItems
        };
        _receiptGrid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        _receiptGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        _receiptGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _receiptGrid.DefaultCellStyle.BackColor = Back;
        _receiptGrid.DefaultCellStyle.ForeColor = TextPrimary;
        _receiptGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(43, 70, 105);
        _receiptGrid.DefaultCellStyle.SelectionForeColor = TextPrimary;
        _receiptGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        _receiptGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(13, 29, 48);
        _receiptGrid.RowTemplate.Height = 30;
        _receiptGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ReceiptDateText",
            HeaderText = "Date",
            Width = 150,
            MinimumWidth = 120,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        _receiptGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Amount",
            HeaderText = "Total Spent",
            Width = 140,
            MinimumWidth = 120,
            DefaultCellStyle = { Format = "C2" },
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        gridHost.Controls.Add(_receiptGrid);

        _calculatorPanel.Resize += (_, _) =>
        {
            gridHost.Width = Math.Max(520, _calculatorPanel.ClientSize.Width - 24);
            gridHost.Height = Math.Max(260, _calculatorPanel.ClientSize.Height - gridHost.Top - 24);
        };

        _backToBillsFromCalculatorButton.Click += (_, _) => ShowListPanel();
        _addReceiptButton.Click += (_, _) => AddReceiptToCalculator();
        _removeReceiptButton.Click += (_, _) => RemoveSelectedReceipt();
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
            _receiptRepository = null;
            _bills.Clear();
            _receiptItems.Clear();
            _caseStatusLabel.Text = "No active case is open. Open or create a case before adding bills or spending items.";
            _caseStatusLabel.ForeColor = Danger;
            RefreshPersonChoiceDropdowns();
            UpdateStats();
            return;
        }

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
        _repository = new BillsRepository(databasePath);
        _receiptRepository = new BillReceiptsRepository(databasePath);
        RefreshPersonChoiceDropdowns();
        _caseStatusLabel.Text = "Active case folder: " + _caseFolder;
        _caseStatusLabel.ForeColor = TextMuted;
        ReloadBills();
    }

    private void ReloadBills()
    {
        _bills.Clear();

        if (_repository is null)
        {
            UpdateStats();
            return;
        }

        foreach (var bill in _repository.GetAll())
            _bills.Add(bill);

        UpdateStats();
        ListPageGridHelper.ApplyInactiveRowStyles(_grid, item => item is Bill bill && bill.IsActive);
        UpdateSelectionFromGrid();
    }

    private void UpdateStats()
    {
        var activeBills = _bills.Count(b => b.IsActive);
        var monthlyTotal = _bills.Sum(b => b.MonthlyEquivalent);
        var pastDueTotal = _bills.Sum(b => b.PastDueAmount);
        _statsLabel.Text = $"Bills / spending items: {_bills.Count}     Active: {activeBills}     Estimated monthly cost: {monthlyTotal:C2}     Past due: {pastDueTotal:C2}";
    }

    private void UpdateSelectionFromGrid()
    {
        _selectedBill = _grid.CurrentRow?.DataBoundItem as Bill;
        var hasSelection = _selectedBill is not null;
        _viewProfileButton.Enabled = hasSelection;
        _editSelectedButton.Enabled = hasSelection;
    }

    private void BeginAddBill()
    {
        if (_repository is null)
        {
            MessageBox.Show("Open or create a case first.", "No active case", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _selectedBill = new Bill { IsActive = true, Frequency = "Monthly", Priority = "Normal" };
        LoadEditor(_selectedBill, isNew: true);
        ShowEditPanel();
    }

    private void BeginEditSelectedBill()
    {
        if (_selectedBill is null)
        {
            MessageBox.Show("Select a bill or expense first.", "No bill selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        LoadEditor(_selectedBill, isNew: false);
        ShowEditPanel();
    }

    private void ShowSelectedProfile()
    {
        if (_selectedBill is null)
        {
            MessageBox.Show("Select a bill or expense first.", "No bill selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _profileTitleLabel.Text = _selectedBill.BillName;
        PopulateProfileDetails(_selectedBill);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var bill = _bills.FirstOrDefault(b => b.Id == recordId);
        if (bill is null && _repository is not null)
            bill = _repository.GetAll().FirstOrDefault(b => b.Id == recordId);

        if (bill is null)
            return false;

        _selectedBill = bill;
        SelectGridRowByBillId(recordId);
        _profileTitleLabel.Text = bill.BillName;
        PopulateProfileDetails(bill);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByBillId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is Bill bill && bill.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void LoadEditor(Bill bill, bool isNew)
    {
        _editTitleLabel.Text = isNew ? "Add Bill / Expense" : "Edit Bill / Expense";
        _billNameTextBox.Text = bill.BillName;
        SetComboValue(_categoryComboBox, bill.Category, "Other");
        _amountTextBox.Text = bill.Amount == 0m ? string.Empty : bill.Amount.ToString("0.##", CultureInfo.CurrentCulture);
        SetComboValue(_frequencyComboBox, bill.Frequency, "Monthly");
        _dueDateTextBox.Text = bill.DueDate;
        _autopayCheckBox.Checked = bill.IsAutopay;
        RefreshPersonChoiceDropdowns();
        SetPersonChoiceValue(_paidByComboBox, _paidByOtherLabel, _paidByOtherTextBox, bill.PaidBy);
        SetPersonChoiceValue(_responsibilityOwnerComboBox, _responsibilityOwnerOtherLabel, _responsibilityOwnerOtherTextBox, bill.ResponsibilityOwner);
        _pastDueAmountTextBox.Text = bill.PastDueAmount == 0m ? string.Empty : bill.PastDueAmount.ToString("0.##", CultureInfo.CurrentCulture);
        SetComboValue(_priorityComboBox, bill.Priority, "Normal");
        _activeCheckBox.Checked = bill.IsActive;
        _notesTextBox.Text = bill.Notes;
    }

    private static void SetComboValue(ComboBox comboBox, string? value, string fallback)
    {
        comboBox.SelectedItem = string.IsNullOrWhiteSpace(value) ? fallback : value;
        if (comboBox.SelectedItem is null)
            comboBox.SelectedItem = fallback;
    }

    private void SaveBillFromEditor()
    {
        if (_repository is null || _selectedBill is null)
            return;

        var billName = _billNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(billName))
        {
            MessageBox.Show("Enter a bill or expense name first.", "Bill name needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _billNameTextBox.Focus();
            return;
        }

        if (!decimal.TryParse(_amountTextBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount))
        {
            MessageBox.Show("Enter a valid amount.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _amountTextBox.Focus();
            return;
        }

        var pastDueText = _pastDueAmountTextBox.Text.Trim();
        var pastDue = 0m;
        if (!string.IsNullOrWhiteSpace(pastDueText) && !decimal.TryParse(pastDueText, NumberStyles.Currency, CultureInfo.CurrentCulture, out pastDue))
        {
            MessageBox.Show("Enter a valid past due amount, or leave it blank.", "Invalid past due amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _pastDueAmountTextBox.Focus();
            return;
        }

        _selectedBill.BillName = billName;
        _selectedBill.Category = _categoryComboBox.SelectedItem?.ToString() ?? "Other";
        _selectedBill.Amount = amount;
        _selectedBill.Frequency = _frequencyComboBox.SelectedItem?.ToString() ?? "Monthly";
        _selectedBill.DueDate = _dueDateTextBox.Text.Trim();
        _selectedBill.IsAutopay = _autopayCheckBox.Checked;
        _selectedBill.PaidBy = GetPersonChoiceValue(_paidByComboBox, _paidByOtherTextBox);
        _selectedBill.ResponsibilityOwner = GetPersonChoiceValue(_responsibilityOwnerComboBox, _responsibilityOwnerOtherTextBox);
        _selectedBill.PastDueAmount = pastDue;
        _selectedBill.Priority = _priorityComboBox.SelectedItem?.ToString() ?? "Normal";
        _selectedBill.IsActive = _activeCheckBox.Checked;
        _selectedBill.Notes = _notesTextBox.Text.Trim();

        _repository.Upsert(_selectedBill);
        ReloadBills();
        ShowListPanel();
    }

    private void RemoveSelectedBill()
    {
        if (_repository is null || _selectedBill is null || _selectedBill.Id <= 0)
            return;

        var result = MessageBox.Show(
            $"Remove {_selectedBill.BillName}?\n\nThis deletes this bill/spending item from the current case.",
            "Remove bill / expense",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedBill.Id);
        _selectedBill = null;
        ReloadBills();
        ShowListPanel();
    }

    private void CancelEdit()
    {
        if (_selectedBill is not null && _selectedBill.Id > 0)
            ShowSelectedProfile();
        else
            ShowListPanel();
    }

    private void ExportCurrentBillPdf()
    {
        if (_selectedBill is null || _selectedBill.Id <= 0)
            return;

        var exportFolder = GetDefaultExportFolder();

        using var dialog = new SaveFileDialog
        {
            Title = "Export Bill / Expense Profile PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(_selectedBill.BillName) + "_Bill_Profile.pdf",
            InitialDirectory = exportFolder,
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WriteBillProfile(dialog.FileName, _selectedBill);
            MessageBox.Show("Bill / expense profile PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        _calculatorPanel.Visible = false;
        _listPanel.BringToFront();
    }

    private void ShowProfilePanel()
    {
        _listPanel.Visible = false;
        _profilePanel.Visible = true;
        _editPanel.Visible = false;
        _calculatorPanel.Visible = false;
        _profilePanel.BringToFront();
        _profilePanel.AutoScrollPosition = Point.Empty;
        UpdateProfileContentWidth();
    }

    private void ShowEditPanel()
    {
        _listPanel.Visible = false;
        _profilePanel.Visible = false;
        _editPanel.Visible = true;
        _calculatorPanel.Visible = false;
        _editPanel.BringToFront();
        _editPanel.AutoScrollPosition = Point.Empty;
    }

    private void ShowReceiptCalculator(string receiptType)
    {
        if (_receiptRepository is null)
        {
            MessageBox.Show("Open or create a case first.", "No active case", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _activeCalculatorType = receiptType;
        _calculatorTitleLabel.Text = receiptType == "Fuel" ? "Fuel Bill Calculator" : "Grocery Bill Calculator";
        ReloadReceiptItems();

        _listPanel.Visible = false;
        _profilePanel.Visible = false;
        _editPanel.Visible = false;
        _calculatorPanel.Visible = true;
        _calculatorPanel.BringToFront();
        _calculatorPanel.AutoScrollPosition = Point.Empty;
    }

    private void ReloadReceiptItems()
    {
        _receiptItems.Clear();

        if (_receiptRepository is not null)
        {
            foreach (var receipt in _receiptRepository.GetByType(_activeCalculatorType))
                _receiptItems.Add(receipt);
        }

        UpdateReceiptCalculatorStats();
    }

    private void AddReceiptToCalculator()
    {
        if (_receiptRepository is null)
            return;

        using var dialog = new ReceiptEntryDialog(_activeCalculatorType);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _receiptRepository.Add(new BillReceipt
        {
            ReceiptType = _activeCalculatorType,
            ReceiptDate = dialog.ReceiptDate,
            Amount = dialog.Amount
        });

        ReloadReceiptItems();
        UpdateCalculatorBillEstimate();
    }

    private void RemoveSelectedReceipt()
    {
        if (_receiptRepository is null || _receiptGrid.SelectedRows.Count == 0)
            return;

        var receipts = _receiptGrid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem)
            .OfType<BillReceipt>()
            .Where(receipt => receipt.Id > 0)
            .DistinctBy(receipt => receipt.Id)
            .ToList();

        if (receipts.Count == 0)
            return;

        var result = MessageBox.Show(
            receipts.Count == 1
                ? $"Remove this {_activeCalculatorType.ToLowerInvariant()} receipt for {receipts[0].Amount:C2}?"
                : $"Remove these {receipts.Count} {_activeCalculatorType.ToLowerInvariant()} receipts?",
            "Remove receipts",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        foreach (var receipt in receipts)
            _receiptRepository.Delete(receipt.Id);

        ReloadReceiptItems();
        UpdateCalculatorBillEstimate();
    }

    private void UpdateCalculatorBillEstimate()
    {
        if (_repository is null)
            return;

        var monthlyEstimate = CalculateRoundedMonthlyReceiptAverage(_receiptItems);
        var billName = _activeCalculatorType == "Fuel" ? "Fuel Bill Estimate" : "Grocery Bill Estimate";
        var category = _activeCalculatorType == "Fuel" ? "Vehicle" : "Food / Groceries";
        var selfPayer = BuildPersonChoiceOptions().FirstOrDefault() ?? string.Empty;
        var bill = _bills.FirstOrDefault(b => string.Equals(b.BillName, billName, StringComparison.OrdinalIgnoreCase));
        bill ??= new Bill
        {
            BillName = billName,
            Frequency = "Monthly",
            IsActive = true,
            Priority = "Normal",
            PaidBy = selfPayer,
            ResponsibilityOwner = selfPayer
        };

        bill.BillName = billName;
        bill.Category = category;
        bill.Amount = monthlyEstimate;
        bill.Frequency = "Monthly";
        bill.IsAutopay = false;
        bill.PastDueAmount = 0m;
        bill.Priority = string.IsNullOrWhiteSpace(bill.Priority) ? "Normal" : bill.Priority;
        bill.IsActive = monthlyEstimate > 0m;
        if (string.IsNullOrWhiteSpace(bill.PaidBy))
            bill.PaidBy = selfPayer;
        if (string.IsNullOrWhiteSpace(bill.ResponsibilityOwner))
            bill.ResponsibilityOwner = selfPayer;
        bill.Notes = monthlyEstimate > 0m
            ? $"Automatically calculated from {_activeCalculatorType.ToLowerInvariant()} receipts. Monthly estimate is based on average monthly receipt totals and rounded up."
            : $"Automatically calculated from {_activeCalculatorType.ToLowerInvariant()} receipts. No receipt data is currently entered.";

        _repository.Upsert(bill);
        ReloadBills();
        ReloadReceiptItems();
    }

    private void UpdateReceiptCalculatorStats()
    {
        var total = _receiptItems.Sum(r => r.Amount);
        var monthlyEstimate = CalculateRoundedMonthlyReceiptAverage(_receiptItems);
        var monthCount = _receiptItems
            .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
            .Count();

        _calculatorStatsLabel.Text = $"Receipts: {_receiptItems.Count}     Months tracked: {monthCount}     Total entered: {total:C2}     Rounded monthly estimate: {monthlyEstimate:C2}";
    }

    private static decimal CalculateRoundedMonthlyReceiptAverage(IEnumerable<BillReceipt> receipts)
    {
        var monthlyTotals = receipts
            .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
            .Select(g => g.Sum(r => r.Amount))
            .Where(total => total > 0m)
            .ToList();

        if (monthlyTotals.Count == 0)
            return 0m;

        var average = monthlyTotals.Average();
        return Math.Ceiling(average);
    }

    private void PopulateProfileDetails(Bill bill)
    {
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Basic Information");
        AddProfileRow("Bill / expense", bill.BillName);
        AddProfileRow("Category", bill.Category);
        AddProfileRow("Priority", bill.Priority);
        AddProfileRow("Active", YesNo(bill.IsActive));

        AddProfileSpacer();
        AddProfileSection("Payment Details");
        AddProfileRow("Amount", bill.Amount.ToString("C2"));
        AddProfileRow("Frequency", bill.Frequency);
        AddProfileRow("Monthly equivalent", bill.MonthlyEquivalent.ToString("C2"));
        AddProfileRow("Due date / expected date", bill.DueDate);
        AddProfileRow("Payment method", bill.AutopayText);
        AddProfileRow("Past due amount", bill.PastDueAmount.ToString("C2"));

        AddProfileSpacer();
        AddProfileSection("Responsibility");
        AddProfileRow("Who pays this?", bill.PaidBy);
        AddProfileRow("Responsibility / owner", bill.ResponsibilityOwner);

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(bill.Notes) ? "None" : bill.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", bill.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", bill.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

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

    private TextBox CreateTextBoxField(FlowLayoutPanel parent, string labelText, int width, bool multiline = false)
    {
        var fieldPanel = CreateFieldPanel(Math.Max(58, multiline ? 136 : 58), Math.Max(700, width + 44));

        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 0)
        };

        var textBox = new TextBox
        {
            Location = new Point(22, 22),
            Size = new Size(width, multiline ? 100 : 26),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
        };

        fieldPanel.Controls.Add(label);
        fieldPanel.Controls.Add(textBox);
        parent.Controls.Add(fieldPanel);
        return textBox;
    }

    private ComboBox CreateComboBoxField(FlowLayoutPanel parent, string labelText, int width, string[] values, string defaultValue)
    {
        var fieldPanel = CreateFieldPanel(62, Math.Max(700, width + 44));

        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 0)
        };

        var comboBox = new ComboBox
        {
            Location = new Point(22, 22),
            Size = new Size(width, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        comboBox.Items.AddRange(values.Cast<object>().ToArray());
        comboBox.SelectedItem = defaultValue;

        fieldPanel.Controls.Add(label);
        fieldPanel.Controls.Add(comboBox);
        parent.Controls.Add(fieldPanel);
        return comboBox;
    }

    private ComboBox CreatePersonChoiceComboBoxField(FlowLayoutPanel parent, string labelText, int width)
    {
        var fieldPanel = CreateFieldPanel(62, Math.Max(700, width + 44));

        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, 0)
        };

        var comboBox = new ComboBox
        {
            Location = new Point(22, 22),
            Size = new Size(width, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };

        fieldPanel.Controls.Add(label);
        fieldPanel.Controls.Add(comboBox);
        parent.Controls.Add(fieldPanel);
        return comboBox;
    }

    private (Label Label, TextBox TextBox) CreateOtherNameField(FlowLayoutPanel parent, string labelText, int width)
    {
        var fieldPanel = CreateFieldPanel(58, Math.Max(700, width + 66));
        fieldPanel.Tag = "OtherFieldPanel";
        fieldPanel.Visible = false;

        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(42, 0),
            Visible = true
        };

        var textBox = new TextBox
        {
            Location = new Point(42, 22),
            Size = new Size(width, 26),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Visible = true,
            Enabled = false
        };

        fieldPanel.Controls.Add(label);
        fieldPanel.Controls.Add(textBox);
        parent.Controls.Add(fieldPanel);
        return (label, textBox);
    }

    private CheckBox CreateCheckBoxField(FlowLayoutPanel parent, string text)
    {
        var fieldPanel = CreateFieldPanel(34, 700);
        var checkBox = CreateCheckBox(text, 0);
        checkBox.Location = new Point(22, 2);
        fieldPanel.Controls.Add(checkBox);
        parent.Controls.Add(fieldPanel);
        return checkBox;
    }

    private static Panel CreateFieldPanel(int height, int width)
    {
        return new Panel
        {
            Width = width,
            Height = height,
            BackColor = Back,
            Margin = new Padding(0, 0, 0, 10)
        };
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

    private ComboBox CreateComboBox(Control parent, string labelText, int y, int width, string[] values, string defaultValue)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, y)
        };
        parent.Controls.Add(label);

        var comboBox = new ComboBox
        {
            Location = new Point(22, y + 22),
            Size = new Size(width, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        comboBox.Items.AddRange(values.Cast<object>().ToArray());
        comboBox.SelectedItem = defaultValue;
        parent.Controls.Add(comboBox);
        return comboBox;
    }

    private ComboBox CreatePersonChoiceComboBox(Control parent, string labelText, int y, int width)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, y)
        };
        parent.Controls.Add(label);

        var comboBox = new ComboBox
        {
            Location = new Point(22, y + 22),
            Size = new Size(width, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        parent.Controls.Add(comboBox);
        return comboBox;
    }

    private (Label Label, TextBox TextBox) CreateOtherNameField(Control parent, string labelText, int y, int width)
    {
        var label = new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(42, y),
            Visible = false
        };

        var textBox = new TextBox
        {
            Location = new Point(42, y + 22),
            Size = new Size(width, 26),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Visible = false,
            Enabled = false
        };

        parent.Controls.Add(label);
        parent.Controls.Add(textBox);
        return (label, textBox);
    }

    private void RefreshPersonChoiceDropdowns()
    {
        if (_paidByComboBox is null || _responsibilityOwnerComboBox is null)
            return;

        var paidValue = GetPersonChoiceValue(_paidByComboBox, _paidByOtherTextBox);
        var ownerValue = GetPersonChoiceValue(_responsibilityOwnerComboBox, _responsibilityOwnerOtherTextBox);
        var choices = BuildPersonChoiceOptions();

        LoadPersonChoiceItems(_paidByComboBox, choices);
        LoadPersonChoiceItems(_responsibilityOwnerComboBox, choices);

        SetPersonChoiceValue(_paidByComboBox, _paidByOtherLabel, _paidByOtherTextBox, paidValue);
        SetPersonChoiceValue(_responsibilityOwnerComboBox, _responsibilityOwnerOtherLabel, _responsibilityOwnerOtherTextBox, ownerValue);
    }

    private List<string> BuildPersonChoiceOptions()
    {
        var choices = new List<string>();
        var activeCase = AppState.ActiveCase;
        var primaryName = activeCase?.PrimaryPersonName?.Trim();

        if (string.IsNullOrWhiteSpace(primaryName))
            primaryName = activeCase?.DisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(primaryName))
            primaryName = "Case Primary Person";

        var selfChoice = $"Self ({primaryName})";
        choices.Add(selfChoice);

        try
        {
            if (!string.IsNullOrWhiteSpace(_caseFolder))
            {
                var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
                var peopleRepository = new HouseholdPeopleRepository(databasePath);

                foreach (var person in peopleRepository.GetAll())
                {
                    var name = person.FullName.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (string.Equals(name, primaryName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!choices.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
                        choices.Add(name);
                }
            }
        }
        catch
        {
            // Keep the bills form usable even if the people list cannot be loaded yet.
        }

        choices.Add("Other");
        return choices;
    }

    private static void LoadPersonChoiceItems(ComboBox comboBox, IReadOnlyList<string> choices)
    {
        comboBox.BeginUpdate();
        comboBox.Items.Clear();
        foreach (var choice in choices)
            comboBox.Items.Add(choice);
        comboBox.EndUpdate();
    }

    private static string GetPersonChoiceValue(ComboBox comboBox, TextBox otherTextBox)
    {
        var selected = comboBox.SelectedItem?.ToString() ?? string.Empty;
        if (string.Equals(selected, "Other", StringComparison.OrdinalIgnoreCase))
            return otherTextBox.Text.Trim();

        return selected.Trim();
    }

    private static void SetPersonChoiceValue(ComboBox comboBox, Label otherLabel, TextBox otherTextBox, string? value)
    {
        value = value?.Trim() ?? string.Empty;

        if (comboBox.Items.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            comboBox.SelectedIndex = 0;
            otherTextBox.Text = string.Empty;
            UpdatePersonChoiceOtherVisibility(comboBox, otherLabel, otherTextBox);
            return;
        }

        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            var itemText = comboBox.Items[i]?.ToString() ?? string.Empty;
            if (string.Equals(itemText, value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedIndex = i;
                otherTextBox.Text = string.Empty;
                UpdatePersonChoiceOtherVisibility(comboBox, otherLabel, otherTextBox);
                return;
            }
        }

        var selfIndex = -1;
        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            var itemText = comboBox.Items[i]?.ToString() ?? string.Empty;
            if (itemText.StartsWith("Self (", StringComparison.OrdinalIgnoreCase) &&
                itemText.EndsWith(")", StringComparison.Ordinal) &&
                string.Equals(itemText[6..^1], value, StringComparison.OrdinalIgnoreCase))
            {
                selfIndex = i;
                break;
            }
        }

        if (selfIndex >= 0)
        {
            comboBox.SelectedIndex = selfIndex;
            otherTextBox.Text = string.Empty;
            UpdatePersonChoiceOtherVisibility(comboBox, otherLabel, otherTextBox);
            return;
        }

        comboBox.SelectedItem = "Other";
        otherTextBox.Text = value;
        UpdatePersonChoiceOtherVisibility(comboBox, otherLabel, otherTextBox);
    }

    private static void UpdatePersonChoiceOtherVisibility(ComboBox comboBox, Label otherLabel, TextBox otherTextBox)
    {
        var showOther = string.Equals(comboBox.SelectedItem?.ToString(), "Other", StringComparison.OrdinalIgnoreCase);

        if (otherLabel.Parent is Control host && Equals(host.Tag, "OtherFieldPanel"))
            host.Visible = showOther;
        else
        {
            otherLabel.Visible = showOther;
            otherTextBox.Visible = showOther;
        }

        otherTextBox.Enabled = showOther;

        if (!showOther)
            otherTextBox.Text = string.Empty;
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

    private Button CreateGreenButton(string text, int x, int y, int width)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 38),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Color.FromArgb(34, 130, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Bill" : safe.Trim();
    }

    private sealed class ReceiptEntryDialog : Form
    {
        private readonly DateTimePicker _datePicker;
        private readonly TextBox _amountTextBox;

        public DateTime ReceiptDate => _datePicker.Value.Date;
        public decimal Amount { get; private set; }

        public ReceiptEntryDialog(string receiptType)
        {
            Text = $"Add {receiptType} Receipt";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 190);
            BackColor = Back;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            var dateLabel = new Label
            {
                Text = "Receipt date",
                Location = new Point(22, 18),
                AutoSize = true,
                ForeColor = TextMuted
            };
            Controls.Add(dateLabel);

            _datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(22, 42),
                Size = new Size(140, 28)
            };
            Controls.Add(_datePicker);

            var amountLabel = new Label
            {
                Text = "Total spent",
                Location = new Point(22, 82),
                AutoSize = true,
                ForeColor = TextMuted
            };
            Controls.Add(amountLabel);

            _amountTextBox = new TextBox
            {
                Location = new Point(22, 106),
                Size = new Size(160, 26),
                BackColor = Back,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_amountTextBox);

            var okButton = new Button
            {
                Text = "Add Receipt",
                Location = new Point(126, 146),
                Size = new Size(110, 34),
                BackColor = Color.FromArgb(34, 130, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.None
            };
            okButton.Click += (_, _) => Confirm();
            Controls.Add(okButton);

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(246, 146),
                Size = new Size(90, 34),
                BackColor = Panel2,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void Confirm()
        {
            if (!decimal.TryParse(_amountTextBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount) || amount <= 0m)
            {
                MessageBox.Show(this, "Enter a valid receipt total.", "Invalid receipt total", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _amountTextBox.Focus();
                return;
            }

            Amount = amount;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private static class MinimalPdfWriter
    {
        public static void WriteBillProfile(string path, Bill bill)
        {
            var lines = new List<PdfLine>
            {
                new(bill.BillName, "Title"),
                new($"Exported: {DateTime.Now:yyyy-MM-dd h:mm tt}", "Muted", 8),

                new("Basic Information", "Section"),
                Row("Bill / expense", bill.BillName),
                Row("Category", bill.Category),
                Row("Priority", bill.Priority),
                Row("Active", YesNo(bill.IsActive)),

                new("Payment Details", "Section"),
                Row("Amount", bill.Amount.ToString("C2")),
                Row("Frequency", bill.Frequency),
                Row("Monthly equivalent", bill.MonthlyEquivalent.ToString("C2")),
                Row("Due date / expected date", bill.DueDate),
                Row("Payment method", bill.AutopayText),
                Row("Past due amount", bill.PastDueAmount.ToString("C2")),

                new("Responsibility", "Section"),
                Row("Who pays this?", bill.PaidBy),
                Row("Responsibility / owner", bill.ResponsibilityOwner),

                new("Notes", "Section"),
                new(string.IsNullOrWhiteSpace(bill.Notes) ? "None" : bill.Notes, "Paragraph", 4),

                new("Record Info", "Section"),
                Row("Created UTC", bill.CreatedUtc.ToString("yyyy-MM-dd HH:mm")),
                Row("Updated UTC", bill.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"))
            };

            var content = BuildPdfContent(lines, "Bill / Expense Profile");
            WritePdf(path, content);
        }

        private static PdfLine Row(string label, string? value) => new($"{label}||{Clean(value)}", "Row");

        private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();

        private static string YesNo(bool value) => value ? "Yes" : "No";

        private sealed record PdfLine(string Text, string Style, int ExtraGapAfter = 0);

        private static string BuildPdfContent(IReadOnlyList<PdfLine> lines, string documentTitle)
        {
            var builder = new StringBuilder();

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
