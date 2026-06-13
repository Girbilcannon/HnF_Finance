using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class PeoplePage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel = Color.FromArgb(16, 34, 55);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Accent = Color.FromArgb(52, 82, 120);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Warning = Color.FromArgb(255, 205, 70);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);

    private readonly BindingList<HouseholdPerson> _people = new();

    private HouseholdPeopleRepository? _repository;
    private string? _caseFolder;
    private string? _databasePath;
    private HouseholdPerson? _selectedPerson;

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addPersonButton = null!;
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
    private TextBox _fullNameTextBox = null!;
    private TextBox _relationshipTextBox = null!;
    private TextBox _roleTextBox = null!;
    private CheckBox _livesInHouseholdCheckBox = null!;
    private CheckBox _paysRentCheckBox = null!;
    private ComboBox _contributionModeComboBox = null!;
    private ComboBox _linkedIncomeSourceComboBox = null!;
    private Label _linkedIncomeSourceLabel = null!;
    private Button _createIncomeSourceButton = null!;
    private IReadOnlyList<IncomeSource> _incomeSources = Array.Empty<IncomeSource>();
    private string _lastStableContributionMode = "No Contribution";
    private CheckBox _usesVehicleCheckBox = null!;
    private CheckBox _receivesRidesCheckBox = null!;
    private Label _notesLabel = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Label _editTitleLabel = null!;

    public AppPageKey PageKey => AppPageKey.People;
    public string PageTitle => "People / Household";

    public PeoplePage()
    {
        InitializeLayout();
        WireEvents();
        Load += (_, _) =>
        {
            UpdateContentBounds();
            LoadCaseAndPeople();
        };
        Resize += (_, _) => UpdateContentBounds();
    }

    public void OnNavigatedTo()
    {
        LoadCaseAndPeople();
    }

    public bool CanNavigateAway() => true;

    private void InitializeLayout()
    {
        BackColor = Back;
        ForeColor = TextPrimary;
        Dock = DockStyle.Fill;
        Padding = new Padding(28, 26, 28, 22);

        var title = new Label
        {
            Text = "People / Household",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 24)
        };

        var subtitle = new Label
        {
            Text = "Track household members, helpers, drivers, trustees, attorneys, and people creating financial load.",
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(30, 66)
        };

        _caseStatusLabel = new Label
        {
            Text = "No case loaded yet.",
            ForeColor = Warning,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(30, 98)
        };

        _statsLabel = new Label
        {
            Text = "Household members: 0   Contributors: 0   Monthly contribution: $0.00",
            ForeColor = Good,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(30, 124)
        };

        _contentHost = new Panel
        {
            Location = new Point(30, 158),
            Size = new Size(860, 330),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            BackColor = Back
        };

        BuildListPanel();
        BuildProfilePanel();
        BuildEditPanel();

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(_caseStatusLabel);
        Controls.Add(_statsLabel);
        Controls.Add(_contentHost);

        ShowListPanel();
    }

    private void UpdateContentBounds()
    {
        if (_contentHost is null)
            return;

        const int left = 30;
        const int top = 158;
        const int rightPadding = 28;
        const int bottomPadding = 22;

        int width = Math.Max(620, ClientSize.Width - left - rightPadding);
        int height = Math.Max(320, ClientSize.Height - top - bottomPadding);

        _contentHost.Location = new Point(left, top);
        _contentHost.Size = new Size(width, height);
    }

    private void BuildListPanel()
    {
        _listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            Padding = new Padding(0, 0, 0, 0)
        };

        var listLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            ColumnCount = 1,
            RowCount = 3,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 270f));
        listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
        listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        listLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            BackgroundColor = Panel,
            BorderStyle = BorderStyle.FixedSingle,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            EnableHeadersVisualStyles = false,
            GridColor = Border,
            ForeColor = TextPrimary,
            ScrollBars = ScrollBars.Both
        };

        _grid.ColumnHeadersDefaultCellStyle.BackColor = Panel2;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _grid.DefaultCellStyle.BackColor = Panel;
        _grid.DefaultCellStyle.ForeColor = TextPrimary;
        _grid.DefaultCellStyle.SelectionBackColor = Accent;
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(13, 29, 48);
        _grid.DataSource = _people;

        AddFillTextColumn("FullName", "Name", 36f);
        AddFillTextColumn("Relationship", "Relationship", 28f);
        AddFillTextColumn("Role", "Role / Purpose", 36f);

        var buttonRow = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _addPersonButton = CreateButton("Add Person", 0, 8, 150);
        _viewProfileButton = CreateButton("View Profile", 164, 8, 150);
        _editSelectedButton = CreateButton("Edit Selected", 328, 8, 150);

        buttonRow.Controls.Add(_addPersonButton);
        buttonRow.Controls.Add(_viewProfileButton);
        buttonRow.Controls.Add(_editSelectedButton);

        listLayout.Controls.Add(_grid, 0, 0);
        listLayout.Controls.Add(buttonRow, 0, 1);

        _listPanel.Controls.Add(listLayout);
        _contentHost.Controls.Add(_listPanel);
    }

    private void BuildProfilePanel()
    {
        _profilePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(0),
            AutoScroll = true,
            Visible = false
        };

        _profileTitleLabel = new Label
        {
            Text = "Person profile",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(0, 0)
        };

        _profileBackButton = CreateButton("Back to List", 0, 52, 130);
        _profileEditButton = CreateButton("Edit", 144, 52, 110);
        _profileRemoveButton = CreateButton("Remove", 268, 52, 110);
        _profileRemoveButton.ForeColor = Danger;
        _profileExportButton = CreateButton("Export PDF", 392, 52, 130);

        _profileDetailsPanel = new FlowLayoutPanel
        {
            Location = new Point(0, 108),
            Width = 820,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Back,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, 0, 0, 24),
            Margin = new Padding(0)
        };

        _profilePanel.Controls.Add(_profileTitleLabel);
        _profilePanel.Controls.Add(_profileBackButton);
        _profilePanel.Controls.Add(_profileEditButton);
        _profilePanel.Controls.Add(_profileRemoveButton);
        _profilePanel.Controls.Add(_profileExportButton);
        _profilePanel.Controls.Add(_profileDetailsPanel);
        _profilePanel.Resize += (_, _) => UpdateProfileContentWidth();
        _contentHost.Controls.Add(_profilePanel);
    }

    private void BuildEditPanel()
    {
        _editPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Back,
            BorderStyle = BorderStyle.None,
            AutoScroll = true,
            Padding = new Padding(0),
            Visible = false
        };

        _editTitleLabel = new Label
        {
            Text = "Add person",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(22, 18)
        };

        _fullNameTextBox = CreateTextBox(_editPanel, "Full name", 74, width: 600);
        _relationshipTextBox = CreateTextBox(_editPanel, "Relationship", 134, width: 600);
        _roleTextBox = CreateTextBox(_editPanel, "Role / purpose", 194, width: 600);

        _livesInHouseholdCheckBox = CreateCheckBox("Lives in household", 256);
        _paysRentCheckBox = CreateCheckBox("Pays rent / contributes", 286);
        _usesVehicleCheckBox = CreateCheckBox("Uses household vehicle", 316);
        _receivesRidesCheckBox = CreateCheckBox("Receives rides / transport help", 346);

        _contributionModeComboBox = CreateComboBox(_editPanel, "Contribution income", 392, width: 360);
        _contributionModeComboBox.Items.AddRange(new object[]
        {
            "No Contribution",
            "Select Existing Income Source",
            "Create Income Source Now"
        });
        _contributionModeComboBox.SelectedItem = "No Contribution";

        _linkedIncomeSourceLabel = CreateLabel("Linked income source", 452);
        _linkedIncomeSourceComboBox = CreateComboBoxNoLabel(22, 474, 520);
        _createIncomeSourceButton = CreateButton("Create Income Source Now", 560, 472, 220);

        var contributionToolTip = new ToolTip
        {
            AutoPopDelay = 12000,
            InitialDelay = 350,
            ReshowDelay = 100,
            ShowAlways = true
        };
        contributionToolTip.SetToolTip(
            _contributionModeComboBox,
            "Link household contributions to an Income Source so the money is counted once and can include frequency, tax clarity, and deposit details.");
        contributionToolTip.SetToolTip(
            _linkedIncomeSourceComboBox,
            "Choose the existing Income Source that represents this person's physical cash transfer to the head of household.");

        _notesLabel = CreateLabel("Notes", 530);
        _notesTextBox = new TextBox
        {
            Location = new Point(22, 552),
            Size = new Size(700, 110),
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        _saveButton = CreateButton("Save Person", 22, 670, 150);
        _cancelButton = CreateButton("Cancel", 186, 670, 120);

        _editPanel.Controls.Add(_editTitleLabel);
        _editPanel.Controls.Add(_livesInHouseholdCheckBox);
        _editPanel.Controls.Add(_paysRentCheckBox);
        _editPanel.Controls.Add(_usesVehicleCheckBox);
        _editPanel.Controls.Add(_receivesRidesCheckBox);
        _editPanel.Controls.Add(_linkedIncomeSourceLabel);
        _editPanel.Controls.Add(_linkedIncomeSourceComboBox);
        _editPanel.Controls.Add(_createIncomeSourceButton);
        _editPanel.Controls.Add(_notesLabel);
        _editPanel.Controls.Add(_notesTextBox);
        _editPanel.Controls.Add(_saveButton);
        _editPanel.Controls.Add(_cancelButton);
        _contentHost.Controls.Add(_editPanel);
    }

    private void WireEvents()
    {
        _grid.SelectionChanged += (_, _) => CaptureSelectedGridPerson();
        _grid.CellDoubleClick += (_, _) => ShowSelectedProfile();
        ListPageGridHelper.AttachRightClickRemove(_grid, CaptureSelectedGridPerson, DeleteCurrentPerson);
        _addPersonButton.Click += (_, _) => StartNewPerson();
        _viewProfileButton.Click += (_, _) => ShowSelectedProfile();
        _editSelectedButton.Click += (_, _) => EditSelectedPerson();
        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileEditButton.Click += (_, _) => EditSelectedPerson();
        _profileRemoveButton.Click += (_, _) => DeleteCurrentPerson();
        _profileExportButton.Click += (_, _) => ExportCurrentPersonPdf();
        _saveButton.Click += (_, _) => SaveCurrentPerson();
        _cancelButton.Click += (_, _) => CancelEdit();
        _contributionModeComboBox.SelectedIndexChanged += (_, _) =>
        {
            EnsureContributionModeOptions();
            var mode = (_contributionModeComboBox.SelectedItem as string) ?? "No Contribution";
            if (mode != "Create Income Source Now")
                _lastStableContributionMode = mode;
            HandleContributionModeChanged();
        };
        _createIncomeSourceButton.Click += (_, _) => CreateContributionIncomeSource();
    }

    private void LoadCaseAndPeople()
    {
        try
        {
            var activeCase = GrannyManager.App.AppState.ActiveCase;
            _caseFolder = activeCase?.CaseFolderPath;

            if (activeCase is null || string.IsNullOrWhiteSpace(_caseFolder))
            {
                _caseStatusLabel.Text = "No active case found. Create or open a case first from Case Setup.";
                _caseStatusLabel.ForeColor = Warning;
                _repository = null;
                _databasePath = null;
                _selectedPerson = null;
                _people.Clear();
                UpdateStats();
                ShowListPanel();
                return;
            }

            _databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
            _repository = new HouseholdPeopleRepository(_databasePath);
            LoadIncomeSources();

            _caseStatusLabel.Text = $"Active case folder: {_caseFolder}";
            _caseStatusLabel.ForeColor = Good;

            ReloadPeople();
        }
        catch (Exception ex)
        {
            _caseStatusLabel.Text = $"Could not load case database: {ex.Message}";
            _caseStatusLabel.ForeColor = Warning;
        }
    }

    private void ReloadPeople()
    {
        var selectedId = _selectedPerson?.Id ?? 0;
        _people.Clear();

        if (_repository is not null)
        {
            foreach (var person in _repository.GetAll())
                _people.Add(person);
        }

        UpdateStats();
        ShowListPanel();

        if (_people.Count > 0)
        {
            var rowToSelect = 0;
            if (selectedId > 0)
            {
                for (var i = 0; i < _grid.Rows.Count; i++)
                {
                    if (_grid.Rows[i].DataBoundItem is HouseholdPerson p && p.Id == selectedId)
                    {
                        rowToSelect = i;
                        break;
                    }
                }
            }

            _grid.Rows[rowToSelect].Selected = true;
            _grid.CurrentCell = _grid.Rows[rowToSelect].Cells[0];
            CaptureSelectedGridPerson();
        }
        else
        {
            _selectedPerson = null;
        }
    }

    private void UpdateStats()
    {
        var householdCount = _people.Count(p => p.LivesInHousehold);
        var contributors = _people.Count(p => p.LinkedIncomeSourceId > 0 || p.PaysRent);
        var linkedIncomeById = _incomeSources.Where(source => source.IsActive).ToDictionary(source => source.Id);
        var monthlyContribution = _people
            .Where(p => p.LinkedIncomeSourceId > 0)
            .Select(p => p.LinkedIncomeSourceId)
            .Distinct()
            .Select(id => linkedIncomeById.TryGetValue(id, out var source) ? source.MonthlyEquivalent : 0m)
            .Sum();
        var rides = _people.Count(p => p.ReceivesRides);
        var vehicleUsers = _people.Count(p => p.UsesHouseholdVehicle);

        _statsLabel.Text =
            $"Household members: {householdCount}   Contributors: {contributors}   Monthly contribution: {monthlyContribution:C2}   Rides: {rides}   Vehicle users: {vehicleUsers}";
    }

    private void CaptureSelectedGridPerson()
    {
        if (_grid.CurrentRow?.DataBoundItem is HouseholdPerson person)
            _selectedPerson = person;
    }

    private void StartNewPerson()
    {
        if (_repository is null)
        {
            MessageBox.Show("Create or open a case before adding people.", "No case loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _selectedPerson = new HouseholdPerson();
        _editTitleLabel.Text = "Add person";
        ClearEditor();
        ShowEditPanel();
        _fullNameTextBox.Focus();
    }

    private void EditSelectedPerson()
    {
        if (_selectedPerson is null || _selectedPerson.Id <= 0)
        {
            MessageBox.Show("Select a person first.", "No person selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _editTitleLabel.Text = "Edit person";
        LoadPersonIntoEditor(_selectedPerson);
        ShowEditPanel();
        _fullNameTextBox.Focus();
    }

    private void ShowSelectedProfile()
    {
        CaptureSelectedGridPerson();

        if (_selectedPerson is null || _selectedPerson.Id <= 0)
        {
            MessageBox.Show("Select a person first.", "No person selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _profileTitleLabel.Text = _selectedPerson.FullName;
        PopulateProfileDetails(_selectedPerson);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var person = _people.FirstOrDefault(p => p.Id == recordId);
        if (person is null && _repository is not null)
            person = _repository.GetAll().FirstOrDefault(p => p.Id == recordId);

        if (person is null)
            return false;

        _selectedPerson = person;
        SelectGridRowByPersonId(recordId);
        _profileTitleLabel.Text = person.FullName;
        PopulateProfileDetails(person);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByPersonId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is HouseholdPerson person && person.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void ClearEditor()
    {
        _fullNameTextBox.Text = string.Empty;
        _relationshipTextBox.Text = string.Empty;
        _roleTextBox.Text = string.Empty;
        _livesInHouseholdCheckBox.Checked = false;
        _paysRentCheckBox.Checked = false;
        _contributionModeComboBox.SelectedItem = "No Contribution";
        SelectLinkedIncomeSource(0);
        _usesVehicleCheckBox.Checked = false;
        _receivesRidesCheckBox.Checked = false;
        _notesTextBox.Text = string.Empty;
    }

    private void LoadPersonIntoEditor(HouseholdPerson person)
    {
        _fullNameTextBox.Text = person.FullName;
        _relationshipTextBox.Text = person.Relationship;
        _roleTextBox.Text = person.Role;
        _livesInHouseholdCheckBox.Checked = person.LivesInHousehold;
        _paysRentCheckBox.Checked = person.PaysRent;
        var contributionMode = string.IsNullOrWhiteSpace(person.ContributionHandling)
            ? (person.LinkedIncomeSourceId > 0 ? "Select Existing Income Source" : "No Contribution")
            : person.ContributionHandling;
        if (contributionMode.Equals("Create Income Source Now", StringComparison.OrdinalIgnoreCase))
            contributionMode = "Select Existing Income Source";
        _contributionModeComboBox.SelectedItem = contributionMode;
        if (_contributionModeComboBox.SelectedIndex < 0)
            _contributionModeComboBox.SelectedItem = "No Contribution";
        SelectLinkedIncomeSource(person.LinkedIncomeSourceId);
        _usesVehicleCheckBox.Checked = person.UsesHouseholdVehicle;
        _receivesRidesCheckBox.Checked = person.ReceivesRides;
        _notesTextBox.Text = person.Notes;
    }

    private void SaveCurrentPerson()
    {
        if (_repository is null)
        {
            MessageBox.Show("Create or open a case before saving people.", "No case loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var name = _fullNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Enter a full name before saving.", "Name required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _fullNameTextBox.Focus();
            return;
        }

        var contributionMode = (_contributionModeComboBox.SelectedItem as string) ?? "No Contribution";
        var linkedIncome = _linkedIncomeSourceComboBox.SelectedItem as IncomeSourceListItem;

        if (contributionMode == "Select Existing Income Source" && (linkedIncome is null || linkedIncome.Id <= 0))
        {
            MessageBox.Show("Select an income source for this person's contribution, or choose No Contribution.", "Income source required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _linkedIncomeSourceComboBox.Focus();
            return;
        }

        if (contributionMode == "Create Income Source Now")
        {
            MessageBox.Show("Create the income source first, then save the person.", "Income source required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CreateContributionIncomeSource();
            return;
        }

        _selectedPerson ??= new HouseholdPerson();
        _selectedPerson.FullName = name;
        _selectedPerson.Relationship = _relationshipTextBox.Text.Trim();
        _selectedPerson.Role = _roleTextBox.Text.Trim();
        _selectedPerson.LivesInHousehold = _livesInHouseholdCheckBox.Checked;
        _selectedPerson.PaysRent = _paysRentCheckBox.Checked;
        _selectedPerson.MonthlyContribution = 0m;
        _selectedPerson.ContributionHandling = contributionMode;
        _selectedPerson.LinkedIncomeSourceId = contributionMode == "Select Existing Income Source" && linkedIncome is not null ? linkedIncome.Id : 0;
        _selectedPerson.LinkedIncomeSourceName = contributionMode == "Select Existing Income Source" && linkedIncome is not null ? linkedIncome.Name : string.Empty;
        _selectedPerson.UsesHouseholdVehicle = _usesVehicleCheckBox.Checked;
        _selectedPerson.ReceivesRides = _receivesRidesCheckBox.Checked;
        _selectedPerson.Notes = _notesTextBox.Text.Trim();

        _repository.Upsert(_selectedPerson);
        ReloadPeople();
        ShowListPanel();
    }

    private void LoadIncomeSources()
    {
        if (string.IsNullOrWhiteSpace(_databasePath))
        {
            _incomeSources = Array.Empty<IncomeSource>();
            PopulateIncomeSourceDropdown();
            return;
        }

        try
        {
            _incomeSources = new IncomeSourcesRepository(_databasePath).GetAll()
                .OrderBy(source => !source.IsActive)
                .ThenBy(source => source.SourceName)
                .ToList();
        }
        catch
        {
            _incomeSources = Array.Empty<IncomeSource>();
        }

        PopulateIncomeSourceDropdown();
    }

    private void PopulateIncomeSourceDropdown()
    {
        if (_linkedIncomeSourceComboBox is null)
            return;

        var previousId = (_linkedIncomeSourceComboBox.SelectedItem as IncomeSourceListItem)?.Id ?? 0;
        _linkedIncomeSourceComboBox.BeginUpdate();
        _linkedIncomeSourceComboBox.Items.Clear();

        foreach (var source in _incomeSources)
            _linkedIncomeSourceComboBox.Items.Add(new IncomeSourceListItem(source.Id, source.SourceName, source.MonthlyEquivalent, source.IsActive));

        _linkedIncomeSourceComboBox.EndUpdate();

        if (previousId > 0)
            SelectLinkedIncomeSource(previousId);
        else if (_linkedIncomeSourceComboBox.Items.Count > 0)
            _linkedIncomeSourceComboBox.SelectedIndex = 0;

        HandleContributionModeChanged();
    }

    private void SelectLinkedIncomeSource(long id)
    {
        if (_linkedIncomeSourceComboBox is null)
            return;

        if (id <= 0)
        {
            _linkedIncomeSourceComboBox.SelectedIndex = _linkedIncomeSourceComboBox.Items.Count > 0 ? 0 : -1;
            return;
        }

        for (var i = 0; i < _linkedIncomeSourceComboBox.Items.Count; i++)
        {
            if (_linkedIncomeSourceComboBox.Items[i] is IncomeSourceListItem item && item.Id == id)
            {
                _linkedIncomeSourceComboBox.SelectedIndex = i;
                return;
            }
        }

        _linkedIncomeSourceComboBox.SelectedIndex = _linkedIncomeSourceComboBox.Items.Count > 0 ? 0 : -1;
    }

    private void HandleContributionModeChanged()
    {
        if (_contributionModeComboBox is null || _linkedIncomeSourceComboBox is null)
            return;

        EnsureContributionModeOptions();

        var mode = (_contributionModeComboBox.SelectedItem as string) ?? "No Contribution";
        var selectingExisting = mode == "Select Existing Income Source";
        var creatingNow = mode == "Create Income Source Now";

        const int x = 22;
        const int normalGap = 14;
        const int labelToInputGap = 22;
        const int sectionGap = 30;

        var nextY = _contributionModeComboBox.Bottom + normalGap;

        _linkedIncomeSourceLabel.Visible = selectingExisting;
        _linkedIncomeSourceComboBox.Visible = selectingExisting;
        _createIncomeSourceButton.Visible = creatingNow;

        if (selectingExisting)
        {
            _linkedIncomeSourceLabel.Location = new Point(x, nextY);
            _linkedIncomeSourceComboBox.Location = new Point(x, _linkedIncomeSourceLabel.Bottom + 6);
            nextY = _linkedIncomeSourceComboBox.Bottom + sectionGap;
        }
        else if (creatingNow)
        {
            _createIncomeSourceButton.Location = new Point(x, nextY);
            nextY = _createIncomeSourceButton.Bottom + sectionGap;
        }

        _notesLabel.Location = new Point(x, nextY);
        _notesTextBox.Location = new Point(x, _notesLabel.Bottom + 6);
        _saveButton.Location = new Point(x, _notesTextBox.Bottom + 10);
        _cancelButton.Location = new Point(186, _notesTextBox.Bottom + 10);
    }

    private void EnsureContributionModeOptions()
    {
        if (_contributionModeComboBox is null)
            return;

        var current = _contributionModeComboBox.SelectedItem as string;
        var required = new[]
        {
            "No Contribution",
            "Select Existing Income Source",
            "Create Income Source Now"
        };

        foreach (var item in required)
        {
            if (!_contributionModeComboBox.Items.Contains(item))
                _contributionModeComboBox.Items.Add(item);
        }

        if (string.IsNullOrWhiteSpace(current))
            _contributionModeComboBox.SelectedItem = "No Contribution";
        else if (_contributionModeComboBox.Items.Contains(current))
            _contributionModeComboBox.SelectedItem = current;
    }

    private void CreateContributionIncomeSource()
    {
        if (string.IsNullOrWhiteSpace(_databasePath))
            return;

        var suggestedName = string.IsNullOrWhiteSpace(_fullNameTextBox.Text)
            ? "Household Contribution"
            : _fullNameTextBox.Text.Trim() + " Contribution";

        using var dialog = new ContributionIncomeDialog(suggestedName);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            EnsureContributionModeOptions();
            var fallbackMode = string.IsNullOrWhiteSpace(_lastStableContributionMode)
                ? "No Contribution"
                : _lastStableContributionMode;

            if (fallbackMode == "Create Income Source Now")
                fallbackMode = _linkedIncomeSourceComboBox.SelectedItem is IncomeSourceListItem item && item.Id > 0
                    ? "Select Existing Income Source"
                    : "No Contribution";

            _contributionModeComboBox.SelectedItem = fallbackMode;
            HandleContributionModeChanged();
            return;
        }

        try
        {
            var source = new IncomeSource
            {
                SourceName = dialog.SourceName,
                IncomeType = "Family Contribution",
                Amount = dialog.Amount,
                TaxesWithheld = false,
                Frequency = dialog.Frequency,
                ExpectedDayOrDate = dialog.ExpectedDayOrDate,
                DepositedToAccount = dialog.DepositedToAccount,
                IsActive = true,
                Notes = "Created from People / Household contribution link."
            };

            var repo = new IncomeSourcesRepository(_databasePath);
            repo.Upsert(source);

            LoadIncomeSources();
            EnsureContributionModeOptions();
            _contributionModeComboBox.SelectedItem = "Select Existing Income Source";
            _lastStableContributionMode = "Select Existing Income Source";
            SelectLinkedIncomeSource(source.Id);
            HandleContributionModeChanged();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not create income source:\n{ex.Message}", "Income source failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }


    private void DeleteCurrentPerson()
    {
        if (_repository is null || _selectedPerson is null || _selectedPerson.Id <= 0)
            return;

        var result = MessageBox.Show(
            $"Remove {_selectedPerson.FullName}?\n\nThis deletes the person from this case and removes them from the People / Household list.",
            "Remove person",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedPerson.Id);
        _selectedPerson = null;
        ReloadPeople();
    }

    private void CancelEdit()
    {
        if (_selectedPerson is not null && _selectedPerson.Id > 0)
            ShowSelectedProfile();
        else
            ShowListPanel();
    }

    private void ExportCurrentPersonPdf()
    {
        if (_selectedPerson is null || _selectedPerson.Id <= 0)
            return;

        var exportFolder = GetDefaultExportFolder();

        using var dialog = new SaveFileDialog
        {
            Title = "Export Person Profile PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(_selectedPerson.FullName) + "_Profile.pdf",
            InitialDirectory = exportFolder,
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WritePersonProfile(dialog.FileName, _selectedPerson, BuildProfileText(_selectedPerson));
            MessageBox.Show("Profile PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        _editPanel.AutoScrollPosition = Point.Empty;
    }

    private void PopulateProfileDetails(HouseholdPerson person)
    {
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Basic Information");
        AddProfileRow("Name", person.FullName);
        AddProfileRow("Relationship", person.Relationship);
        AddProfileRow("Role / Purpose", person.Role);

        AddProfileSpacer();
        AddProfileSection("Household / Financial Load");
        AddProfileRow("Lives in household", YesNo(person.LivesInHousehold));
        AddProfileRow("Pays rent / contributes", YesNo(person.PaysRent));
        AddProfileRow("Contribution income", GetContributionDisplayName(person));
        AddProfileRow("Uses household vehicle", YesNo(person.UsesHouseholdVehicle));
        AddProfileRow("Receives rides / transport help", YesNo(person.ReceivesRides));

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(person.Notes) ? "None" : person.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", person.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", person.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

        _profileDetailsPanel.ResumeLayout();
        UpdateProfileContentWidth();
    }

    private void UpdateProfileContentWidth()
    {
        if (_profilePanel is null || _profileDetailsPanel is null)
        {
            return;
        }

        var usableWidth = Math.Max(400, _profilePanel.ClientSize.Width - 24);
        _profileDetailsPanel.Width = usableWidth;

        foreach (Control control in _profileDetailsPanel.Controls)
        {
            control.Width = usableWidth;
        }
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

    private static string GetContributionDisplayName(HouseholdPerson person)
    {
        if (person.LinkedIncomeSourceId > 0 && !string.IsNullOrWhiteSpace(person.LinkedIncomeSourceName))
            return person.LinkedIncomeSourceName.Trim();

        if (person.LinkedIncomeSourceId > 0)
            return "Linked income source #" + person.LinkedIncomeSourceId.ToString(CultureInfo.InvariantCulture);

        if (person.MonthlyContribution > 0m)
            return "Legacy unlinked contribution: " + person.MonthlyContribution.ToString("C2", CultureInfo.CurrentCulture);

        return "No linked contribution";
    }

    private static string BuildProfileText(HouseholdPerson person)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Name: {person.FullName}");
        builder.AppendLine($"Relationship: {person.Relationship}");
        builder.AppendLine($"Role / Purpose: {person.Role}");
        builder.AppendLine();
        builder.AppendLine("Household / Financial Load");
        builder.AppendLine($"Lives in household: {YesNo(person.LivesInHousehold)}");
        builder.AppendLine($"Pays rent / contributes: {YesNo(person.PaysRent)}");
        builder.AppendLine($"Contribution income: {GetContributionDisplayName(person)}");
        builder.AppendLine($"Uses household vehicle: {YesNo(person.UsesHouseholdVehicle)}");
        builder.AppendLine($"Receives rides / transport help: {YesNo(person.ReceivesRides)}");
        builder.AppendLine();
        builder.AppendLine("Notes");
        builder.AppendLine(string.IsNullOrWhiteSpace(person.Notes) ? "None" : person.Notes);
        builder.AppendLine();
        builder.AppendLine($"Created UTC: {person.CreatedUtc:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Updated UTC: {person.UpdatedUtc:yyyy-MM-dd HH:mm}");
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
            SortMode = DataGridViewColumnSortMode.Automatic
        };

        if (!string.IsNullOrWhiteSpace(format))
            column.DefaultCellStyle.Format = format;

        _grid.Columns.Add(column);
    }

    private void AddCheckColumn(string property, string header, int width)
    {
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = property,
            HeaderText = header,
            Width = width,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
    }

    private Label CreateLabel(string labelText, int y)
    {
        return new Label
        {
            Text = labelText,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(22, y)
        };
    }

    private ComboBox CreateComboBox(Control parent, string labelText, int y, int width)
    {
        var label = CreateLabel(labelText, y);
        var comboBox = CreateComboBoxNoLabel(22, y + 22, width);
        parent.Controls.Add(label);
        parent.Controls.Add(comboBox);
        return comboBox;
    }

    private ComboBox CreateComboBoxNoLabel(int x, int y, int width)
    {
        return new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 28),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
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
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Person" : safe.Trim();
    }

    private sealed class IncomeSourceListItem
    {
        public IncomeSourceListItem(long id, string name, decimal monthlyEquivalent, bool isActive)
        {
            Id = id;
            Name = name;
            MonthlyEquivalent = monthlyEquivalent;
            IsActive = isActive;
        }

        public long Id { get; }
        public string Name { get; }
        public decimal MonthlyEquivalent { get; }
        public bool IsActive { get; }

        public override string ToString()
        {
            var suffix = IsActive ? string.Empty : " (inactive)";
            return $"{Name} - {MonthlyEquivalent:C2}/mo{suffix}";
        }
    }

    private sealed class ContributionIncomeDialog : Form
    {
        private readonly TextBox _sourceNameTextBox;
        private readonly TextBox _amountTextBox;
        private readonly ComboBox _frequencyComboBox;
        private readonly TextBox _expectedTextBox;
        private readonly ComboBox _depositComboBox;

        public ContributionIncomeDialog(string suggestedName)
        {
            Text = "Create Income Source";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 360);
            BackColor = Back;
            ForeColor = TextPrimary;

            var title = new Label
            {
                Text = "Create contribution income source",
                Location = new Point(22, 18),
                AutoSize = true,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };
            Controls.Add(title);

            _sourceNameTextBox = AddDialogTextBox("Source name", suggestedName, 66, 440);
            _amountTextBox = AddDialogTextBox("Payment amount", "0.00", 126, 180);

            _frequencyComboBox = AddDialogComboBox("Frequency", 186, 220);
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

            _expectedTextBox = AddDialogTextBox("Expected day/date", string.Empty, 246, 220);

            _depositComboBox = AddDialogComboBox("Deposit method / destination", 246, 220, x: 260);
            _depositComboBox.Items.AddRange(new object[] { "Cash", "Check" });
            _depositComboBox.SelectedItem = "Cash";

            var okButton = new Button
            {
                Text = "Create Income Source",
                DialogResult = DialogResult.OK,
                Location = new Point(22, 308),
                Size = new Size(180, 36),
                BackColor = Panel2,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            okButton.Click += (_, e) =>
            {
                if (string.IsNullOrWhiteSpace(SourceName))
                {
                    MessageBox.Show(this, "Enter a source name.", "Source name required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (!decimal.TryParse(_amountTextBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount) || amount < 0m)
                {
                    MessageBox.Show(this, "Enter a valid payment amount.", "Amount required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                }
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(214, 308),
                Size = new Size(120, 36),
                BackColor = Panel2,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            Controls.Add(okButton);
            Controls.Add(cancelButton);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        public string SourceName => _sourceNameTextBox.Text.Trim();
        public decimal Amount => decimal.TryParse(_amountTextBox.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out var amount) ? amount : 0m;
        public string Frequency => (_frequencyComboBox.SelectedItem as string) ?? "Monthly";
        public string ExpectedDayOrDate => _expectedTextBox.Text.Trim();
        public string DepositedToAccount => (_depositComboBox.SelectedItem as string) ?? "Cash";

        private TextBox AddDialogTextBox(string labelText, string value, int y, int width, int x = 22)
        {
            Controls.Add(new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            });

            var textBox = new TextBox
            {
                Text = value,
                Location = new Point(x, y + 22),
                Size = new Size(width, 26),
                BackColor = Back,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular)
            };
            Controls.Add(textBox);
            return textBox;
        }

        private ComboBox AddDialogComboBox(string labelText, int y, int width, int x = 22)
        {
            Controls.Add(new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            });

            var comboBox = new ComboBox
            {
                Location = new Point(x, y + 22),
                Size = new Size(width, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Back,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular)
            };
            Controls.Add(comboBox);
            return comboBox;
        }
    }

    private static class MinimalPdfWriter
    {
        public static void WritePersonProfile(string path, HouseholdPerson person, string profileText)
        {
            var lines = new List<string>
            {
                "Family Finance & Trust Manager",
                "Person Profile",
                string.Empty,
                $"Exported: {DateTime.Now:yyyy-MM-dd h:mm tt}",
                string.Empty
            };

            lines.AddRange(profileText.Replace("\r", string.Empty).Split('\n'));

            var content = BuildPdfContent(lines.Where(line => line is not null).ToList());
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
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

        private static string BuildPdfContent(IReadOnlyList<string> lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BT");
            builder.AppendLine("/F1 11 Tf");
            builder.AppendLine("50 742 Td");
            builder.AppendLine("14 TL");

            var written = 0;
            foreach (var rawLine in lines)
            {
                if (written >= 48)
                    break;

                var line = rawLine.Length > 95 ? rawLine[..95] : rawLine;
                builder.Append('(').Append(EscapePdfText(line)).AppendLine(") Tj");
                builder.AppendLine("T*");
                written++;
            }

            builder.AppendLine("ET");
            return builder.ToString();
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
