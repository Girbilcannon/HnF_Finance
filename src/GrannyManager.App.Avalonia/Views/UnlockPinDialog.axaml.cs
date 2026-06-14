using Avalonia.Controls;
using Avalonia.Input;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class UnlockPinDialog : Window
    {
        private readonly TextBox? _pinTextBox;
        private readonly TextBlock? _validationTextBlock;

        public UnlockPinDialog()
        {
            InitializeComponent();

            _pinTextBox = this.FindControl<TextBox>("PinTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            if (_pinTextBox is not null)
                _pinTextBox.KeyDown += PinTextBox_KeyDown;

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var unlockButton = this.FindControl<Button>("UnlockButton");
            if (unlockButton is not null)
                unlockButton.Click += (_, _) => Close(true);
        }

        public string Pin => _pinTextBox?.Text ?? string.Empty;

        private void PinTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                Close(true);
            }
        }

        public void SetValidationMessage(string message)
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = message;
        }
    }
}
