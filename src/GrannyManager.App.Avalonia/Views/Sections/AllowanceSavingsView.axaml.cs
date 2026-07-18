using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class AllowanceSavingsView : UserControl
    {
        public AllowanceSavingsView()
        {
            InitializeComponent();

            var addButton = this.FindControl<Button>("AddAllowanceSavingsItemButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditAllowanceSavingsItemButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveAllowanceSavingsItemButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not AllowanceSavingsViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new AllowanceSavingsItemDialog();
            dialog.SetMode(
                "Add Allowance / Savings Item",
                viewModel.CreateBlankItem(),
                viewModel.LoadBankAccounts(),
                viewModel.CreateBlankBankAccount,
                viewModel.SaveBankAccount,
                viewModel.LoadBankAccounts);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveItem(dialog.Item);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not AllowanceSavingsViewModel viewModel)
                return;

            var item = viewModel.CreateEditableCopyOfSelectedItem();
            if (item is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new AllowanceSavingsItemDialog();
            dialog.SetMode(
                "Edit Allowance / Savings Item",
                item,
                viewModel.LoadBankAccounts(),
                viewModel.CreateBlankBankAccount,
                viewModel.SaveBankAccount,
                viewModel.LoadBankAccounts);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveItem(dialog.Item);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not AllowanceSavingsViewModel viewModel || !viewModel.CanRemoveItem)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmDeleteAllowanceSavingsItemDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedItem();
        }
    }
}
