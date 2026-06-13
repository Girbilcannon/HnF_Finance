using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class AllowanceSavingsPage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);
    private static readonly Color ButtonBack = Color.FromArgb(31, 55, 84);
    private static readonly Color Panel = Color.FromArgb(15, 34, 56);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Accent = Color.FromArgb(58, 91, 132);

    private readonly BindingList<AllowanceSavingsItem> _items = new();

    private AllowanceSavingsRepository? _repository;
    private string? _caseFolder;
    private AllowanceSavingsItem? _selectedItem;

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addButton = null!;
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
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private Label _amountLabel = null!;
    private Label _frequencyLabel = null!;
    private Label _whereMethodLabel = null!;
    private Label _notesLabel = null!;
    private TextBox _nameTextBox = null!;
    private ComboBox _typeComboBox = null!;
    private TextBox _amountTextBox = null!;
    private ComboBox _frequencyComboBox = null!;
    private ComboBox _whereMethodComboBox = null!;
    private Label _linkedBankLabel = null!;
    private ComboBox _linkedBankComboBox = null!;
    private Button _createBankButton = null!;
    private Label _otherWhereLabel = null!;
    private TextBox _otherWhereTextBox = null!;
    private CheckBox _activeCheckBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public AllowanceSavingsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
    }

    public AppPageKey PageKey => AppPageKey.AllowanceSavings;
    public string PageTitle => "Allowance / Savings";

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
            Text = "Allowance / Savings",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Reserve money for fun-money allowances and savings goals without mixing them into regular bills.",
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
            Height = 36,
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
            BackColor = Back
        };
        _contentHost.Controls.Add(_listPanel);

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back,
            Padding = new Padding(0, 8, 0, 0)
        };
        _listPanel.Controls.Add(buttonRow);

        _addButton = CreateButton("Add Allowance / Savings", 0, 0, 185);
        _viewProfileButton = CreateButton("View Profile", 0, 0, 120);
        _editSelectedButton = CreateButton("Edit Selected", 0, 0, 125);
        buttonRow.Controls.Add(_addButton);
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
            DataSource = _items
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Panel2;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
        _grid.DefaultCellStyle.BackColor = Panel;
        _grid.DefaultCellStyle.ForeColor = TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = Accent;
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(13, 29, 48);
        _grid.RowTemplate.Height = 28;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AllowanceSavingsItem.ItemName), HeaderText = "Name", FillWeight = 38 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AllowanceSavingsItem.ItemType), HeaderText = "Type", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Monthly $", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(AllowanceSavingsItem.WhereStored), HeaderText = "Where", FillWeight = 26 });
        _grid.CellFormatting += Grid_CellFormatting;
        _grid.SelectionChanged += (_, _) => UpdateSelectedFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowProfileForSelected();
        ListPageGridHelper.AttachRightClickRemove(_grid, UpdateSelectedFromGrid, RemoveSelectedItem);

        gridHost.Controls.Add(_grid);

        _addButton.Click += (_, _) => BeginAddItem();
        _viewProfileButton.Click += (_, _) => ShowProfileForSelected();
        _editSelectedButton.Click += (_, _) => BeginEditSelectedItem();
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
            Size = new Size(760, 48),
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
            Size = new Size(760, 40),
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
            BackColor = Back,
            Visible = true
        };
        _profilePanel.Controls.Add(_profileDetailsPanel);

        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileEditButton.Click += (_, _) => BeginEditSelectedItem();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedItem();
        _profileExportButton.Click += (_, _) => ExportCurrentItemPdf();
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
            Text = "Add Allowance / Savings",
            Location = new Point(0, 0),
            Size = new Size(760, 42),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _editPanel.Controls.Add(_editTitleLabel);

        int y = 58;
        _nameLabel = CreateFieldLabel("Name / purpose", y);
        _editPanel.Controls.Add(_nameLabel);
        y += 22;
        _nameTextBox = CreateRawTextBox(18, y, 430);
        _editPanel.Controls.Add(_nameTextBox);
        y += 52;

        _typeLabel = CreateFieldLabel("Type", y);
        _editPanel.Controls.Add(_typeLabel);
        y += 22;
        _typeComboBox = CreateRawComboBox(18, y, new[] { "Allowance", "Savings" }, 220);
        _editPanel.Controls.Add(_typeComboBox);
        y += 54;

        _amountLabel = CreateFieldLabel("Amount", y);
        _editPanel.Controls.Add(_amountLabel);
        y += 22;
        _amountTextBox = CreateRawTextBox(18, y, 220);
        _editPanel.Controls.Add(_amountTextBox);
        y += 52;

        _frequencyLabel = CreateFieldLabel("Frequency", y);
        _editPanel.Controls.Add(_frequencyLabel);
        y += 22;
        _frequencyComboBox = CreateRawComboBox(18, y, GetFrequencies(), 260);
        _editPanel.Controls.Add(_frequencyComboBox);
        y += 54;

        _whereMethodLabel = CreateFieldLabel("Where / account / envelope", y);
        _editPanel.Controls.Add(_whereMethodLabel);
        y += 22;
        _whereMethodComboBox = CreateRawComboBox(18, y, GetWhereMethods(), 320);
        _editPanel.Controls.Add(_whereMethodComboBox);
        y += 54;

        _linkedBankLabel = CreateFieldLabel("Bank account", y);
        _editPanel.Controls.Add(_linkedBankLabel);
        _linkedBankComboBox = new ComboBox
        {
            Location = new Point(18, y + 22),
            Size = new Size(520, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f)
        };
        _editPanel.Controls.Add(_linkedBankComboBox);

        _createBankButton = CreateButton("Create Bank Account Now", 18, y + 22, 210);
        _editPanel.Controls.Add(_createBankButton);

        _otherWhereLabel = CreateFieldLabel("Other location / envelope", y);
        _editPanel.Controls.Add(_otherWhereLabel);
        _otherWhereTextBox = new TextBox
        {
            Location = new Point(18, y + 22),
            Size = new Size(430, 26),
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };
        _editPanel.Controls.Add(_otherWhereTextBox);

        _activeCheckBox = new CheckBox
        {
            Text = "Active allowance / savings item",
            Location = new Point(18, y + 4),
            Size = new Size(360, 28),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5f),
            Checked = true
        };
        _editPanel.Controls.Add(_activeCheckBox);

        _notesLabel = CreateFieldLabel("Notes", y);
        _editPanel.Controls.Add(_notesLabel);
        y += 22;
        _notesTextBox = new TextBox
        {
            Location = new Point(18, y),
            Size = new Size(690, 130),
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        _editPanel.Controls.Add(_notesTextBox);
        y += _notesTextBox.Height + 18;

        var buttonRow = new FlowLayoutPanel
        {
            Location = new Point(18, y + 8),
            Size = new Size(600, 48),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back
        };
        _saveButton = CreateButton("Save Item", 0, 0, 150);
        _cancelButton = CreateButton("Cancel", 0, 0, 120);
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_cancelButton);
        _editPanel.Controls.Add(buttonRow);

        _whereMethodComboBox.SelectedIndexChanged += (_, _) => HandleWhereMethodChanged();
        _createBankButton.Click += (_, _) => CreateBankFromEdit();
        _saveButton.Click += (_, _) => SaveCurrentEdit();
        _cancelButton.Click += (_, _) => ShowListPanel();
    }

    private void InitializeForActiveCase()
    {
        if (AppState.ActiveCase is null)
        {
            _caseFolder = null;
            _repository = null;
            _items.Clear();
            _caseStatusLabel.Text = "No active case. Open or create a case first.";
            _statsLabel.Text = "";
            ShowListPanel();
            return;
        }

        _caseFolder = AppState.ActiveCase.CaseFolderPath;
        string databasePath = Path.Combine(_caseFolder, "data.db");
        DatabaseInitializer.EnsureCreated(databasePath);
        _repository = new AllowanceSavingsRepository(databasePath);
        _caseStatusLabel.Text = $"Active case folder: {_caseFolder}";
        LoadItems();
        ShowListPanel();
    }

    private void LoadItems()
    {
        _items.Clear();
        if (_repository is null)
        {
            UpdateStats();
            return;
        }

        foreach (var item in _repository.GetAll())
            _items.Add(item);

        UpdateStats();
        ListPageGridHelper.ApplyInactiveRowStyles(_grid, item => item is AllowanceSavingsItem allowanceItem && allowanceItem.IsActive);
        UpdateSelectedFromGrid();
    }

    private void UpdateStats()
    {
        var active = _items.Where(i => i.IsActive).ToList();
        decimal allowance = active.Where(i => i.IsAllowance).Sum(i => i.GetMonthlyEquivalent());
        decimal savings = active.Where(i => i.IsSavings).Sum(i => i.GetMonthlyEquivalent());
        decimal total = allowance + savings;

        _statsLabel.Text = $"Items: {_items.Count}     Active: {active.Count}     Allowance: {allowance:C}     Savings: {savings:C}     Total reserved monthly: {total:C}";
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _items.Count)
            return;

        if (_grid.Columns[e.ColumnIndex].HeaderText == "Monthly $")
        {
            e.Value = _items[e.RowIndex].GetMonthlyEquivalent().ToString("C", CultureInfo.CurrentCulture);
            e.FormattingApplied = true;
        }
    }

    private void UpdateSelectedFromGrid()
    {
        _selectedItem = _grid.CurrentRow?.DataBoundItem as AllowanceSavingsItem;
    }

    private void BeginAddItem()
    {
        _selectedItem = new AllowanceSavingsItem();
        LoadEditFields(_selectedItem);
        _editTitleLabel.Text = "Add Allowance / Savings";
        ShowEditPanel();
    }

    private void BeginEditSelectedItem()
    {
        UpdateSelectedFromGrid();
        if (_selectedItem is null)
        {
            MessageBox.Show("Select an item first.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        LoadEditFields(_selectedItem);
        _editTitleLabel.Text = "Edit Allowance / Savings";
        ShowEditPanel();
    }

    private void LoadEditFields(AllowanceSavingsItem item)
    {
        _nameTextBox.Text = item.ItemName;
        _typeComboBox.SelectedItem = string.IsNullOrWhiteSpace(item.ItemType) ? "Allowance" : item.ItemType;
        _amountTextBox.Text = item.Amount.ToString("0.00", CultureInfo.CurrentCulture);
        _frequencyComboBox.SelectedItem = string.IsNullOrWhiteSpace(item.Frequency) ? "Monthly" : item.Frequency;
        LoadBankAccountsIntoCombo(item.LinkedBankAssetId);
        _whereMethodComboBox.SelectedItem = string.IsNullOrWhiteSpace(item.StorageMethod) ? InferWhereMethod(item) : item.StorageMethod;
        _otherWhereTextBox.Text = item.WhereStored;
        _activeCheckBox.Checked = item.IsActive;
        HandleWhereMethodChanged();
        _notesTextBox.Text = item.Notes;
    }

    private void SaveCurrentEdit()
    {
        if (_repository is null)
        {
            MessageBox.Show("Open or create a case first.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_selectedItem is null)
            _selectedItem = new AllowanceSavingsItem();

        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            MessageBox.Show("Enter a name or purpose for this item.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(_amountTextBox.Text, NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal amount))
        {
            MessageBox.Show("Enter a valid amount.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _selectedItem.ItemName = _nameTextBox.Text.Trim();
        _selectedItem.ItemType = (_typeComboBox.SelectedItem as string) ?? "Allowance";
        _selectedItem.Amount = amount;
        _selectedItem.Frequency = (_frequencyComboBox.SelectedItem as string) ?? "Monthly";
        if (!ApplyWhereSelection(_selectedItem))
            return;
        _selectedItem.IsActive = _activeCheckBox.Checked;
        _selectedItem.Notes = _notesTextBox.Text.Trim();

        int id = _repository.Save(_selectedItem);
        _selectedItem.Id = id;
        LoadItems();
        ShowListPanel();
    }

    private void ShowProfileForSelected()
    {
        UpdateSelectedFromGrid();
        if (_selectedItem is null)
        {
            MessageBox.Show("Select an item first.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        BuildProfileDetails(_selectedItem);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var item = _items.FirstOrDefault(i => i.Id == recordId);
        if (item is null && _repository is not null)
            item = _repository.GetAll().FirstOrDefault(i => i.Id == recordId);

        if (item is null)
            return false;

        _selectedItem = item;
        SelectGridRowByItemId(recordId);
        BuildProfileDetails(item);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByItemId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is AllowanceSavingsItem item && item.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void BuildProfileDetails(AllowanceSavingsItem item)
    {
        _profileTitleLabel.Text = item.ItemName;
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Basic Information");
        AddProfileRow("Name / purpose", item.ItemName);
        AddProfileRow("Type", item.ItemType);
        AddProfileRow("Status", item.IsActive ? "Active" : "Inactive");

        AddProfileSpacer();
        AddProfileSection("Amount / Schedule");
        AddProfileRow("Amount", item.Amount.ToString("C2", CultureInfo.CurrentCulture));
        AddProfileRow("Frequency", item.Frequency);
        AddProfileRow("Monthly equivalent", item.GetMonthlyEquivalent().ToString("C2", CultureInfo.CurrentCulture));
        AddProfileRow("Where / account / envelope", string.IsNullOrWhiteSpace(item.WhereStored) ? "Not specified" : item.WhereStored);
        AddProfileRow("Where method", string.IsNullOrWhiteSpace(item.StorageMethod) ? "Not specified" : item.StorageMethod);
        if (item.LinkedBankAssetId > 0)
            AddProfileRow("Linked bank asset", item.LinkedBankAssetName);

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(item.Notes) ? "None" : item.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", item.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", item.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

        _profileDetailsPanel.ResumeLayout();
        _profileDetailsPanel.Visible = true;
        _profileDetailsPanel.BringToFront();
        UpdateProfileContentWidth();
    }

    private void RemoveSelectedItem()
    {
        if (_repository is null || _selectedItem is null)
            return;

        var result = MessageBox.Show(
            $"Remove '{_selectedItem.ItemName}' from this case?",
            "Remove Allowance / Savings Item",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedItem.Id);
        _selectedItem = null;
        LoadItems();
        ShowListPanel();
    }

    private void SelectItemById(int id)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is AllowanceSavingsItem item && item.Id == id)
            {
                row.Selected = true;
                _grid.CurrentCell = row.Cells[0];
                _selectedItem = item;
                return;
            }
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
        _editPanel.AutoScrollPosition = Point.Empty;
    }


    private static string[] GetWhereMethods()
    {
        return new[] { "Cash / Envelope", "Select Bank Account", "Create Bank Account Now", "Other" };
    }

    private static string InferWhereMethod(AllowanceSavingsItem item)
    {
        if (item.LinkedBankAssetId > 0)
            return "Select Bank Account";
        if (string.IsNullOrWhiteSpace(item.WhereStored))
            return "Cash / Envelope";
        if (item.WhereStored.Equals("Cash / Envelope", StringComparison.OrdinalIgnoreCase) || item.WhereStored.Equals("Cash", StringComparison.OrdinalIgnoreCase))
            return "Cash / Envelope";
        return "Other";
    }

    private void HandleWhereMethodChanged()
    {
        string method = (_whereMethodComboBox.SelectedItem as string) ?? "Cash / Envelope";

        if (method == "Create Bank Account Now")
        {
            // Do not leave this pseudo-action selected if the dialog is canceled.
            CreateBankFromEdit();
            return;
        }

        bool showBank = method == "Select Bank Account";
        bool showOther = method == "Other";

        _linkedBankLabel.Visible = showBank;
        _linkedBankComboBox.Visible = showBank;
        _createBankButton.Visible = false;
        _otherWhereLabel.Visible = showOther;
        _otherWhereTextBox.Visible = showOther;

        RelayoutEditFields();
    }

    private void RelayoutEditFields()
    {
        int y = 58;
        PositionField(_nameLabel, _nameTextBox, ref y);
        PositionField(_typeLabel, _typeComboBox, ref y);
        PositionField(_amountLabel, _amountTextBox, ref y);
        PositionField(_frequencyLabel, _frequencyComboBox, ref y);
        PositionField(_whereMethodLabel, _whereMethodComboBox, ref y);

        if (_linkedBankComboBox.Visible)
            PositionField(_linkedBankLabel, _linkedBankComboBox, ref y);

        if (_createBankButton.Visible)
        {
            _createBankButton.Location = new Point(18, y);
            y += 50;
        }

        if (_otherWhereTextBox.Visible)
            PositionField(_otherWhereLabel, _otherWhereTextBox, ref y);

        _activeCheckBox.Location = new Point(18, y + 4);
        y += 44;

        PositionField(_notesLabel, _notesTextBox, ref y, _notesTextBox.Height + 18);

        if (_saveButton.Parent is Control buttonRow)
            buttonRow.Location = new Point(18, y + 8);
    }

    private static void PositionField(Label label, Control control, ref int y, int spacingAfter = 48)
    {
        label.Location = new Point(18, y);
        control.Location = new Point(18, y + 22);
        y += control.Height + spacingAfter;
    }

    private void LoadBankAccountsIntoCombo(long selectedId = 0)
    {
        _linkedBankComboBox.Items.Clear();
        var banks = LoadBankAssets();
        if (banks.Count == 0)
        {
            _linkedBankComboBox.Items.Add(new BankAssetListItem(0, "No bank accounts yet"));
            _linkedBankComboBox.SelectedIndex = 0;
            return;
        }

        foreach (var bank in banks)
            _linkedBankComboBox.Items.Add(bank);

        int selectedIndex = 0;
        for (int i = 0; i < _linkedBankComboBox.Items.Count; i++)
        {
            if (_linkedBankComboBox.Items[i] is BankAssetListItem item && item.Id == selectedId)
            {
                selectedIndex = i;
                break;
            }
        }
        _linkedBankComboBox.SelectedIndex = selectedIndex;
    }

    private List<BankAssetListItem> LoadBankAssets()
    {
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return new List<BankAssetListItem>();

        try
        {
            string databasePath = Path.Combine(_caseFolder, "data.db");
            return new AssetsRepository(databasePath).GetAll()
                .Where(a => a.IsActive && a.AssetType.Equals("Bank", StringComparison.OrdinalIgnoreCase))
                .Select(a => new BankAssetListItem(a.Id, BuildBankDisplayName(a)))
                .ToList();
        }
        catch
        {
            return new List<BankAssetListItem>();
        }
    }

    private static string BuildBankDisplayName(AssetItem asset)
    {
        string institution = string.IsNullOrWhiteSpace(asset.InstitutionName) ? asset.LocationOrInstitution : asset.InstitutionName;
        string nickname = string.IsNullOrWhiteSpace(asset.AccountNickname) ? asset.AssetName : asset.AccountNickname;
        if (!string.IsNullOrWhiteSpace(institution) && !string.IsNullOrWhiteSpace(nickname) && !nickname.Contains(institution, StringComparison.OrdinalIgnoreCase))
            return $"{asset.AssetName} ({institution} - {nickname})";
        return string.IsNullOrWhiteSpace(asset.AssetName) ? "Bank Account" : asset.AssetName;
    }

    private bool ApplyWhereSelection(AllowanceSavingsItem item)
    {
        string method = (_whereMethodComboBox.SelectedItem as string) ?? "Cash / Envelope";
        item.StorageMethod = method;
        item.LinkedBankAssetId = 0;
        item.LinkedBankAssetName = string.Empty;

        if (method == "Select Bank Account")
        {
            if (_linkedBankComboBox.SelectedItem is not BankAssetListItem bank || bank.Id <= 0)
            {
                MessageBox.Show("Select a bank account, or choose Create Bank Account Now.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            item.LinkedBankAssetId = bank.Id;
            item.LinkedBankAssetName = bank.Name;
            item.WhereStored = bank.Name;
            return true;
        }

        if (method == "Other")
        {
            item.WhereStored = _otherWhereTextBox.Text.Trim();
            return true;
        }

        item.WhereStored = "Cash / Envelope";
        return true;
    }

    private void CreateBankFromEdit()
    {
        long newId = CreateBankAccountDialog();
        if (newId > 0)
        {
            LoadBankAccountsIntoCombo(newId);
            _whereMethodComboBox.SelectedItem = "Select Bank Account";
        }
        else
        {
            _whereMethodComboBox.SelectedItem = "Select Bank Account";
            LoadBankAccountsIntoCombo();
        }
        HandleWhereMethodChanged();
    }

    private long CreateBankAccountDialog()
    {
        if (string.IsNullOrWhiteSpace(_caseFolder))
        {
            MessageBox.Show("Open or create a case first.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }

        using var form = new Form
        {
            Text = "Create Bank Account Asset",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(440, 290),
            BackColor = Back,
            ForeColor = TextPrimary
        };

        var nameLabel = DialogLabel("Asset name", 18, 18);
        var nameBox = DialogTextBox(18, 42, 390);
        var institutionLabel = DialogLabel("Bank / institution", 18, 78);
        var institutionBox = DialogTextBox(18, 102, 390);
        var nicknameLabel = DialogLabel("Account nickname", 18, 138);
        var nicknameBox = DialogTextBox(18, 162, 390);
        var balanceLabel = DialogLabel("Current balance / value (optional)", 18, 198);
        var balanceBox = DialogTextBox(18, 222, 180);
        var ok = CreateButton("Create", 214, 236, 90);
        var cancel = CreateButton("Cancel", 316, 236, 90);

        form.Controls.AddRange(new Control[] { nameLabel, nameBox, institutionLabel, institutionBox, nicknameLabel, nicknameBox, balanceLabel, balanceBox, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;

        long newId = 0;
        ok.Click += (_, _) =>
        {
            string assetName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(assetName))
            {
                MessageBox.Show(form, "Enter a bank account asset name.", "Create Bank Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal.TryParse(balanceBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal balance);
            string databasePath = Path.Combine(_caseFolder, "data.db");
            var asset = new AssetItem
            {
                AssetName = assetName,
                AssetType = "Bank",
                Owner = AppState.ActiveCase?.PrimaryPersonName ?? string.Empty,
                Status = "Active / In Use",
                LocationOrInstitution = institutionBox.Text.Trim(),
                InstitutionName = institutionBox.Text.Trim(),
                AccountNickname = nicknameBox.Text.Trim(),
                CurrentBalanceValue = balance,
                EstimatedValue = balance,
                IsActive = true
            };
            newId = new AssetsRepository(databasePath).Upsert(asset);
            form.DialogResult = DialogResult.OK;
        };

        return form.ShowDialog(this) == DialogResult.OK ? newId : 0;
    }

    private static Label DialogLabel(string text, int x, int y)
    {
        return new Label { Text = text, Location = new Point(x, y), Size = new Size(390, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 9.5f) };
    }

    private static TextBox DialogTextBox(int x, int y, int width)
    {
        return new TextBox { Location = new Point(x, y), Size = new Size(width, 26), BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f) };
    }

    private static string[] GetFrequencies()
    {
        return new[]
        {
            "Weekly",
            "Every 2 weeks",
            "Twice monthly",
            "Monthly",
            "Quarterly",
            "Yearly",
            "One-time / irregular"
        };
    }

    private Button CreateButton(string text, int x, int y, int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 38),
            Margin = new Padding(0, 0, 12, 0),
            BackColor = ButtonBack,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        return button;
    }


    private TextBox CreateRawTextBox(int x, int y, int width)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 26),
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };
    }

    private ComboBox CreateRawComboBox(int x, int y, string[] values, int width)
    {
        var comboBox = new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f)
        };
        comboBox.Items.AddRange(values);
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
        return comboBox;
    }

    private TextBox CreateTextBox(Control parent, string labelText, ref int y, int width)
    {
        var label = CreateFieldLabel(labelText, y);
        parent.Controls.Add(label);
        y += 22;

        var textBox = new TextBox
        {
            Location = new Point(18, y),
            Size = new Size(width, 26),
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };
        parent.Controls.Add(textBox);
        y += 52;
        return textBox;
    }

    private TextBox CreateMultilineTextBox(Control parent, string labelText, ref int y, int width, int height)
    {
        var label = CreateFieldLabel(labelText, y);
        parent.Controls.Add(label);
        y += 22;

        var textBox = new TextBox
        {
            Location = new Point(18, y),
            Size = new Size(width, height),
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        parent.Controls.Add(textBox);
        y += height + 18;
        return textBox;
    }

    private ComboBox CreateComboBox(Control parent, string labelText, ref int y, string[] values, int width)
    {
        var label = CreateFieldLabel(labelText, y);
        parent.Controls.Add(label);
        y += 22;

        var comboBox = new ComboBox
        {
            Location = new Point(18, y),
            Size = new Size(width, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f)
        };
        comboBox.Items.AddRange(values);
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
        parent.Controls.Add(comboBox);
        y += 54;
        return comboBox;
    }

    private static Label CreateFieldLabel(string text, int y)
    {
        return new Label
        {
            Text = text,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(18, y)
        };
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
        return string.IsNullOrWhiteSpace(value) ? "Not specified" : value;
    }

    private void UpdateProfileContentWidth()
    {
        if (_profilePanel is null || _profileDetailsPanel is null)
            return;

        var usableWidth = Math.Max(400, _profilePanel.ClientSize.Width - 24);
        _profileTitleLabel.Width = usableWidth;
        _profileDetailsPanel.Width = usableWidth;

        foreach (Control control in _profileDetailsPanel.Controls)
            control.Width = usableWidth;
    }

    private string GetExportsFolder()
    {
        if (!string.IsNullOrWhiteSpace(_caseFolder))
        {
            string exportsFolder = Path.Combine(_caseFolder, "exports");
            Directory.CreateDirectory(exportsFolder);
            return exportsFolder;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private void ExportCurrentItemPdf()
    {
        if (_selectedItem is null)
            return;

        string safeName = MakeSafeFileName(_selectedItem.ItemName);
        string defaultPath = Path.Combine(GetExportsFolder(), $"{safeName}_allowance_savings_profile.pdf");

        using var dialog = new SaveFileDialog
        {
            Title = "Export allowance / savings profile",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = Path.GetFileName(defaultPath),
            InitialDirectory = Path.GetDirectoryName(defaultPath)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        WriteSimplePdf(dialog.FileName, _selectedItem);
        MessageBox.Show("Export complete.", "Allowance / Savings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();
        foreach (char c in value)
            builder.Append(invalid.Contains(c) ? '_' : c);
        return string.IsNullOrWhiteSpace(builder.ToString()) ? "allowance_savings" : builder.ToString().Trim();
    }

    private static void WriteSimplePdf(string path, AllowanceSavingsItem item)
    {
        var lines = new List<(string Text, bool Bold, int GapAfter)>
        {
            ("Allowance / Savings Profile", true, 18),
            ($"Generated: {DateTime.Now:g}", false, 18),
            (item.ItemName, true, 14),
            ("Allowance / Savings Details", true, 8),
            ($"Type: {item.ItemType}", false, 4),
            ($"Amount: {item.Amount:C}", false, 4),
            ($"Frequency: {item.Frequency}", false, 4),
            ($"Monthly equivalent: {item.GetMonthlyEquivalent():C}", false, 4),
            ($"Where / account / envelope: {(string.IsNullOrWhiteSpace(item.WhereStored) ? "Not specified" : item.WhereStored)}", false, 4),
            ($"Where method: {(string.IsNullOrWhiteSpace(item.StorageMethod) ? "Not specified" : item.StorageMethod)}", false, 4),
            ($"Linked bank asset: {(item.LinkedBankAssetId > 0 ? item.LinkedBankAssetName : "None")}", false, 4),
            ($"Status: {(item.IsActive ? "Active" : "Inactive")}", false, 16),
            ("Notes", true, 8),
            (string.IsNullOrWhiteSpace(item.Notes) ? "None" : item.Notes, false, 4)
        };

        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 11 Tf");
        content.AppendLine("50 760 Td");

        bool first = true;
        foreach (var line in lines)
        {
            foreach (string wrapped in WrapText(line.Text, 92))
            {
                if (!first)
                    content.AppendLine("0 -16 Td");
                content.AppendLine(line.Bold ? "/F2 11 Tf" : "/F1 11 Tf");
                content.AppendLine($"({EscapePdf(wrapped)}) Tj");
                first = false;
            }

            if (line.GapAfter > 0)
                content.AppendLine($"0 -{line.GapAfter} Td");
        }
        content.AppendLine("ET");

        WritePdfDocument(path, content.ToString());
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return string.Empty;
            yield break;
        }

        foreach (string paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            string remaining = paragraph.Trim();
            while (remaining.Length > maxChars)
            {
                int split = remaining.LastIndexOf(' ', Math.Min(maxChars, remaining.Length - 1));
                if (split <= 0)
                    split = maxChars;
                yield return remaining[..split].Trim();
                remaining = remaining[split..].Trim();
            }
            yield return remaining;
        }
    }

    private static string EscapePdf(string text)
    {
        return text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static void WritePdfDocument(string path, string streamText)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(streamText)} >>\nstream\n{streamText}\nendstream"
        };

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(fs, Encoding.ASCII);
        var offsets = new List<long>();

        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(fs.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        long xref = fs.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (long offset in offsets)
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xref);
        writer.WriteLine("%%EOF");
    }
    private sealed class BankAssetListItem
    {
        public BankAssetListItem(long id, string name)
        {
            Id = id;
            Name = name;
        }

        public long Id { get; }
        public string Name { get; }
        public override string ToString() => Name;
    }

}
