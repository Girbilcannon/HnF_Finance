using Avalonia.Controls;
using Avalonia.Input;
using GrannyManager.App.Avalonia.ViewModels.Sections;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class PasswordVaultEntryDialog : Window
    {
        private readonly TextBlock? _titleTextBlock;
        private readonly TextBox? _entryTitleTextBox;
        private readonly TextBox? _websiteTextBox;
        private readonly TextBox? _userNameTextBox;
        private readonly TextBox? _passwordTextBox;
        private readonly TextBox? _publicNotesTextBox;
        private readonly TextBox? _secureNotesTextBox;
        private readonly TextBlock? _validationTextBlock;

        public PasswordVaultEntryDialog()
        {
            InitializeComponent();

            _titleTextBlock = this.FindControl<TextBlock>("TitleTextBlock");
            _entryTitleTextBox = this.FindControl<TextBox>("EntryTitleTextBox");
            _websiteTextBox = this.FindControl<TextBox>("WebsiteTextBox");
            _userNameTextBox = this.FindControl<TextBox>("UserNameTextBox");
            _passwordTextBox = this.FindControl<TextBox>("PasswordTextBox");
            _publicNotesTextBox = this.FindControl<TextBox>("PublicNotesTextBox");
            _secureNotesTextBox = this.FindControl<TextBox>("SecureNotesTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var saveButton = this.FindControl<Button>("SaveButton");
            if (saveButton is not null)
                saveButton.Click += (_, _) => TrySave();

            KeyDown += Dialog_KeyDown;
        }

        public PasswordVaultEntryInput EntryInput => new(
            _entryTitleTextBox?.Text ?? string.Empty,
            _userNameTextBox?.Text ?? string.Empty,
            _passwordTextBox?.Text ?? string.Empty,
            _websiteTextBox?.Text ?? string.Empty,
            _publicNotesTextBox?.Text ?? string.Empty,
            _secureNotesTextBox?.Text ?? string.Empty);

        public void SetMode(string title, PasswordVaultEntryInput? input = null)
        {
            if (_titleTextBlock is not null)
                _titleTextBlock.Text = title;

            if (input is null)
                return;

            if (_entryTitleTextBox is not null)
                _entryTitleTextBox.Text = input.Title;

            if (_userNameTextBox is not null)
                _userNameTextBox.Text = input.UserName;

            if (_passwordTextBox is not null)
                _passwordTextBox.Text = input.Password;

            if (_websiteTextBox is not null)
                _websiteTextBox.Text = input.Website;

            if (_publicNotesTextBox is not null)
                _publicNotesTextBox.Text = input.PublicNotes;

            if (_secureNotesTextBox is not null)
                _secureNotesTextBox.Text = input.SecureNotes;
        }

        private void Dialog_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close(false);
            }
        }

        private void TrySave()
        {
            var input = EntryInput;

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                SetValidationMessage("Enter a title for this vault entry.");
                return;
            }

            if (string.IsNullOrWhiteSpace(input.UserName) && string.IsNullOrWhiteSpace(input.Password) && string.IsNullOrWhiteSpace(input.Website))
            {
                SetValidationMessage("Enter at least a username, password, or website.");
                return;
            }

            Close(true);
        }

        private void SetValidationMessage(string message)
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = message;
        }
    }
}
