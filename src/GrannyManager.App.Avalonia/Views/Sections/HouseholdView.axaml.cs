using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class HouseholdView : UserControl
    {
        public HouseholdView()
        {
            InitializeComponent();

            var addButton = this.FindControl<Button>("AddHouseholdPersonButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditHouseholdPersonButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveHouseholdPersonButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not HouseholdViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new HouseholdPersonDialog();
            dialog.SetMode("Add Household Member", viewModel.CreateBlankPerson());

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SavePerson(dialog.Person);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not HouseholdViewModel viewModel)
                return;

            var person = viewModel.CreateEditableCopyOfSelectedPerson();
            if (person is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new HouseholdPersonDialog();
            dialog.SetMode("Edit Household Member", person);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SavePerson(dialog.Person);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not HouseholdViewModel viewModel || !viewModel.CanRemovePerson)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmDeleteHouseholdPersonDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedPerson();
        }
    }
}
