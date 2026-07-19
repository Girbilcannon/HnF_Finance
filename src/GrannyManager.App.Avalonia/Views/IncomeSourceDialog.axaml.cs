using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class IncomeSourceDialog : Window
    {
        private readonly TextBlock? _dialogTitleTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly TextBlock? _amountLabelTextBlock;
        private readonly TextBox? _sourceNameTextBox;
        private readonly ComboBox? _incomeTypeComboBox;
        private readonly ComboBox? _frequencyComboBox;
        private readonly CheckBox? _taxesWithheldCheckBox;
        private readonly TextBox? _amountTextBox;
        private readonly TextBox? _expectedDayTextBox;
        private readonly ComboBox? _depositDestinationComboBox;
        private readonly StackPanel? _bankAccountPanel;
        private readonly StackPanel? _multipleBankAccountsPanel;
        private readonly ComboBox? _bankAccountComboBox;
        private readonly Button? _createBankAccountButton;
        private readonly Button? _addBankAccountLineButton;
        private readonly Button? _createBankAccountForMultipleButton;
        private readonly StackPanel? _multipleBankAccountsHost;
        private readonly ComboBox? _householdPersonComboBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private readonly List<HouseholdPerson> _householdPeople = new();
        private readonly List<AssetItem> _bankAccounts = new();
        private Func<AssetItem>? _createBlankBankAccount;
        private Func<AssetItem, bool>? _saveBankAccount;
        private Func<IReadOnlyList<AssetItem>>? _reloadBankAccounts;
        private IncomeSource _source = new();

        public IncomeSourceDialog()
        {
            InitializeComponent();

            _dialogTitleTextBlock = this.FindControl<TextBlock>("DialogTitleTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _amountLabelTextBlock = this.FindControl<TextBlock>("AmountLabelTextBlock");
            _sourceNameTextBox = this.FindControl<TextBox>("SourceNameTextBox");
            _incomeTypeComboBox = this.FindControl<ComboBox>("IncomeTypeComboBox");
            _frequencyComboBox = this.FindControl<ComboBox>("FrequencyComboBox");
            _taxesWithheldCheckBox = this.FindControl<CheckBox>("TaxesWithheldCheckBox");
            _amountTextBox = this.FindControl<TextBox>("AmountTextBox");
            _expectedDayTextBox = this.FindControl<TextBox>("ExpectedDayTextBox");
            _depositDestinationComboBox = this.FindControl<ComboBox>("DepositDestinationComboBox");
            _bankAccountPanel = this.FindControl<StackPanel>("BankAccountPanel");
            _multipleBankAccountsPanel = this.FindControl<StackPanel>("MultipleBankAccountsPanel");
            _bankAccountComboBox = this.FindControl<ComboBox>("BankAccountComboBox");
            _createBankAccountButton = this.FindControl<Button>("CreateBankAccountButton");
            _addBankAccountLineButton = this.FindControl<Button>("AddBankAccountLineButton");
            _createBankAccountForMultipleButton = this.FindControl<Button>("CreateBankAccountForMultipleButton");
            _multipleBankAccountsHost = this.FindControl<StackPanel>("MultipleBankAccountsHost");
            _householdPersonComboBox = this.FindControl<ComboBox>("HouseholdPersonComboBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_taxesWithheldCheckBox is not null)
                _taxesWithheldCheckBox.IsCheckedChanged += (_, _) => RefreshAmountLabel();

            if (_depositDestinationComboBox is not null)
                _depositDestinationComboBox.SelectionChanged += (_, _) => RefreshDepositDestinationVisibility();

            if (_createBankAccountButton is not null)
                _createBankAccountButton.Click += async (_, _) => await CreateBankAccountAsync();

            if (_addBankAccountLineButton is not null)
                _addBankAccountLineButton.Click += (_, _) => AddMultipleBankAccountLine();

            if (_createBankAccountForMultipleButton is not null)
                _createBankAccountForMultipleButton.Click += async (_, _) =>
                {
                    var createdId = await CreateBankAccountAsync();
                    SelectComboValue(_depositDestinationComboBox, "Select Multiple Bank Accounts");
                    RefreshDepositDestinationVisibility();
                    AddMultipleBankAccountLine(createdId);
                };

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public IncomeSource Source => _source;

        public void SetMode(
            string title,
            IncomeSource source,
            IReadOnlyList<HouseholdPerson> householdPeople,
            IReadOnlyList<AssetItem>? bankAccounts = null,
            Func<AssetItem>? createBlankBankAccount = null,
            Func<AssetItem, bool>? saveBankAccount = null,
            Func<IReadOnlyList<AssetItem>>? reloadBankAccounts = null)
        {
            _source = source ?? new IncomeSource();

            _householdPeople.Clear();
            if (householdPeople is not null)
                _householdPeople.AddRange(householdPeople);

            _bankAccounts.Clear();
            if (bankAccounts is not null)
                _bankAccounts.AddRange(bankAccounts);

            _createBlankBankAccount = createBlankBankAccount;
            _saveBankAccount = saveBankAccount;
            _reloadBankAccounts = reloadBankAccounts;

            if (_dialogTitleTextBlock is not null)
                _dialogTitleTextBlock.Text = title;

            if (_sourceNameTextBox is not null)
                _sourceNameTextBox.Text = _source.SourceName;

            SelectComboValue(_incomeTypeComboBox, string.IsNullOrWhiteSpace(_source.IncomeType) ? "Social Security" : _source.IncomeType);
            SelectComboValue(_frequencyComboBox, string.IsNullOrWhiteSpace(_source.Frequency) ? "Monthly" : _source.Frequency);

            if (_taxesWithheldCheckBox is not null)
                _taxesWithheldCheckBox.IsChecked = _source.TaxesWithheld;

            if (_amountTextBox is not null)
                _amountTextBox.Text = _source.Amount <= 0 ? string.Empty : _source.Amount.ToString("0.##");

            if (_expectedDayTextBox is not null)
                _expectedDayTextBox.Text = _source.ExpectedDayOrDate;

            SelectComboValue(_depositDestinationComboBox, string.IsNullOrWhiteSpace(_source.DepositDestination) ? "Cash/Check" : _source.DepositDestination);

            PopulateBankAccounts(_source.LinkedBankAssetId);
            PopulateMultipleBankAccountsFromSource();
            PopulateHouseholdPeople(_source.LinkedHouseholdPersonId);

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _source.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _source.Notes;

            RefreshAmountLabel();
            RefreshDepositDestinationVisibility();
        }

        private async System.Threading.Tasks.Task<long> CreateBankAccountAsync()
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
            SelectComboValue(_depositDestinationComboBox, "Select Bank Account");
            RefreshDepositDestinationVisibility();
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


        private void PopulateMultipleBankAccountsFromSource()
        {
            if (_multipleBankAccountsHost is null)
                return;

            _multipleBankAccountsHost.Children.Clear();

            if (!string.Equals(_source.DepositDestination, "Select Multiple Bank Accounts", StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.IsNullOrWhiteSpace(_source.LinkedBankAssetName))
            {
                foreach (var split in ParseStoredMultipleBankAccountSplits(_source.LinkedBankAssetName))
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
                Background = global::Avalonia.Media.Brush.Parse("#0F1B2A"),
                Foreground = global::Avalonia.Media.Brushes.White,
                BorderBrush = global::Avalonia.Media.Brush.Parse("#30445F"),
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
                Watermark = "Deposit Amount",
                Height = 34,
                Padding = new global::Avalonia.Thickness(10, 6),
                Background = global::Avalonia.Media.Brush.Parse("#0F1B2A"),
                Foreground = global::Avalonia.Media.Brushes.White,
                BorderBrush = global::Avalonia.Media.Brush.Parse("#30445F"),
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch
            };

            Grid.SetColumn(amountBox, 1);
            row.Children.Add(amountBox);

            var remove = new Button
            {
                Content = "🗑",
                Width = 34,
                Height = 34,
                Background = global::Avalonia.Media.Brush.Parse("#8B2E2E"),
                Foreground = global::Avalonia.Media.Brushes.White,
                BorderBrush = global::Avalonia.Media.Brush.Parse("#B84A4A"),
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center
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

        private void PopulateHouseholdPeople(long selectedId)
        {
            if (_householdPersonComboBox is null)
                return;

            _householdPersonComboBox.Items.Clear();
            _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = "None" });
            _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = "Self (Primary Person)", Tag = 0L });

            foreach (var person in _householdPeople)
                _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = person.FullName, Tag = person.Id });

            _householdPersonComboBox.SelectedIndex = selectedId == 0 && !string.IsNullOrWhiteSpace(_source.LinkedHouseholdPersonName) ? 1 : 0;

            for (var index = 2; index < _householdPersonComboBox.ItemCount; index++)
            {
                if (_householdPersonComboBox.Items[index] is ComboBoxItem item && item.Tag is long id && id == selectedId)
                {
                    _householdPersonComboBox.SelectedIndex = index;
                    break;
                }
            }
        }

        private void RefreshAmountLabel()
        {
            var destination = GetComboValue(_depositDestinationComboBox, "Cash/Check");

            if (_amountLabelTextBlock is not null)
            {
                _amountLabelTextBlock.Text = destination == "Select Multiple Bank Accounts"
                    ? "Contribution Amount (calculated from deposit amounts)"
                    : (_taxesWithheldCheckBox?.IsChecked == true ? "After Taxes" : "Gross Pay");
            }

            if (_amountTextBox is not null)
                _amountTextBox.IsEnabled = destination != "Select Multiple Bank Accounts";
        }

        private void RefreshDepositDestinationVisibility()
        {
            var destination = GetComboValue(_depositDestinationComboBox, "Cash/Check");

            if (_bankAccountPanel is not null)
                _bankAccountPanel.IsVisible = destination == "Select Bank Account";

            if (_multipleBankAccountsPanel is not null)
                _multipleBankAccountsPanel.IsVisible = destination == "Select Multiple Bank Accounts";

            if (destination == "Select Multiple Bank Accounts" &&
                _multipleBankAccountsHost is not null &&
                _multipleBankAccountsHost.Children.Count == 0)
            {
                AddMultipleBankAccountLine();
            }

            RefreshAmountLabel();
        }

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var sourceName = _sourceNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a source name before saving.";
                return;
            }

            var selectedDepositDestination = GetComboValue(_depositDestinationComboBox, "Cash/Check");
            var amount = 0m;

            if (selectedDepositDestination != "Select Multiple Bank Accounts")
            {
                var amountText = _amountTextBox?.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(amountText) && !decimal.TryParse(amountText, out amount))
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Amount must be a valid number.";
                    return;
                }
            }

            _source.SourceName = sourceName;
            _source.IncomeType = GetComboValue(_incomeTypeComboBox, "Other");
            _source.Frequency = GetComboValue(_frequencyComboBox, "Monthly");
            _source.TaxesWithheld = _taxesWithheldCheckBox?.IsChecked == true;
            _source.Amount = amount;
            _source.ExpectedDayOrDate = _expectedDayTextBox?.Text?.Trim() ?? string.Empty;
            _source.DepositDestination = selectedDepositDestination;

            _source.LinkedBankAssetId = 0;
            _source.LinkedBankAssetName = string.Empty;
            if (_source.DepositDestination == "Select Bank Account" && _bankAccountComboBox?.SelectedItem is ComboBoxItem selectedAccount)
            {
                if (selectedAccount.Tag is long bankId)
                    _source.LinkedBankAssetId = bankId;

                var selectedName = selectedAccount.Content?.ToString() ?? string.Empty;
                if (_source.LinkedBankAssetId > 0)
                    _source.LinkedBankAssetName = selectedName;
            }
            else if (_source.DepositDestination == "Select Multiple Bank Accounts")
            {
                var selectedAccounts = GetSelectedMultipleBankAccounts();
                if (selectedAccounts.Count == 0)
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Choose at least one bank account, or use + Create Bank Account first.";
                    return;
                }

                var totalDepositAmount = selectedAccounts.Sum(account => ParseMoney(account.Amount));
                if (totalDepositAmount <= 0m)
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Enter a deposit amount for each selected bank account.";
                    return;
                }

                _source.Amount = totalDepositAmount;
                _source.LinkedBankAssetId = selectedAccounts[0].Id;
                _source.LinkedBankAssetName = string.Join(", ", selectedAccounts.Select(account => $"{account.Name}: ${ParseMoney(account.Amount):0.##}"));
            }

            _source.LinkedHouseholdPersonId = 0;
            _source.LinkedHouseholdPersonName = string.Empty;
            if (_householdPersonComboBox?.SelectedItem is ComboBoxItem selectedPerson)
            {
                if (selectedPerson.Tag is long personId)
                    _source.LinkedHouseholdPersonId = personId;

                var selectedName = selectedPerson.Content?.ToString() ?? string.Empty;
                if (selectedName != "None")
                    _source.LinkedHouseholdPersonName = selectedName;
            }

            _source.IsActive = _isActiveCheckBox?.IsChecked == true;
            _source.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
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
