using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class BillDialog : Window
    {
        private readonly TextBlock? _dialogTitleTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly TextBox? _billNameTextBox;
        private readonly ComboBox? _categoryComboBox;
        private readonly ComboBox? _priorityComboBox;
        private readonly TextBox? _amountTextBox;
        private readonly ComboBox? _frequencyComboBox;
        private readonly TextBox? _dueDateTextBox;
        private readonly TextBox? _pastDueAmountTextBox;
        private readonly CheckBox? _autopayCheckBox;
        private readonly ComboBox? _paymentMethodComboBox;
        private readonly StackPanel? _paymentBankPanel;
        private readonly StackPanel? _paymentDebtPanel;
        private readonly StackPanel? _paymentOtherPanel;
        private readonly TextBox? _paymentOtherTextBox;
        private readonly ComboBox? _paidByComboBox;
        private readonly StackPanel? _outsidePayerPanel;
        private readonly TextBox? _outsidePayerTextBox;
        private readonly ComboBox? _responsibilityOwnerComboBox;
        private readonly StackPanel? _outsideOwnerPanel;
        private readonly TextBox? _outsideOwnerTextBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;
        private readonly List<HouseholdPerson> _householdPeople = new();
        private Bill _bill = new();

        public BillDialog()
        {
            InitializeComponent();

            _dialogTitleTextBlock = this.FindControl<TextBlock>("DialogTitleTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _billNameTextBox = this.FindControl<TextBox>("BillNameTextBox");
            _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
            _priorityComboBox = this.FindControl<ComboBox>("PriorityComboBox");
            _amountTextBox = this.FindControl<TextBox>("AmountTextBox");
            _frequencyComboBox = this.FindControl<ComboBox>("FrequencyComboBox");
            _dueDateTextBox = this.FindControl<TextBox>("DueDateTextBox");
            _pastDueAmountTextBox = this.FindControl<TextBox>("PastDueAmountTextBox");
            _autopayCheckBox = this.FindControl<CheckBox>("AutopayCheckBox");
            _paymentMethodComboBox = this.FindControl<ComboBox>("PaymentMethodComboBox");
            _paymentBankPanel = this.FindControl<StackPanel>("PaymentBankPanel");
            _paymentDebtPanel = this.FindControl<StackPanel>("PaymentDebtPanel");
            _paymentOtherPanel = this.FindControl<StackPanel>("PaymentOtherPanel");
            _paymentOtherTextBox = this.FindControl<TextBox>("PaymentOtherTextBox");
            _paidByComboBox = this.FindControl<ComboBox>("PaidByComboBox");
            _outsidePayerPanel = this.FindControl<StackPanel>("OutsidePayerPanel");
            _outsidePayerTextBox = this.FindControl<TextBox>("OutsidePayerTextBox");
            _responsibilityOwnerComboBox = this.FindControl<ComboBox>("ResponsibilityOwnerComboBox");
            _outsideOwnerPanel = this.FindControl<StackPanel>("OutsideOwnerPanel");
            _outsideOwnerTextBox = this.FindControl<TextBox>("OutsideOwnerTextBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_paymentMethodComboBox is not null)
                _paymentMethodComboBox.SelectionChanged += (_, _) => RefreshPaymentMethodVisibility();

            if (_paidByComboBox is not null)
                _paidByComboBox.SelectionChanged += (_, _) => RefreshOtherVisibility();

            if (_responsibilityOwnerComboBox is not null)
                _responsibilityOwnerComboBox.SelectionChanged += (_, _) => RefreshOtherVisibility();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public Bill Bill => _bill;

        public void SetMode(string title, Bill bill, IReadOnlyList<HouseholdPerson> householdPeople)
        {
            _bill = bill ?? new Bill();
            _householdPeople.Clear();
            if (householdPeople is not null)
                _householdPeople.AddRange(householdPeople);

            if (_dialogTitleTextBlock is not null)
                _dialogTitleTextBlock.Text = title;

            if (_billNameTextBox is not null)
                _billNameTextBox.Text = _bill.BillName;

            SelectComboValue(_categoryComboBox, string.IsNullOrWhiteSpace(_bill.Category) ? "Utilities" : _bill.Category);
            SelectComboValue(_priorityComboBox, string.IsNullOrWhiteSpace(_bill.Priority) ? "Normal" : _bill.Priority);
            SelectComboValue(_frequencyComboBox, string.IsNullOrWhiteSpace(_bill.Frequency) ? "Monthly" : _bill.Frequency);

            if (_amountTextBox is not null)
                _amountTextBox.Text = _bill.Amount <= 0 ? string.Empty : _bill.Amount.ToString("0.##");

            if (_dueDateTextBox is not null)
                _dueDateTextBox.Text = _bill.DueDate;

            if (_pastDueAmountTextBox is not null)
                _pastDueAmountTextBox.Text = _bill.PastDueAmount <= 0 ? string.Empty : _bill.PastDueAmount.ToString("0.##");

            if (_autopayCheckBox is not null)
                _autopayCheckBox.IsChecked = _bill.IsAutopay;

            var paymentMethod = string.IsNullOrWhiteSpace(_bill.PaymentMethod) ? "Cash/Check" : _bill.PaymentMethod;
            if (paymentMethod == "Manual" || paymentMethod == "Autopay")
                paymentMethod = "Cash/Check";
            if (paymentMethod.StartsWith("Other:", StringComparison.OrdinalIgnoreCase))
            {
                SelectComboValue(_paymentMethodComboBox, "Other");
                if (_paymentOtherTextBox is not null)
                    _paymentOtherTextBox.Text = paymentMethod.Substring("Other:".Length).Trim();
            }
            else
            {
                SelectComboValue(_paymentMethodComboBox, paymentMethod);
            }

            PopulatePersonCombo(_paidByComboBox, _bill.PaidByHouseholdPersonId, _bill.PaidBy);
            PopulatePersonCombo(_responsibilityOwnerComboBox, _bill.ResponsibilityOwnerHouseholdPersonId, _bill.ResponsibilityOwner);

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _bill.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _bill.Notes;

            RefreshOtherVisibility();
            RefreshPaymentMethodVisibility();
        }

        private void PopulatePersonCombo(ComboBox? comboBox, long selectedId, string selectedText)
        {
            if (comboBox is null)
                return;

            comboBox.Items.Clear();

            var primaryPerson = _householdPeople.FirstOrDefault(person =>
                string.Equals(person.Relationship, "Self", StringComparison.OrdinalIgnoreCase) ||
                person.Role.Contains("Primary", StringComparison.OrdinalIgnoreCase));

            if (primaryPerson is not null)
                comboBox.Items.Add(new ComboBoxItem { Content = $"Self ({primaryPerson.FullName})", Tag = primaryPerson.Id });
            else
                comboBox.Items.Add(new ComboBoxItem { Content = "Self (Primary Person)", Tag = 0L });

            foreach (var person in _householdPeople)
            {
                if (primaryPerson is not null && person.Id == primaryPerson.Id)
                    continue;

                comboBox.Items.Add(new ComboBoxItem { Content = person.FullName, Tag = person.Id });
            }

            comboBox.Items.Add(new ComboBoxItem { Content = "Other", Tag = -1L });
            comboBox.SelectedIndex = 0;

            if (selectedId > 0)
            {
                for (var index = 1; index < comboBox.ItemCount; index++)
                {
                    if (comboBox.Items[index] is ComboBoxItem item && item.Tag is long id && id == selectedId)
                    {
                        comboBox.SelectedIndex = index;
                        return;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedText) &&
                selectedText != "Self (Primary Person)" &&
                !selectedText.StartsWith("Self (", StringComparison.OrdinalIgnoreCase) &&
                !_householdPeople.Exists(p => string.Equals(p.FullName, selectedText, StringComparison.OrdinalIgnoreCase)))
            {
                comboBox.SelectedIndex = comboBox.ItemCount - 1;
                if (ReferenceEquals(comboBox, _paidByComboBox) && _outsidePayerTextBox is not null)
                    _outsidePayerTextBox.Text = selectedText;
                if (ReferenceEquals(comboBox, _responsibilityOwnerComboBox) && _outsideOwnerTextBox is not null)
                    _outsideOwnerTextBox.Text = selectedText;
            }
        }

        private void RefreshPaymentMethodVisibility()
        {
            var value = GetComboValue(_paymentMethodComboBox, "Cash/Check");

            if (_paymentBankPanel is not null)
                _paymentBankPanel.IsVisible = value == "Bank/Debit";

            if (_paymentDebtPanel is not null)
                _paymentDebtPanel.IsVisible = value == "Credit Card";

            if (_paymentOtherPanel is not null)
                _paymentOtherPanel.IsVisible = value == "Other";
        }

        private void RefreshOtherVisibility()
        {
            if (_outsidePayerPanel is not null)
                _outsidePayerPanel.IsVisible = GetComboValue(_paidByComboBox, "Self (Primary Person)") == "Other";

            if (_outsideOwnerPanel is not null)
                _outsideOwnerPanel.IsVisible = GetComboValue(_responsibilityOwnerComboBox, "Self (Primary Person)") == "Other";
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

        private static long GetComboPersonId(ComboBox? comboBox)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item && item.Tag is long id && id > 0)
                return id;

            return 0;
        }

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var billName = _billNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(billName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a bill name before saving.";
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

            var pastDue = 0m;
            var pastDueText = _pastDueAmountTextBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pastDueText) && !decimal.TryParse(pastDueText, out pastDue))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Past due amount must be a valid number.";
                return;
            }

            _bill.BillName = billName;
            _bill.Category = GetComboValue(_categoryComboBox, "Other");
            _bill.Priority = GetComboValue(_priorityComboBox, "Normal");
            _bill.Amount = amount;
            _bill.Frequency = GetComboValue(_frequencyComboBox, "Monthly");
            _bill.DueDate = _dueDateTextBox?.Text?.Trim() ?? string.Empty;
            _bill.PastDueAmount = pastDue;
            _bill.IsAutopay = _autopayCheckBox?.IsChecked == true;
            var paymentMethod = GetComboValue(_paymentMethodComboBox, "Cash/Check");
            _bill.PaymentMethod = paymentMethod == "Other"
                ? $"Other: {_paymentOtherTextBox?.Text?.Trim() ?? string.Empty}".Trim()
                : paymentMethod;

            _bill.PaidByHouseholdPersonId = GetComboPersonId(_paidByComboBox);
            _bill.PaidBy = GetComboValue(_paidByComboBox, "Self (Primary Person)") == "Other"
                ? _outsidePayerTextBox?.Text?.Trim() ?? "Other"
                : GetComboValue(_paidByComboBox, "Self (Primary Person)");

            _bill.ResponsibilityOwnerHouseholdPersonId = GetComboPersonId(_responsibilityOwnerComboBox);
            _bill.ResponsibilityOwner = GetComboValue(_responsibilityOwnerComboBox, "Self (Primary Person)") == "Other"
                ? _outsideOwnerTextBox?.Text?.Trim() ?? "Other"
                : GetComboValue(_responsibilityOwnerComboBox, "Self (Primary Person)");

            _bill.IsActive = _isActiveCheckBox?.IsChecked == true;
            _bill.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
        }
    }
}
