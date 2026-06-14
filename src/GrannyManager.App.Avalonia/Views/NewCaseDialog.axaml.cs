using System.Linq;
using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class NewCaseDialog : Window
    {
        private readonly TextBox? _primaryPersonTextBox;
        private readonly TextBox? _caseNameTextBox;
        private readonly TextBox? _managerNameTextBox;
        private readonly TextBox? _caseRootTextBox;
        private readonly TextBox? _pinTextBox;
        private readonly TextBox? _confirmPinTextBox;
        private readonly TextBlock? _validationTextBlock;

        public NewCaseDialog(string defaultCaseRoot)
        {
            InitializeComponent();

            _primaryPersonTextBox = this.FindControl<TextBox>("PrimaryPersonTextBox");
            _caseNameTextBox = this.FindControl<TextBox>("CaseNameTextBox");
            _managerNameTextBox = this.FindControl<TextBox>("ManagerNameTextBox");
            _caseRootTextBox = this.FindControl<TextBox>("CaseRootTextBox");
            _pinTextBox = this.FindControl<TextBox>("PinTextBox");
            _confirmPinTextBox = this.FindControl<TextBox>("ConfirmPinTextBox");
            _validationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            if (_caseRootTextBox is not null)
                _caseRootTextBox.Text = defaultCaseRoot;

            var browseButton = this.FindControl<Button>("BrowseButton");
            if (browseButton is not null)
                browseButton.Click += BrowseButton_Click;

            var cancelButton = this.FindControl<Button>("CancelButton");
            if (cancelButton is not null)
                cancelButton.Click += CancelButton_Click;

            var getStartedButton = this.FindControl<Button>("GetStartedButton");
            if (getStartedButton is not null)
                getStartedButton.Click += GetStartedButton_Click;
        }

        public NewCaseRequest? Request { get; private set; }

        public void SetValidationMessage(string message)
        {
            if (_validationTextBlock is not null)
                _validationTextBlock.Text = message;
        }

        private async void BrowseButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new global::Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Choose where case folders should be created",
                AllowMultiple = false
            });

            var folder = folders.FirstOrDefault();
            if (folder is not null && _caseRootTextBox is not null)
                _caseRootTextBox.Text = folder.Path.LocalPath;
        }

        private void CancelButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close(false);
        }

        private void GetStartedButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            Request = new NewCaseRequest
            {
                PrimaryPersonName = _primaryPersonTextBox?.Text ?? string.Empty,
                CaseName = _caseNameTextBox?.Text ?? string.Empty,
                CaseManagerName = _managerNameTextBox?.Text ?? string.Empty,
                CaseRootFolder = _caseRootTextBox?.Text ?? string.Empty,
                SecurityPin = _pinTextBox?.Text ?? string.Empty,
                ConfirmSecurityPin = _confirmPinTextBox?.Text ?? string.Empty
            };

            Close(true);
        }
    }
}
