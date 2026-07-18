using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class DebtsView : UserControl
    {
        public DebtsView()
        {
            InitializeComponent();

            var addButton = this.FindControl<Button>("AddDebtButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditDebtButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveDebtButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DebtsViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new DebtDialog();
            dialog.SetMode("Add Debt", viewModel.CreateBlankDebt(), viewModel.GetHouseholdPeople());

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveDebt(dialog.Debt);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DebtsViewModel viewModel)
                return;

            var debt = viewModel.CreateEditableCopyOfSelectedDebt();
            if (debt is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new DebtDialog();
            dialog.SetMode("Edit Debt", debt, viewModel.GetHouseholdPeople());

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveDebt(dialog.Debt);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DebtsViewModel viewModel || !viewModel.CanRemoveDebt)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmDeleteDebtDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedDebt();
        }
    }
}
