using Avalonia.Controls;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class ConfirmDeleteVaultEntryDialog : Window
    {
        private readonly TextBlock? _messageTextBlock;

        public ConfirmDeleteVaultEntryDialog()
        {
            InitializeComponent();

            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var deleteButton = this.FindControl<Button>("DeleteButton");
            if (deleteButton is not null)
                deleteButton.Click += (_, _) => Close(true);
        }

        public void SetEntryTitle(string title)
        {
            if (_messageTextBlock is not null)
                _messageTextBlock.Text = "Are you sure you would like to delete this entry?";
        }
    }
}
