using Avalonia.Controls;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class ConfirmDeleteBillDialog : Window
    {
        public ConfirmDeleteBillDialog()
        {
            InitializeComponent();

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += (_, _) => Close(false);

            var removeButton = this.FindControl<Button>("RemoveButton");
            if (removeButton is not null)
                removeButton.Click += (_, _) => Close(true);
        }
    }
}
