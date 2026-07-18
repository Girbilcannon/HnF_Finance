using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class AssetsView : UserControl
    {
        public AssetsView()
        {
            InitializeComponent();

            var addButton = this.FindControl<Button>("AddAssetButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditAssetButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveAssetButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not AssetsViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new AssetItemDialog();
            dialog.SetMode("Add Asset", viewModel.CreateBlankAsset());

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveAsset(dialog.Asset);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not AssetsViewModel viewModel)
                return;

            var asset = viewModel.CreateEditableCopyOfSelectedAsset();
            if (asset is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new AssetItemDialog();
            dialog.SetMode("Edit Asset", asset);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveAsset(dialog.Asset);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not AssetsViewModel viewModel || !viewModel.CanRemoveAsset)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmDeleteAssetItemDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedAsset();
        }
    }
}
