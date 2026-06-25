using System;
using Avalonia.Controls;
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

            var startNewCaseButton = this.FindControl<Button>("StartNewCaseButton");
            if (startNewCaseButton is not null)
                startNewCaseButton.Click += StartNewCaseButton_Click;

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

        private void SecureLockActiveCase(string reason)
        {
            if (DataContext is MainWindowViewModel mainViewModel)
                mainViewModel.SecureLockActiveCase(reason);
        }
    }
}
