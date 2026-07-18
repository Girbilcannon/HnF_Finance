using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GrannyManager.Application.Services;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class DocumentImportDialog : Window
    {
        private readonly List<string> _selectedFiles = new();
        private Func<string, IReadOnlyList<DocumentConnectionOption>>? _loadConnectionOptions;
        private Func<DocumentImportRequest, bool>? _folderExists;

        private readonly ListBox? _selectedFilesListBox;
        private readonly ComboBox? _importModeComboBox;
        private readonly TextBox? _mergedFileNameTextBox;
        private readonly ComboBox? _personComboBox;
        private readonly ComboBox? _categoryComboBox;
        private readonly ComboBox? _linkedRecordComboBox;
        private readonly TextBox? _customFolderTextBox;
        private readonly TextBox? _tagsTextBox;
        private readonly TextBox? _notesTextBox;
        private readonly CheckBox? _passwordProtectCheckBox;
        private readonly TextBox? _passwordTextBox;
        private readonly TextBox? _confirmPasswordTextBox;
        private readonly TextBlock? _validationTextBlock;

        public DocumentImportDialog()
        {
            InitializeComponent();

            _selectedFilesListBox = this.FindControl<ListBox>("SelectedFilesListBox");
            _importModeComboBox = this.FindControl<ComboBox>("ImportModeComboBox");
            _mergedFileNameTextBox = this.FindControl<TextBox>("MergedFileNameTextBox");
            _personComboBox = this.FindControl<ComboBox>("PersonComboBox");
            _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
            _linkedRecordComboBox = this.FindControl<ComboBox>("LinkedRecordComboBox");
            _customFolderTextBox = this.FindControl<TextBox>("CustomFolderTextBox");
            _tagsTextBox = this.FindControl<TextBox>("TagsTextBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");
            _passwordProtectCheckBox = this.FindControl<CheckBox>("PasswordProtectCheckBox");
            _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            _confirmPasswordTextBox = this.FindControl<TextBox>("ConfirmPasswordTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            if (_categoryComboBox is not null)
                _categoryComboBox.SelectionChanged += (_, _) => RefreshLinkedRecords();

            if (_importModeComboBox is not null)
                _importModeComboBox.SelectionChanged += (_, _) => RefreshImportModeVisibility();

            if (_passwordProtectCheckBox is not null)
                _passwordProtectCheckBox.IsCheckedChanged += (_, _) => RefreshPasswordVisibility();

            var chooseButton = this.FindControl<Button>("ChooseFilesButton");
            if (chooseButton is not null)
                chooseButton.Click += async (_, _) => await ChooseFilesAsync();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var importButton = this.FindControl<Button>("ImportButton");
            if (importButton is not null)
                importButton.Click += async (_, _) => await ImportAsync();

            SelectComboValue(_importModeComboBox, "Import Individually");
            SelectComboValue(_categoryComboBox, "Other");
            RefreshImportModeVisibility();
            RefreshPasswordVisibility();
        }

        public DocumentImportRequest Request { get; private set; } = new();

        public void SetMode(
            IReadOnlyList<string> people,
            Func<string, IReadOnlyList<DocumentConnectionOption>> loadConnectionOptions,
            Func<DocumentImportRequest, bool> folderExists)
        {
            _loadConnectionOptions = loadConnectionOptions;
            _folderExists = folderExists;

            if (_personComboBox is not null)
            {
                _personComboBox.Items.Clear();

                foreach (var person in people)
                    _personComboBox.Items.Add(new ComboBoxItem { Content = person });

                if (_personComboBox.ItemCount == 0)
                    _personComboBox.Items.Add(new ComboBoxItem { Content = "General" });

                _personComboBox.SelectedIndex = 0;
            }

            RefreshLinkedRecords();
        }

        private void RefreshImportModeVisibility()
        {
            var mergeSelected = GetComboValue(_importModeComboBox, "Import Individually") == "Merge into Single PDF";

            if (_mergedFileNameTextBox is not null)
                _mergedFileNameTextBox.IsEnabled = mergeSelected;
        }

        private void RefreshPasswordVisibility()
        {
            var enabled = _passwordProtectCheckBox?.IsChecked == true;

            if (_passwordTextBox is not null)
                _passwordTextBox.IsEnabled = enabled;

            if (_confirmPasswordTextBox is not null)
                _confirmPasswordTextBox.IsEnabled = enabled;
        }

        private async System.Threading.Tasks.Task ChooseFilesAsync()
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose documents to import",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Documents")
                    {
                        Patterns = new[] { "*.pdf", "*.jpg", "*.jpeg", "*.png", "*.doc", "*.docx", "*.txt" }
                    },
                    FilePickerFileTypes.All
                }
            });

            foreach (var file in files)
            {
                if (file.Path.LocalPath is { Length: > 0 } path && !_selectedFiles.Contains(path))
                    _selectedFiles.Add(path);
            }

            RefreshSelectedFiles();
        }

        private void RefreshSelectedFiles()
        {
            if (_selectedFilesListBox is null)
                return;

            _selectedFilesListBox.Items.Clear();

            foreach (var file in _selectedFiles)
                _selectedFilesListBox.Items.Add(Path.GetFileName(file));
        }

        private void RefreshLinkedRecords()
        {
            if (_linkedRecordComboBox is null)
                return;

            _linkedRecordComboBox.Items.Clear();
            _linkedRecordComboBox.Items.Add(new ComboBoxItem { Content = "No linked record", Tag = null });

            var category = GetComboValue(_categoryComboBox, "Other");
            if (_loadConnectionOptions is not null)
            {
                foreach (var option in _loadConnectionOptions(category))
                    _linkedRecordComboBox.Items.Add(new ComboBoxItem { Content = option.Name, Tag = option });
            }

            _linkedRecordComboBox.SelectedIndex = 0;
        }

        private async System.Threading.Tasks.Task ImportAsync()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            if (_selectedFiles.Count == 0)
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Choose at least one document to import.";
                return;
            }

            var request = BuildRequest(useExistingFolderWhenConflict: true);

            var needsPdfProcessing = request.ImportMode == "Merge into Single PDF" || request.PasswordProtectRequested;
            if (needsPdfProcessing && request.SourceFilePaths.Any(path => !string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase)))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "PDF merge and password protection only work with PDF files. Remove non-PDF files or turn off PDF processing.";
                return;
            }

            if (request.ImportMode == "Merge into Single PDF" && string.IsNullOrWhiteSpace(request.MergedFileName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a merged PDF file name.";
                return;
            }

            if (request.PasswordProtectRequested && string.IsNullOrWhiteSpace(request.Password))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a password for protected PDFs.";
                return;
            }

            if (request.PasswordProtectRequested && !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Document password and confirmation do not match.";
                return;
            }

            if (_folderExists is not null && _folderExists(request))
            {
                var owner = this.Owner as Window;
                if (owner is null)
                    return;

                var conflictDialog = new ExistingFolderChoiceDialog();
                conflictDialog.SetFolderName(request.CustomFolder);

                var result = await conflictDialog.ShowDialog<bool>(owner);
                if (!result)
                    return;

                request = BuildRequest(conflictDialog.AddToExistingFolder);
            }

            Request = request;
            Close(true);
        }

        private DocumentImportRequest BuildRequest(bool useExistingFolderWhenConflict)
        {
            var linkedOption = (_linkedRecordComboBox?.SelectedItem as ComboBoxItem)?.Tag as DocumentConnectionOption;
            var category = GetComboValue(_categoryComboBox, "Other");

            return new DocumentImportRequest
            {
                SourceFilePaths = _selectedFiles.ToList(),
                ImportMode = GetComboValue(_importModeComboBox, "Import Individually"),
                MergedFileName = _mergedFileNameTextBox?.Text?.Trim() ?? string.Empty,
                PersonName = GetComboValue(_personComboBox, "General"),
                Category = category,
                LinkedSection = linkedOption?.Section ?? category,
                LinkedRecordId = linkedOption?.Id ?? 0,
                LinkedRecordName = linkedOption?.Name ?? string.Empty,
                CustomFolder = _customFolderTextBox?.Text?.Trim() ?? string.Empty,
                Tags = _tagsTextBox?.Text?.Trim() ?? string.Empty,
                Notes = _notesTextBox?.Text?.Trim() ?? string.Empty,
                PasswordProtectRequested = _passwordProtectCheckBox?.IsChecked == true,
                Password = _passwordTextBox?.Text ?? string.Empty,
                ConfirmPassword = _confirmPasswordTextBox?.Text ?? string.Empty,
                UseExistingFolderWhenConflict = useExistingFolderWhenConflict
            };
        }

        private static void SelectComboValue(ComboBox? comboBox, string value)
        {
            if (comboBox is null)
                return;

            for (var index = 0; index < comboBox.ItemCount; index++)
            {
                if (comboBox.Items[index] is ComboBoxItem item &&
                    string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private static string GetComboValue(ComboBox? comboBox, string fallback)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? fallback;

            return fallback;
        }
    }
}
