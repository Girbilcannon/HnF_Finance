using System.Linq;
using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            var browseCaseButton = this.FindControl<Button>("BrowseCaseButton");
            if (browseCaseButton is not null)
                browseCaseButton.Click += BrowseCaseButton_Click;

            var recentCasesListBox = this.FindControl<ListBox>("RecentCasesListBox");
            if (recentCasesListBox is not null)
                recentCasesListBox.DoubleTapped += (_, _) => OpenSelectedRecentCaseWithPinIfNeeded();

            var openSelectedCaseButton = this.FindControl<Button>("OpenSelectedCaseButton");
            if (openSelectedCaseButton is not null)
                openSelectedCaseButton.Click += (_, _) => OpenSelectedRecentCaseWithPinIfNeeded();
        }

        private async void BrowseCaseButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DashboardViewModel viewModel)
                return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Open Home & Family Finance Case",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new global::Avalonia.Platform.Storage.FilePickerFileType("Home & Family Finance case")
                    {
                        Patterns = new[] { "*.gmcase" }
                    },
                    new global::Avalonia.Platform.Storage.FilePickerFileType("All files")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            });

            var file = files.FirstOrDefault();
            if (file is not null)
                await OpenCaseWithPinIfNeeded(file.Path.LocalPath);
        }

        public async void OpenSelectedRecentCaseWithPinIfNeeded()
        {
            if (DataContext is not DashboardViewModel viewModel)
                return;

            if (viewModel.SelectedRecentCase is null || viewModel.SelectedRecentCase.IsPlaceholder)
                return;

            await OpenCaseWithPinIfNeeded(viewModel.SelectedRecentCase.CaseFilePath);
        }

        private async System.Threading.Tasks.Task OpenCaseWithPinIfNeeded(string caseFilePath)
        {
            if (DataContext is not DashboardViewModel viewModel)
                return;

            if (viewModel.TryOpenCaseFile(caseFilePath, out var requiresPin))
                return;

            if (!requiresPin)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new UnlockPinDialog();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                var result = await dialog.ShowDialog<bool>(owner);
                if (!result)
                {
                    viewModel.CancelPinUnlock();
                    return;
                }

                if (viewModel.CompletePinUnlock(dialog.Pin, out var message))
                    return;

                dialog = new UnlockPinDialog();
                dialog.SetValidationMessage(message);
            }
        }
    }
}
