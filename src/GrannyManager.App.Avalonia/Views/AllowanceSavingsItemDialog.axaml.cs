using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
        private readonly StackPanel? _whereStoredPanel;
        private readonly ComboBox? _storageMethodComboBox;
        private readonly StackPanel? _bankAccountPanel;
        private readonly StackPanel? _multipleBankAccountsPanel;
        private readonly StackPanel? _otherStoragePanel;
        private readonly ComboBox? _bankAccountComboBox;
        private readonly Button? _createBankAccountButton;
        private readonly Button? _addBankAccountLineButton;
        private readonly Button? _createBankAccountForMultipleButton;
        private readonly StackPanel? _multipleBankAccountsHost;
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
            _whereStoredPanel = this.FindControl<StackPanel>("WhereStoredPanel");
            _storageMethodComboBox = this.FindControl<ComboBox>("StorageMethodComboBox");
            _bankAccountPanel = this.FindControl<StackPanel>("BankAccountPanel");
            _multipleBankAccountsPanel = this.FindControl<StackPanel>("MultipleBankAccountsPanel");
            _otherStoragePanel = this.FindControl<StackPanel>("OtherStoragePanel");
            _bankAccountComboBox = this.FindControl<ComboBox>("BankAccountComboBox");
            _createBankAccountButton = this.FindControl<Button>("CreateBankAccountButton");
            _addBankAccountLineButton = this.FindControl<Button>("AddBankAccountLineButton");
            _createBankAccountForMultipleButton = this.FindControl<Button>("CreateBankAccountForMultipleButton");
            _multipleBankAccountsHost = this.FindControl<StackPanel>("MultipleBankAccountsHost");
            _otherStorageMethodTextBox = this.FindControl<TextBox>("OtherStorageMethodTextBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_storageMethodComboBox is not null)
                _storageMethodComboBox.SelectionChanged += (_, _) => RefreshStorageVisibility();

            if (_createBankAccountButton is not null)
                _createBankAccountButton.Click += async (_, _) => await CreateBankAccountAsync(selectMultiple: false);

            if (_addBankAccountLineButton is not null)
                _addBankAccountLineButton.Click += (_, _) => AddMultipleBankAccountLine();

            if (_createBankAccountForMultipleButton is not null)
                _createBankAccountForMultipleButton.Click += async (_, _) =>
                {
                    var createdId = await CreateBankAccountAsync(selectMultiple: true);
                    SelectComboValue(_storageMethodComboBox, "Select Multiple Bank Accounts");
                    RefreshStorageVisibility();
                    if (createdId > 0)
                        AddMultipleBankAccountLine(createdId);
                };

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

            var storageMethod = NormalizeStorageMethodForUi(_item.StorageMethod);
            if (storageMethod == "Other")
            {
                SelectComboValue(_storageMethodComboBox, "Other");
                if (_otherStorageMethodTextBox is not null)
                    _otherStorageMethodTextBox.Text = ExtractOtherStorageText(_item.StorageMethod, _item.WhereStored);
            }
            else
            {
                SelectComboValue(_storageMethodComboBox, storageMethod);
            }

            PopulateBankAccounts(_item.LinkedBankAssetId);
            PopulateMultipleBankAccountsFromSource();

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _item.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _item.Notes;

            RefreshStorageVisibility();
        }

        private async System.Threading.Tasks.Task<long> CreateBankAccountAsync(bool selectMultiple)
        {
            if (_createBlankBankAccount is null || _saveBankAccount is null)
                return 0;

            var owner = this.Owner as Window;
            if (owner is null)
                return 0;

            var asset = _createBlankBankAccount();
            var dialog = new AssetItemDialog();
            dialog.SetMode("Create Bank Account", asset);

            var result = await dialog.ShowDialog<bool>(owner);
            if (!result)
                return 0;

            if (!_saveBankAccount(dialog.Asset))
                return 0;

            _bankAccounts.Clear();
            if (_reloadBankAccounts is not null)
                _bankAccounts.AddRange(_reloadBankAccounts());

            PopulateBankAccounts(dialog.Asset.Id);

            if (!selectMultiple)
            {
                SelectComboValue(_storageMethodComboBox, "Select Bank Account");
                RefreshStorageVisibility();
            }

            return dialog.Asset.Id;
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
            var method = GetComboValue(_storageMethodComboBox, "Cash/Check");

            if (_whereStoredPanel is not null)
                _whereStoredPanel.IsVisible = method == "Cash/Check";

            if (_bankAccountPanel is not null)
                _bankAccountPanel.IsVisible = method == "Select Bank Account";

            if (_multipleBankAccountsPanel is not null)
                _multipleBankAccountsPanel.IsVisible = method == "Select Multiple Bank Accounts";

            if (_otherStoragePanel is not null)
                _otherStoragePanel.IsVisible = method == "Other";

            if (_amountTextBox is not null)
                _amountTextBox.IsEnabled = method != "Select Multiple Bank Accounts";

            if (method == "Select Multiple Bank Accounts" &&
                _multipleBankAccountsHost is not null &&
                _multipleBankAccountsHost.Children.Count == 0)
            {
                AddMultipleBankAccountLine();
            }
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

            var storageMethod = GetComboValue(_storageMethodComboBox, "Cash/Check");
            var amount = 0m;

            if (storageMethod != "Select Multiple Bank Accounts")
            {
                var amountText = _amountTextBox?.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(amountText) && !decimal.TryParse(amountText, out amount))
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Amount must be a valid number.";
                    return;
                }
            }

            _item.ItemName = itemName;
            _item.ItemType = GetComboValue(_itemTypeComboBox, "Allowance");
            _item.Frequency = GetComboValue(_frequencyComboBox, "Monthly");
            _item.StorageMethod = storageMethod;
            _item.WhereStored = string.Empty;
            _item.LinkedBankAssetId = 0;
            _item.LinkedBankAssetName = string.Empty;

            if (storageMethod == "Cash/Check")
            {
                _item.WhereStored = _whereStoredTextBox?.Text?.Trim() ?? "Cash/Check";
            }
            else if (storageMethod == "Other")
            {
                var other = _otherStorageMethodTextBox?.Text?.Trim() ?? string.Empty;
                _item.WhereStored = other;
                _item.StorageMethod = string.IsNullOrWhiteSpace(other) ? "Other" : $"Other: {other}";
            }
            else if (storageMethod == "Select Bank Account" && _bankAccountComboBox?.SelectedItem is ComboBoxItem selectedAccount)
            {
                if (selectedAccount.Tag is long bankId)
                    _item.LinkedBankAssetId = bankId;

                var selectedName = selectedAccount.Content?.ToString() ?? string.Empty;
                if (_item.LinkedBankAssetId > 0)
                {
                    _item.LinkedBankAssetName = selectedName;
                    _item.WhereStored = selectedName;
                }
            }
            else if (storageMethod == "Select Multiple Bank Accounts")
            {
                var selectedAccounts = GetSelectedMultipleBankAccounts();
                if (selectedAccounts.Count == 0)
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Choose at least one bank account.";
                    return;
                }

                var totalAmount = selectedAccounts.Sum(account => ParseMoney(account.Amount));
                if (totalAmount <= 0m)
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Enter a deposit amount for each selected bank account.";
                    return;
                }

                amount = totalAmount;
                _item.LinkedBankAssetId = selectedAccounts[0].Id;
                _item.LinkedBankAssetName = string.Join(", ", selectedAccounts.Select(account => $"{account.Name}: ${ParseMoney(account.Amount):0.##}"));
                _item.WhereStored = _item.LinkedBankAssetName;
            }

            _item.Amount = amount;
            _item.IsActive = _isActiveCheckBox?.IsChecked == true;
            _item.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
        }

        private void PopulateMultipleBankAccountsFromSource()
        {
            if (_multipleBankAccountsHost is null)
                return;

            _multipleBankAccountsHost.Children.Clear();

            if (!string.Equals(NormalizeStorageMethodForUi(_item.StorageMethod), "Select Multiple Bank Accounts", StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.IsNullOrWhiteSpace(_item.LinkedBankAssetName))
            {
                foreach (var split in ParseStoredMultipleBankAccountSplits(_item.LinkedBankAssetName))
                {
                    var account = _bankAccounts.FirstOrDefault(a =>
                        string.Equals(a.DisplayName, split.AccountName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a.AssetName, split.AccountName, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(a.AccountLastFour) &&
                         split.AccountName.Contains(a.AccountLastFour.Trim(), StringComparison.OrdinalIgnoreCase)));

                    AddMultipleBankAccountLine(account?.Id ?? 0, split.Amount);
                }
            }

            if (_multipleBankAccountsHost.Children.Count == 0)
                AddMultipleBankAccountLine();
        }

        private void AddMultipleBankAccountLine(long selectedId = 0, string depositAmount = "")
        {
            if (_multipleBankAccountsHost is null)
                return;

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,160,Auto"),
                ColumnSpacing = 8
            };

            var combo = new ComboBox
            {
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                MinHeight = 34
            };

            combo.Items.Add(new ComboBoxItem { Content = "Choose account", Tag = 0L });
            foreach (var account in _bankAccounts)
                combo.Items.Add(new ComboBoxItem { Content = account.DisplayName, Tag = account.Id });

            combo.SelectedIndex = 0;
            for (var index = 1; index < combo.ItemCount; index++)
            {
                if (combo.Items[index] is ComboBoxItem item && item.Tag is long id && id == selectedId)
                {
                    combo.SelectedIndex = index;
                    break;
                }
            }

            Grid.SetColumn(combo, 0);
            row.Children.Add(combo);

            var amountBox = new TextBox
            {
                Text = depositAmount,
                Watermark = "Amount",
                Height = 34,
                Padding = new Thickness(10, 6),
                Background = Brush.Parse("#0F1B2A"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#30445F"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            Grid.SetColumn(amountBox, 1);
            row.Children.Add(amountBox);

            var remove = new Button
            {
                Content = "🗑",
                Width = 34,
                Height = 34,
                Background = Brush.Parse("#8B2E2E"),
                Foreground = Brushes.White,
                BorderBrush = Brush.Parse("#B84A4A"),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            remove.Click += (_, _) => _multipleBankAccountsHost.Children.Remove(row);
            Grid.SetColumn(remove, 2);
            row.Children.Add(remove);

            _multipleBankAccountsHost.Children.Add(row);
        }

        private List<(long Id, string Name, string Amount)> GetSelectedMultipleBankAccounts()
        {
            var selected = new List<(long Id, string Name, string Amount)>();

            if (_multipleBankAccountsHost is null)
                return selected;

            foreach (var child in _multipleBankAccountsHost.Children)
            {
                if (child is not Grid row)
                    continue;

                var combo = row.Children.OfType<ComboBox>().FirstOrDefault();
                var amountBox = row.Children.OfType<TextBox>().FirstOrDefault();

                if (combo?.SelectedItem is ComboBoxItem item &&
                    item.Tag is long id &&
                    id > 0)
                {
                    var name = item.Content?.ToString() ?? string.Empty;
                    var amount = amountBox?.Text?.Trim() ?? string.Empty;

                    if (!selected.Any(existing => existing.Id == id))
                        selected.Add((id, name, amount));
                }
            }

            return selected;
        }

        private static List<(string AccountName, string Amount)> ParseStoredMultipleBankAccountSplits(string storedValue)
        {
            var splits = new List<(string AccountName, string Amount)>();

            if (string.IsNullOrWhiteSpace(storedValue))
                return splits;

            foreach (var rawPart in storedValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var part = rawPart.Trim();
                var colonIndex = part.LastIndexOf(':');

                if (colonIndex < 0)
                {
                    splits.Add((part, string.Empty));
                    continue;
                }

                var accountName = part[..colonIndex].Trim();
                var amount = part[(colonIndex + 1)..].Trim().Replace("$", string.Empty).Trim();
                splits.Add((accountName, amount));
            }

            return splits;
        }

        private static string NormalizeStorageMethodForUi(string? storageMethod)
        {
            var value = (storageMethod ?? string.Empty).Trim();

            return value.ToLowerInvariant() switch
            {
                "" => "Cash/Check",
                "cash / envelope" => "Cash/Check",
                "cash/envelope" => "Cash/Check",
                "cash reserve" => "Cash/Check",
                "bank account" => "Select Bank Account",
                "select bank account" => "Select Bank Account",
                "multiple bank accounts" => "Select Multiple Bank Accounts",
                "select multiple bank accounts" => "Select Multiple Bank Accounts",
                _ when value.StartsWith("Other:", StringComparison.OrdinalIgnoreCase) => "Other",
                _ => value
            };
        }

        private static string ExtractOtherStorageText(string? storageMethod, string? whereStored)
        {
            var value = (storageMethod ?? string.Empty).Trim();
            if (value.StartsWith("Other:", StringComparison.OrdinalIgnoreCase))
                return value["Other:".Length..].Trim();

            return (whereStored ?? string.Empty).Trim();
        }

        private static decimal ParseMoney(string? value)
        {
            var text = (value ?? string.Empty).Trim().Replace("$", string.Empty).Replace(",", string.Empty);
            return decimal.TryParse(text, out var parsed) ? parsed : 0m;
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
