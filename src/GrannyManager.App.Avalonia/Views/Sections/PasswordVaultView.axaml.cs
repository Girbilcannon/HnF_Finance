using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;
using System.Threading.Tasks;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class PasswordVaultView : UserControl
    {
        public PasswordVaultView()
        {
            InitializeComponent();

            var pinTextBox = this.FindControl<TextBox>("VaultPinTextBox");
            if (pinTextBox is not null)
                pinTextBox.KeyDown += PinTextBox_KeyDown;

            var addButton = this.FindControl<Button>("AddVaultEntryButton");
            if (addButton is not null)
                addButton.Click += AddButton_Click;

            var editButton = this.FindControl<Button>("EditVaultEntryButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveVaultEntryButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;

            var copyUserNameButton = this.FindControl<Button>("CopyUserNameButton");
            if (copyUserNameButton is not null)
                copyUserNameButton.Click += CopyUserNameButton_Click;

            var copyPasswordButton = this.FindControl<Button>("CopyPasswordButton");
            if (copyPasswordButton is not null)
                copyPasswordButton.Click += CopyPasswordButton_Click;

            var revealPasswordButton = this.FindControl<Button>("RevealPasswordButton");
            if (revealPasswordButton is not null)
                revealPasswordButton.Click += RevealPasswordButton_Click;

            var hidePasswordButton = this.FindControl<Button>("HidePasswordButton");
            if (hidePasswordButton is not null)
                hidePasswordButton.Click += HidePasswordButton_Click;
        }

        private void PinTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return)
                return;

            e.Handled = true;

            if (DataContext is PasswordVaultViewModel viewModel && viewModel.CanUnlock)
                viewModel.UnlockVaultCommand.Execute(null);
        }

        private async void AddButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not PasswordVaultViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new PasswordVaultEntryDialog();
            dialog.SetMode("Add Vault Entry");

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.AddEntry(dialog.EntryInput);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not PasswordVaultViewModel viewModel)
                return;

            var input = viewModel.GetSelectedEntryInput();
            if (input is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new PasswordVaultEntryDialog();
            dialog.SetMode("Edit Vault Entry", input);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.UpdateSelectedEntry(dialog.EntryInput);
        }

        private void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is PasswordVaultViewModel viewModel && viewModel.CanRemoveEntry)
                viewModel.RemoveSelectedEntry();
        }

        private async void CopyUserNameButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not PasswordVaultViewModel viewModel)
                return;

            await CopyToClipboardWithAutoClear(viewModel.SelectedUserNameForCopy, "Username copied. Clipboard will clear in 30 seconds.");
        }

        private async void CopyPasswordButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not PasswordVaultViewModel viewModel)
                return;

            await CopyToClipboardWithAutoClear(viewModel.SelectedPasswordForCopy, "Password copied. Clipboard will clear in 30 seconds.");
        }

        private async void RevealPasswordButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not PasswordVaultViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new VaultPinConfirmDialog();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                var result = await dialog.ShowDialog<bool>(owner);
                if (!result)
                    return;

                if (viewModel.RevealSelectedPassword(dialog.Pin))
                    return;

                dialog = new VaultPinConfirmDialog();
                dialog.SetValidationMessage("That PIN did not match this case.");
            }
        }

        private void HidePasswordButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is PasswordVaultViewModel viewModel)
                viewModel.HideSelectedPassword();
        }

        private async Task CopyToClipboardWithAutoClear(string value, string statusMessage)
        {
            if (DataContext is not PasswordVaultViewModel viewModel)
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                viewModel.SetClipboardStatus("Nothing was copied because the selected field is empty.");
                return;
            }

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
            {
                viewModel.SetClipboardStatus("Clipboard is not available.");
                return;
            }

            await clipboard.SetTextAsync(value);
            viewModel.SetClipboardStatus(statusMessage);

            _ = ClearClipboardLaterAsync(value);
        }

        private async Task ClearClipboardLaterAsync(string copiedValue)
        {
            await Task.Delay(30_000);

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
                return;

            await clipboard.SetTextAsync(string.Empty);

            if (DataContext is PasswordVaultViewModel viewModel)
                viewModel.SetClipboardStatus("Clipboard cleared.");
        }
    }
}
