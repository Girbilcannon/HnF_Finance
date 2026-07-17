using System;
using Avalonia.Controls;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class AllowanceSavingsItemDialog : Window
    {
        private readonly TextBlock? _dialogTitleTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly TextBox? _itemNameTextBox;
        private readonly ComboBox? _itemTypeComboBox;
        private readonly ComboBox? _frequencyComboBox;
        private readonly TextBox? _amountTextBox;
        private readonly TextBox? _whereStoredTextBox;
        private readonly ComboBox? _storageMethodComboBox;
        private readonly StackPanel? _bankAccountPanel;
        private readonly StackPanel? _multipleBankAccountsPanel;
        private readonly StackPanel? _otherStoragePanel;
        private readonly TextBox? _otherStorageMethodTextBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private AllowanceSavingsItem _item = new();

        public AllowanceSavingsItemDialog()
        {
            InitializeComponent();

            _dialogTitleTextBlock = this.FindControl<TextBlock>("DialogTitleTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _itemNameTextBox = this.FindControl<TextBox>("ItemNameTextBox");
            _itemTypeComboBox = this.FindControl<ComboBox>("ItemTypeComboBox");
            _frequencyComboBox = this.FindControl<ComboBox>("FrequencyComboBox");
            _amountTextBox = this.FindControl<TextBox>("AmountTextBox");
            _whereStoredTextBox = this.FindControl<TextBox>("WhereStoredTextBox");
            _storageMethodComboBox = this.FindControl<ComboBox>("StorageMethodComboBox");
            _bankAccountPanel = this.FindControl<StackPanel>("BankAccountPanel");
            _multipleBankAccountsPanel = this.FindControl<StackPanel>("MultipleBankAccountsPanel");
            _otherStoragePanel = this.FindControl<StackPanel>("OtherStoragePanel");
            _otherStorageMethodTextBox = this.FindControl<TextBox>("OtherStorageMethodTextBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_storageMethodComboBox is not null)
                _storageMethodComboBox.SelectionChanged += (_, _) => RefreshStorageVisibility();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public AllowanceSavingsItem Item => _item;

        public void SetMode(string title, AllowanceSavingsItem item)
        {
            _item = item ?? new AllowanceSavingsItem();

            if (_dialogTitleTextBlock is not null)
                _dialogTitleTextBlock.Text = title;

            if (_itemNameTextBox is not null)
                _itemNameTextBox.Text = _item.ItemName;

            SelectComboValue(_itemTypeComboBox, string.IsNullOrWhiteSpace(_item.ItemType) ? "Allowance" : _item.ItemType);
            SelectComboValue(_frequencyComboBox, string.IsNullOrWhiteSpace(_item.Frequency) ? "Monthly" : _item.Frequency);

            if (_amountTextBox is not null)
                _amountTextBox.Text = _item.Amount <= 0 ? string.Empty : _item.Amount.ToString("0.##");

            if (_whereStoredTextBox is not null)
                _whereStoredTextBox.Text = _item.WhereStored;

            var storageMethod = string.IsNullOrWhiteSpace(_item.StorageMethod) ? "Cash / Envelope" : _item.StorageMethod;
            if (storageMethod.StartsWith("Other:", StringComparison.OrdinalIgnoreCase))
            {
                SelectComboValue(_storageMethodComboBox, "Other");

                if (_otherStorageMethodTextBox is not null)
                    _otherStorageMethodTextBox.Text = storageMethod.Substring("Other:".Length).Trim();
            }
            else
            {
                SelectComboValue(_storageMethodComboBox, storageMethod);
            }

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _item.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _item.Notes;

            RefreshStorageVisibility();
        }

        private void RefreshStorageVisibility()
        {
            var method = GetComboValue(_storageMethodComboBox, "Cash / Envelope");

            if (_bankAccountPanel is not null)
                _bankAccountPanel.IsVisible = method == "Bank Account";

            if (_multipleBankAccountsPanel is not null)
                _multipleBankAccountsPanel.IsVisible = method == "Multiple Bank Accounts";

            if (_otherStoragePanel is not null)
                _otherStoragePanel.IsVisible = method == "Other";
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

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var itemName = _itemNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a name before saving.";
                return;
            }

            var amount = 0m;
            var amountText = _amountTextBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(amountText) && !decimal.TryParse(amountText, out amount))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Amount must be a valid number.";
                return;
            }

            var storageMethod = GetComboValue(_storageMethodComboBox, "Cash / Envelope");
            if (storageMethod == "Other")
            {
                var other = _otherStorageMethodTextBox?.Text?.Trim() ?? string.Empty;
                storageMethod = string.IsNullOrWhiteSpace(other) ? "Other" : $"Other: {other}";
            }

            _item.ItemName = itemName;
            _item.ItemType = GetComboValue(_itemTypeComboBox, "Allowance");
            _item.Amount = amount;
            _item.Frequency = GetComboValue(_frequencyComboBox, "Monthly");
            _item.WhereStored = _whereStoredTextBox?.Text?.Trim() ?? string.Empty;
            _item.StorageMethod = storageMethod;

            if (storageMethod != "Bank Account")
            {
                _item.LinkedBankAssetId = 0;
                _item.LinkedBankAssetName = string.Empty;
            }

            _item.IsActive = _isActiveCheckBox?.IsChecked == true;
            _item.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
        }
    }
}
