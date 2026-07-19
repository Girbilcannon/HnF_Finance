using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class DebtDialog : Window
    {
        private readonly TextBlock? _dialogTitleTextBlock;
        private readonly TextBlock? _validationTextBlock;
        private readonly TextBox? _debtNameTextBox;
        private readonly ComboBox? _debtTypeComboBox;
        private readonly TextBox? _creditorCollectorTextBox;
        private readonly TextBox? _currentBalanceTextBox;
        private readonly TextBox? _minimumPaymentTextBox;
        private readonly ComboBox? _paymentFrequencyComboBox;
        private readonly TextBox? _dueDateTextBox;
        private readonly ComboBox? _statusComboBox;
        private readonly ComboBox? _priorityComboBox;
        private readonly ComboBox? _responsibilityOwnerComboBox;
        private readonly ComboBox? _paidByComboBox;
        private readonly ComboBox? _paymentTrackingComboBox;
        private readonly StackPanel? _linkedBillPanel;
        private readonly ComboBox? _linkedBillComboBox;
        private readonly Button? _createLinkedBillButton;
        private readonly StackPanel? _outsideOwnerPanel;
        private readonly TextBox? _outsideOwnerTextBox;
        private readonly StackPanel? _outsidePayerPanel;
        private readonly TextBox? _outsidePayerTextBox;
        private readonly CheckBox? _isActiveCheckBox;
        private readonly TextBox? _notesTextBox;

        private readonly List<HouseholdPerson> _householdPeople = new();
        private readonly List<Bill> _bills = new();
        private Debt _debt = new();

        public DebtDialog()
        {
            InitializeComponent();

            _dialogTitleTextBlock = this.FindControl<TextBlock>("DialogTitleTextBlock");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");
            _debtNameTextBox = this.FindControl<TextBox>("DebtNameTextBox");
            _debtTypeComboBox = this.FindControl<ComboBox>("DebtTypeComboBox");
            _creditorCollectorTextBox = this.FindControl<TextBox>("CreditorCollectorTextBox");
            _currentBalanceTextBox = this.FindControl<TextBox>("CurrentBalanceTextBox");
            _minimumPaymentTextBox = this.FindControl<TextBox>("MinimumPaymentTextBox");
            _paymentFrequencyComboBox = this.FindControl<ComboBox>("PaymentFrequencyComboBox");
            _dueDateTextBox = this.FindControl<TextBox>("DueDateTextBox");
            _statusComboBox = this.FindControl<ComboBox>("StatusComboBox");
            _priorityComboBox = this.FindControl<ComboBox>("PriorityComboBox");
            _responsibilityOwnerComboBox = this.FindControl<ComboBox>("ResponsibilityOwnerComboBox");
            _paidByComboBox = this.FindControl<ComboBox>("PaidByComboBox");
            _paymentTrackingComboBox = this.FindControl<ComboBox>("PaymentTrackingComboBox");
            _linkedBillPanel = this.FindControl<StackPanel>("LinkedBillPanel");
            _linkedBillComboBox = this.FindControl<ComboBox>("LinkedBillComboBox");
            _createLinkedBillButton = this.FindControl<Button>("CreateLinkedBillButton");
            _outsideOwnerPanel = this.FindControl<StackPanel>("OutsideOwnerPanel");
            _outsideOwnerTextBox = this.FindControl<TextBox>("OutsideOwnerTextBox");
            _outsidePayerPanel = this.FindControl<StackPanel>("OutsidePayerPanel");
            _outsidePayerTextBox = this.FindControl<TextBox>("OutsidePayerTextBox");
            _isActiveCheckBox = this.FindControl<CheckBox>("IsActiveCheckBox");
            _notesTextBox = this.FindControl<TextBox>("NotesTextBox");

            if (_responsibilityOwnerComboBox is not null)
                _responsibilityOwnerComboBox.SelectionChanged += (_, _) => RefreshOtherVisibility();

            if (_paidByComboBox is not null)
                _paidByComboBox.SelectionChanged += (_, _) => RefreshOtherVisibility();

            if (_paymentTrackingComboBox is not null)
                _paymentTrackingComboBox.SelectionChanged += (_, _) => RefreshPaymentTrackingVisibility();

            if (_createLinkedBillButton is not null)
                _createLinkedBillButton.Click += (_, _) => CreateLinkedBillRequested?.Invoke(this, EventArgs.Empty);

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => SaveAndClose();
        }

        public Debt Debt => _debt;
        public event EventHandler? CreateLinkedBillRequested;


        public void SetMode(string title, Debt debt, IReadOnlyList<HouseholdPerson>? householdPeople = null, IReadOnlyList<Bill>? bills = null)
        {
            _debt = debt ?? new Debt();

            _householdPeople.Clear();
            if (householdPeople is not null)
                _householdPeople.AddRange(householdPeople);

            _bills.Clear();
            if (bills is not null)
                _bills.AddRange(bills.Where(bill => bill.IsActive || bill.Id == _debt.LinkedBillId));

            if (_dialogTitleTextBlock is not null)
                _dialogTitleTextBlock.Text = title;

            if (_debtNameTextBox is not null)
                _debtNameTextBox.Text = _debt.DebtName;

            SelectComboValue(_debtTypeComboBox, string.IsNullOrWhiteSpace(_debt.DebtType) ? "Credit Card" : _debt.DebtType);

            if (_creditorCollectorTextBox is not null)
                _creditorCollectorTextBox.Text = _debt.CreditorCollector;

            if (_currentBalanceTextBox is not null)
                _currentBalanceTextBox.Text = _debt.CurrentBalance <= 0 ? string.Empty : _debt.CurrentBalance.ToString("0.##");

            if (_minimumPaymentTextBox is not null)
                _minimumPaymentTextBox.Text = _debt.MinimumPayment <= 0 ? string.Empty : _debt.MinimumPayment.ToString("0.##");

            SelectComboValue(_paymentFrequencyComboBox, string.IsNullOrWhiteSpace(_debt.PaymentFrequency) ? "Monthly" : _debt.PaymentFrequency);

            if (_dueDateTextBox is not null)
                _dueDateTextBox.Text = _debt.DueDate;

            SelectComboValue(_statusComboBox, string.IsNullOrWhiteSpace(_debt.Status) ? "Current" : _debt.Status);
            SelectComboValue(_priorityComboBox, string.IsNullOrWhiteSpace(_debt.Priority) ? "Normal" : _debt.Priority);

            PopulatePersonCombo(_responsibilityOwnerComboBox, _debt.ResponsibilityOwner, _outsideOwnerTextBox);
            PopulatePersonCombo(_paidByComboBox, _debt.PaidBy, _outsidePayerTextBox);
            PopulateLinkedBills(_debt.LinkedBillId);

                        var paymentTracking = string.IsNullOrWhiteSpace(_debt.PaymentTracking) ? "Not Linked" : _debt.PaymentTracking;
            if (string.Equals(paymentTracking, "Create Linked Bill From Debt", StringComparison.OrdinalIgnoreCase))
                paymentTracking = _debt.LinkedBillId > 0 ? "Select Existing Bill" : "Not Linked";

            SelectComboValue(_paymentTrackingComboBox, paymentTracking);

            if (_isActiveCheckBox is not null)
                _isActiveCheckBox.IsChecked = _debt.IsActive;

            if (_notesTextBox is not null)
                _notesTextBox.Text = _debt.Notes;

            RefreshOtherVisibility();
            RefreshPaymentTrackingVisibility();
        }

        private void PopulateLinkedBills(long selectedId)
        {
            if (_linkedBillComboBox is null)
                return;

            _linkedBillComboBox.Items.Clear();
            _linkedBillComboBox.Items.Add(new ComboBoxItem { Content = "Choose bill", Tag = 0L });

            foreach (var bill in _bills.OrderBy(bill => bill.BillName))
                _linkedBillComboBox.Items.Add(new ComboBoxItem { Content = bill.BillName, Tag = bill.Id });

            _linkedBillComboBox.SelectedIndex = 0;
            for (var index = 1; index < _linkedBillComboBox.ItemCount; index++)
            {
                if (_linkedBillComboBox.Items[index] is ComboBoxItem item && item.Tag is long id && id == selectedId)
                {
                    _linkedBillComboBox.SelectedIndex = index;
                    break;
                }
            }
        }

        public void AddAndSelectLinkedBill(Bill bill)
        {
            if (bill is null)
                return;

            var existingIndex = _bills.FindIndex(existing => existing.Id == bill.Id);
            if (existingIndex >= 0)
                _bills[existingIndex] = bill;
            else
                _bills.Add(bill);

            _debt.PaymentTracking = "Select Existing Bill";
            _debt.LinkedBillId = bill.Id;
            _debt.LinkedBillName = bill.BillName;

            PopulateLinkedBills(bill.Id);
            SelectComboValue(_paymentTrackingComboBox, "Select Existing Bill");
            RefreshPaymentTrackingVisibility();
        }

        private void PopulatePersonCombo(ComboBox? comboBox, string selectedText, TextBox? outsideTextBox)
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

            if (string.IsNullOrWhiteSpace(selectedText))
                return;

            for (var index = 0; index < comboBox.ItemCount; index++)
            {
                if (comboBox.Items[index] is ComboBoxItem item &&
                    string.Equals(item.Content?.ToString(), selectedText, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
            }

            comboBox.SelectedIndex = comboBox.ItemCount - 1;

            if (outsideTextBox is not null)
                outsideTextBox.Text = selectedText;
        }

        private void RefreshOtherVisibility()
        {
            if (_outsideOwnerPanel is not null)
                _outsideOwnerPanel.IsVisible = GetComboValue(_responsibilityOwnerComboBox, "Self (Primary Person)") == "Other";

            if (_outsidePayerPanel is not null)
                _outsidePayerPanel.IsVisible = GetComboValue(_paidByComboBox, "Self (Primary Person)") == "Other";
        }

        private void RefreshPaymentTrackingVisibility()
        {
            var tracking = GetComboValue(_paymentTrackingComboBox, "Not Linked");
            if (_linkedBillPanel is not null)
                _linkedBillPanel.IsVisible = tracking == "Select Existing Bill";
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

        private string GetPersonSelection(ComboBox? comboBox, TextBox? outsideTextBox)
        {
            var value = GetComboValue(comboBox, "Self (Primary Person)");

            if (value == "Other")
                return outsideTextBox?.Text?.Trim() ?? "Other";

            return value;
        }

        private void SaveAndClose()
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = string.Empty;

            var debtName = _debtNameTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(debtName))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Enter a debt name before saving.";
                return;
            }

            var currentBalance = 0m;
            var currentBalanceText = _currentBalanceTextBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentBalanceText) && !decimal.TryParse(currentBalanceText, out currentBalance))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Current balance must be a valid number.";
                return;
            }

            var minimumPayment = 0m;
            var minimumPaymentText = _minimumPaymentTextBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(minimumPaymentText) && !decimal.TryParse(minimumPaymentText, out minimumPayment))
            {
                if (_validationTextBlock is not null)
                    _validationTextBlock.Text = "Minimum payment must be a valid number.";
                return;
            }

            _debt.DebtName = debtName;
            _debt.DebtType = GetComboValue(_debtTypeComboBox, "Credit Card");
            _debt.CreditorCollector = _creditorCollectorTextBox?.Text?.Trim() ?? string.Empty;
            _debt.CurrentBalance = currentBalance;
            _debt.MinimumPayment = minimumPayment;
            _debt.PaymentFrequency = GetComboValue(_paymentFrequencyComboBox, "Monthly");
            _debt.DueDate = _dueDateTextBox?.Text?.Trim() ?? string.Empty;
            _debt.Status = GetComboValue(_statusComboBox, "Current");
            _debt.Priority = GetComboValue(_priorityComboBox, "Normal");
            _debt.ResponsibilityOwner = GetPersonSelection(_responsibilityOwnerComboBox, _outsideOwnerTextBox);
            _debt.PaidBy = GetPersonSelection(_paidByComboBox, _outsidePayerTextBox);
            _debt.PaymentTracking = GetComboValue(_paymentTrackingComboBox, "Not Linked");
            _debt.LinkedBillId = 0;
            _debt.LinkedBillName = string.Empty;

            if (_debt.PaymentTracking == "Select Existing Bill")
            {
                if (_linkedBillComboBox?.SelectedItem is ComboBoxItem selectedBill &&
                    selectedBill.Tag is long billId &&
                    billId > 0)
                {
                    _debt.LinkedBillId = billId;
                    _debt.LinkedBillName = selectedBill.Content?.ToString() ?? string.Empty;
                }
                else
                {
                    if (_validationTextBlock is not null)
                        _validationTextBlock.Text = "Choose the existing bill linked to this debt.";
                    return;
                }
            }

            _debt.IsActive = _isActiveCheckBox?.IsChecked == true;
            _debt.Notes = _notesTextBox?.Text?.Trim() ?? string.Empty;

            Close(true);
        }
    }
}
