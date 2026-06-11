using GrannyManager.App.Navigation;
using GrannyManager.Core.Models;
using GrannyManager.Data.Database;
using GrannyManager.Data.Repositories;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace GrannyManager.App.Pages;

public sealed class DocumentsPage : UserControl, IAppPage, ISearchResultPage
{
    private static readonly Color Back = Color.FromArgb(10, 24, 40);
    private static readonly Color Panel2 = Color.FromArgb(21, 43, 68);
    private static readonly Color Border = Color.FromArgb(65, 88, 116);
    private static readonly Color TextPrimary = Color.FromArgb(245, 248, 252);
    private static readonly Color TextMuted = Color.FromArgb(185, 198, 214);
    private static readonly Color Good = Color.FromArgb(82, 220, 128);
    private static readonly Color Danger = Color.FromArgb(255, 120, 120);

    private readonly BindingList<DocumentRecord> _documents = new();
    private readonly List<LinkedRecordChoice> _linkedChoices = new();

    private DocumentsRepository? _repository;
    private string? _caseFolder;
    private DocumentRecord? _selectedDocument;

    private Label _caseStatusLabel = null!;
    private Label _statsLabel = null!;
    private Panel _contentHost = null!;

    private Panel _listPanel = null!;
    private DataGridView _grid = null!;
    private Button _addButton = null!;
    private Button _viewButton = null!;
    private Button _editButton = null!;
    private Button _favoriteButton = null!;
    private Button _exportIndexButton = null!;

    private Panel _profilePanel = null!;
    private Label _profileTitleLabel = null!;
    private FlowLayoutPanel _profileDetailsPanel = null!;
    private Button _profileBackButton = null!;
    private Button _profileOpenButton = null!;
    private Button _profileEditButton = null!;
    private Button _profileRemoveButton = null!;
    private Button _profileExportButton = null!;

    private Panel _editPanel = null!;
    private Label _editTitleLabel = null!;
    private TextBox _titleTextBox = null!;
    private ComboBox _categoryComboBox = null!;
    private TextBox _tagsTextBox = null!;
    private ComboBox _linkedTypeComboBox = null!;
    private Panel _linkedRecordPanel = null!;
    private ComboBox _linkedRecordComboBox = null!;
    private TextBox _selectedFileTextBox = null!;
    private Button _browseFileButton = null!;
    private CheckBox _activeCheckBox = null!;
    private TextBox _notesTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    public DocumentsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Back;
        BuildUi();
        AppState.ActiveCaseChanged += (_, _) => InitializeForActiveCase();
    }

    public AppPageKey PageKey => AppPageKey.Documents;
    public string PageTitle => "Documents";
    public void OnNavigatedTo() => InitializeForActiveCase();
    public bool CanNavigateAway() => true;

    private void BuildUi()
    {
        Controls.Clear();

        var root = new Panel { Dock = DockStyle.Fill, BackColor = Back, Padding = new Padding(28) };
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "Documents",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        });

        var subtitle = new Label
        {
            Text = "Store important documents, tag them with searchable keywords, and link them to the records they belong to.",
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

        _addButton = CreateButton("Add Document", 0, 0, 135);
        _viewButton = CreateButton("View Profile", 0, 0, 120);
        _editButton = CreateButton("Edit Selected", 0, 0, 125);
        _favoriteButton = CreateButton("Flag Important", 0, 0, 135);
        _exportIndexButton = CreateButton("Export Index PDF", 0, 0, 150);
        buttonRow.Controls.Add(_addButton);
        buttonRow.Controls.Add(_viewButton);
        buttonRow.Controls.Add(_editButton);
        buttonRow.Controls.Add(_favoriteButton);
        buttonRow.Controls.Add(_exportIndexButton);

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
            DataSource = _documents
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

        AddFillTextColumn("Title", "Title", 30f);
        AddFillTextColumn("Category", "Category", 16f);
        AddFillTextColumn("ImportantText", "Important", 12f);
        AddFillTextColumn("Tags", "Tags / Keywords", 28f);
        AddFillTextColumn("LinkedDisplay", "Linked To", 24f);
        AddFillTextColumn("FileNameDisplay", "File", 24f);
        AddFillTextColumn("StatusText", "Status", 12f);

        _grid.SelectionChanged += (_, _) => UpdateSelectionFromGrid();
        _grid.CellDoubleClick += (_, _) => ShowSelectedProfile();
        AttachDocumentsContextMenu();
        gridHost.Controls.Add(_grid);

        _addButton.Click += (_, _) => BeginAddDocument();
        _viewButton.Click += (_, _) => ShowSelectedProfile();
        _editButton.Click += (_, _) => BeginEditSelectedDocument();
        _favoriteButton.Click += (_, _) => ToggleSelectedDocumentImportant();
        _exportIndexButton.Click += (_, _) => ExportDocumentIndexPdf();
    }

    private void BuildProfilePanel()
    {
        _profilePanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false };
        _contentHost.Controls.Add(_profilePanel);
        _profilePanel.Resize += (_, _) => UpdateProfileContentWidth();

        var buttonRow = new FlowLayoutPanel { Location = new Point(0, 0), Size = new Size(880, 46), FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Back };
        _profilePanel.Controls.Add(buttonRow);

        _profileBackButton = CreateButton("Back to List", 0, 0, 120);
        _profileOpenButton = CreateButton("Open File", 0, 0, 105);
        _profileEditButton = CreateButton("Edit", 0, 0, 90);
        _profileRemoveButton = CreateButton("Remove", 0, 0, 100);
        _profileExportButton = CreateButton("Export PDF", 0, 0, 120);
        buttonRow.Controls.Add(_profileBackButton);
        buttonRow.Controls.Add(_profileOpenButton);
        buttonRow.Controls.Add(_profileEditButton);
        buttonRow.Controls.Add(_profileRemoveButton);
        buttonRow.Controls.Add(_profileExportButton);

        _profileTitleLabel = new Label { Location = new Point(0, 64), Size = new Size(860, 42), ForeColor = TextPrimary, Font = new Font("Segoe UI", 18f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _profilePanel.Controls.Add(_profileTitleLabel);

        _profileDetailsPanel = new FlowLayoutPanel { Location = new Point(0, 116), Size = new Size(900, 600), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Back };
        _profilePanel.Controls.Add(_profileDetailsPanel);

        _profileBackButton.Click += (_, _) => ShowListPanel();
        _profileOpenButton.Click += (_, _) => OpenSelectedDocumentFile();
        _profileEditButton.Click += (_, _) => BeginEditSelectedDocument();
        _profileRemoveButton.Click += (_, _) => RemoveSelectedDocument();
        _profileExportButton.Click += (_, _) => ExportCurrentDocumentPdf();
    }

    private void BuildEditPanel()
    {
        _editPanel = new Panel { Dock = DockStyle.Fill, BackColor = Back, AutoScroll = true, Visible = false };
        _contentHost.Controls.Add(_editPanel);

        _editTitleLabel = new Label { Location = new Point(0, 0), Size = new Size(860, 44), ForeColor = TextPrimary, Font = new Font("Segoe UI", 16f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        _editPanel.Controls.Add(_editTitleLabel);

        var fieldsPanel = new FlowLayoutPanel { Location = new Point(0, 56), Size = new Size(760, 900), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, BackColor = Back };
        _editPanel.Controls.Add(fieldsPanel);

        _titleTextBox = AddField(fieldsPanel, "Document title", 500);
        _categoryComboBox = AddComboField(fieldsPanel, "Category", 330, new[] { "Legal", "Taxes", "Bank Statement", "Bill / Utility", "Vehicle", "Insurance", "Medical", "Trust / Estate", "Identification", "Property", "Home", "Income", "Debt", "Other" });
        _tagsTextBox = AddField(fieldsPanel, "Tags / Keywords (separate with commas)", 650);
        fieldsPanel.Controls.Add(new Label { Text = "Example: IRS, truck, registration, loan", Width = 650, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI", 9f), Margin = new Padding(0, -6, 0, 8), BackColor = Back });
        _linkedTypeComboBox = AddComboField(fieldsPanel, "Linked section", 330, new[] { "None", "People", "Income Sources", "Bills / Spending", "Allowance / Savings", "Assets", "Debts" });
        _linkedRecordPanel = AddLinkedRecordField(fieldsPanel);
        (_selectedFileTextBox, _browseFileButton) = AddFilePickerField(fieldsPanel, "Document file", 650);
        _activeCheckBox = new CheckBox { Text = "Active document", ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f), AutoSize = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 8, 0, 8) };
        fieldsPanel.Controls.Add(_activeCheckBox);
        _notesTextBox = AddMultilineField(fieldsPanel, "Notes", 650, 120);

        var buttonRow = new FlowLayoutPanel { Width = 760, Height = 46, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Back, Margin = new Padding(0, 8, 0, 0) };
        _saveButton = CreateButton("Save Document", 0, 0, 145);
        _cancelButton = CreateButton("Cancel", 0, 0, 110);
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_cancelButton);
        fieldsPanel.Controls.Add(buttonRow);

        _browseFileButton.Click += (_, _) => BrowseForDocumentFile();
        _linkedTypeComboBox.SelectedIndexChanged += (_, _) => RefreshLinkedRecordChoices();
        _saveButton.Click += (_, _) => SaveDocumentFromEditor();
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
            _documents.Clear();
            _caseStatusLabel.Text = "No active case is open. Open or create a case before adding documents.";
            _caseStatusLabel.ForeColor = Danger;
            UpdateStats();
            return;
        }

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
        _repository = new DocumentsRepository(databasePath);
        EnsureCaseDocumentFolder();
        _caseStatusLabel.Text = "Active case folder: " + _caseFolder;
        _caseStatusLabel.ForeColor = TextMuted;
        ReloadDocuments();
    }

    private void ReloadDocuments()
    {
        _documents.Clear();
        if (_repository is not null)
        {
            foreach (var document in _repository.GetAll())
                _documents.Add(document);
        }
        UpdateStats();
        ApplyDocumentRowStyles();
        UpdateSelectionFromGrid();
    }


    private void ApplyDocumentRowStyles()
    {
        ListPageGridHelper.ApplyInactiveRowStyles(_grid, item => item is DocumentRecord document && document.IsActive);

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is not DocumentRecord document || !document.IsImportant || !document.IsActive)
                continue;

            row.DefaultCellStyle.BackColor = Color.FromArgb(24, 78, 50);
            row.DefaultCellStyle.ForeColor = Color.FromArgb(242, 255, 246);
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(38, 118, 72);
            row.DefaultCellStyle.SelectionForeColor = Color.White;
        }
    }

    private void UpdateStats()
    {
        var active = _documents.Count(d => d.IsActive);
        var categories = _documents.Where(d => d.IsActive && !string.IsNullOrWhiteSpace(d.Category)).Select(d => d.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        _statsLabel.Text = $"Documents: {_documents.Count}     Active: {active}     Categories: {categories}";
    }

    private void UpdateSelectionFromGrid()
    {
        _selectedDocument = _grid.CurrentRow?.DataBoundItem as DocumentRecord;
        var has = _selectedDocument is not null;
        _viewButton.Enabled = has;
        _editButton.Enabled = has;
        _favoriteButton.Enabled = has;
        if (has)
        {
            _favoriteButton.Text = _selectedDocument!.IsImportant ? "Unflag Important" : "Flag Important";
        }
        else
        {
            _favoriteButton.Text = "Flag Important";
        }
    }

    private void AttachDocumentsContextMenu()
    {
        var menu = new ContextMenuStrip();

        var toggleImportantItem = new ToolStripMenuItem("Flag as important");
        toggleImportantItem.Click += (_, _) => ToggleSelectedDocumentImportant();
        menu.Items.Add(toggleImportantItem);

        menu.Items.Add(new ToolStripSeparator());

        var removeItem = new ToolStripMenuItem("Remove");
        removeItem.Click += (_, _) => RemoveSelectedDocument();
        menu.Items.Add(removeItem);

        menu.Opening += (_, e) =>
        {
            UpdateSelectionFromGrid();
            if (_selectedDocument is null)
            {
                e.Cancel = true;
                return;
            }

            toggleImportantItem.Text = _selectedDocument.IsImportant
                ? "Remove important flag"
                : "Flag as important";
        };

        _grid.ContextMenuStrip = menu;
        _grid.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right)
                return;

            var hit = _grid.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0 || hit.RowIndex >= _grid.Rows.Count)
                return;

            _grid.ClearSelection();
            _grid.Rows[hit.RowIndex].Selected = true;
            if (hit.ColumnIndex >= 0)
                _grid.CurrentCell = _grid.Rows[hit.RowIndex].Cells[hit.ColumnIndex];
            else if (_grid.Rows[hit.RowIndex].Cells.Count > 0)
                _grid.CurrentCell = _grid.Rows[hit.RowIndex].Cells[0];

            UpdateSelectionFromGrid();
        };
    }

    private void ToggleSelectedDocumentImportant()
    {
        if (_repository is null || _selectedDocument is null)
            return;

        _selectedDocument.IsImportant = !_selectedDocument.IsImportant;
        _repository.Upsert(_selectedDocument);
        ReloadDocuments();
        ShowListPanel();
    }

    private void BeginAddDocument()
    {
        if (_repository is null)
        {
            MessageBox.Show("Open or create a case first.", "No active case", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _selectedDocument = new DocumentRecord { IsActive = true, Category = "Other", LinkedRecordType = "None" };
        LoadEditor(_selectedDocument, true);
        ShowEditPanel();
    }

    private void BeginEditSelectedDocument()
    {
        if (_selectedDocument is null)
        {
            MessageBox.Show("Select a document first.", "No document selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        LoadEditor(_selectedDocument, false);
        ShowEditPanel();
    }

    private void ShowSelectedProfile()
    {
        if (_selectedDocument is null)
        {
            MessageBox.Show("Select a document first.", "No document selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _profileTitleLabel.Text = _selectedDocument.Title;
        PopulateProfileDetails(_selectedDocument);
        ShowProfilePanel();
    }


    public bool OpenSearchResult(long recordId)
    {
        if (recordId <= 0)
            return false;

        var document = _documents.FirstOrDefault(d => d.Id == recordId);
        if (document is null && _repository is not null)
            document = _repository.GetAll().FirstOrDefault(d => d.Id == recordId);

        if (document is null)
            return false;

        _selectedDocument = document;
        SelectGridRowByDocumentId(recordId);
        _profileTitleLabel.Text = document.Title;
        PopulateProfileDetails(document);
        ShowProfilePanel();
        return true;
    }

    private void SelectGridRowByDocumentId(long recordId)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is DocumentRecord document && document.Id == recordId)
            {
                _grid.ClearSelection();
                row.Selected = true;
                if (row.Cells.Count > 0)
                    _grid.CurrentCell = row.Cells[0];
                break;
            }
        }
    }


    private void LoadEditor(DocumentRecord document, bool isNew)
    {
        _editTitleLabel.Text = isNew ? "Add Document" : "Edit Document";
        _titleTextBox.Text = document.Title;
        SetComboValue(_categoryComboBox, document.Category, "Other");
        _tagsTextBox.Text = document.Tags;
        SetComboValue(_linkedTypeComboBox, document.LinkedRecordType, "None");
        RefreshLinkedRecordChoices();
        SelectLinkedRecord(document.LinkedRecordId, document.LinkedRecordName);
        _selectedFileTextBox.Text = document.Id > 0 ? document.StoredFilePath : document.SourceFilePath;
        _activeCheckBox.Checked = document.IsActive;
        _notesTextBox.Text = document.Notes;
    }

    private void SaveDocumentFromEditor()
    {
        if (_repository is null || _selectedDocument is null)
            return;

        var title = _titleTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Enter a document title.", "Missing document title", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _titleTextBox.Focus();
            return;
        }

        _selectedDocument.Title = title;
        _selectedDocument.Category = _categoryComboBox.SelectedItem?.ToString() ?? "Other";
        _selectedDocument.Tags = _tagsTextBox.Text.Trim();
        _selectedDocument.LinkedRecordType = _linkedTypeComboBox.SelectedItem?.ToString() ?? "None";
        if (string.Equals(_selectedDocument.LinkedRecordType, "None", StringComparison.OrdinalIgnoreCase))
        {
            _selectedDocument.LinkedRecordId = 0;
            _selectedDocument.LinkedRecordName = string.Empty;
        }
        else if (_linkedRecordComboBox.SelectedItem is LinkedRecordChoice choice)
        {
            _selectedDocument.LinkedRecordId = choice.Id;
            _selectedDocument.LinkedRecordName = choice.Name;
        }

        var selectedPath = _selectedFileTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            try
            {
                var storedPath = CopyFileIntoCaseDocumentsFolder(selectedPath, title);
                _selectedDocument.SourceFilePath = selectedPath;
                _selectedDocument.StoredFilePath = storedPath;
                _selectedDocument.OriginalFileName = Path.GetFileName(selectedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not copy the selected file into the case documents folder:\n{ex.Message}", "Document copy failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        _selectedDocument.IsActive = _activeCheckBox.Checked;
        _selectedDocument.Notes = _notesTextBox.Text.Trim();
        _repository.Upsert(_selectedDocument);
        ReloadDocuments();
        ShowListPanel();
    }

    private void CancelEdit()
    {
        if (_selectedDocument is not null && _selectedDocument.Id > 0)
            ShowSelectedProfile();
        else
            ShowListPanel();
    }

    private void RemoveSelectedDocument()
    {
        if (_repository is null || _selectedDocument is null)
            return;

        var document = _selectedDocument;
        var result = MessageBox.Show(
            "Remove this document from Granny Manager?\n\nThis removes it from the app list only. The copied file will stay in the case documents folder.",
            "Remove document",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        var copiedFilePath = document.StoredFilePath;
        _repository.Delete(document.Id);

        if (!string.IsNullOrWhiteSpace(copiedFilePath) && System.IO.File.Exists(copiedFilePath))
        {
            var deleteFile = MessageBox.Show(
                "The document record was removed.\n\nDo you also want to delete the copied file from the case documents folder?",
                "Delete copied file?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (deleteFile == DialogResult.Yes)
            {
                try
                {
                    System.IO.File.Delete(copiedFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"The document record was removed, but the copied file could not be deleted:\n{ex.Message}", "File delete failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        _selectedDocument = null;
        ReloadDocuments();
        ShowListPanel();
    }

    private void BrowseForDocumentFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose Document",
            Filter = "All supported files|*.pdf;*.jpg;*.jpeg;*.png;*.txt;*.doc;*.docx;*.xls;*.xlsx;*.csv;*.rtf|PDF files (*.pdf)|*.pdf|Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _selectedFileTextBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(_titleTextBox.Text))
            _titleTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
    }

    private string CopyFileIntoCaseDocumentsFolder(string sourcePath, string title)
    {
        if (!System.IO.File.Exists(sourcePath))
        {
            if (!string.IsNullOrWhiteSpace(_selectedDocument?.StoredFilePath) && string.Equals(sourcePath, _selectedDocument.StoredFilePath, StringComparison.OrdinalIgnoreCase))
                return sourcePath;
            throw new FileNotFoundException("Selected file was not found.", sourcePath);
        }

        var documentsFolder = EnsureCaseDocumentCategoryFolder(_selectedDocument?.Category ?? _categoryComboBox.SelectedItem?.ToString() ?? "Other");
        var extension = Path.GetExtension(sourcePath);
        var baseName = SanitizeFileName(title);
        var target = Path.Combine(documentsFolder, baseName + extension);
        var counter = 2;

        var sourceFullPath = Path.GetFullPath(sourcePath);
        while (System.IO.File.Exists(target) && !string.Equals(sourceFullPath, Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
        {
            target = Path.Combine(documentsFolder, $"{baseName}_{counter}{extension}");
            counter++;
        }

        if (!string.Equals(sourceFullPath, Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            System.IO.File.Copy(sourcePath, target, overwrite: false);

        return target;
    }

    private string EnsureCaseDocumentFolder()
    {
        var fallback = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return fallback;
        var folder = Path.Combine(_caseFolder, "documents");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string EnsureCaseDocumentCategoryFolder(string category)
    {
        var root = EnsureCaseDocumentFolder();
        var folderName = SanitizeFolderName(string.IsNullOrWhiteSpace(category) ? "Other" : category);
        var folder = Path.Combine(root, folderName);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private void OpenSelectedDocumentFile()
    {
        if (_selectedDocument is null)
            return;
        var path = _selectedDocument.StoredFilePath;
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            MessageBox.Show("The copied document file could not be found.", "File missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open file:\n{ex.Message}", "Open failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void PopulateProfileDetails(DocumentRecord document)
    {
        _profileDetailsPanel.SuspendLayout();
        _profileDetailsPanel.Controls.Clear();

        AddProfileSection("Document Details");
        AddProfileRow("Title", document.Title);
        AddProfileRow("Category", document.Category);
        AddProfileRow("Tags / keywords", document.Tags);
        AddProfileRow("Linked to", document.LinkedDisplay);
        AddProfileRow("Important", document.IsImportant ? "Yes" : "No");
        AddProfileRow("Active", document.IsActive ? "Yes" : "No");

        AddProfileSpacer();
        AddProfileSection("File");
        AddProfileRow("Original file", document.OriginalFileName);
        AddProfileRow("Copied case file", document.StoredFilePath);

        AddProfileSpacer();
        AddProfileSection("Notes");
        AddProfileParagraph(string.IsNullOrWhiteSpace(document.Notes) ? "None" : document.Notes);

        AddProfileSpacer();
        AddProfileSection("Record Info");
        AddProfileRow("Created UTC", document.CreatedUtc.ToString("yyyy-MM-dd HH:mm"));
        AddProfileRow("Updated UTC", document.UpdatedUtc.ToString("yyyy-MM-dd HH:mm"));

        _profileDetailsPanel.ResumeLayout();
        UpdateProfileContentWidth();
    }

    private void RefreshLinkedRecordChoices()
    {
        if (_linkedRecordPanel is null || _linkedRecordComboBox is null)
            return;

        var type = _linkedTypeComboBox.SelectedItem?.ToString() ?? "None";
        _linkedChoices.Clear();
        _linkedRecordComboBox.Items.Clear();

        if (string.Equals(type, "None", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(_caseFolder))
        {
            _linkedRecordPanel.Visible = false;
            return;
        }

        var databasePath = CaseDatabaseLocator.GetDatabasePathForCaseFolder(_caseFolder);
        try
        {
            switch (type)
            {
                case "People":
                    foreach (var p in new HouseholdPeopleRepository(databasePath).GetAll())
                        AddLinkedChoice(p.Id, p.FullName);
                    break;
                case "Income Sources":
                    foreach (var i in new IncomeSourcesRepository(databasePath).GetAll())
                        AddLinkedChoice(i.Id, i.SourceName);
                    break;
                case "Bills / Spending":
                    foreach (var b in new BillsRepository(databasePath).GetAll())
                        AddLinkedChoice(b.Id, b.BillName);
                    break;
                case "Allowance / Savings":
                    foreach (var a in new AllowanceSavingsRepository(databasePath).GetAll())
                        AddLinkedChoice(a.Id, a.ItemName);
                    break;
                case "Assets":
                    foreach (var a in new AssetsRepository(databasePath).GetAll())
                        AddLinkedChoice(a.Id, a.AssetName);
                    break;
                case "Debts":
                    foreach (var d in new DebtsRepository(databasePath).GetAll())
                        AddLinkedChoice(d.Id, d.DebtName);
                    break;
            }
        }
        catch { }

        foreach (var choice in _linkedChoices)
            _linkedRecordComboBox.Items.Add(choice);
        _linkedRecordComboBox.DisplayMember = nameof(LinkedRecordChoice.Name);
        if (_linkedRecordComboBox.Items.Count > 0)
            _linkedRecordComboBox.SelectedIndex = 0;
        _linkedRecordPanel.Visible = _linkedRecordComboBox.Items.Count > 0;
    }

    private void AddLinkedChoice(long id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        _linkedChoices.Add(new LinkedRecordChoice(id, name));
    }

    private void SelectLinkedRecord(long id, string name)
    {
        foreach (var item in _linkedRecordComboBox.Items)
        {
            if (item is LinkedRecordChoice choice && (choice.Id == id || string.Equals(choice.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                _linkedRecordComboBox.SelectedItem = choice;
                return;
            }
        }
    }

    private void ExportCurrentDocumentPdf()
    {
        if (_selectedDocument is null || _selectedDocument.Id <= 0)
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Export Document Profile PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = SanitizeFileName(_selectedDocument.Title) + "_Document_Profile.pdf",
            InitialDirectory = GetDefaultExportFolder(),
            AddExtension = true,
            DefaultExt = "pdf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WriteDocumentProfile(dialog.FileName, _selectedDocument);
            MessageBox.Show("Document profile PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not export PDF:\n{ex.Message}", "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportDocumentIndexPdf()
    {
        if (_documents.Count == 0)
        {
            MessageBox.Show("There are no documents to export.", "No documents", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export Document Index PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = "Document_Index.pdf",
            InitialDirectory = GetDefaultExportFolder(),
            AddExtension = true,
            DefaultExt = "pdf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            MinimalPdfWriter.WriteDocumentIndex(dialog.FileName, _documents.ToList());
            MessageBox.Show("Document index PDF exported.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        var right = new Label { Text = string.IsNullOrWhiteSpace(value) ? "None" : value.Trim(), Location = new Point(220, 0), Size = new Size(680, 24), ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(left);
        panel.Controls.Add(right);
        _profileDetailsPanel.Controls.Add(panel);
    }

    private void AddProfileParagraph(string text)
    {
        var width = Math.Max(600, _profilePanel.ClientSize.Width - 60);
        var label = new Label { Text = text, AutoSize = false, Width = width, Height = 90, ForeColor = TextPrimary, Font = new Font("Segoe UI", 9.5f), Margin = new Padding(0, 0, 0, 4) };
        _profileDetailsPanel.Controls.Add(label);
    }

    private void AddFillTextColumn(string propertyName, string headerText, float fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = propertyName, HeaderText = headerText, FillWeight = fillWeight, MinimumWidth = 100 });
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

    private Panel AddLinkedRecordField(FlowLayoutPanel parent)
    {
        var panel = MakeFieldPanel("Linked record", 430);
        panel.Tag = "LinkedRecordPanel";
        _linkedRecordComboBox = new ComboBox { Width = 430, Height = 26, Location = new Point(0, 22), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Back, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f) };
        panel.Controls.Add(_linkedRecordComboBox);
        panel.Visible = false;
        parent.Controls.Add(panel);
        return panel;
    }

    private (TextBox TextBox, Button Button) AddFilePickerField(FlowLayoutPanel parent, string labelText, int width)
    {
        var panel = MakeFieldPanel(labelText, width, 60);
        var textBox = new TextBox { Width = width - 120, Height = 26, Location = new Point(0, 22), BackColor = Back, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), ReadOnly = true };
        var button = CreateButton("Browse", width - 110, 17, 100);
        button.Height = 32;
        panel.Controls.Add(textBox);
        panel.Controls.Add(button);
        parent.Controls.Add(panel);
        return (textBox, button);
    }

    private Panel MakeFieldPanel(string labelText, int width, int height = 56)
    {
        var panel = new Panel { Width = 760, Height = height, BackColor = Back, Margin = new Padding(0, 0, 0, 8) };
        panel.Controls.Add(new Label { Text = labelText, Location = new Point(0, 0), Size = new Size(width, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 9.5f) });
        return panel;
    }

    private static void SetComboValue(ComboBox combo, string value, string fallback)
    {
        if (string.Equals(value, "Tax", StringComparison.OrdinalIgnoreCase))
            value = "Taxes";

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

    private string GetDefaultExportFolder()
    {
        var fallback = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrWhiteSpace(_caseFolder))
            return fallback;
        try { var folder = Path.Combine(_caseFolder, "exports"); Directory.CreateDirectory(folder); return folder; }
        catch { return fallback; }
    }

    private Button CreateButton(string text, int x, int y, int width) => new() { Text = text, Location = new Point(x, y), Size = new Size(width, 38), Anchor = AnchorStyles.Left | AnchorStyles.Top, BackColor = Panel2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
    private static string SanitizeFileName(string value) { var invalid = Path.GetInvalidFileNameChars(); var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()); return string.IsNullOrWhiteSpace(safe) ? "Document" : safe.Trim(); }
    private static string SanitizeFolderName(string value) { var invalid = Path.GetInvalidFileNameChars(); var safe = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim(); return string.IsNullOrWhiteSpace(safe) ? "Other" : safe; }

    private sealed record LinkedRecordChoice(long Id, string Name)
    {
        public override string ToString() => Name;
    }

    private static class MinimalPdfWriter
    {
        public static void WriteDocumentProfile(string path, DocumentRecord document)
        {
            var lines = new List<string>
            {
                "Family Finance & Trust Manager",
                "Document Profile",
                "",
                document.Title,
                "Exported: " + DateTime.Now.ToString("yyyy-MM-dd h:mm tt"),
                "",
                "Document Details",
                $"Title: {Clean(document.Title)}",
                $"Category: {Clean(document.Category)}",
                $"Tags / keywords: {Clean(document.Tags)}",
                $"Linked to: {Clean(document.LinkedDisplay)}",
                $"Active: {(document.IsActive ? "Yes" : "No")}",
                "",
                "File",
                $"Original file: {Clean(document.OriginalFileName)}",
                $"Copied case file: {Clean(document.StoredFilePath)}",
                "",
                "Notes",
                string.IsNullOrWhiteSpace(document.Notes) ? "None" : document.Notes
            };
            WriteSimplePdf(path, lines);
        }

        public static void WriteDocumentIndex(string path, IReadOnlyList<DocumentRecord> documents)
        {
            var lines = new List<string>
            {
                "Family Finance & Trust Manager",
                "Document Index",
                "Exported: " + DateTime.Now.ToString("yyyy-MM-dd h:mm tt"),
                ""
            };
            foreach (var doc in documents.Take(36))
                lines.Add($"{Clean(doc.Category)} - {Clean(doc.Title)} - Tags: {Clean(doc.Tags)}");
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
            System.IO.File.WriteAllBytes(path, Encoding.ASCII.GetBytes(pdf.ToString()));
        }

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
