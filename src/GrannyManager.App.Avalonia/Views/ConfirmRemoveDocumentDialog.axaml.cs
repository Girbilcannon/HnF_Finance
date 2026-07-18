using Avalonia.Controls;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class ConfirmRemoveDocumentDialog : Window
    {
        private readonly CheckBox? _deleteFileCheckBox;

        public ConfirmRemoveDocumentDialog()
        {
            InitializeComponent();

            _deleteFileCheckBox = this.FindControl<CheckBox>("DeleteFileCheckBox");

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var removeButton = this.FindControl<Button>("RemoveButton");
            if (removeButton is not null)
                removeButton.Click += (_, _) =>
                {
                    DeleteFile = _deleteFileCheckBox?.IsChecked == true;
                    Close(true);
                };
        }

        public bool DeleteFile { get; private set; }
    }
}
