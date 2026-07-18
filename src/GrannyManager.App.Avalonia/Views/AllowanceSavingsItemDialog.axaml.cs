using System;
using System.Collections.Generic;
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
        private readonly ComboBox? _bankAccountComboBox;
        private readonly Button? _createBankAccountButton;
        private readonly TextBox? _otherStorageMethodTextBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private readonly List<AssetItem> _bankAccounts = new();
        private Func<AssetItem>? _createBlankBankAccount;
        private Func<AssetItem, bool>? _saveBankAccount;
        private Func<IReadOnlyList<AssetItem>>? _reloadBankAccounts;
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
            _bankAccountComboBox = this.FindControl<ComboBox>("BankAccountComboBox");
            _createBankAccountButton = this.FindControl<Button>("CreateBankAccountButton");
            _otherStorageMethodTextBox = this.FindControl<TextBox>("OtherStorageMethodTextBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_storageMethodComboBox is not null)
                _storageMethodComboBox.SelectionChanged += (_, _) => RefreshStorageVisibility();

            if (_createBankAccountButton is not null)
                _createBankAccountButton.Click += async (_, _) => await CreateBankAccountAsync();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public AllowanceSavingsItem Item => _item;

        public void SetMode(
            string title,
            AllowanceSavingsItem item,
            IReadOnlyList<AssetItem>? bankAccounts = null,
            Func<AssetItem>? createBlankBankAccount = null,
            Func<AssetItem, bool>? saveBankAccount = null,
            Func<IReadOnlyList<AssetItem>>? reloadBankAccounts = null)
        {
            _item = item ?? new AllowanceSavingsItem();

            _bankAccounts.Clear();
            if (bankAccounts is not null)
                _bankAccounts.AddRange(bankAccounts);

            _createBlankBankAccount = createBlankBankAccount;
            _saveBankAccount = saveBankAccount;
            _reloadBankAccounts = reloadBankAccounts;

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

            PopulateBankAccounts(_item.LinkedBankAssetId);

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _item.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _item.Notes;

            RefreshStorageVisibility();
        }

        private async System.Threading.Tasks.Task CreateBankAccountAsync()
        {
            if (_createBlankBankAccount is null || _saveBankAccount is null)
                return;

            var owner = this.Owner as Window;
            if (owner is null)
                return;

            var asset = _createBlankBankAccount();
            var dialog = new AssetItemDialog();
            dialog.SetMode("Create Bank Account", asset);

            var result = await dialog.ShowDialog<bool>(owner);
            if (!result)
                return;

            if (!_saveBankAccount(dialog.Asset))
                return;

            _bankAccounts.Clear();
            if (_reloadBankAccounts is not null)
                _bankAccounts.AddRange(_reloadBankAccounts());

            PopulateBankAccounts(dialog.Asset.Id);
            SelectComboValue(_storageMethodComboBox, "Bank Account");
            RefreshStorageVisibility();
        }

        private void PopulateBankAccounts(long selectedId)
        {
            if (_bankAccountComboBox is null)
                return;

            _bankAccountComboBox.Items.Clear();
            _bankAccountComboBox.Items.Add(new ComboBoxItem { Content = "Choose account", Tag = 0L });

            foreach (var account in _bankAccounts)
                _bankAccountComboBox.Items.Add(new ComboBoxItem { Content = account.DisplayName, Tag = account.Id });

            _bankAccountComboBox.SelectedIndex = 0;
            for (var index = 1; index < _bankAccountComboBox.ItemCount; index++)
            {
                if (_bankAccountComboBox.Items[index] is ComboBoxItem item && item.Tag is long id && id == selectedId)
                {
                    _bankAccountComboBox.SelectedIndex = index;
                    break;
                }
            }
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

            _item.LinkedBankAssetId = 0;
            _item.LinkedBankAssetName = string.Empty;
            if (storageMethod == "Bank Account" && _bankAccountComboBox?.SelectedItem is ComboBoxItem selectedAccount)
            {
                if (selectedAccount.Tag is long bankId)
                    _item.LinkedBankAssetId = bankId;

                var selectedName = selectedAccount.Content?.ToString() ?? string.Empty;
                if (_item.LinkedBankAssetId > 0)
                    _item.LinkedBankAssetName = selectedName;
            }

            _item.IsActive = _isActiveCheckBox?.IsChecked == true;
            _item.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

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
