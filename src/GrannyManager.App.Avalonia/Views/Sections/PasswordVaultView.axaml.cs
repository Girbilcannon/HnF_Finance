using Avalonia.Controls;
using Avalonia.Input;
using GrannyManager.App.Avalonia.ViewModels.Sections;

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
        }

        private void PinTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return)
                return;

            e.Handled = true;

            if (DataContext is PasswordVaultViewModel viewModel && viewModel.CanUnlock)
                viewModel.UnlockVaultCommand.Execute(null);
        }
    }
}
