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
        private readonly TextBlock? _createBankAccountPlaceholderTextBlock;
        private readonly ComboBox? _householdPersonComboBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;
        private readonly List<HouseholdPerson> _householdPeople = new();
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
            _createBankAccountPlaceholderTextBlock = this.FindControl<TextBlock>("CreateBankAccountPlaceholderTextBlock");
            _householdPersonComboBox = this.FindControl<ComboBox>("HouseholdPersonComboBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_taxesWithheldCheckBox is not null)
                _taxesWithheldCheckBox.IsCheckedChanged += (_, _) => RefreshAmountLabel();

            if (_depositDestinationComboBox is not null)
                _depositDestinationComboBox.SelectionChanged += (_, _) => RefreshDepositDestinationVisibility();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public IncomeSource Source => _source;

        public void SetMode(string title, IncomeSource source, IReadOnlyList<HouseholdPerson> householdPeople)
        {
            _source = source ?? new IncomeSource();
            _householdPeople.Clear();
            if (householdPeople is not null)
                _householdPeople.AddRange(householdPeople);

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

            PopulateHouseholdPeople(_source.LinkedHouseholdPersonId);

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _source.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _source.Notes;

            RefreshAmountLabel();
            RefreshDepositDestinationVisibility();
        }

        private void PopulateHouseholdPeople(long selectedId)
        {
            if (_householdPersonComboBox is null)
                return;

            _householdPersonComboBox.Items.Clear();
            _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = "None", Tag = 0L });

            var primaryPerson = _householdPeople.FirstOrDefault(person =>
                string.Equals(person.Relationship, "Self", StringComparison.OrdinalIgnoreCase) ||
                person.Role.Contains("Primary", StringComparison.OrdinalIgnoreCase));

            if (primaryPerson is not null)
                _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = $"Self ({primaryPerson.FullName})", Tag = primaryPerson.Id });
            else
                _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = "Self (Primary Person)", Tag = 0L });

            foreach (var person in _householdPeople)
            {
                if (primaryPerson is not null && person.Id == primaryPerson.Id)
                    continue;

                _householdPersonComboBox.Items.Add(new ComboBoxItem { Content = person.FullName, Tag = person.Id });
            }

            _householdPersonComboBox.SelectedIndex = selectedId == 0 && !string.IsNullOrWhiteSpace(_source.LinkedHouseholdPersonName) ? 1 : 0;

            for (var index = 1; index < _householdPersonComboBox.ItemCount; index++)
            {
                if (_householdPersonComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag is long id &&
                    id == selectedId)
                {
                    _householdPersonComboBox.SelectedIndex = index;
                    break;
                }
            }
        }

        private void RefreshAmountLabel()
        {
            if (_amountLabelTextBlock is not null)
                _amountLabelTextBlock.Text = _taxesWithheldCheckBox?.IsChecked == true ? "After Taxes" : "Gross Pay";
        }

        private void RefreshDepositDestinationVisibility()
        {
            var value = GetComboValue(_depositDestinationComboBox, "Cash/Check");

            if (_bankAccountPanel is not null)
                _bankAccountPanel.IsVisible = value == "Select Bank Account";

            if (_multipleBankAccountsPanel is not null)
                _multipleBankAccountsPanel.IsVisible = value == "Select Multiple Bank Accounts";

            if (_createBankAccountPlaceholderTextBlock is not null)
                _createBankAccountPlaceholderTextBlock.IsVisible = value == "Create Bank Account";
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

            var sourceName = _sourceNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sourceName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a source name before saving.";
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

            _source.SourceName = sourceName;
            _source.IncomeType = GetComboValue(_incomeTypeComboBox, "Other");
            _source.Frequency = GetComboValue(_frequencyComboBox, "Monthly");
            _source.TaxesWithheld = _taxesWithheldCheckBox?.IsChecked == true;
            _source.Amount = amount;
            _source.ExpectedDayOrDate = _expectedDayTextBox?.Text?.Trim() ?? string.Empty;
            _source.DepositDestination = GetComboValue(_depositDestinationComboBox, "Cash/Check");

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
    }
}
