using System;
using System.Collections.Generic;
using Avalonia.Controls;
using GrannyManager.Application.Services;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class DocumentEditDialog : Window
    {
        private readonly TextBox? _displayNameTextBox;
        private readonly ComboBox? _personComboBox;
        private readonly ComboBox? _categoryComboBox;
        private readonly ComboBox? _linkedRecordComboBox;
        private readonly TextBox? _customFolderTextBox;
        private readonly TextBox? _tagsTextBox;
        private readonly TextBox? _notesTextBox;
        private readonly TextBlock? _validationTextBlock;

        private Func<string, IReadOnlyList<DocumentConnectionOption>>? _loadConnectionOptions;
        private DocumentRecord _document = new();

        public DocumentEditDialog()
        {
            InitializeComponent();

            _displayNameTextBox = this.FindControl<TextBox>("DisplayNameTextBox");
            _personComboBox = this.FindControl<ComboBox>("PersonComboBox");
            _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
            _linkedRecordComboBox = this.FindControl<ComboBox>("LinkedRecordComboBox");
            _customFolderTextBox = this.FindControl<TextBox>("CustomFolderTextBox");
            _tagsTextBox = this.FindControl<TextBox>("TagsTextBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            if (_categoryComboBox is not null)
                _categoryComboBox.SelectionChanged += (_, _) => RefreshLinkedRecords();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public DocumentEditRequest Request { get; private set; } = new();

        public void SetMode(
            DocumentRecord document,
            IReadOnlyList<string> people,
            Func<string, IReadOnlyList<DocumentConnectionOption>> loadConnectionOptions)
        {
            _document = document ?? new DocumentRecord();
            _loadConnectionOptions = loadConnectionOptions;

            if (_displayNameTextBox is not null)
                _displayNameTextBox.Text = _document.DisplayName;

            if (_personComboBox is not null)
            {
                _personComboBox.Items.Clear();
                foreach (var person in people)
                    _personComboBox.Items.Add(new ComboBoxItem { Content = person });

                if (_personComboBox.ItemCount == 0)
                    _personComboBox.Items.Add(new ComboBoxItem { Content = "General" });

                SelectComboValue(_personComboBox, string.IsNullOrWhiteSpace(_document.PersonName) ? "General" : _document.PersonName);
            }

            SelectComboValue(_categoryComboBox, string.IsNullOrWhiteSpace(_document.Category) ? "Other" : _document.Category);

            if (_customFolderTextBox is not null)
                _customFolderTextBox.Text = _document.CustomFolder;

            if (_tagsTextBox is not null)
                _tagsTextBox.Text = _document.Tags;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _document.Notes;

            RefreshLinkedRecords();
        }

        private void RefreshLinkedRecords()
        {
            if (_linkedRecordComboBox is null)
                return;

            var selectedId = _document.LinkedRecordId;

            _linkedRecordComboBox.Items.Clear();
            _linkedRecordComboBox.Items.Add(new ComboBoxItem { Content = "No linked record", Tag = null });

            var category = GetComboValue(_categoryComboBox, "Other");
            if (_loadConnectionOptions is not null)
            {
                foreach (var option in _loadConnectionOptions(category))
                    _linkedRecordComboBox.Items.Add(new ComboBoxItem { Content = option.Name, Tag = option });
            }

            _linkedRecordComboBox.SelectedIndex = 0;

            for (var index = 1; index < _linkedRecordComboBox.ItemCount; index++)
            {
                if (_linkedRecordComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag is DocumentConnectionOption option &&
                    option.Id == selectedId)
                {
                    _linkedRecordComboBox.SelectedIndex = index;
                    break;
                }
            }
        }

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var displayName = _displayNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a display name before saving.";
                return;
            }

            var linkedOption = (_linkedRecordComboBox?.SelectedItem as ComboBoxItem)?.Tag as DocumentConnectionOption;
            var category = GetComboValue(_categoryComboBox, "Other");

            Request = new DocumentEditRequest
            {
                DisplayName = displayName,
                PersonName = GetComboValue(_personComboBox, "General"),
                Category = category,
                LinkedSection = linkedOption?.Section ?? category,
                LinkedRecordId = linkedOption?.Id ?? 0,
                LinkedRecordName = linkedOption?.Name ?? string.Empty,
                CustomFolder = _customFolderTextBox?.Text?.Trim() ?? string.Empty,
                Tags = _tagsTextBox?.Text?.Trim() ?? string.Empty,
                Notes = _notesTextBox?.Text?.Trim() ?? string.Empty
            };

            Close(true);
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
