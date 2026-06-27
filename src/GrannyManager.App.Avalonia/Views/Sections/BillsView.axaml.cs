using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class BillsView : UserControl
    {
        public BillsView()
        {
            InitializeComponent();

            var addButton = this.FindControl<Button>("AddBillButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditBillButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveBillButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;

            var fuelAverageButton = this.FindControl<Button>("FuelAverageButton");
            if (fuelAverageButton is not null)
                fuelAverageButton.Click += (_, _) => OpenReceiptAverage("Fuel");

            var groceryAverageButton = this.FindControl<Button>("GroceryAverageButton");
            if (groceryAverageButton is not null)
                groceryAverageButton.Click += (_, _) => OpenReceiptAverage("Grocery");
        }

        private async void OpenReceiptAverage(string receiptType)
        {
            if (DataContext is not BillsViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ReceiptAverageDialog();
            dialog.SetReceiptType(receiptType, viewModel);
            await dialog.ShowDialog(owner);
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not BillsViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new BillDialog();
            dialog.SetMode("Add Bill / Spending", viewModel.CreateBlankBill(), viewModel.GetHouseholdPeople());

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveBill(dialog.Bill);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not BillsViewModel viewModel)
                return;

            var bill = viewModel.CreateEditableCopyOfSelectedBill();
            if (bill is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new BillDialog();
            dialog.SetMode("Edit Bill / Spending", bill, viewModel.GetHouseholdPeople());

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveBill(dialog.Bill);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not BillsViewModel viewModel || !viewModel.CanRemoveBill)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmDeleteBillDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedBill();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            RefreshWhenVisible();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsVisibleProperty)
                RefreshWhenVisible();
        }

        private void RefreshWhenVisible()
        {
            if (IsVisible && DataContext is BillsViewModel viewModel)
                viewModel.RefreshFromNavigation();
        }
    }
}
