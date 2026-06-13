using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class DebtsPage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);

    private readonly BindingList<Debt> _debts = new();
    private readonly List<Bill> _availableBills = new();

    private DebtsRepository? _repository;
    private string? _caseFolder;
    private Debt? _selectedDebt;

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addButton = null!;
    private Button _viewButton = null!;
    private Button _editButton = null!;

    private Panel _profilePanel = null!;
    private Label _profileTitleLabel = null!;
    private FlowLayoutPanel _profileDetailsPanel = null!;
    private Button _profileBackButton = null!;
    private Button _profileEditButton = null!;
    private Button _profileRemoveButton = null!;
    private Button _profileExportButton = null!;

    private Panel _editPanel = null!;
    private Label _editTitleLabel = null!;
    private TextBox _debtNameTextBox = null!;
    private ComboBox _debtTypeComboBox = null!;
    private TextBox _creditorTextBox = null!;
    private TextBox _balanceTextBox = null!;
    private TextBox _minimumPaymentTextBox = null!;
    private ComboBox _frequencyComboBox = null!;
    private TextBox _dueDateTextBox = null!;
    private ComboBox _ownerComboBox = null!;
    private Label _ownerOtherLabel = null!;
    private TextBox _ownerOtherTextBox = null!;
    private ComboBox _paidByComboBox = null!;
    private Label _paidByOtherLabel = null!;
    private TextBox _paidByOtherTextBox = null!;
    private ComboBox _paymentTrackingComboBox = null!;
    private Panel _linkedBillPanel = null!;
    private ComboBox _linkedBillComboBox = null!;
    private ComboBox _statusComboBox = null!;
    private ComboBox _priorityComboBox = null!;
    private CheckBox _activeCheckBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public DebtsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
    }

    public AppPageKey PageKey => AppPageKey.Debts;
    public string PageTitle => "Debts";
    public void OnNavigatedTo() => InitializeForActiveCase();
    public bool CanNavigateAway() => true;

    private void BuildUi()
    {
        Controls.Clear();

        var root = new Panel { Dock = DockStyle.Fill, BackColor = Back, Padding = new Padding(28) };
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "Debts",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });

        var subtitle = new Label
        {
            Text = "Track debts, balances, responsibility, status, and linked monthly payments without double-counting bills.",
            Dock = DockStyle.Top,
            Height = 34,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(subtitle);
        subtitle.BringToFront();

        _caseStatusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f),
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

        _contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Back, Padding = new Padding(0, 12, 0, 0) };
        root.Controls.Add(_contentHost);
        _contentHost.BringToFront();

        BuildListPanel();
        BuildProfilePanel();
        BuildEditPanel();
        ShowListPanel();
    }

    private void BuildListPanel()
    {
        _listPanel = new Panel { Dock = DockStyle.Fill, BackColor = Back };
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

        _addButton = CreateButton("Add Debt", 0, 0, 120);
        _viewButton = CreateButton("View Profile", 0, 0, 120);
        _editButton = CreateButton("Edit Selected", 0, 0, 125);
        buttonRow.Controls.Add(_addButton);
        buttonRow.Controls.Add(_viewButton);
        buttonRow.Controls.Add(_editButton);

        var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Border, Padding = new Padding(1) };
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
            DataSource = _debts
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.DefaultCellStyle.BackColor = Back;
        _grid.DefaultCellStyle.ForeColor = TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(43, 70, 105);
        _grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
        _grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(13, 29, 48);
        _grid.RowTemplate.Height = 30;

        AddFillTextColumn("DebtName", "Debt", 30f);
        AddFillTextColumn("DebtType", "Type", 18f);
        AddFillTextColumn("CreditorCollector", "Creditor / Collector", 24f);
        AddTextColumn("CurrentBalance", "Balance", 115, "C2");
        AddTextColumn("MinimumPayment", "Min. Pay", 105, "C2");
        AddFillTextColumn("Status", "Status", 16f);

        _grid.SelectionChanged += (_, _) => UpdateSelectionFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowSelectedProfile();
        ListPageGridHelper.AttachRightClickRemove(_grid, UpdateSelectionFromGrid, RemoveSelectedDebt);
        gridHost.Controls.Add(_grid);

        _addButton.Click += (_, _) => BeginAddDebt();
        _viewButton.Click += (_, _) => ShowSelectedProfile();
        _editButton.Click += (_, _) => BeginEditSelectedDebt();
    }

    private void BuildProfilePanel()
    {
        _profilePanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false };
        _contentHost.Controls.Add(_profilePanel);
        _profilePanel.Resize += (_, _) => UpdateProfileContentWidth();

        var buttonRow = new FlowLayoutPanel { Location = new Point(0, 0), Size = new Size(760, 46), FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Back };
        _profilePanel.Controls.Add(buttonRow);

        _profileBackButton = CreateButton("Back to List", 0, 0, 120);
        _profileEditButton = CreateButton("Edit", 0, 0, 90);
        _profileRemoveButton = CreateButton("Remove", 0, 0, 100);
        _profileExportButton = CreateButton("Export PDF", 0, 0, 120);
        buttonRow.Controls.Add(_profileBackButton);
        buttonRow.Controls.Add(_profileEditButton);
        buttonRow.Controls.Add(_profileRemoveButton);
        buttonRow.Controls.Add(_profileExportButton);

        _profileTitleLabel = new Label { Location = new Point(0, 64), Size = new Size(860, 42), ForeColor = TextPrimary, Font = new Font("Segoe UI", 18f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _profilePanel.Controls.Add(_profileTitleLabel);

        _profileDetailsPanel = new FlowLayoutPanel { Location = new Point(0, 116), Size = new Size(900, 600), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Back };
        _profilePanel.Controls.Add(_profileDetailsPanel);

        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileEditButton.Click += (_, _) => BeginEditSelectedDebt();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedDebt();
        _profileExportButton.Click += (_, _) => ExportCurrentDebtPdf();
    }

    private void BuildEditPanel()
    {
        _editPanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false };
        _contentHost.Controls.Add(_editPanel);

        _editTitleLabel = new Label { Location = new Point(0, 0), Size = new Size(860, 44), ForeColor = TextPrimary, Font = new Font("Segoe UI", 16f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _editPanel.Controls.Add(_editTitleLabel);

        var fieldsPanel = new FlowLayoutPanel { Location = new Point(0, 56), Size = new Size(700, 950), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Back };
        _editPanel.Controls.Add(fieldsPanel);

        _debtNameTextBox = AddField(fieldsPanel, "Debt name", 430);
        _debtTypeComboBox = AddComboField(fieldsPanel, "Debt type", 280, new[] { "Credit Card", "Vehicle Loan", "Mortgage", "Personal Loan", "Medical", "Tax Debt", "Collection", "Family / Informal", "Other" });
        _creditorTextBox = AddField(fieldsPanel, "Creditor / collector", 430);
        _balanceTextBox = AddField(fieldsPanel, "Current balance", 220);
        _minimumPaymentTextBox = AddField(fieldsPanel, "Minimum payment", 220);
        _frequencyComboBox = AddComboField(fieldsPanel, "Payment frequency", 220, new[] { "Weekly", "Every 2 weeks", "Twice monthly", "Monthly", "Quarterly", "Yearly", "One-time / irregular" });
        _dueDateTextBox = AddField(fieldsPanel, "Due date / expected date", 280);
        _ownerComboBox = AddComboField(fieldsPanel, "Responsibility / owner", 340, Array.Empty<string>());
        (_ownerOtherLabel, _ownerOtherTextBox) = AddOtherField(fieldsPanel, "Outside responsible party name", 360);
        _paidByComboBox = AddComboField(fieldsPanel, "Who pays this?", 340, Array.Empty<string>());
        (_paidByOtherLabel, _paidByOtherTextBox) = AddOtherField(fieldsPanel, "Outside payer name", 360);
        _paymentTrackingComboBox = AddComboField(fieldsPanel, "Monthly payment tracking", 280, new[] { "Not Linked", "Select Existing Bill", "Create Bill Now" });
        _linkedBillPanel = AddLinkedBillField(fieldsPanel);
        _statusComboBox = AddComboField(fieldsPanel, "Status", 260, new[] { "Current", "Past Due", "Collections", "Settlement / Payment Plan", "Paid Off", "Disputed / Unknown" });
        _priorityComboBox = AddComboField(fieldsPanel, "Priority", 220, new[] { "Low", "Normal", "High", "Urgent" });
        _activeCheckBox = new CheckBox { Text = "Active debt", ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f), AutoSize = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 8, 0, 8) };
        fieldsPanel.Controls.Add(_activeCheckBox);
        _notesTextBox = AddMultilineField(fieldsPanel, "Notes", 650, 110);

        var buttonRow = new FlowLayoutPanel { Width = 700, Height = 46, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Back, Margin = new Padding(0, 8, 0, 0) };
        _saveButton = CreateButton("Save Debt", 0, 0, 135);
        _cancelButton = CreateButton("Cancel", 0, 0, 110);
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_cancelButton);
        fieldsPanel.Controls.Add(buttonRow);

        _ownerComboBox.SelectedIndexChanged += (_, _) => UpdatePersonOtherVisibility(_ownerComboBox, _ownerOtherLabel, _ownerOtherTextBox);
        _paidByComboBox.SelectedIndexChanged += (_, _) => UpdatePersonOtherVisibility(_paidByComboBox, _paidByOtherLabel, _paidByOtherTextBox);
        _paymentTrackingComboBox.SelectedIndexChanged += (_, _) => UpdateBillLinkVisibility();
        _saveButton.Click += (_, _) => SaveDebtFromEditor();
        _cancelButton.Click += (_, _) => CancelEdit();
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
            _debts.Clear();
            _caseStatusLabel.Text = "No active case is open. Open or create a case before adding debts.";
            _caseStatusLabel.ForeColor = Danger;
            UpdateStats();
            return;
        }

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
        _repository = new DebtsRepository(databasePath);
        _caseStatusLabel.Text = "Active case folder: " + _caseFolder;
        _caseStatusLabel.ForeColor = TextMuted;
        RefreshPersonChoiceDropdowns();
        RefreshBillChoices();
        ReloadDebts();
    }

    private void ReloadDebts()
    {
        _debts.Clear();
        if (_repository is not null)
        {
            foreach (var debt in _repository.GetAll())
                _debts.Add(debt);
        }
        UpdateStats();
        ListPageGridHelper.ApplyInactiveRowStyles(_grid, item => item is Debt debt && debt.IsActive);
        UpdateSelectionFromGrid();
    }

    private void UpdateStats()
    {
        var active = _debts.Count(d => d.IsActive);
        var totalBalance = _debts.Where(d => d.IsActive).Sum(d => d.CurrentBalance);
        var monthly = _debts.Where(d => d.IsActive && !string.Equals(d.PaymentTracking, "Select Existing Bill", StringComparison.OrdinalIgnoreCase)).Sum(d => d.MonthlyEquivalent);
        _statsLabel.Text = $"Debts: {_debts.Count}     Active: {active}     Total balance: {totalBalance:C2}     Unlinked monthly payments: {monthly:C2}";
    }

    private void UpdateSelectionFromGrid()
    {
        _selectedDebt = _grid.CurrentRow?.DataBoundItem as Debt;
        var has = _selectedDebt is not null;
        _viewButton.Enabled = has;
        _editButton.Enabled = has;
    }

    private void BeginAddDebt()
    {
        if (_repository is null)
        {
            MessageBox.Show("Open or create a case first.", "No active case", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _selectedDebt = new Debt { IsActive = true, PaymentFrequency = "Monthly", Status = "Current", Priority = "Normal" };
        LoadEditor(_selectedDebt, true);
        ShowEditPanel();
    }

    private void BeginEditSelectedDebt()
    {
        if (_selectedDebt is null)
        {
            MessageBox.Show("Select a debt first.", "No debt selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        LoadEditor(_selectedDebt, false);
        ShowEditPanel();
    }

    private void ShowSelectedProfile()
    {
        if (_selectedDebt is null)
        {
            MessageBox.Show("Select a debt first.", "No debt selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _profileTitleLabel.Text = _selectedDebt.DebtName;
        PopulateProfileDetails(_selectedDebt);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var debt = _debts.FirstOrDefault(d => d.Id == recordId);
        if (debt is null && _repository is not null)
            debt = _repository.GetAll().FirstOrDefault(d => d.Id == recordId);

        if (debt is null)
            return false;

        _selectedDebt = debt;
        SelectGridRowByDebtId(recordId);
        _profileTitleLabel.Text = debt.DebtName;
        PopulateProfileDetails(debt);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByDebtId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is Debt debt && debt.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void LoadEditor(Debt debt, bool isNew)
    {
        _editTitleLabel.Text = isNew ? "Add Debt" : "Edit Debt";
        _debtNameTextBox.Text = debt.DebtName;
        SetComboValue(_debtTypeComboBox, debt.DebtType, "Other");
        _creditorTextBox.Text = debt.CreditorCollector;
        _balanceTextBox.Text = debt.CurrentBalance == 0m ? string.Empty : debt.CurrentBalance.ToString("0.##", CultureInfo.CurrentCulture);
        _minimumPaymentTextBox.Text = debt.MinimumPayment == 0m ? string.Empty : debt.MinimumPayment.ToString("0.##", CultureInfo.CurrentCulture);
        SetComboValue(_frequencyComboBox, debt.PaymentFrequency, "Monthly");
        _dueDateTextBox.Text = debt.DueDate;
        RefreshPersonChoiceDropdowns();
        SetPersonChoiceValue(_ownerComboBox, _ownerOtherLabel, _ownerOtherTextBox, debt.ResponsibilityOwner);
        SetPersonChoiceValue(_paidByComboBox, _paidByOtherLabel, _paidByOtherTextBox, debt.PaidBy);
        RefreshBillChoices();
        SetComboValue(_paymentTrackingComboBox, debt.PaymentTracking, "Not Linked");
        SelectLinkedBill(debt.LinkedBillId, debt.LinkedBillName);
        SetComboValue(_statusComboBox, debt.Status, "Current");
        SetComboValue(_priorityComboBox, debt.Priority, "Normal");
        _activeCheckBox.Checked = debt.IsActive;
        _notesTextBox.Text = debt.Notes;
        UpdateBillLinkVisibility();
    }

    private void SaveDebtFromEditor()
    {
        if (_repository is null || _selectedDebt is null)
            return;

        var name = _debtNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a debt name.", "Missing debt name", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _debtNameTextBox.Focus();
            return;
        }

        if (!TryParseMoney(_balanceTextBox.Text, out var balance))
        {
            MessageBox.Show("Enter a valid current balance, or leave it blank.", "Invalid balance", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!TryParseMoney(_minimumPaymentTextBox.Text, out var minPayment))
        {
            MessageBox.Show("Enter a valid minimum payment, or leave it blank.", "Invalid minimum payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tracking = _paymentTrackingComboBox.SelectedItem?.ToString() ?? "Not Linked";
        if (string.Equals(tracking, "Create Bill Now", StringComparison.OrdinalIgnoreCase))
        {
            var created = CreateBillForDebt(name, minPayment);
            if (created is null)
                return;
            tracking = "Select Existing Bill";
            RefreshBillChoices();
            SelectLinkedBill(created.Id, created.BillName);
            _paymentTrackingComboBox.SelectedItem = tracking;
            UpdateBillLinkVisibility();
        }

        var linkedBillId = 0L;
        var linkedBillName = string.Empty;
        if (string.Equals(tracking, "Select Existing Bill", StringComparison.OrdinalIgnoreCase))
        {
            if (_linkedBillComboBox.SelectedItem is Bill bill)
            {
                linkedBillId = bill.Id;
                linkedBillName = bill.BillName;
            }
            else
            {
                MessageBox.Show("Select the bill/payment linked to this debt.", "Missing linked bill", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        _selectedDebt.DebtName = name;
        _selectedDebt.DebtType = _debtTypeComboBox.SelectedItem?.ToString() ?? "Other";
        _selectedDebt.CreditorCollector = _creditorTextBox.Text.Trim();
        _selectedDebt.CurrentBalance = balance;
        _selectedDebt.MinimumPayment = minPayment;
        _selectedDebt.PaymentFrequency = _frequencyComboBox.SelectedItem?.ToString() ?? "Monthly";
        _selectedDebt.DueDate = _dueDateTextBox.Text.Trim();
        _selectedDebt.ResponsibilityOwner = GetPersonChoiceValue(_ownerComboBox, _ownerOtherTextBox);
        _selectedDebt.PaidBy = GetPersonChoiceValue(_paidByComboBox, _paidByOtherTextBox);
        _selectedDebt.PaymentTracking = tracking;
        _selectedDebt.LinkedBillId = linkedBillId;
        _selectedDebt.LinkedBillName = linkedBillName;
        _selectedDebt.Status = _statusComboBox.SelectedItem?.ToString() ?? "Current";
        _selectedDebt.Priority = _priorityComboBox.SelectedItem?.ToString() ?? "Normal";
        _selectedDebt.IsActive = _activeCheckBox.Checked;
        _selectedDebt.Notes = _notesTextBox.Text.Trim();

        _repository.Upsert(_selectedDebt);
        ReloadDebts();
        ShowListPanel();
    }

    private Bill? CreateBillForDebt(string debtName, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return null;

        using var form = new Form
        {
            Text = "Create Bill for Debt Payment",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(430, 245),
            BackColor = Back,
            ForeColor = TextPrimary
        };

        var nameBox = new TextBox { Location = new Point(22, 42), Size = new Size(380, 26), Text = debtName + " Payment", BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        var amountBox = new TextBox { Location = new Point(22, 98), Size = new Size(160, 26), Text = amount == 0m ? string.Empty : amount.ToString("0.##"), BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        var frequency = new ComboBox { Location = new Point(22, 154), Size = new Size(180, 26), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Back, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
        frequency.Items.AddRange(new object[] { "Weekly", "Every 2 weeks", "Twice monthly", "Monthly", "Quarterly", "Yearly", "One-time / irregular" });
        frequency.SelectedItem = _frequencyComboBox.SelectedItem?.ToString() ?? "Monthly";

        form.Controls.Add(MakeDialogLabel("Bill / expense name", 22, 20));
        form.Controls.Add(nameBox);
        form.Controls.Add(MakeDialogLabel("Amount", 22, 76));
        form.Controls.Add(amountBox);
        form.Controls.Add(MakeDialogLabel("Frequency", 22, 132));
        form.Controls.Add(frequency);

        var ok = CreateDialogButton("Create Bill", 214, 190, 100);
        var cancel = CreateDialogButton("Cancel", 324, 190, 80);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        Bill? created = null;
        ok.Click += (_, _) =>
        {
            var billName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(billName))
            {
                MessageBox.Show(form, "Enter a bill name.", "Missing bill name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!TryParseMoney(amountBox.Text, out var billAmount))
            {
                MessageBox.Show(form, "Enter a valid amount, or leave it blank.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
            var repository = new BillsRepository(databasePath);
            created = new Bill
            {
                BillName = billName,
                Category = "Debt Payment",
                Amount = billAmount,
                Frequency = frequency.SelectedItem?.ToString() ?? "Monthly",
                DueDate = _dueDateTextBox.Text.Trim(),
                PaidBy = GetPersonChoiceValue(_paidByComboBox, _paidByOtherTextBox),
                ResponsibilityOwner = GetPersonChoiceValue(_ownerComboBox, _ownerOtherTextBox),
                Priority = _priorityComboBox.SelectedItem?.ToString() ?? "Normal",
                IsActive = true,
                Notes = "Created from Debts page for: " + debtName
            };
            repository.Upsert(created);
            form.DialogResult = DialogResult.OK;
            form.Close();
        };
        cancel.Click += (_, _) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

        return form.ShowDialog(this) == DialogResult.OK ? created : null;
    }

    private void RemoveSelectedDebt()
    {
        if (_repository is null || _selectedDebt is null || _selectedDebt.Id <= 0)
            return;

        var result = MessageBox.Show($"Remove {_selectedDebt.DebtName}?\n\nThis deletes this debt from the current case.", "Remove debt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedDebt.Id);
        _selectedDebt = null;
        ReloadDebts();
        ShowListPanel();
    }

    private void CancelEdit()
    {
        if (_selectedDebt is not null && _selectedDebt.Id > 0)
            ShowSelectedProfile();
        else
            ShowListPanel();
    }

    private void PopulateProfileDetails(Debt debt)
    {
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Basic Information");
        AddProfileRow("Debt", debt.DebtName);
        AddProfileRow("Type", debt.DebtType);
        AddProfileRow("Creditor / collector", debt.CreditorCollector);
        AddProfileRow("Status", debt.Status);
        AddProfileRow("Priority", debt.Priority);
        AddProfileRow("Active", YesNo(debt.IsActive));

        AddProfileSpacer();
        AddProfileSection("Money Details");
        AddProfileRow("Current balance", debt.CurrentBalance.ToString("C2"));
        AddProfileRow("Minimum payment", debt.MinimumPayment.ToString("C2"));
        AddProfileRow("Payment frequency", debt.PaymentFrequency);
        AddProfileRow("Monthly equivalent", debt.MonthlyEquivalent.ToString("C2"));
        AddProfileRow("Due date / expected date", debt.DueDate);

        AddProfileSpacer();
        AddProfileSection("Responsibility");
        AddProfileRow("Responsibility / owner", debt.ResponsibilityOwner);
        AddProfileRow("Who pays this?", debt.PaidBy);

        AddProfileSpacer();
        AddProfileSection("Linked Payment");
        AddProfileRow("Payment tracking", debt.PaymentTracking);
        AddProfileRow("Linked bill", string.IsNullOrWhiteSpace(debt.LinkedBillName) ? "None" : debt.LinkedBillName);

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(debt.Notes) ? "None" : debt.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", debt.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", debt.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

        _profileDetailsPanel.ResumeLayout();
        UpdateProfileContentWidth();
    }

    private void ExportCurrentDebtPdf()
    {
        if (_selectedDebt is null || _selectedDebt.Id <= 0)
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Export Debt Profile PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(_selectedDebt.DebtName) + "_Debt_Profile.pdf",
            InitialDirectory = GetDefaultExportFolder(),
            AddExtension = true,
            DefaultExt = "pdf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WriteDebtProfile(dialog.FileName, _selectedDebt);
            MessageBox.Show("Debt profile PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not export PDF:\n{ex.Message}", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowListPanel() { _listPanel.Visible = true; _profilePanel.Visible = false; _editPanel.Visible = false; _listPanel.BringToFront(); }
    private void ShowProfilePanel() { _listPanel.Visible = false; _profilePanel.Visible = true; _editPanel.Visible = false; _profilePanel.BringToFront(); _profilePanel.AutoScrollPosition = Point.Empty; UpdateProfileContentWidth(); }
    private void ShowEditPanel() { _listPanel.Visible = false; _profilePanel.Visible = false; _editPanel.Visible = true; _editPanel.BringToFront(); _editPanel.AutoScrollPosition = Point.Empty; }

    private void UpdateProfileContentWidth()
    {
        if (_profilePanel is null || _profileDetailsPanel is null)
            return;
        var usableWidth = Math.Max(400, _profilePanel.ClientSize.Width - 24);
        _profileDetailsPanel.Width = usableWidth;
        foreach (Control control in _profileDetailsPanel.Controls)
            control.Width = usableWidth;
    }

    private void AddProfileSection(string text) => _profileDetailsPanel.Controls.Add(new Label { Text = text, ForeColor = TextPrimary, Font = new Font("Segoe UI", 13f, FontStyle.Bold), AutoSize = false, Height = 30, Margin = new Padding(0, 14, 0, 4), TextAlign = ContentAlignment.MiddleLeft });
    private void AddProfileSpacer() => _profileDetailsPanel.Controls.Add(new Panel { Height = 1, BackColor = Border, Margin = new Padding(0, 8, 0, 8) });

    private void AddProfileRow(string label, string? value)
    {
        var panel = new Panel { Height = 24, BackColor = Back, Margin = new Padding(0, 0, 0, 2) };
        var left = new Label { Text = label + ":", Location = new Point(0, 0), Size = new Size(210, 24), ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
        var right = new Label { Text = string.IsNullOrWhiteSpace(value) ? "None" : value.Trim(), Location = new Point(220, 0), Size = new Size(650, 24), ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(left);
        panel.Controls.Add(right);
        _profileDetailsPanel.Controls.Add(panel);
    }

    private void AddProfileParagraph(string text)
    {
        var width = Math.Max(600, _profilePanel.ClientSize.Width - 60);
        var label = new Label { Text = text, AutoSize = false, Width = width, Height = 70, ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f), Margin = new Padding(0, 0, 0, 4) };
        _profileDetailsPanel.Controls.Add(label);
    }

    private void AddFillTextColumn(string propertyName, string headerText, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = propertyName, HeaderText = headerText, FillWeight = fillWeight, MinimumWidth = 100 });
    }

    private void AddTextColumn(string propertyName, string headerText, int width, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn { DataPropertyName = propertyName, HeaderText = headerText, Width = width, MinimumWidth = Math.Min(width, 80), AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
        if (!string.IsNullOrWhiteSpace(format))
            column.DefaultCellStyle.Format = format;
        _grid.Columns.Add(column);
    }

    private TextBox AddField(FlowLayoutPanel parent, string labelText, int width)
    {
        var panel = MakeFieldPanel(labelText, width);
        var textBox = new TextBox { Width = width, Height = 26, Location = new Point(0, 22), BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(textBox);
        parent.Controls.Add(panel);
        return textBox;
    }

    private TextBox AddMultilineField(FlowLayoutPanel parent, string labelText, int width, int height)
    {
        var panel = MakeFieldPanel(labelText, width, height + 26);
        var textBox = new TextBox { Width = width, Height = height, Location = new Point(0, 22), BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(textBox);
        parent.Controls.Add(panel);
        return textBox;
    }

    private ComboBox AddComboField(FlowLayoutPanel parent, string labelText, int width, IEnumerable<string> values)
    {
        var panel = MakeFieldPanel(labelText, width);
        var combo = new ComboBox { Width = width, Height = 26, Location = new Point(0, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Back, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f) };
        foreach (var value in values)
            combo.Items.Add(value);
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
        panel.Controls.Add(combo);
        parent.Controls.Add(panel);
        return combo;
    }

    private Panel MakeFieldPanel(string labelText, int width, int height = 56)
    {
        var panel = new Panel { Width = 700, Height = height, BackColor = Back, Margin = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(new Label { Text = labelText, Location = new Point(0, 0), Size = new Size(width, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 9.5f) });
        return panel;
    }

    private (Label Label, TextBox TextBox) AddOtherField(FlowLayoutPanel parent, string labelText, int width)
    {
        var panel = MakeFieldPanel(labelText, width);
        panel.Tag = "OtherFieldPanel";
        var label = panel.Controls.OfType<Label>().First();
        var textBox = new TextBox { Width = width, Height = 26, Location = new Point(0, 22), BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(textBox);
        panel.Visible = false;
        parent.Controls.Add(panel);
        return (label, textBox);
    }

    private Panel AddLinkedBillField(FlowLayoutPanel parent)
    {
        var panel = MakeFieldPanel("Linked bill / expense", 430);
        panel.Tag = "LinkedBillPanel";
        _linkedBillComboBox = new ComboBox { Width = 430, Height = 26, Location = new Point(0, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Back, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(_linkedBillComboBox);
        panel.Visible = false;
        parent.Controls.Add(panel);
        return panel;
    }

    private void RefreshPersonChoiceDropdowns()
    {
        if (_ownerComboBox is null || _paidByComboBox is null)
            return;
        var ownerValue = GetPersonChoiceValue(_ownerComboBox, _ownerOtherTextBox);
        var paidValue = GetPersonChoiceValue(_paidByComboBox, _paidByOtherTextBox);
        var choices = BuildPersonChoiceOptions();
        LoadComboItems(_ownerComboBox, choices);
        LoadComboItems(_paidByComboBox, choices);
        SetPersonChoiceValue(_ownerComboBox, _ownerOtherLabel, _ownerOtherTextBox, ownerValue);
        SetPersonChoiceValue(_paidByComboBox, _paidByOtherLabel, _paidByOtherTextBox, paidValue);
    }

    private List<string> BuildPersonChoiceOptions()
    {
        var choices = new List<string>();
        var primaryName = AppState.ActiveCase?.PrimaryPersonName?.Trim();
        if (string.IsNullOrWhiteSpace(primaryName))
            primaryName = AppState.ActiveCase?.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(primaryName))
            primaryName = "Case Primary Person";
        choices.Add($"Self ({primaryName})");

        try
        {
            if (!string.IsNullOrWhiteSpace(_caseFolder))
            {
                var peopleRepository = new HouseholdPeopleRepository(CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder));
                foreach (var person in peopleRepository.GetAll())
                {
                    var name = person.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(name) && !choices.Any(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase)) && !string.Equals(name, primaryName, StringComparison.OrdinalIgnoreCase))
                        choices.Add(name);
                }
            }
        }
        catch { }
        choices.Add("Other");
        return choices;
    }

    private void RefreshBillChoices()
    {
        if (_linkedBillComboBox is null)
            return;
        _availableBills.Clear();
        _linkedBillComboBox.Items.Clear();
        try
        {
            if (!string.IsNullOrWhiteSpace(_caseFolder))
            {
                var repository = new BillsRepository(CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder));
                foreach (var bill in repository.GetAll())
                {
                    _availableBills.Add(bill);
                    _linkedBillComboBox.Items.Add(bill);
                }
            }
        }
        catch { }
        _linkedBillComboBox.DisplayMember = nameof(Bill.BillName);
    }

    private void UpdateBillLinkVisibility()
    {
        var choice = _paymentTrackingComboBox.SelectedItem?.ToString() ?? "Not Linked";
        _linkedBillPanel.Visible = string.Equals(choice, "Select Existing Bill", StringComparison.OrdinalIgnoreCase);
    }

    private void SelectLinkedBill(long billId, string billName)
    {
        if (billId <= 0 && string.IsNullOrWhiteSpace(billName))
        {
            if (_linkedBillComboBox.Items.Count > 0)
                _linkedBillComboBox.SelectedIndex = 0;
            return;
        }
        foreach (var item in _linkedBillComboBox.Items)
        {
            if (item is Bill bill && (bill.Id == billId || string.Equals(bill.BillName, billName, StringComparison.OrdinalIgnoreCase)))
            {
                _linkedBillComboBox.SelectedItem = bill;
                return;
            }
        }
    }

    private static void LoadComboItems(ComboBox combo, IEnumerable<string> choices)
    {
        combo.Items.Clear();
        foreach (var choice in choices)
            combo.Items.Add(choice);
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private void SetPersonChoiceValue(ComboBox combo, Label otherLabel, TextBox otherTextBox, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
            UpdatePersonOtherVisibility(combo, otherLabel, otherTextBox);
            return;
        }
        foreach (var item in combo.Items)
        {
            if (string.Equals(item.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                UpdatePersonOtherVisibility(combo, otherLabel, otherTextBox);
                return;
            }
        }
        combo.SelectedItem = "Other";
        otherTextBox.Text = value;
        UpdatePersonOtherVisibility(combo, otherLabel, otherTextBox);
    }

    private string GetPersonChoiceValue(ComboBox combo, TextBox otherTextBox)
    {
        var selected = combo.SelectedItem?.ToString() ?? string.Empty;
        return string.Equals(selected, "Other", StringComparison.OrdinalIgnoreCase) ? otherTextBox.Text.Trim() : selected;
    }

    private void UpdatePersonOtherVisibility(ComboBox combo, Label label, TextBox textBox)
    {
        var show = string.Equals(combo.SelectedItem?.ToString(), "Other", StringComparison.OrdinalIgnoreCase);
        if (label.Parent is Control host && Equals(host.Tag, "OtherFieldPanel"))
            host.Visible = show;
        label.Visible = show;
        textBox.Visible = show;
        textBox.Enabled = show;
        if (!show)
            textBox.Text = string.Empty;
    }

    private static void SetComboValue(ComboBox combo, string value, string fallback)
    {
        foreach (var item in combo.Items)
        {
            if (string.Equals(item?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        foreach (var item in combo.Items)
        {
            if (string.Equals(item?.ToString(), fallback, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private static bool TryParseMoney(string text, out decimal amount)
    {
        amount = 0m;
        return string.IsNullOrWhiteSpace(text) || decimal.TryParse(text, NumberStyles.Currency, CultureInfo.CurrentCulture, out amount);
    }

    private string GetDefaultExportFolder()
    {
        var fallback = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return fallback;
        try { var folder = Path.Combine(_caseFolder, "exports"); Directory.CreateDirectory(folder); return folder; }
        catch { return fallback; }
    }

    private Button CreateButton(string text, int x, int y, int width) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), Anchor = AnchorStyles.Left | AnchorStyles.Top, BackColor = Panel2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
    private Button CreateDialogButton(string text, int x, int y, int width) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 32), BackColor = Panel2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), DialogResult = DialogResult.None };
    private Label MakeDialogLabel(string text, int x, int y) => new() { Text = text, Location = new Point(x, y), Size = new Size(360, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 9.2f) };
    private static string YesNo(bool value) => value ? "Yes" : "No";
    private static string SanitizeFileName(string value) { var invalid = Path.GetInvalidFileNameChars(); var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()); return string.IsNullOrWhiteSpace(safe) ? "Debt" : safe.Trim(); }

    private static class MinimalPdfWriter
    {
        public static void WriteDebtProfile(string path, Debt debt)
        {
            var lines = new List<string>
            {
                "Family Finance & Trust Manager",
                "Debt Profile",
                "",
                debt.DebtName,
                "Exported: " + DateTime.Now.ToString("yyyy-MM-dd h:mm tt"),
                "",
                "Basic Information",
                $"Debt: {Clean(debt.DebtName)}",
                $"Type: {Clean(debt.DebtType)}",
                $"Creditor / collector: {Clean(debt.CreditorCollector)}",
                $"Status: {Clean(debt.Status)}",
                $"Priority: {Clean(debt.Priority)}",
                $"Active: {(debt.IsActive ? "Yes" : "No")}",
                "",
                "Money Details",
                $"Current balance: {debt.CurrentBalance:C2}",
                $"Minimum payment: {debt.MinimumPayment:C2}",
                $"Payment frequency: {Clean(debt.PaymentFrequency)}",
                $"Monthly equivalent: {debt.MonthlyEquivalent:C2}",
                $"Due date / expected date: {Clean(debt.DueDate)}",
                "",
                "Responsibility",
                $"Responsibility / owner: {Clean(debt.ResponsibilityOwner)}",
                $"Who pays this?: {Clean(debt.PaidBy)}",
                "",
                "Linked Payment",
                $"Payment tracking: {Clean(debt.PaymentTracking)}",
                $"Linked bill: {(string.IsNullOrWhiteSpace(debt.LinkedBillName) ? "None" : debt.LinkedBillName)}",
                "",
                "Notes",
                string.IsNullOrWhiteSpace(debt.Notes) ? "None" : debt.Notes
            };
            WriteSimplePdf(path, lines);
        }

        private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();

        private static void WriteSimplePdf(string path, IReadOnlyList<string> lines)
        {
            var content = new StringBuilder();
            content.AppendLine("BT");
            content.AppendLine("/F1 11 Tf");
            content.AppendLine("50 760 Td");
            var first = true;
            foreach (var raw in lines.Take(42))
            {
                var line = Escape(raw);
                if (!first)
                    content.AppendLine("0 -17 Td");
                content.AppendLine($"({line}) Tj");
                first = false;
            }
            content.AppendLine("ET");

            var stream = content.ToString();
            var objects = new List<string>
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
                "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
                $"5 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream\nendobj\n"
            };

            var pdf = new StringBuilder();
            pdf.AppendLine("%PDF-1.4");
            var offsets = new List<int> { 0 };
            foreach (var obj in objects)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.Append(obj);
            }
            var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.AppendLine("xref");
            pdf.AppendLine("0 6");
            pdf.AppendLine("0000000000 65535 f ");
            for (var i = 1; i < offsets.Count; i++)
                pdf.AppendLine(offsets[i].ToString("0000000000") + " 00000 n ");
            pdf.AppendLine("trailer");
            pdf.AppendLine("<< /Size 6 /Root 1 0 R >>");
            pdf.AppendLine("startxref");
            pdf.AppendLine(xref.ToString(CultureInfo.InvariantCulture));
            pdf.AppendLine("%%EOF");
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes(pdf.ToString()));
        }

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
