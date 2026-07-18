using Avalonia.Controls;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class ExistingFolderChoiceDialog : Window
    {
        public ExistingFolderChoiceDialog()
        {
            InitializeComponent();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var existingButton = this.FindControl<Button>("ExistingFolderButton");
            if (existingButton is not null)
                existingButton.Click += (_, _) =>
                {
                    AddToExistingFolder = true;
                    Close(true);
                };

            var newButton = this.FindControl<Button>("NewDatedFolderButton");
            if (newButton is not null)
                newButton.Click += (_, _) =>
                {
                    AddToExistingFolder = false;
                    Close(true);
                };
        }

        public bool AddToExistingFolder { get; private set; } = true;

        public void SetFolderName(string folderName)
        {
            var message = this.FindControl<TextBlock>("MessageTextBlock");
            if (message is not null)
                message.Text = $"A folder named \"{folderName}\" already exists.";
        }
    }
}
