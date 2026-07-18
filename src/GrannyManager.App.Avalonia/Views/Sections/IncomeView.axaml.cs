using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class IncomeView : UserControl
    {
        public IncomeView()
        {
            InitializeComponent();

            var addButton = this.FindControl<Button>("AddIncomeSourceButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditIncomeSourceButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveIncomeSourceButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not IncomeViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new IncomeSourceDialog();
            dialog.SetMode(
                "Add Income Source",
                viewModel.CreateBlankSource(),
                viewModel.GetHouseholdPeople(),
                viewModel.LoadBankAccounts(),
                viewModel.CreateBlankBankAccount,
                viewModel.SaveBankAccount,
                viewModel.LoadBankAccounts);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveSource(dialog.Source);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not IncomeViewModel viewModel)
                return;

            var source = viewModel.CreateEditableCopyOfSelectedSource();
            if (source is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new IncomeSourceDialog();
            dialog.SetMode(
                "Edit Income Source",
                source,
                viewModel.GetHouseholdPeople(),
                viewModel.LoadBankAccounts(),
                viewModel.CreateBlankBankAccount,
                viewModel.SaveBankAccount,
                viewModel.LoadBankAccounts);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveSource(dialog.Source);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not IncomeViewModel viewModel || !viewModel.CanRemoveSource)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmDeleteIncomeSourceDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedSource();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (DataContext is IncomeViewModel viewModel)
                viewModel.RefreshFromNavigation();
        }
    }
}

