using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class AssetsPage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel = Color.FromArgb(15, 34, 56);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Accent = Color.FromArgb(58, 91, 132);

    private const string OtherOption = "Other";
    private const string CostNa = "Not Applicable";
    private const string CostPaidOff = "Paid Off";
    private const string CostSelectBill = "Select Existing Bill";
    private const string CostCreateBill = "Create Bill Now";
    private const string IncomeNone = "No Income";
    private const string IncomeSelect = "Select Existing Income Source";
    private const string IncomeCreate = "Create Income Source Now";

    private readonly BindingList<AssetItem> _assets = new();
    private readonly string[] _assetTypes = { "Vehicle", "Property", "Bank", "Valuable Item", "Other" };
    private readonly Dictionary<string, Control[]> _typeControls = new(StringComparer.OrdinalIgnoreCase);

    private AssetsRepository? _repository;
    private string? _caseFolder;
    private AssetItem? _selectedAsset;
    private string _editingAssetType = "Vehicle";

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addAssetButton = null!;
    private Button _viewProfileButton = null!;
    private Button _editSelectedButton = null!;

    private Panel _typePickerPanel = null!;

    private Panel _profilePanel = null!;
    private Label _profileTitleLabel = null!;
    private FlowLayoutPanel _profileDetailsPanel = null!;
    private Button _profileBackButton = null!;
    private Button _profileEditButton = null!;
    private Button _profileRemoveButton = null!;
    private Button _profileExportButton = null!;

    private Panel _editPanel = null!;
    private FlowLayoutPanel _form = null!;
    private Label _editTitleLabel = null!;
    private Label _assetTypeReadOnlyLabel = null!;
    private TextBox _assetNameTextBox = null!;
    private ComboBox _ownerComboBox = null!;
    private Label _ownerOtherLabel = null!;
    private TextBox _ownerOtherTextBox = null!;
    private TextBox _estimatedValueTextBox = null!;
    private ComboBox _statusComboBox = null!;
    private TextBox _locationTextBox = null!;

    private ComboBox _primaryDriverComboBox = null!;
    private Label _primaryDriverOtherLabel = null!;
    private TextBox _primaryDriverOtherTextBox = null!;
    private TextBox _vehicleYearTextBox = null!;
    private TextBox _vehicleMakeTextBox = null!;
    private TextBox _vehicleModelTextBox = null!;
    private TextBox _vehicleVinTextBox = null!;
    private TextBox _vehiclePlateTextBox = null!;
    private ComboBox _registrationStatusComboBox = null!;
    private TextBox _registrationDueTextBox = null!;
    private TextBox _mileageTextBox = null!;
    private TextBox _mpgTextBox = null!;

    private ComboBox _propertyTypeComboBox = null!;
    private TextBox _propertyAddressTextBox = null!;
    private TextBox _occupantsTextBox = null!;
    private TextBox _hoaTextBox = null!;

    private TextBox _institutionTextBox = null!;
    private TextBox _accountNicknameTextBox = null!;
    private TextBox _currentBalanceTextBox = null!;

    private TextBox _valuableDescriptionTextBox = null!;
    private TextBox _serialTextBox = null!;
    private TextBox _storageLocationTextBox = null!;

    private TextBox _otherDetailsTextBox = null!;

    private ComboBox _costHandlingComboBox = null!;
    private ComboBox _linkedBillComboBox = null!;
    private Label _linkedBillLabel = null!;
    private Label _datePaidOffLabel = null!;
    private TextBox _datePaidOffTextBox = null!;
    private ComboBox _incomeHandlingComboBox = null!;
    private ComboBox _linkedIncomeComboBox = null!;
    private Label _linkedIncomeLabel = null!;

    private CheckBox _activeCheckBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public AssetsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
    }

    public AppPageKey PageKey => AppPageKey.Assets;
    public string PageTitle => "Assets";
    public void OnNavigatedTo() => InitializeForActiveCase();
    public bool CanNavigateAway() => true;

    private void BuildUi()
    {
        Controls.Clear();

        var root = new Panel { Dock = DockStyle.Fill, BackColor = Back, Padding = new Padding(28) };
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "Assets",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });

        var subtitle = new Label
        {
            Text = "Track vehicles, properties, bank accounts, valuable items, and other assets while linking real bills and income records instead of double-counting money.",
            Dock = DockStyle.Top,
            Height = 38,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(subtitle);
        subtitle.BringToFront();

        _caseStatusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 42,
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
        BuildTypePickerPanel();
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

        _addAssetButton = CreateButton("Add Asset", 120);
        _viewProfileButton = CreateButton("View Profile", 120);
        _editSelectedButton = CreateButton("Edit Selected", 125);
        buttonRow.Controls.Add(_addAssetButton);
        buttonRow.Controls.Add(_viewProfileButton);
        buttonRow.Controls.Add(_editSelectedButton);

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
            ScrollBars = ScrollBars.Both,
            BackgroundColor = Panel,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            GridColor = Border,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            DataSource = _assets
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.DefaultCellStyle.BackColor = Panel;
        _grid.DefaultCellStyle.ForeColor = TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = Accent;
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(13, 29, 48);
        _grid.RowTemplate.Height = 28;

        AddFillTextColumn(nameof(AssetItem.AssetName), "Asset", 34);
        AddFillTextColumn(nameof(AssetItem.AssetType), "Type", 16);
        AddFillTextColumn(nameof(AssetItem.Owner), "Owner", 22);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value / Balance", FillWeight = 18, MinimumWidth = 110, SortMode = DataGridViewColumnSortMode.NotSortable });
        AddFillTextColumn(nameof(AssetItem.Status), "Status", 18);

        _grid.CellFormatting += Grid_CellFormatting;
        _grid.SelectionChanged += (_, _) => UpdateSelectedFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowProfileForSelected();
        ListPageGridHelper.AttachRightClickRemove(_grid, UpdateSelectedFromGrid, RemoveSelectedAsset);
        gridHost.Controls.Add(_grid);

        _addAssetButton.Click += (_, _) => ShowTypePickerPanel();
        _viewProfileButton.Click += (_, _) => ShowProfileForSelected();
        _editSelectedButton.Click += (_, _) => BeginEditSelected();
    }

    private void BuildTypePickerPanel()
    {
        _typePickerPanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false };
        _contentHost.Controls.Add(_typePickerPanel);

        var title = new Label
        {
            Text = "Choose Asset Type",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = false,
            Height = 46,
            Dock = DockStyle.Top
        };
        _typePickerPanel.Controls.Add(title);

        var note = new Label
        {
            Text = "Pick the type first so the next page only shows fields that belong to that asset.",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10f),
            AutoSize = false,
            Height = 34,
            Dock = DockStyle.Top
        };
        _typePickerPanel.Controls.Add(note);
        note.BringToFront();

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 16, 0, 0),
            BackColor = Back
        };
        _typePickerPanel.Controls.Add(flow);
        flow.BringToFront();

        foreach (var type in _assetTypes)
        {
            var button = CreateButton($"Add {type}", 220);
            button.Height = 42;
            button.Margin = new Padding(0, 0, 0, 10);
            button.Click += (_, _) => BeginAddAsset(type);
            flow.Controls.Add(button);
        }

        var cancel = CreateButton("Cancel", 100);
        cancel.Margin = new Padding(0, 10, 0, 0);
        cancel.Click += (_, _) => ShowListPanel();
        flow.Controls.Add(cancel);
    }

    private void BuildProfilePanel()
    {
        _profilePanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false };
        _contentHost.Controls.Add(_profilePanel);
        _profilePanel.Resize += (_, _) => UpdateProfileContentWidth();

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back,
            Padding = new Padding(0, 4, 0, 6)
        };
        _profilePanel.Controls.Add(buttonRow);

        _profileBackButton = CreateButton("Back to List", 115);
        _profileEditButton = CreateButton("Edit", 90);
        _profileRemoveButton = CreateButton("Remove", 100);
        _profileExportButton = CreateButton("Export PDF", 120);
        buttonRow.Controls.Add(_profileBackButton);
        buttonRow.Controls.Add(_profileEditButton);
        buttonRow.Controls.Add(_profileRemoveButton);
        buttonRow.Controls.Add(_profileExportButton);

        _profileTitleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 46,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _profilePanel.Controls.Add(_profileTitleLabel);
        _profileTitleLabel.BringToFront();

        _profileDetailsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Back,
            Padding = new Padding(0, 8, 0, 24),
            Margin = new Padding(0)
        };
        _profilePanel.Controls.Add(_profileDetailsPanel);
        _profileDetailsPanel.BringToFront();

        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileEditButton.Click += (_, _) => BeginEditSelected();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedAsset();
        _profileExportButton.Click += (_, _) => ExportCurrentAssetPdf();
    }

    private void BuildEditPanel()
    {
        _editPanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false, Padding = new Padding(0, 0, 18, 0) };
        _contentHost.Controls.Add(_editPanel);

        _form = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Back,
            Padding = new Padding(0, 0, 0, 26),
            Margin = new Padding(0)
        };
        _editPanel.Controls.Add(_form);

        _editTitleLabel = new Label
        {
            Text = "Add Asset",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = false,
            Width = 720,
            Height = 48,
            Margin = new Padding(0, 0, 0, 6)
        };
        _form.Controls.Add(_editTitleLabel);

        _assetTypeReadOnlyLabel = new Label
        {
            Text = "Asset Type: Vehicle",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = false,
            Width = 720,
            Height = 28,
            Margin = new Padding(0, 0, 0, 12)
        };
        _form.Controls.Add(_assetTypeReadOnlyLabel);

        _assetNameTextBox = CreateTextField(_form, "Asset Name");
        _ownerComboBox = CreateComboField(_form, "Owner / Responsible Person", Array.Empty<string>());
        _ownerOtherLabel = CreateFieldLabel("Outside owner / responsible party name");
        _ownerOtherTextBox = CreateTextBox();
        _form.Controls.Add(_ownerOtherLabel);
        _form.Controls.Add(_ownerOtherTextBox);
        _estimatedValueTextBox = CreateTextField(_form, "Estimated Value / Approximate Value");
        _statusComboBox = CreateComboField(_form, "Status", new[] { "Active / In Use", "Owned / No Payment", "Needs Attention", "Sold / Disposed", "Unknown" });
        _locationTextBox = CreateTextField(_form, "Location / Institution / General Location");

        BuildVehicleFields();
        BuildPropertyFields();
        BuildBankFields();
        BuildValuableFields();
        BuildOtherFields();
        BuildLinkedMoneyFields();

        _activeCheckBox = new CheckBox
        {
            Text = "Active asset",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 6, 0, 10)
        };
        _form.Controls.Add(_activeCheckBox);
        _notesTextBox = CreateMultilineField(_form, "Notes", 120);

        var buttonRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Back, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        _form.Controls.Add(buttonRow);

        _saveButton = CreateButton("Save Asset", 120);
        _cancelButton = CreateButton("Cancel", 100);
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_cancelButton);

        _ownerComboBox.SelectedIndexChanged += (_, _) => UpdateOwnerOtherVisibility();
        _primaryDriverComboBox.SelectedIndexChanged += (_, _) => UpdatePrimaryDriverOtherVisibility();
        _costHandlingComboBox.SelectedIndexChanged += (_, _) => HandleCostHandlingChanged();
        _incomeHandlingComboBox.SelectedIndexChanged += (_, _) => HandleIncomeHandlingChanged();
        _saveButton.Click += (_, _) => SaveCurrentAsset();
        _cancelButton.Click += (_, _) => CancelEdit();
    }

    private void BuildVehicleFields()
    {
        var controls = new List<Control>();
        AddSectionLabel("Vehicle Information", controls);
        _vehicleYearTextBox = CreateTextField(_form, "Year", controls);
        _vehicleMakeTextBox = CreateTextField(_form, "Make", controls);
        _vehicleModelTextBox = CreateTextField(_form, "Model", controls);
        _vehicleVinTextBox = CreateTextField(_form, "VIN", controls);
        _vehiclePlateTextBox = CreateTextField(_form, "License Plate", controls);
        _registrationStatusComboBox = CreateComboField(_form, "Registration Status", new[] { "Current", "Expired", "Past Due", "Unknown", "Not Applicable" }, controls);
        _registrationDueTextBox = CreateTextField(_form, "Registration Due / Expiration Date", controls);
        _mileageTextBox = CreateTextField(_form, "Current Mileage", controls);
        _mpgTextBox = CreateTextField(_form, "Estimated MPG", controls);
        _primaryDriverComboBox = CreateComboField(_form, "Primary Driver", Array.Empty<string>(), controls);
        _primaryDriverOtherLabel = CreateFieldLabel("Outside / other driver name");
        _primaryDriverOtherTextBox = CreateTextBox();
        _form.Controls.Add(_primaryDriverOtherLabel);
        _form.Controls.Add(_primaryDriverOtherTextBox);
        controls.Add(_primaryDriverOtherLabel);
        controls.Add(_primaryDriverOtherTextBox);
        _typeControls["Vehicle"] = controls.ToArray();
    }

    private void BuildPropertyFields()
    {
        var controls = new List<Control>();
        AddSectionLabel("Property Information", controls);
        _propertyTypeComboBox = CreateComboField(_form, "Property Type", new[] { "Primary Residence", "Rental Property", "Land", "Vacation / Secondary Property", "Other" }, controls);
        _propertyAddressTextBox = CreateMultilineField(_form, "Property Address", 70, controls);
        _occupantsTextBox = CreateMultilineField(_form, "Occupants / Who Uses It", 70, controls);
        _hoaTextBox = CreateTextField(_form, "HOA / Property Management / Notes", controls);
        _typeControls["Property"] = controls.ToArray();
    }

    private void BuildBankFields()
    {
        var controls = new List<Control>();
        AddSectionLabel("Bank / Cash Account Information", controls);
        _institutionTextBox = CreateTextField(_form, "Bank / Institution Name", controls);
        _accountNicknameTextBox = CreateTextField(_form, "Account Nickname / Purpose", controls);
        _currentBalanceTextBox = CreateTextField(_form, "Current Balance / Amount", controls);
        AddInlineNote(_form, "Do not store full bank account numbers here. Use a nickname or last-four note only if needed.", controls);
        _typeControls["Bank"] = controls.ToArray();
    }

    private void BuildValuableFields()
    {
        var controls = new List<Control>();
        AddSectionLabel("Valuable Item Information", controls);
        _valuableDescriptionTextBox = CreateMultilineField(_form, "Description", 80, controls);
        _serialTextBox = CreateTextField(_form, "Serial Number / Identifier", controls);
        _storageLocationTextBox = CreateTextField(_form, "Storage Location", controls);
        _typeControls["Valuable Item"] = controls.ToArray();
    }

    private void BuildOtherFields()
    {
        var controls = new List<Control>();
        AddSectionLabel("Other Asset Information", controls);
        _otherDetailsTextBox = CreateMultilineField(_form, "Details", 100, controls);
        _typeControls["Other"] = controls.ToArray();
    }

    private void BuildLinkedMoneyFields()
    {
        AddInlineNote(_form, "Use the dropdowns below to link this asset to real Bills / Spending or Income Sources records. These links do not add extra money by themselves, which prevents double-counting.");

        _costHandlingComboBox = CreateComboField(_form, "Related Bill / Expense", new[] { CostNa, CostPaidOff, CostSelectBill, CostCreateBill });
        _linkedBillLabel = CreateFieldLabel("Select Bill / Expense");
        _linkedBillComboBox = CreateComboBox();
        _form.Controls.Add(_linkedBillLabel);
        _form.Controls.Add(_linkedBillComboBox);
        _datePaidOffLabel = CreateFieldLabel("Date Paid Off");
        _datePaidOffTextBox = CreateTextBox();
        _form.Controls.Add(_datePaidOffLabel);
        _form.Controls.Add(_datePaidOffTextBox);

        _incomeHandlingComboBox = CreateComboField(_form, "Related Income Source", new[] { IncomeNone, IncomeSelect, IncomeCreate });
        _linkedIncomeLabel = CreateFieldLabel("Select Income Source");
        _linkedIncomeComboBox = CreateComboBox();
        _form.Controls.Add(_linkedIncomeLabel);
        _form.Controls.Add(_linkedIncomeComboBox);
    }

    private void InitializeForActiveCase()
    {
        var activeCase = AppState.ActiveCase;
        if (activeCase is null || string.IsNullOrWhiteSpace(activeCase.CaseFolderPath))
        {
            _caseFolder = null;
            _repository = null;
            _assets.Clear();
            _caseStatusLabel.Text = "No active case. Open or create a case first.";
            _statsLabel.Text = string.Empty;
            SetButtonsEnabled(false);
            ShowListPanel();
            return;
        }

        _caseFolder = activeCase.CaseFolderPath;
        var databasePath = Path.Combine(_caseFolder, "data.db");
        DatabaseInitializer.EnsureCreated(databasePath);
        _repository = new AssetsRepository(databasePath);
        _caseStatusLabel.Text = $"Active case: {activeCase.DisplayName}  |  Primary person: {activeCase.PrimaryPersonName}";
        SetButtonsEnabled(true);
        LoadAssets();
    }

    private void LoadAssets()
    {
        _assets.Clear();
        if (_repository is null)
            return;

        foreach (var asset in _repository.GetAll())
            _assets.Add(asset);

        ListPageGridHelper.ApplyInactiveRowStyles(_grid, item => item is AssetItem asset && asset.IsActive);
        UpdateSelectedFromGrid();
        UpdateStats();
    }

    private void UpdateStats()
    {
        var activeAssets = _assets.Count(a => a.IsActive);
        var totalValue = _assets.Where(a => a.IsActive).Sum(a => a.AssetType == "Bank" && a.CurrentBalanceValue > 0 ? a.CurrentBalanceValue : a.EstimatedValue);
        var valueText = totalValue <= 0 ? "Tracked value: unknown" : $"Tracked value: {totalValue:C2}";
        _statsLabel.Text = $"Assets: {_assets.Count}  |  Active: {activeAssets}  |  {valueText}";
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _addAssetButton.Enabled = enabled;
        _viewProfileButton.Enabled = enabled && _selectedAsset is not null;
        _editSelectedButton.Enabled = enabled && _selectedAsset is not null;
    }

    private void UpdateSelectedFromGrid()
    {
        _selectedAsset = _grid.CurrentRow?.DataBoundItem as AssetItem;
        var hasSelection = _selectedAsset is not null;
        _viewProfileButton.Enabled = _repository is not null && hasSelection;
        _editSelectedButton.Enabled = _repository is not null && hasSelection;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (_grid.Columns[e.ColumnIndex].HeaderText == "Value / Balance" && _grid.Rows[e.RowIndex].DataBoundItem is AssetItem asset)
        {
            e.Value = asset.AssetType == "Bank" && asset.CurrentBalanceValue > 0 ? asset.CurrentBalanceDisplay : asset.ValueDisplay;
            e.FormattingApplied = true;
        }
    }

    private void BeginAddAsset(string assetType)
    {
        _editingAssetType = assetType;
        _selectedAsset = new AssetItem { AssetType = assetType, IsActive = true };
        _editTitleLabel.Text = $"Add {assetType}";
        PopulatePeopleDropdown(_ownerComboBox, string.Empty);
        PopulatePeopleDropdown(_primaryDriverComboBox, string.Empty);
        PopulateLinkedDropdowns();
        PopulateEditFields(_selectedAsset);
        ShowEditPanel();
    }

    private void BeginEditSelected()
    {
        if (_selectedAsset is null)
            return;

        _editingAssetType = NormalizeAssetType(_selectedAsset.AssetType);
        _editTitleLabel.Text = $"Edit {_editingAssetType}";
        PopulatePeopleDropdown(_ownerComboBox, _selectedAsset.Owner);
        PopulatePeopleDropdown(_primaryDriverComboBox, _selectedAsset.PrimaryDriver);
        PopulateLinkedDropdowns();
        PopulateEditFields(_selectedAsset);
        ShowEditPanel();
    }

    private void PopulateEditFields(AssetItem asset)
    {
        _editingAssetType = NormalizeAssetType(asset.AssetType);
        _assetTypeReadOnlyLabel.Text = $"Asset Type: {_editingAssetType}";
        _assetNameTextBox.Text = asset.AssetName;
        SetPersonChoice(_ownerComboBox, _ownerOtherTextBox, asset.Owner);
        _estimatedValueTextBox.Text = asset.EstimatedValue > 0 ? asset.EstimatedValue.ToString("0.##") : string.Empty;
        SelectComboValue(_statusComboBox, asset.Status, "Active / In Use");
        _locationTextBox.Text = asset.LocationOrInstitution;

        _vehicleYearTextBox.Text = asset.VehicleYear;
        _vehicleMakeTextBox.Text = asset.VehicleMake;
        _vehicleModelTextBox.Text = asset.VehicleModel;
        _vehicleVinTextBox.Text = asset.VehicleVin;
        _vehiclePlateTextBox.Text = asset.VehiclePlate;
        SelectComboValue(_registrationStatusComboBox, asset.RegistrationStatus, "Unknown");
        _registrationDueTextBox.Text = asset.RegistrationDueDate;
        _mileageTextBox.Text = asset.Mileage > 0 ? asset.Mileage.ToString("0.##") : string.Empty;
        _mpgTextBox.Text = asset.Mpg > 0 ? asset.Mpg.ToString("0.##") : string.Empty;
        SetPersonChoice(_primaryDriverComboBox, _primaryDriverOtherTextBox, asset.PrimaryDriver);

        SelectComboValue(_propertyTypeComboBox, asset.PropertyType, "Primary Residence");
        _propertyAddressTextBox.Text = asset.PropertyAddress;
        _occupantsTextBox.Text = asset.Occupants;
        _hoaTextBox.Text = asset.HoaOrManagement;

        _institutionTextBox.Text = asset.InstitutionName;
        _accountNicknameTextBox.Text = asset.AccountNickname;
        _currentBalanceTextBox.Text = asset.CurrentBalanceValue > 0 ? asset.CurrentBalanceValue.ToString("0.##") : string.Empty;

        _valuableDescriptionTextBox.Text = asset.ValuableDescription;
        _serialTextBox.Text = asset.SerialOrIdentifier;
        _storageLocationTextBox.Text = asset.StorageLocation;

        _otherDetailsTextBox.Text = asset.OtherDetails;

        SelectComboValue(_costHandlingComboBox, asset.RecurringCostHandling, CostNa);
        SelectLinkChoice(_linkedBillComboBox, asset.LinkedBillId);
        _datePaidOffTextBox.Text = asset.DatePaidOff;
        SelectComboValue(_incomeHandlingComboBox, asset.IncomeHandling, IncomeNone);
        SelectLinkChoice(_linkedIncomeComboBox, asset.LinkedIncomeSourceId);

        _activeCheckBox.Checked = asset.IsActive;
        _notesTextBox.Text = asset.Notes;

        UpdateTypeSectionVisibility();
        UpdateOwnerOtherVisibility();
        UpdatePrimaryDriverOtherVisibility();
        UpdateLinkedMoneyVisibility();
    }

    private void SaveCurrentAsset()
    {
        if (_repository is null || _selectedAsset is null)
            return;

        var name = _assetNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter an asset name.", "Missing asset name", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _assetNameTextBox.Focus();
            return;
        }

        if (!TryReadMoney(_estimatedValueTextBox.Text, out var estimatedValue))
        {
            MessageBox.Show("Estimated value must be a valid number, or blank if unknown.", "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _estimatedValueTextBox.Focus();
            return;
        }

        if (!TryReadMoney(_mileageTextBox.Text, out var mileage))
        {
            MessageBox.Show("Mileage must be a valid number, or blank if unknown.", "Invalid mileage", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _mileageTextBox.Focus();
            return;
        }

        if (!TryReadMoney(_mpgTextBox.Text, out var mpg))
        {
            MessageBox.Show("MPG must be a valid number, or blank if unknown.", "Invalid MPG", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _mpgTextBox.Focus();
            return;
        }

        if (!TryReadMoney(_currentBalanceTextBox.Text, out var currentBalance))
        {
            MessageBox.Show("Current balance must be a valid number, or blank if unknown.", "Invalid balance", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _currentBalanceTextBox.Focus();
            return;
        }

        _selectedAsset.AssetName = name;
        _selectedAsset.AssetType = _editingAssetType;
        _selectedAsset.Owner = GetPersonChoiceValue(_ownerComboBox, _ownerOtherTextBox);
        _selectedAsset.EstimatedValue = estimatedValue;
        _selectedAsset.Status = _statusComboBox.SelectedItem?.ToString() ?? "Active / In Use";
        _selectedAsset.LocationOrInstitution = _locationTextBox.Text.Trim();

        _selectedAsset.VehicleYear = _vehicleYearTextBox.Text.Trim();
        _selectedAsset.VehicleMake = _vehicleMakeTextBox.Text.Trim();
        _selectedAsset.VehicleModel = _vehicleModelTextBox.Text.Trim();
        _selectedAsset.VehicleVin = _vehicleVinTextBox.Text.Trim();
        _selectedAsset.VehiclePlate = _vehiclePlateTextBox.Text.Trim();
        _selectedAsset.RegistrationStatus = _registrationStatusComboBox.SelectedItem?.ToString() ?? string.Empty;
        _selectedAsset.RegistrationDueDate = _registrationDueTextBox.Text.Trim();
        _selectedAsset.Mileage = mileage;
        _selectedAsset.Mpg = mpg;
        _selectedAsset.PrimaryDriver = GetPersonChoiceValue(_primaryDriverComboBox, _primaryDriverOtherTextBox);

        _selectedAsset.PropertyType = _propertyTypeComboBox.SelectedItem?.ToString() ?? string.Empty;
        _selectedAsset.PropertyAddress = _propertyAddressTextBox.Text.Trim();
        _selectedAsset.Occupants = _occupantsTextBox.Text.Trim();
        _selectedAsset.HoaOrManagement = _hoaTextBox.Text.Trim();

        _selectedAsset.InstitutionName = _institutionTextBox.Text.Trim();
        _selectedAsset.AccountNickname = _accountNicknameTextBox.Text.Trim();
        _selectedAsset.CurrentBalanceValue = currentBalance;

        _selectedAsset.ValuableDescription = _valuableDescriptionTextBox.Text.Trim();
        _selectedAsset.SerialOrIdentifier = _serialTextBox.Text.Trim();
        _selectedAsset.StorageLocation = _storageLocationTextBox.Text.Trim();
        _selectedAsset.OtherDetails = _otherDetailsTextBox.Text.Trim();

        _selectedAsset.RecurringCostHandling = _costHandlingComboBox.SelectedItem?.ToString() ?? CostNa;
        if (_selectedAsset.RecurringCostHandling == CostSelectBill && _linkedBillComboBox.SelectedItem is LinkChoice billChoice)
        {
            _selectedAsset.LinkedBillId = billChoice.Id;
            _selectedAsset.LinkedBillName = billChoice.Name;
        }
        else
        {
            _selectedAsset.LinkedBillId = 0;
            _selectedAsset.LinkedBillName = string.Empty;
        }
        _selectedAsset.DatePaidOff = _selectedAsset.RecurringCostHandling == CostPaidOff ? _datePaidOffTextBox.Text.Trim() : string.Empty;

        _selectedAsset.IncomeHandling = _incomeHandlingComboBox.SelectedItem?.ToString() ?? IncomeNone;
        if (_selectedAsset.IncomeHandling == IncomeSelect && _linkedIncomeComboBox.SelectedItem is LinkChoice incomeChoice)
        {
            _selectedAsset.LinkedIncomeSourceId = incomeChoice.Id;
            _selectedAsset.LinkedIncomeSourceName = incomeChoice.Name;
        }
        else
        {
            _selectedAsset.LinkedIncomeSourceId = 0;
            _selectedAsset.LinkedIncomeSourceName = string.Empty;
        }

        _selectedAsset.IsActive = _activeCheckBox.Checked;
        _selectedAsset.Notes = _notesTextBox.Text.Trim();

        _repository.Upsert(_selectedAsset);
        LoadAssets();
        ShowListPanel();
    }

    private void CancelEdit()
    {
        if (_selectedAsset is not null && _selectedAsset.Id > 0)
            ShowSelectedProfile();
        else
            ShowListPanel();
    }

    private void ShowProfileForSelected()
    {
        if (_selectedAsset is null)
            return;
        ShowSelectedProfile();
    }

    private void ShowSelectedProfile()
    {
        if (_selectedAsset is null)
            return;

        _profileTitleLabel.Text = _selectedAsset.AssetName;
        PopulateProfileDetails(_selectedAsset);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var asset = _assets.FirstOrDefault(a => a.Id == recordId);
        if (asset is null && _repository is not null)
            asset = _repository.GetAll().FirstOrDefault(a => a.Id == recordId);

        if (asset is null)
            return false;

        _selectedAsset = asset;
        SelectGridRowByAssetId(recordId);
        _profileTitleLabel.Text = asset.AssetName;
        PopulateProfileDetails(asset);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByAssetId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is AssetItem asset && asset.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void RemoveSelectedAsset()
    {
        if (_selectedAsset is null || _repository is null)
            return;

        var result = MessageBox.Show(
            $"Remove {_selectedAsset.AssetName}?\n\nThis removes the asset from this case. It does not delete any linked bills, income sources, or documents.",
            "Remove asset",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedAsset.Id);
        _selectedAsset = null;
        LoadAssets();
        ShowListPanel();
    }

    private void PopulateProfileDetails(AssetItem asset)
    {
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Basic Information");
        AddProfileRow("Asset", asset.AssetName);
        AddProfileRow("Type", asset.AssetType);
        AddProfileRow("Owner / responsible person", asset.Owner);
        AddProfileRow("Estimated value", asset.ValueDisplay);
        AddProfileRow("Status", asset.Status);
        AddProfileRow("Active", YesNo(asset.IsActive));
        AddProfileRow("Location / institution", asset.LocationOrInstitution);

        AddProfileSpacer();
        switch (NormalizeAssetType(asset.AssetType))
        {
            case "Vehicle":
                AddProfileSection("Vehicle Information");
                AddProfileRow("Year", asset.VehicleYear);
                AddProfileRow("Make", asset.VehicleMake);
                AddProfileRow("Model", asset.VehicleModel);
                AddProfileRow("VIN", asset.VehicleVin);
                AddProfileRow("License plate", asset.VehiclePlate);
                AddProfileRow("Registration status", asset.RegistrationStatus);
                AddProfileRow("Registration due", asset.RegistrationDueDate);
                AddProfileRow("Mileage", asset.MileageDisplay);
                AddProfileRow("Estimated MPG", asset.MpgDisplay);
                AddProfileRow("Primary driver", asset.PrimaryDriver);
                break;

            case "Property":
                AddProfileSection("Property Information");
                AddProfileRow("Property type", asset.PropertyType);
                AddProfileRow("Address", asset.PropertyAddress);
                AddProfileRow("Occupants / users", asset.Occupants);
                AddProfileRow("HOA / management", asset.HoaOrManagement);
                break;

            case "Bank":
                AddProfileSection("Bank / Cash Account Information");
                AddProfileRow("Institution", asset.InstitutionName);
                AddProfileRow("Account nickname", asset.AccountNickname);
                AddProfileRow("Current balance", asset.CurrentBalanceDisplay);
                break;

            case "Valuable Item":
                AddProfileSection("Valuable Item Information");
                AddProfileRow("Description", asset.ValuableDescription);
                AddProfileRow("Serial / identifier", asset.SerialOrIdentifier);
                AddProfileRow("Storage location", asset.StorageLocation);
                break;

            default:
                AddProfileSection("Other Asset Information");
                AddProfileParagraph(string.IsNullOrWhiteSpace(asset.OtherDetails) ? "None" : asset.OtherDetails);
                break;
        }

        AddProfileSpacer();
        AddProfileSection("Linked Money Records");
        AddProfileRow("Related bill / expense", GetLinkedBillDisplay(asset));
        AddProfileRow("Related income source", GetLinkedIncomeDisplay(asset));

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(asset.Notes) ? "None" : asset.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", asset.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", asset.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

        _profileDetailsPanel.ResumeLayout();
        UpdateProfileContentWidth();
    }

    private void HandleCostHandlingChanged()
    {
        if (_costHandlingComboBox.SelectedItem?.ToString() == CostCreateBill)
        {
            CreateBillNow();
            return;
        }
        UpdateLinkedMoneyVisibility();
    }

    private void HandleIncomeHandlingChanged()
    {
        if (_incomeHandlingComboBox.SelectedItem?.ToString() == IncomeCreate)
        {
            CreateIncomeNow();
            return;
        }
        UpdateLinkedMoneyVisibility();
    }

    private void CreateBillNow()
    {
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return;

        using var dialog = new QuickBillDialog(GetDefaultPayer());
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            SelectComboValue(_costHandlingComboBox, CostNa, CostNa);
            UpdateLinkedMoneyVisibility();
            return;
        }

        var repo = new BillsRepository(Path.Combine(_caseFolder, "data.db"));
        var id = repo.Upsert(dialog.CreatedBill);
        PopulateBillDropdown(id);
        SelectComboValue(_costHandlingComboBox, CostSelectBill, CostNa);
        SelectLinkChoice(_linkedBillComboBox, id);
        UpdateLinkedMoneyVisibility();
    }

    private void CreateIncomeNow()
    {
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return;

        using var dialog = new QuickIncomeDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            SelectComboValue(_incomeHandlingComboBox, IncomeNone, IncomeNone);
            UpdateLinkedMoneyVisibility();
            return;
        }

        var repo = new IncomeSourcesRepository(Path.Combine(_caseFolder, "data.db"));
        var id = repo.Upsert(dialog.CreatedIncome);
        PopulateIncomeDropdown(id);
        SelectComboValue(_incomeHandlingComboBox, IncomeSelect, IncomeNone);
        SelectLinkChoice(_linkedIncomeComboBox, id);
        UpdateLinkedMoneyVisibility();
    }

    private void UpdateLinkedMoneyVisibility()
    {
        var cost = _costHandlingComboBox.SelectedItem?.ToString() ?? CostNa;
        var showBill = cost == CostSelectBill;
        var showPaidOffDate = cost == CostPaidOff;
        _linkedBillLabel.Visible = showBill;
        _linkedBillComboBox.Visible = showBill;
        _datePaidOffLabel.Visible = showPaidOffDate;
        _datePaidOffTextBox.Visible = showPaidOffDate;

        var income = _incomeHandlingComboBox.SelectedItem?.ToString() ?? IncomeNone;
        var showIncome = income == IncomeSelect;
        _linkedIncomeLabel.Visible = showIncome;
        _linkedIncomeComboBox.Visible = showIncome;
    }

    private void UpdateTypeSectionVisibility()
    {
        foreach (var pair in _typeControls)
        {
            var visible = string.Equals(pair.Key, _editingAssetType, StringComparison.OrdinalIgnoreCase);
            foreach (var control in pair.Value)
                control.Visible = visible;
        }
    }

    private void PopulateLinkedDropdowns()
    {
        PopulateBillDropdown(_selectedAsset?.LinkedBillId ?? 0);
        PopulateIncomeDropdown(_selectedAsset?.LinkedIncomeSourceId ?? 0);
    }

    private void PopulateBillDropdown(long selectedId)
    {
        _linkedBillComboBox.Items.Clear();
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return;

        try
        {
            var repo = new BillsRepository(Path.Combine(_caseFolder, "data.db"));
            foreach (var bill in repo.GetAll())
                _linkedBillComboBox.Items.Add(new LinkChoice(bill.Id, bill.BillName));
        }
        catch { }

        SelectLinkChoice(_linkedBillComboBox, selectedId);
    }

    private void PopulateIncomeDropdown(long selectedId)
    {
        _linkedIncomeComboBox.Items.Clear();
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return;

        try
        {
            var repo = new IncomeSourcesRepository(Path.Combine(_caseFolder, "data.db"));
            foreach (var income in repo.GetAll())
                _linkedIncomeComboBox.Items.Add(new LinkChoice(income.Id, income.SourceName));
        }
        catch { }

        SelectLinkChoice(_linkedIncomeComboBox, selectedId);
    }

    private void SelectLinkChoice(ComboBox comboBox, long id)
    {
        if (comboBox.Items.Count == 0)
        {
            comboBox.SelectedIndex = -1;
            return;
        }

        foreach (var item in comboBox.Items)
        {
            if (item is LinkChoice choice && choice.Id == id)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }

    private void PopulatePeopleDropdown(ComboBox comboBox, string currentValue)
    {
        comboBox.Items.Clear();

        var primary = AppState.ActiveCase?.PrimaryPersonName?.Trim();
        comboBox.Items.Add(!string.IsNullOrWhiteSpace(primary) ? $"Self ({primary})" : "Self");

        if (!string.IsNullOrWhiteSpace(_caseFolder))
        {
            try
            {
                var peopleRepo = new HouseholdPeopleRepository(Path.Combine(_caseFolder, "data.db"));
                foreach (var person in peopleRepo.GetAll())
                {
                    if (string.IsNullOrWhiteSpace(person.FullName))
                        continue;
                    if (comboBox.Items.Cast<object>().Any(i => string.Equals(i.ToString(), person.FullName, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    comboBox.Items.Add(person.FullName);
                }
            }
            catch { }
        }

        comboBox.Items.Add(OtherOption);
        SetPersonChoice(comboBox, comboBox == _ownerComboBox ? _ownerOtherTextBox : _primaryDriverOtherTextBox, currentValue);
    }

    private void SetPersonChoice(ComboBox comboBox, TextBox otherTextBox, string? value)
    {
        value = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            comboBox.SelectedIndex = comboBox.Items.Count > 0 ? 0 : -1;
            otherTextBox.Text = string.Empty;
            return;
        }

        foreach (var item in comboBox.Items)
        {
            var text = item?.ToString() ?? string.Empty;
            if (string.Equals(text, value, StringComparison.OrdinalIgnoreCase) ||
                (text.StartsWith("Self (", StringComparison.OrdinalIgnoreCase) && text.Contains(value, StringComparison.OrdinalIgnoreCase)))
            {
                comboBox.SelectedItem = item;
                otherTextBox.Text = string.Empty;
                return;
            }
        }

        comboBox.SelectedItem = OtherOption;
        otherTextBox.Text = value;
    }

    private string GetPersonChoiceValue(ComboBox comboBox, TextBox otherTextBox)
    {
        var selected = comboBox.SelectedItem?.ToString() ?? string.Empty;
        return string.Equals(selected, OtherOption, StringComparison.OrdinalIgnoreCase) ? otherTextBox.Text.Trim() : selected.Trim();
    }

    private void UpdateOwnerOtherVisibility()
    {
        var showOther = string.Equals(_ownerComboBox.SelectedItem?.ToString(), OtherOption, StringComparison.OrdinalIgnoreCase);
        _ownerOtherLabel.Visible = showOther;
        _ownerOtherTextBox.Visible = showOther;
        _ownerOtherTextBox.Enabled = showOther;
        if (!showOther) _ownerOtherTextBox.Text = string.Empty;
    }

    private void UpdatePrimaryDriverOtherVisibility()
    {
        var showOther = string.Equals(_primaryDriverComboBox.SelectedItem?.ToString(), OtherOption, StringComparison.OrdinalIgnoreCase);
        _primaryDriverOtherLabel.Visible = showOther && _editingAssetType == "Vehicle";
        _primaryDriverOtherTextBox.Visible = showOther && _editingAssetType == "Vehicle";
        _primaryDriverOtherTextBox.Enabled = showOther && _editingAssetType == "Vehicle";
        if (!showOther) _primaryDriverOtherTextBox.Text = string.Empty;
    }

    private string GetDefaultPayer()
    {
        var owner = GetPersonChoiceValue(_ownerComboBox, _ownerOtherTextBox);
        return string.IsNullOrWhiteSpace(owner) ? (AppState.ActiveCase?.PrimaryPersonName ?? string.Empty) : owner;
    }

    private void ExportCurrentAssetPdf()
    {
        if (_selectedAsset is null || _selectedAsset.Id <= 0)
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Export Asset Profile PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(_selectedAsset.AssetName) + "_Asset_Profile.pdf",
            InitialDirectory = GetDefaultExportFolder(),
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WriteAssetProfile(dialog.FileName, _selectedAsset);
            MessageBox.Show("Asset profile PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        catch { return fallbackFolder; }
    }

    private void ShowListPanel()
    {
        _listPanel.Visible = true;
        _typePickerPanel.Visible = false;
        _profilePanel.Visible = false;
        _editPanel.Visible = false;
        _listPanel.BringToFront();
    }

    private void ShowTypePickerPanel()
    {
        _listPanel.Visible = false;
        _typePickerPanel.Visible = true;
        _profilePanel.Visible = false;
        _editPanel.Visible = false;
        _typePickerPanel.BringToFront();
        _typePickerPanel.AutoScrollPosition = Point.Empty;
    }

    private void ShowProfilePanel()
    {
        _listPanel.Visible = false;
        _typePickerPanel.Visible = false;
        _profilePanel.Visible = true;
        _editPanel.Visible = false;
        _profilePanel.BringToFront();
        _profilePanel.AutoScrollPosition = Point.Empty;
        UpdateProfileContentWidth();
    }

    private void ShowEditPanel()
    {
        _listPanel.Visible = false;
        _typePickerPanel.Visible = false;
        _profilePanel.Visible = false;
        _editPanel.Visible = true;
        _editPanel.BringToFront();
        _editPanel.AutoScrollPosition = Point.Empty;
    }

    private TextBox CreateTextField(FlowLayoutPanel form, string label, List<Control>? collector = null)
    {
        var labelControl = CreateFieldLabel(label);
        form.Controls.Add(labelControl);
        collector?.Add(labelControl);
        var textBox = CreateTextBox();
        form.Controls.Add(textBox);
        collector?.Add(textBox);
        return textBox;
    }

    private TextBox CreateMultilineField(FlowLayoutPanel form, string label, int height, List<Control>? collector = null)
    {
        var labelControl = CreateFieldLabel(label);
        form.Controls.Add(labelControl);
        collector?.Add(labelControl);
        var textBox = CreateTextBox();
        textBox.Multiline = true;
        textBox.Height = height;
        textBox.ScrollBars = ScrollBars.Vertical;
        form.Controls.Add(textBox);
        collector?.Add(textBox);
        return textBox;
    }

    private ComboBox CreateComboField(FlowLayoutPanel form, string label, IEnumerable<string> values, List<Control>? collector = null)
    {
        var labelControl = CreateFieldLabel(label);
        form.Controls.Add(labelControl);
        collector?.Add(labelControl);
        var comboBox = CreateComboBox();
        foreach (var value in values)
            comboBox.Items.Add(value);
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
        form.Controls.Add(comboBox);
        collector?.Add(comboBox);
        return comboBox;
    }

    private static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            Width = 520,
            Height = 32,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(235, 239, 245),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 10f),
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = false,
            Width = 520,
            Height = 22,
            Margin = new Padding(0, 4, 0, 2)
        };
    }

    private static TextBox CreateTextBox()
    {
        return new TextBox
        {
            Width = 520,
            Height = 32,
            BackColor = Color.FromArgb(235, 239, 245),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    private void AddSectionLabel(string text, List<Control> collector)
    {
        var label = new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = false,
            Width = 720,
            Height = 34,
            Margin = new Padding(0, 12, 0, 6)
        };
        _form.Controls.Add(label);
        collector.Add(label);
    }

    private void AddInlineNote(FlowLayoutPanel form, string text, List<Control>? collector = null)
    {
        var label = new Label
        {
            Text = text,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = false,
            Width = 700,
            Height = 44,
            Margin = new Padding(0, 8, 0, 8)
        };
        form.Controls.Add(label);
        collector?.Add(label);
    }

    private Button CreateButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Size = new Size(width, 38),
            BackColor = Panel2,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
    }

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
            Font = new Font("Segoe UI", 10.5f),
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
        using var font = new Font("Segoe UI", 10.5f);
        var measured = TextRenderer.MeasureText(text, font, new Size(Math.Max(300, width - 8), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        _profileDetailsPanel.Controls.Add(new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10.5f),
            AutoSize = false,
            Width = width,
            Height = Math.Max(46, measured.Height + 12),
            Margin = new Padding(0, 0, 0, 2)
        });
    }

    private void AddProfileSpacer()
    {
        _profileDetailsPanel.Controls.Add(new Label { Text = string.Empty, AutoSize = false, Width = 1, Height = 14, Margin = new Padding(0) });
    }

    private static void SelectComboValue(ComboBox comboBox, string? value, string fallback)
    {
        var desired = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        foreach (var item in comboBox.Items)
        {
            if (string.Equals(item?.ToString(), desired, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
    }

    private static bool TryReadMoney(string text, out decimal value)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0m;
            return true;
        }
        text = text.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private static string NormalizeAssetType(string? assetType)
    {
        var value = assetType?.Trim() ?? string.Empty;
        return value switch
        {
            "Property / Real Estate" => "Property",
            "Bank / Cash Account" => "Bank",
            "Vehicle" => "Vehicle",
            "Property" => "Property",
            "Bank" => "Bank",
            "Valuable Item" => "Valuable Item",
            _ => "Other"
        };
    }

    private static string GetLinkedBillDisplay(AssetItem asset)
    {
        return asset.RecurringCostHandling switch
        {
            CostPaidOff => string.IsNullOrWhiteSpace(asset.DatePaidOff) ? "Paid off" : $"Paid off on {asset.DatePaidOff}",
            CostSelectBill => string.IsNullOrWhiteSpace(asset.LinkedBillName) ? "Selected bill not found" : asset.LinkedBillName,
            _ => asset.RecurringCostHandling
        };
    }

    private static string GetLinkedIncomeDisplay(AssetItem asset)
    {
        return asset.IncomeHandling == IncomeSelect
            ? (string.IsNullOrWhiteSpace(asset.LinkedIncomeSourceName) ? "Selected income source not found" : asset.LinkedIncomeSourceName)
            : asset.IncomeHandling;
    }

    private static string CleanProfileValue(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string YesNo(bool value) => value ? "Yes" : "No";

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Asset" : safe.Trim();
    }

    private sealed record LinkChoice(long Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class QuickBillDialog : Form
    {
        private readonly TextBox _name = new();
        private readonly ComboBox _category = new();
        private readonly TextBox _amount = new();
        private readonly ComboBox _frequency = new();
        private readonly TextBox _dueDate = new();
        private readonly CheckBox _active = new();
        private readonly string _defaultPayer;
        public Bill CreatedBill { get; private set; } = new();

        public QuickBillDialog(string defaultPayer)
        {
            _defaultPayer = defaultPayer;
            Text = "Create Bill / Expense";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(520, 430);
            BackColor = Back;
            ForeColor = TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Build();
        }

        private void Build()
        {
            var form = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(22), BackColor = Back };
            Controls.Add(form);
            AddText(form, "Bill Name", _name);
            AddCombo(form, "Category", _category, new[] { "Vehicle", "Property", "Insurance", "Debt Payment", "Utilities", "Other" });
            AddText(form, "Amount", _amount);
            AddCombo(form, "Frequency", _frequency, new[] { "Weekly", "Every 2 weeks", "Twice monthly", "Monthly", "Quarterly", "Yearly", "One-time / irregular" });
            AddText(form, "Due Date", _dueDate);
            _active.Text = "Active bill / expense";
            _active.Checked = true;
            _active.ForeColor = TextPrimary;
            _active.AutoSize = true;
            form.Controls.Add(_active);
            var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = Back, Margin = new Padding(0, 12, 0, 0) };
            form.Controls.Add(row);
            var save = CreateDialogButton("Create", 100);
            var cancel = CreateDialogButton("Cancel", 100);
            row.Controls.Add(save);
            row.Controls.Add(cancel);
            save.Click += (_, _) => Save();
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show(this, "Enter a bill name.", "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!TryReadMoney(_amount.Text, out var amount))
            {
                MessageBox.Show(this, "Amount must be a valid number.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CreatedBill = new Bill
            {
                BillName = _name.Text.Trim(),
                Category = _category.SelectedItem?.ToString() ?? "Other",
                Amount = amount,
                Frequency = _frequency.SelectedItem?.ToString() ?? "Monthly",
                DueDate = _dueDate.Text.Trim(),
                PaidBy = _defaultPayer,
                ResponsibilityOwner = _defaultPayer,
                Priority = "Normal",
                IsActive = _active.Checked
            };
            DialogResult = DialogResult.OK;
        }
    }

    private sealed class QuickIncomeDialog : Form
    {
        private readonly TextBox _name = new();
        private readonly ComboBox _type = new();
        private readonly TextBox _amount = new();
        private readonly CheckBox _taxes = new();
        private readonly ComboBox _frequency = new();
        private readonly TextBox _expected = new();
        private readonly TextBox _deposit = new();
        private readonly CheckBox _active = new();
        public IncomeSource CreatedIncome { get; private set; } = new();

        public QuickIncomeDialog()
        {
            Text = "Create Income Source";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(520, 500);
            BackColor = Back;
            ForeColor = TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Build();
        }

        private void Build()
        {
            var form = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(22), BackColor = Back };
            Controls.Add(form);
            AddText(form, "Source Name", _name);
            AddCombo(form, "Income Type", _type, new[] { "Social Security", "Pension", "Survivor Benefits", "Disability", "Employment / Wages", "Family Contribution", "Rental Income", "Retirement Account", "Settlement / Lump Sum", "Other" });
            AddText(form, "Gross Pay / Payment Amount", _amount);
            _taxes.Text = "Taxes withheld";
            _taxes.ForeColor = TextPrimary;
            _taxes.AutoSize = true;
            form.Controls.Add(_taxes);
            AddCombo(form, "Frequency", _frequency, new[] { "Weekly", "Every 2 weeks", "Twice monthly", "Monthly", "Quarterly", "Yearly", "One-time / irregular" });
            AddText(form, "Expected Day / Date", _expected);
            AddText(form, "Deposited To Account", _deposit);
            _active.Text = "Active income source";
            _active.Checked = true;
            _active.ForeColor = TextPrimary;
            _active.AutoSize = true;
            form.Controls.Add(_active);
            var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = Back, Margin = new Padding(0, 12, 0, 0) };
            form.Controls.Add(row);
            var save = CreateDialogButton("Create", 100);
            var cancel = CreateDialogButton("Cancel", 100);
            row.Controls.Add(save);
            row.Controls.Add(cancel);
            save.Click += (_, _) => Save();
            cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_name.Text))
            {
                MessageBox.Show(this, "Enter an income source name.", "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!TryReadMoney(_amount.Text, out var amount))
            {
                MessageBox.Show(this, "Amount must be a valid number.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            CreatedIncome = new IncomeSource
            {
                SourceName = _name.Text.Trim(),
                IncomeType = _type.SelectedItem?.ToString() ?? "Other",
                Amount = amount,
                TaxesWithheld = _taxes.Checked,
                Frequency = _frequency.SelectedItem?.ToString() ?? "Monthly",
                ExpectedDayOrDate = _expected.Text.Trim(),
                DepositedToAccount = _deposit.Text.Trim(),
                IsActive = _active.Checked
            };
            DialogResult = DialogResult.OK;
        }
    }

    private static void AddText(FlowLayoutPanel form, string label, TextBox textBox)
    {
        form.Controls.Add(new Label { Text = label, ForeColor = TextPrimary, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Width = 420, Height = 22 });
        textBox.Width = 420;
        textBox.Height = 30;
        textBox.BackColor = Color.FromArgb(235, 239, 245);
        textBox.ForeColor = Color.Black;
        textBox.Font = new Font("Segoe UI", 10f);
        textBox.Margin = new Padding(0, 0, 0, 8);
        form.Controls.Add(textBox);
    }

    private static void AddCombo(FlowLayoutPanel form, string label, ComboBox combo, IEnumerable<string> values)
    {
        form.Controls.Add(new Label { Text = label, ForeColor = TextPrimary, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Width = 420, Height = 22 });
        combo.Width = 420;
        combo.Height = 30;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = Color.FromArgb(235, 239, 245);
        combo.ForeColor = Color.Black;
        combo.Font = new Font("Segoe UI", 10f);
        combo.Margin = new Padding(0, 0, 0, 8);
        foreach (var value in values)
            combo.Items.Add(value);
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
        form.Controls.Add(combo);
    }

    private static Button CreateDialogButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Size = new Size(width, 36),
            BackColor = Panel2,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
    }

    private static class MinimalPdfWriter
    {
        public static void WriteAssetProfile(string path, AssetItem asset)
        {
            var lines = new List<PdfLine>
            {
                new(asset.AssetName, "Title"),
                new($"Exported: {DateTime.Now:yyyy-MM-dd h:mm tt}", "Muted", 8),
                new("Basic Information", "Section"),
                Row("Asset", asset.AssetName),
                Row("Type", asset.AssetType),
                Row("Owner / responsible person", asset.Owner),
                Row("Estimated value", asset.ValueDisplay),
                Row("Status", asset.Status),
                Row("Active", YesNo(asset.IsActive)),
                Row("Location / institution", asset.LocationOrInstitution)
            };

            switch (NormalizeAssetType(asset.AssetType))
            {
                case "Vehicle":
                    lines.Add(new("Vehicle Information", "Section"));
                    lines.Add(Row("Year", asset.VehicleYear));
                    lines.Add(Row("Make", asset.VehicleMake));
                    lines.Add(Row("Model", asset.VehicleModel));
                    lines.Add(Row("VIN", asset.VehicleVin));
                    lines.Add(Row("License plate", asset.VehiclePlate));
                    lines.Add(Row("Registration status", asset.RegistrationStatus));
                    lines.Add(Row("Registration due", asset.RegistrationDueDate));
                    lines.Add(Row("Mileage", asset.MileageDisplay));
                    lines.Add(Row("Estimated MPG", asset.MpgDisplay));
                    lines.Add(Row("Primary driver", asset.PrimaryDriver));
                    break;
                case "Property":
                    lines.Add(new("Property Information", "Section"));
                    lines.Add(Row("Property type", asset.PropertyType));
                    lines.Add(Row("Address", asset.PropertyAddress));
                    lines.Add(Row("Occupants", asset.Occupants));
                    lines.Add(Row("HOA / management", asset.HoaOrManagement));
                    break;
                case "Bank":
                    lines.Add(new("Bank / Cash Account Information", "Section"));
                    lines.Add(Row("Institution", asset.InstitutionName));
                    lines.Add(Row("Account nickname", asset.AccountNickname));
                    lines.Add(Row("Current balance", asset.CurrentBalanceDisplay));
                    break;
                case "Valuable Item":
                    lines.Add(new("Valuable Item Information", "Section"));
                    lines.Add(Row("Description", asset.ValuableDescription));
                    lines.Add(Row("Serial / identifier", asset.SerialOrIdentifier));
                    lines.Add(Row("Storage location", asset.StorageLocation));
                    break;
                default:
                    lines.Add(new("Other Asset Information", "Section"));
                    lines.Add(new(string.IsNullOrWhiteSpace(asset.OtherDetails) ? "None" : asset.OtherDetails, "Paragraph", 4));
                    break;
            }

            lines.Add(new("Linked Money Records", "Section"));
            lines.Add(Row("Related bill / expense", GetLinkedBillDisplay(asset)));
            lines.Add(Row("Related income source", GetLinkedIncomeDisplay(asset)));
            lines.Add(new("Notes", "Section"));
            lines.Add(new(string.IsNullOrWhiteSpace(asset.Notes) ? "None" : asset.Notes, "Paragraph", 4));
            lines.Add(new("Record Info", "Section"));
            lines.Add(Row("Created UTC", asset.CreatedUtc.ToString("yyyy-MM-dd HH:mm")));
            lines.Add(Row("Updated UTC", asset.UpdatedUtc.ToString("yyyy-MM-dd HH:mm")));

            WritePdf(path, BuildPdfContent(lines, "Asset Profile"));
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
                if (y < 58) break;
                switch (line.Style)
                {
                    case "Title": DrawText(builder, line.Text, 42, y, "F2", 20, "0 0 0"); y -= 30 + line.ExtraGapAfter; break;
                    case "Section": y -= 10; builder.AppendLine("0.72 0.72 0.72 rg"); builder.AppendLine($"42 {y - 5} 528 1.2 re f"); DrawText(builder, line.Text, 42, y + 6, "F2", 13, "0 0 0"); y -= 24 + line.ExtraGapAfter; break;
                    case "Row":
                        var parts = line.Text.Split(new[] { "||" }, 2, StringSplitOptions.None);
                        DrawText(builder, (parts.Length > 0 ? parts[0] : string.Empty) + ":", 42, y, "F2", 10.5, "0 0 0");
                        foreach (var wrapped in Wrap(parts.Length > 1 ? parts[1] : string.Empty, 62)) { DrawText(builder, wrapped, 220, y, "F1", 10.5, "0 0 0"); y -= 15; if (y < 58) break; }
                        y -= 3 + line.ExtraGapAfter;
                        break;
                    case "Paragraph": foreach (var wrapped in Wrap(line.Text, 92)) { DrawText(builder, wrapped, 42, y, "F1", 10.5, "0 0 0"); y -= 15; if (y < 58) break; } y -= 4 + line.ExtraGapAfter; break;
                    case "Muted": DrawText(builder, line.Text, 42, y, "F1", 9, "0.35 0.35 0.35"); y -= 14 + line.ExtraGapAfter; break;
                }
            }
            return builder.ToString();
        }

        private static void DrawText(StringBuilder builder, string text, int x, int y, string fontKey, double size, string rgb)
        {
            builder.AppendLine("BT"); builder.AppendLine($"{rgb} rg"); builder.AppendLine($"/{fontKey} {size.ToString("0.###", CultureInfo.InvariantCulture)} Tf"); builder.AppendLine($"{x} {y} Td"); builder.Append('(').Append(EscapePdfText(text)).AppendLine(") Tj"); builder.AppendLine("ET");
        }

        private static IEnumerable<string> Wrap(string? value, int maxChars)
        {
            value = string.IsNullOrWhiteSpace(value) ? "None" : value.Trim().Replace("\r", string.Empty);
            foreach (var paragraph in value.Split('\n'))
            {
                var remaining = paragraph.Trim();
                if (remaining.Length == 0) { yield return string.Empty; continue; }
                while (remaining.Length > maxChars)
                {
                    var cut = remaining.LastIndexOf(' ', Math.Min(maxChars, remaining.Length - 1));
                    if (cut < 24) cut = Math.Min(maxChars, remaining.Length);
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
            for (var i = 0; i < objects.Count; i++) { writer.Flush(); offsets.Add(stream.Position); writer.WriteLine($"{i + 1} 0 obj"); writer.WriteLine(objects[i]); writer.WriteLine("endobj"); }
            writer.Flush();
            var xrefOffset = stream.Position;
            writer.WriteLine("xref"); writer.WriteLine($"0 {objects.Count + 1}"); writer.WriteLine("0000000000 65535 f ");
            for (var i = 1; i < offsets.Count; i++) writer.WriteLine($"{offsets[i]:0000000000} 00000 n ");
            writer.WriteLine("trailer"); writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>"); writer.WriteLine("startxref"); writer.WriteLine(xrefOffset); writer.WriteLine("%%EOF");
        }

        private static string EscapePdfText(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
