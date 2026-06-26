using Avalonia.Controls;
using Avalonia.Input;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class VaultPinConfirmDialog : Window
    {
        private readonly TextBlock? _titleTextBlock;
        private readonly TextBlock? _messageTextBlock;
        private readonly Button? _revealButton;
        private readonly TextBox? _pinTextBox;
        private readonly TextBlock? _validationTextBlock;

        public VaultPinConfirmDialog()
        {
            InitializeComponent();

            _titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _revealButton = this.FindControl<Button>("RevealButton");
            _pinTextBox = this.FindControl<TextBox>("PinTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            if (_pinTextBox is not null)
                _pinTextBox.KeyDown += PinTextBox_KeyDown;

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            if (_revealButton is not null)
                _revealButton.Click += (_, _) => Close(true);
        }

        public string Pin => _pinTextBox?.Text ?? string.Empty;

        public void SetPrompt(string title, string message, string confirmButtonText)
        {
            if (_titleTextBlock is not null)
                _titleTextBlock.Text = title;

            if (_messageTextBlock is not null)
                _messageTextBlock.Text = message;

            if (_revealButton is not null)
                _revealButton.Content = confirmButtonText;
        }

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
