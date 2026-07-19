using System;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using GrannyManager.Application.Services;
using GrannyManager.App.Avalonia.Services.Security;
using GrannyManager.App.Avalonia.ViewModels;

namespace GrannyManager.App.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private readonly CaseSecurityLockCoordinator _caseSecurityLockCoordinator;

        public MainWindow()
        {
            InitializeComponent();

            BuildWindowContextMenu();

            var startNewCaseButton = this.FindControl<Button>("StartNewCaseButton");
            if (startNewCaseButton is not null)
                startNewCaseButton.Click += StartNewCaseButton_Click;

            var caseSetupWizardButton = this.FindControl<Button>("CaseSetupWizardButton");
            if (caseSetupWizardButton is not null)
                caseSetupWizardButton.Click += CaseSetupWizardButton_Click;

            var globalSearchTextBox = this.FindControl<TextBox>("GlobalSearchTextBox");
            if (globalSearchTextBox is not null)
            {
                globalSearchTextBox.GotFocus += (_, _) =>
                {
                    if (DataContext is MainWindowViewModel viewModel)
                        viewModel.IsSearchBoxFocused = true;
                };

                globalSearchTextBox.LostFocus += async (_, _) =>
                {
                    await System.Threading.Tasks.Task.Delay(180);

                    if (DataContext is MainWindowViewModel viewModel)
                        viewModel.IsSearchBoxFocused = false;
                };
            }

            _caseSecurityLockCoordinator = new CaseSecurityLockCoordinator(reason => SecureLockActiveCase(reason));
            _caseSecurityLockCoordinator.Start();

            Closed += (_, _) => _caseSecurityLockCoordinator.Dispose();
        }

        private async void StartNewCaseButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel mainViewModel)
                return;

            var dashboard = mainViewModel.Dashboard;
            var dialog = new NewCaseDialog(dashboard.DefaultCaseRootFolder);

            while (true)
            {
                var result = await dialog.ShowDialog<bool>(this);
                if (!result)
                    return;

                if (dialog.Request is null)
                    return;

                if (dashboard.TryCreateCase(dialog.Request, out var message))
                    return;

                dialog = new NewCaseDialog(dashboard.DefaultCaseRootFolder);
                dialog.SetValidationMessage(message);
            }
        }


        private async void CaseSetupWizardButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel mainViewModel)
                return;

            var wizard = new FinanceSetupWizardWindow(mainViewModel.Dashboard);
            await wizard.ShowDialog(this);
        }



        private void BuildWindowContextMenu()
        {
            var refreshMenuItem = new MenuItem
            {
                Header = "Refresh app"
            };
            refreshMenuItem.Click += RefreshApplicationMenuItem_Click;

            ContextMenu = new ContextMenu
            {
                Items =
                {
                    refreshMenuItem
                }
            };
        }

        private void RefreshApplicationMenuItem_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            RefreshApplicationWithoutChangingCaseOrSection();
        }

        private void RefreshApplicationWithoutChangingCaseOrSection()
        {
            if (DataContext is null)
                return;

            AppDataChangeNotifier.NotifyAllFinanceChanged();

            var sectionProperties = DataContext
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.GetIndexParameters().Length == 0);

            foreach (var property in sectionProperties)
            {
                var sectionViewModel = property.GetValue(DataContext);
                if (sectionViewModel is null)
                    continue;

                var refreshMethod =
                    sectionViewModel.GetType().GetMethod("RefreshFromNavigation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadBills", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadAssets", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadSources", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadDebts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadPeople", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    sectionViewModel.GetType().GetMethod("LoadDocuments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (refreshMethod is not null && refreshMethod.GetParameters().Length == 0)
                    refreshMethod.Invoke(sectionViewModel, null);
            }
        }

        private void SecureLockActiveCase(string reason)
        {
            if (DataContext is MainWindowViewModel mainViewModel)
                mainViewModel.SecureLockActiveCase(reason);
        }
    }
}
