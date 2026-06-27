using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class ReceiptAverageDialog : Window
    {
        private readonly TextBlock? _titleTextBlock;
        private readonly TextBlock? _summaryTextBlock;
        private readonly TextBox? _amountTextBox;
        private readonly CalendarDatePicker? _receiptDatePicker;
        private readonly ListBox? _receiptsListBox;

        private string _receiptType = string.Empty;
        private BillsViewModel? _viewModel;

        public ReceiptAverageDialog()
        {
            InitializeComponent();

            _titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            _summaryTextBlock = this.FindControl<TextBlock>("SummaryTextBlock");
            _amountTextBox = this.FindControl<TextBox>("AmountTextBox");
            _receiptDatePicker = this.FindControl<CalendarDatePicker>("ReceiptDatePicker");
            _receiptsListBox = this.FindControl<ListBox>("ReceiptsListBox");

            if (_receiptDatePicker is not null)
                _receiptDatePicker.SelectedDate = DateTime.Today;

            var addButton = this.FindControl<Button>("AddReceiptButton");
            if (addButton is not null)
                addButton.Click += (_, _) => AddReceipt();

            var closeButton = this.FindControl<Button>("CloseButton");
            if (closeButton is not null)
                closeButton.Click += (_, _) => Close();
        }

        public void SetReceiptType(string receiptType, BillsViewModel viewModel)
        {
            _receiptType = receiptType;
            _viewModel = viewModel;

            if (_titleTextBlock is not null)
                _titleTextBlock.Text = $"{receiptType} Average";

            RefreshReceipts();
        }

        private void AddReceipt()
        {
            if (_viewModel is null)
                return;

            var amountText = _amountTextBox?.Text?.Trim() ?? string.Empty;
            if (!decimal.TryParse(amountText, out var amount) || amount <= 0)
            {
                SetSummary("Enter a valid receipt amount.");
                return;
            }

            var date = _receiptDatePicker?.SelectedDate?.Date ?? DateTime.Today;

            if (!_viewModel.AddReceipt(_receiptType, date, amount, out var message))
            {
                SetSummary(message);
                return;
            }

            if (_amountTextBox is not null)
                _amountTextBox.Text = string.Empty;

            RefreshReceipts();
        }

        public void DeleteReceipt_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel is null || sender is not Button button || button.Tag is not long receiptId)
                return;

            if (!_viewModel.DeleteReceipt(receiptId, out var message))
            {
                SetSummary(message);
                return;
            }

            RefreshReceipts();
        }

        private void RefreshReceipts()
        {
            if (_viewModel is null)
                return;

            var receipts = _viewModel.LoadReceipts(_receiptType);

            if (_receiptsListBox is not null)
            {
                _receiptsListBox.Items.Clear();

                foreach (var receipt in receipts.OrderByDescending(r => r.ReceiptDate))
                    _receiptsListBox.Items.Add(new ReceiptRowViewModel(receipt));
            }

            var summary = _viewModel.LoadReceiptAverage(_receiptType);
            if (summary.RoundedMonthlyEstimate > 0)
                SetSummary($"Rounded monthly average: {summary.RoundedMonthlyEstimate:C0}");
            else
                SetSummary("No receipts added.");
        }

        private void SetSummary(string text)
        {
            if (_summaryTextBlock is not null)
                _summaryTextBlock.Text = text;
        }
    }

    public sealed class ReceiptRowViewModel
    {
        public ReceiptRowViewModel(BillReceipt receipt)
        {
            Id = receipt.Id;
            DisplayText = $"{receipt.ReceiptDate:d}  -  {receipt.Amount:C2}";
        }

        public long Id { get; }
        public string DisplayText { get; }
    }
}
