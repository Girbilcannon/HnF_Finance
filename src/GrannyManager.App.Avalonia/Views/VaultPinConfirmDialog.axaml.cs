using Avalonia.Controls;
using Avalonia.Input;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class VaultPinConfirmDialog : Window
    {
        private readonly TextBox? _pinTextBox;
        private readonly TextBlock? _validationTextBlock;

        public VaultPinConfirmDialog()
        {
            InitializeComponent();

            _pinTextBox = this.FindControl<TextBox>("PinTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            if (_pinTextBox is not null)
                _pinTextBox.KeyDown += PinTextBox_KeyDown;

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var revealButton = this.FindControl<Button>("RevealButton");
            if (revealButton is not null)
                revealButton.Click += (_, _) => Close(true);
        }

        public string Pin => _pinTextBox?.Text ?? string.Empty;

        public void SetValidationMessage(string message)
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = message;
        }

        private void PinTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                Close(true);
            }

            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close(false);
            }
        }
    }
}
