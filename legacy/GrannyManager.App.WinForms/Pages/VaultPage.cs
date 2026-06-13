using System.ComponentModel;
using System.Diagnostics;
using GrannyManager.App.Navigation;
using GrannyManager.App.Services;
using GrannyManager.Core.Models;
using GrannyManager.Core.Services;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using GrannyManager.Security.Crypto;

namespace GrannyManager.App.Pages;

public sealed class VaultPage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel = Color.FromArgb(16, 34, 55);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);

    private readonly BindingList<CredentialRecord> _credentials = new();
    private readonly List<LinkOption> _linkOptions = new();
    private readonly CaseFolderService _caseFolderService = new();

    private CredentialVaultRepository? _repository;
    private string? _caseFolder;
    private CredentialRecord? _selectedCredential;
    private string _lastClipboardValue = string.Empty;
    private readonly System.Windows.Forms.Timer _clipboardTimer = new() { Interval = 30000 };
    private readonly System.Windows.Forms.Timer _revealTimer = new() { Interval = 30000 };

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
    private Button _profileOpenWebsiteButton = null!;
    private Button _profileCopyUsernameButton = null!;
    private Button _profileCopyPasswordButton = null!;
    private Button _profileRevealPasswordButton = null!;
    private Label _profilePasswordValueLabel = null!;

    private Panel _editPanel = null!;
    private Label _editTitleLabel = null!;
    private TextBox _accountNameTextBox = null!;
    private TextBox _websiteTextBox = null!;
    private TextBox _usernameTextBox = null!;
    private TextBox _passwordTextBox = null!;
    private TextBox _recoveryTextBox = null!;
    private TextBox _securityNotesTextBox = null!;
    private ComboBox _linkedSectionComboBox = null!;
    private Label _linkedRecordLabel = null!;
    private ComboBox _linkedRecordComboBox = null!;
    private CheckBox _activeCheckBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public VaultPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
        _clipboardTimer.Tick += (_, _) => ClearClipboardIfNeeded();
        _revealTimer.Tick += (_, _) => HideRevealedPassword();
    }

    public AppPageKey PageKey => AppPageKey.Vault;
    public string PageTitle => "Credential Vault";
    public bool CanNavigateAway() => true;

    public void OnNavigatedTo()
    {
        InitializeForActiveCase();
    }

    public bool OpenSearchResult(long recordId)
    {
        InitializeForActiveCase();
        var item = _credentials.FirstOrDefault(c => c.Id == recordId);
        if (item is null)
            return false;

        _selectedCredential = item;
        ShowProfile(item);
        return true;
    }

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
            Text = "Credential Vault",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Store encrypted logins for bills, bank accounts, benefits, debts, and important financial sites.",
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

        _addButton = CreateButton("Add Credential", 0, 0, 145);
        _viewButton = CreateButton("View Profile", 0, 0, 120);
        _editButton = CreateButton("Edit Selected", 0, 0, 125);
        buttonRow.Controls.Add(_addButton);
        buttonRow.Controls.Add(_viewButton);
        buttonRow.Controls.Add(_editButton);

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
            DataSource = _credentials
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

        AddFillTextColumn("AccountName", "Account", 34f);
        AddFillTextColumn("WebsiteUrl", "Website", 28f);
        AddFillTextColumn("LinkedDisplay", "Linked To", 28f);
        AddTextColumn("StatusText", "Status", 90);

        _grid.SelectionChanged += (_, _) => UpdateSelectionFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowSelectedProfile();
        ListPageGridHelper.AttachRightClickRemove(_grid, UpdateSelectionFromGrid, RemoveSelectedCredential);
        gridHost.Controls.Add(_grid);

        _addButton.Click += (_, _) => BeginAddCredential();
        _viewButton.Click += (_, _) => ShowSelectedProfile();
        _editButton.Click += (_, _) => BeginEditSelectedCredential();
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
            Size = new Size(920, 46),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Back
        };

        _profileBackButton = CreateButton("Back to List", 0, 0, 120);
        _profileEditButton = CreateButton("Edit", 0, 0, 90);
        _profileRemoveButton = CreateButton("Remove", 0, 0, 100);
        _profileOpenWebsiteButton = CreateButton("Open Website", 0, 0, 125);
        _profileCopyUsernameButton = CreateButton("Copy Username", 0, 0, 130);
        _profileCopyPasswordButton = CreateButton("Copy Password", 0, 0, 130);
        _profileRevealPasswordButton = CreateButton("Reveal Password", 0, 0, 140);
        buttonRow.Controls.Add(_profileBackButton);
        buttonRow.Controls.Add(_profileEditButton);
        buttonRow.Controls.Add(_profileRemoveButton);
        buttonRow.Controls.Add(_profileOpenWebsiteButton);
        buttonRow.Controls.Add(_profileCopyUsernameButton);
        buttonRow.Controls.Add(_profileCopyPasswordButton);
        buttonRow.Controls.Add(_profileRevealPasswordButton);
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
            BackColor = Back
        };
        _profilePanel.Controls.Add(_profileDetailsPanel);

        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileEditButton.Click += (_, _) => BeginEditSelectedCredential();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedCredential();
        _profileOpenWebsiteButton.Click += (_, _) => OpenSelectedWebsite();
        _profileCopyUsernameButton.Click += (_, _) => CopySelectedUsername();
        _profileCopyPasswordButton.Click += (_, _) => CopySelectedPassword();
        _profileRevealPasswordButton.Click += (_, _) => RevealSelectedPassword();
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
            Text = "Add Credential",
            Location = new Point(0, 0),
            Size = new Size(700, 42),
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _editPanel.Controls.Add(_editTitleLabel);

        _accountNameTextBox = CreateTextBox(_editPanel, "Account name", 58, 420);
        _websiteTextBox = CreateTextBox(_editPanel, "Website / login URL", 116, 520);
        _usernameTextBox = CreateTextBox(_editPanel, "Username", 174, 420);
        _passwordTextBox = CreateTextBox(_editPanel, "Password", 232, 420, usePassword: true);
        _recoveryTextBox = CreateTextBox(_editPanel, "Recovery email / phone", 290, 420);

        var linkedSectionLabel = CreateLabel("Linked section", 348);
        _editPanel.Controls.Add(linkedSectionLabel);
        _linkedSectionComboBox = CreateComboBox(22, 370, 260);
        _linkedSectionComboBox.Items.AddRange(new object[]
        {
            "Not linked",
            "People",
            "Income Sources",
            "Bills / Spending",
            "Allowance / Savings",
            "Assets",
            "Debts",
            "Documents",
            "Other"
        });
        _linkedSectionComboBox.SelectedItem = "Not linked";
        _linkedSectionComboBox.SelectedIndexChanged += (_, _) => HandleLinkedSectionChanged();
        _editPanel.Controls.Add(_linkedSectionComboBox);

        _linkedRecordLabel = CreateLabel("Linked record", 416);
        _linkedRecordLabel.Visible = false;
        _editPanel.Controls.Add(_linkedRecordLabel);
        _linkedRecordComboBox = CreateComboBox(22, 438, 460);
        _linkedRecordComboBox.Visible = false;
        _editPanel.Controls.Add(_linkedRecordComboBox);

        _activeCheckBox = CreateCheckBox("Active credential", 492);
        _activeCheckBox.Checked = true;
        _editPanel.Controls.Add(_activeCheckBox);

        _securityNotesTextBox = CreateTextBox(_editPanel, "Security notes", 534, 650, multiline: true);

        _saveButton = CreateButton("Save Credential", 22, 656, 160);
        _cancelButton = CreateButton("Cancel", 195, 656, 110);
        _editPanel.Controls.Add(_saveButton);
        _editPanel.Controls.Add(_cancelButton);

        _saveButton.Click += (_, _) => SaveCredentialFromEditor();
        _cancelButton.Click += (_, _) => CancelEdit();
    }

    private void InitializeForActiveCase()
    {
        if (AppState.ActiveCase is null)
        {
            _repository = null;
            _caseFolder = null;
            _credentials.Clear();
            _caseStatusLabel.Text = "No active case found. Create or open a case first from Case Setup.";
            _caseStatusLabel.ForeColor = Color.FromArgb(255, 210, 80);
            _statsLabel.Text = "Credentials: 0";
            ShowListPanel();
            return;
        }

        _caseFolder = AppState.ActiveCase.CaseFolderPath;
        string databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
        _repository = new CredentialVaultRepository(databasePath);
        _caseStatusLabel.Text = $"Active case folder: {_caseFolder}";
        _caseStatusLabel.ForeColor = Good;
        LoadCredentials();
    }

    private void LoadCredentials()
    {
        _credentials.Clear();
        if (_repository is null)
        {
            UpdateStats();
            return;
        }

        foreach (var item in _repository.GetAll())
            _credentials.Add(item);

        if (_grid is not null)
        {
            _grid.ClearSelection();
            ListPageGridHelper.ApplyInactiveRowStyles(_grid, row => row is CredentialRecord c && c.IsActive);
        }

        _selectedCredential = null;
        UpdateStats();
        ShowListPanel();
    }

    private void UpdateStats()
    {
        int total = _credentials.Count;
        int active = _credentials.Count(c => c.IsActive);
        _statsLabel.Text = $"Credentials: {total}    Active: {active}";
    }

    private void ShowListPanel()
    {
        _listPanel.Visible = true;
        _listPanel.BringToFront();
        _profilePanel.Visible = false;
        _editPanel.Visible = false;
    }

    private void ShowProfilePanel()
    {
        _profilePanel.Visible = true;
        _profilePanel.BringToFront();
        _listPanel.Visible = false;
        _editPanel.Visible = false;
        _profilePanel.VerticalScroll.Value = 0;
    }

    private void ShowEditPanel()
    {
        _editPanel.Visible = true;
        _editPanel.BringToFront();
        _listPanel.Visible = false;
        _profilePanel.Visible = false;
        _editPanel.VerticalScroll.Value = 0;
    }

    private void UpdateSelectionFromGrid()
    {
        _selectedCredential = _grid.CurrentRow?.DataBoundItem as CredentialRecord;
    }

    private void ShowSelectedProfile()
    {
        UpdateSelectionFromGrid();
        if (_selectedCredential is null)
        {
            MessageBox.Show("Select a credential first.", "No credential selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ShowProfile(_selectedCredential);
    }

    private void ShowProfile(CredentialRecord item)
    {
        _selectedCredential = item;
        HideRevealedPassword();
        _profileTitleLabel.Text = string.IsNullOrWhiteSpace(item.AccountName) ? "Credential" : item.AccountName;
        _profileDetailsPanel.Controls.Clear();

        string username = Decrypt(item.EncryptedUsername);
        string recovery = Decrypt(item.EncryptedRecoveryInfo);
        string notes = Decrypt(item.EncryptedSecurityNotes);

        AddSection("Login Details");
        AddDetail("Website", string.IsNullOrWhiteSpace(item.WebsiteUrl) ? "None" : item.WebsiteUrl);
        AddDetail("Username", string.IsNullOrWhiteSpace(username) ? "None" : username);
        _profilePasswordValueLabel = AddDetail("Password", "••••••••••••");
        AddDetail("Recovery info", string.IsNullOrWhiteSpace(recovery) ? "None" : recovery);

        AddSection("Linked Record");
        AddDetail("Linked to", item.LinkedDisplay);
        AddDetail("Status", item.StatusText);

        AddSection("Security Notes");
        AddWrappedText(string.IsNullOrWhiteSpace(notes) ? "None" : notes);

        UpdateProfileContentWidth();
        ShowProfilePanel();
    }

    private void BeginAddCredential()
    {
        if (!EnsureCaseReady())
            return;

        _selectedCredential = null;
        _editTitleLabel.Text = "Add Credential";
        _accountNameTextBox.Text = string.Empty;
        _websiteTextBox.Text = string.Empty;
        _usernameTextBox.Text = string.Empty;
        _passwordTextBox.Text = string.Empty;
        _recoveryTextBox.Text = string.Empty;
        _securityNotesTextBox.Text = string.Empty;
        _linkedSectionComboBox.SelectedItem = "Not linked";
        _linkedRecordComboBox.Items.Clear();
        _linkedRecordLabel.Visible = false;
        _linkedRecordComboBox.Visible = false;
        _activeCheckBox.Checked = true;
        ShowEditPanel();
    }

    private void BeginEditSelectedCredential()
    {
        if (_listPanel.Visible)
            UpdateSelectionFromGrid();
        if (_selectedCredential is null)
        {
            MessageBox.Show("Select a credential first.", "No credential selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _selectedCredential;
        _editTitleLabel.Text = "Edit Credential";
        _accountNameTextBox.Text = item.AccountName;
        _websiteTextBox.Text = item.WebsiteUrl;
        _usernameTextBox.Text = Decrypt(item.EncryptedUsername);
        _passwordTextBox.Text = Decrypt(item.EncryptedPassword);
        _recoveryTextBox.Text = Decrypt(item.EncryptedRecoveryInfo);
        _securityNotesTextBox.Text = Decrypt(item.EncryptedSecurityNotes);
        _linkedSectionComboBox.SelectedItem = string.IsNullOrWhiteSpace(item.LinkedRecordType) ? "Not linked" : item.LinkedRecordType;
        PopulateLinkedRecords(item.LinkedRecordType, item.LinkedRecordId);
        _activeCheckBox.Checked = item.IsActive;
        ShowEditPanel();
    }

    private void SaveCredentialFromEditor()
    {
        if (_repository is null || !EnsureCaseReady())
            return;

        string accountName = _accountNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(accountName))
        {
            MessageBox.Show("Enter an account name first.", "Account name needed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _accountNameTextBox.Focus();
            return;
        }

        string linkedType = (_linkedSectionComboBox.SelectedItem as string) ?? "Not linked";
        if (linkedType == "Not linked")
            linkedType = string.Empty;

        long linkedId = 0;
        string linkedName = string.Empty;
        if (_linkedRecordComboBox.Visible && _linkedRecordComboBox.SelectedItem is LinkOption option)
        {
            linkedId = option.Id;
            linkedName = option.Name;
        }

        var item = _selectedCredential ?? new CredentialRecord();
        item.AccountName = accountName;
        item.WebsiteUrl = _websiteTextBox.Text.Trim();
        item.EncryptedUsername = Encrypt(_usernameTextBox.Text);
        item.EncryptedPassword = Encrypt(_passwordTextBox.Text);
        item.EncryptedRecoveryInfo = Encrypt(_recoveryTextBox.Text);
        item.EncryptedSecurityNotes = Encrypt(_securityNotesTextBox.Text);
        item.LinkedRecordType = linkedType;
        item.LinkedRecordId = linkedId;
        item.LinkedRecordName = linkedName;
        item.IsActive = _activeCheckBox.Checked;

        _repository.Upsert(item);
        LoadCredentials();
    }

    private void CancelEdit()
    {
        ShowListPanel();
    }

    private void RemoveSelectedCredential()
    {
        if (_listPanel.Visible)
            UpdateSelectionFromGrid();
        if (_selectedCredential is null || _repository is null)
        {
            MessageBox.Show("Select a credential first.", "No credential selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Remove this credential from the vault?\r\n\r\n{_selectedCredential.AccountName}",
            "Remove credential",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _repository.Delete(_selectedCredential.Id);
        LoadCredentials();
    }

    private void OpenSelectedWebsite()
    {
        if (_selectedCredential is null || string.IsNullOrWhiteSpace(_selectedCredential.WebsiteUrl))
        {
            MessageBox.Show("This credential does not have a website URL.", "No website", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            string url = _selectedCredential.WebsiteUrl.Trim();
            if (!url.Contains("://", StringComparison.Ordinal))
                url = "https://" + url;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not open website", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopySelectedUsername()
    {
        if (_selectedCredential is null)
            return;
        CopyToClipboard(Decrypt(_selectedCredential.EncryptedUsername), "Username copied. Clipboard clears in 30 seconds.");
    }

    private void CopySelectedPassword()
    {
        if (_selectedCredential is null)
            return;
        CopyToClipboard(Decrypt(_selectedCredential.EncryptedPassword), "Password copied. Clipboard clears in 30 seconds.");
    }

    private void RevealSelectedPassword()
    {
        if (_selectedCredential is null || AppState.ActiveCase is null)
            return;

        if (!CasePinPrompt.VerifyCasePin(this, AppState.ActiveCase, _caseFolderService))
            return;

        string password = Decrypt(_selectedCredential.EncryptedPassword);
        if (string.IsNullOrEmpty(password))
            password = "None";

        _profilePasswordValueLabel.Text = password;
        _revealTimer.Stop();
        _revealTimer.Start();
    }

    private void HideRevealedPassword()
    {
        _revealTimer.Stop();
        if (_profilePasswordValueLabel is not null)
            _profilePasswordValueLabel.Text = "••••••••••••";
    }

    private void CopyToClipboard(string value, string message)
    {
        if (string.IsNullOrEmpty(value))
        {
            MessageBox.Show("Nothing to copy.", "Credential Vault", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Clipboard.SetText(value);
            _lastClipboardValue = value;
            _clipboardTimer.Stop();
            _clipboardTimer.Start();
            MessageBox.Show(message, "Credential Vault", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Could not copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearClipboardIfNeeded()
    {
        _clipboardTimer.Stop();
        try
        {
            if (!string.IsNullOrEmpty(_lastClipboardValue) && Clipboard.ContainsText() && Clipboard.GetText() == _lastClipboardValue)
                Clipboard.Clear();
        }
        catch { }
        _lastClipboardValue = string.Empty;
    }

    private void HandleLinkedSectionChanged()
    {
        string section = (_linkedSectionComboBox.SelectedItem as string) ?? "Not linked";
        PopulateLinkedRecords(section, 0);
    }

    private void PopulateLinkedRecords(string? section, long selectedId)
    {
        _linkOptions.Clear();
        _linkedRecordComboBox.Items.Clear();

        if (string.IsNullOrWhiteSpace(section) || section == "Not linked" || section == "Other" || AppState.ActiveCase is null)
        {
            _linkedRecordLabel.Visible = false;
            _linkedRecordComboBox.Visible = false;
            return;
        }

        string databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(AppState.ActiveCase.CaseFolderPath);
        try
        {
            switch (section)
            {
                case "People":
                    foreach (var item in new HouseholdPeopleRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.FullName));
                    break;
                case "Income Sources":
                    foreach (var item in new IncomeSourcesRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.SourceName));
                    break;
                case "Bills / Spending":
                    foreach (var item in new BillsRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.BillName));
                    break;
                case "Allowance / Savings":
                    foreach (var item in new AllowanceSavingsRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.ItemName));
                    break;
                case "Assets":
                    foreach (var item in new AssetsRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.AssetName));
                    break;
                case "Debts":
                    foreach (var item in new DebtsRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.DebtName));
                    break;
                case "Documents":
                    foreach (var item in new DocumentsRepository(databasePath).GetAll())
                        _linkOptions.Add(new LinkOption(item.Id, item.Title));
                    break;
            }
        }
        catch { }

        foreach (var option in _linkOptions)
            _linkedRecordComboBox.Items.Add(option);

        _linkedRecordLabel.Visible = true;
        _linkedRecordComboBox.Visible = true;

        if (_linkedRecordComboBox.Items.Count == 0)
        {
            _linkedRecordComboBox.Items.Add(new LinkOption(0, "No records available yet"));
        }

        LinkOption? selected = _linkOptions.FirstOrDefault(o => o.Id == selectedId);
        _linkedRecordComboBox.SelectedItem = selected ?? _linkedRecordComboBox.Items[0];
    }

    private bool EnsureCaseReady()
    {
        if (AppState.ActiveCase is not null && _repository is not null)
            return true;

        MessageBox.Show("Open or create a case before using the credential vault.", "No case open", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private string Encrypt(string value) => CredentialVaultCrypto.Encrypt(AppState.ActiveCase, value);

    private string Decrypt(string value) => CredentialVaultCrypto.Decrypt(AppState.ActiveCase, value);

    private void AddFillTextColumn(string propertyName, string headerText, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void AddTextColumn(string propertyName, string headerText, int width)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Width = width,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private static Button CreateButton(string text, int x, int y, int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Panel2,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(43, 70, 105);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(33, 58, 88);
        return button;
    }

    private static Label CreateLabel(string text, int y)
    {
        return new Label
        {
            Text = text,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Location = new Point(22, y)
        };
    }

    private static TextBox CreateTextBox(Control parent, string label, int y, int width, bool multiline = false, bool usePassword = false)
    {
        parent.Controls.Add(CreateLabel(label, y));
        var box = new TextBox
        {
            Location = new Point(22, y + 22),
            Size = multiline ? new Size(width, 92) : new Size(width, 26),
            Multiline = multiline,
            ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
            BackColor = Back,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f),
            UseSystemPasswordChar = usePassword
        };
        parent.Controls.Add(box);
        return box;
    }

    private static CheckBox CreateCheckBox(string text, int y)
    {
        return new CheckBox
        {
            Text = text,
            Location = new Point(22, y),
            Size = new Size(360, 26),
            ForeColor = TextPrimary,
            BackColor = Back,
            Font = new Font("Segoe UI", 9.5f)
        };
    }

    private static ComboBox CreateComboBox(int x, int y, int width)
    {
        return new ComboBox
        {
            Location = new Point(x, y),
            Size = new Size(width, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Back,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f)
        };
    }

    private void AddSection(string title)
    {
        _profileDetailsPanel.Controls.Add(new Label
        {
            Text = title,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 6)
        });
    }

    private Label AddDetail(string label, string value)
    {
        var row = new Label
        {
            Text = $"{label}: {value}",
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10f),
            AutoSize = false,
            Size = new Size(Math.Max(650, _profileDetailsPanel.Width - 20), 24),
            Margin = new Padding(0, 0, 0, 4)
        };
        _profileDetailsPanel.Controls.Add(row);
        return row;
    }

    private void AddWrappedText(string text)
    {
        _profileDetailsPanel.Controls.Add(new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 10f),
            AutoSize = false,
            Size = new Size(Math.Max(650, _profileDetailsPanel.Width - 20), Math.Max(40, EstimateTextHeight(text, Math.Max(650, _profileDetailsPanel.Width - 20)))),
            Margin = new Padding(0, 0, 0, 8)
        });
    }

    private static int EstimateTextHeight(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 28;
        int charsPerLine = Math.Max(30, width / 8);
        int lines = Math.Max(1, (int)Math.Ceiling(text.Length / (double)charsPerLine));
        return lines * 24 + 12;
    }

    private void UpdateProfileContentWidth()
    {
        if (_profileDetailsPanel is null)
            return;

        int width = Math.Max(650, _profilePanel.ClientSize.Width - 30);
        _profileTitleLabel.Width = width;
        _profileDetailsPanel.Width = width;
        foreach (Control control in _profileDetailsPanel.Controls)
        {
            if (control is Label label && !label.AutoSize)
                label.Width = width;
        }
    }

    private sealed record LinkOption(long Id, string Name)
    {
        public override string ToString() => Name;
    }
}
