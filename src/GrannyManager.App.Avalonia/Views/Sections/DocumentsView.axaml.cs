using Avalonia.Controls;
using GrannyManager.App.Avalonia.ViewModels.Sections;
using GrannyManager.App.Avalonia.Views;

namespace GrannyManager.App.Avalonia.Views.Sections
{
    public partial class DocumentsView : UserControl
    {
        public DocumentsView()
        {
            InitializeComponent();

            var importButton = this.FindControl<Button>("ImportDocumentsButton");
            if (importButton is not null)
                importButton.Click += ImportButton_Click;

            var openButton = this.FindControl<Button>("OpenDocumentButton");
            if (openButton is not null)
                openButton.Click += (_, _) => (DataContext as DocumentsViewModel)?.OpenSelectedDocument();

            var showButton = this.FindControl<Button>("ShowDocumentButton");
            if (showButton is not null)
                showButton.Click += (_, _) => (DataContext as DocumentsViewModel)?.ShowSelectedDocumentInFileBrowser();

            var editButton = this.FindControl<Button>("EditDocumentButton");
            if (editButton is not null)
                editButton.Click += EditButton_Click;

            var removeButton = this.FindControl<Button>("RemoveDocumentButton");
            if (removeButton is not null)
                removeButton.Click += RemoveButton_Click;
        }

        private async void ImportButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DocumentsViewModel viewModel)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new DocumentImportDialog();
            dialog.SetMode(
                viewModel.GetPeopleForFolders(),
                viewModel.GetConnectionOptions,
                viewModel.FolderExists);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.ImportDocuments(dialog.Request);
        }

        private async void EditButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DocumentsViewModel viewModel)
                return;

            var document = viewModel.CreateEditableCopyOfSelectedDocument();
            if (document is null)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new DocumentEditDialog();
            dialog.SetMode(
                document,
                viewModel.GetPeopleForFolders(),
                viewModel.GetConnectionOptions);

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.SaveDocumentMetadata(document, dialog.Request);
        }

        private async void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is not DocumentsViewModel viewModel || !viewModel.CanRemoveDocument)
                return;

            var owner = TopLevel.GetTopLevel(this) as Window;
            if (owner is null)
                return;

            var dialog = new ConfirmRemoveDocumentDialog();

            var result = await dialog.ShowDialog<bool>(owner);
            if (result)
                viewModel.RemoveSelectedDocument(dialog.DeleteFile);
        }
    }
}
