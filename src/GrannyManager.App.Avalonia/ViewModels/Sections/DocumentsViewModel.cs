using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class DocumentsViewModel : ViewModelBase
{
    private readonly DocumentsService _documentsService;

    public DocumentsViewModel(ActiveCaseState activeCaseState, DocumentsService documentsService)
    {
        _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadDocuments();

        LoadDocuments();
    }

    public ObservableCollection<DocumentListItemViewModel> Documents { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDocument))]
    [NotifyPropertyChangedFor(nameof(SelectedTitle))]
    [NotifyPropertyChangedFor(nameof(SelectedCategory))]
    [NotifyPropertyChangedFor(nameof(SelectedTags))]
    [NotifyPropertyChangedFor(nameof(SelectedLinkedDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedImportant))]
    [NotifyPropertyChangedFor(nameof(SelectedActive))]
    [NotifyPropertyChangedFor(nameof(SelectedOriginalFileName))]
    [NotifyPropertyChangedFor(nameof(SelectedStoredFilePath))]
    [NotifyPropertyChangedFor(nameof(SelectedSourceFilePath))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedCreatedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedUpdatedUtc))]
    private DocumentListItemViewModel? _selectedDocument;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasActiveCase;

    public bool HasSelectedDocument => SelectedDocument is not null;

    private DocumentRecord? Document => SelectedDocument?.Document;

    public string SelectedTitle => Document?.Title ?? "No document selected";
    public string SelectedCategory => Clean(Document?.Category);
    public string SelectedTags => Clean(Document?.Tags);
    public string SelectedLinkedDisplay => Document?.LinkedDisplay ?? "None";
    public string SelectedImportant => YesNo(Document?.IsImportant);
    public string SelectedActive => YesNo(Document?.IsActive);
    public string SelectedOriginalFileName => Clean(Document?.OriginalFileName);
    public string SelectedStoredFilePath => Clean(Document?.StoredFilePath);
    public string SelectedSourceFilePath => Clean(Document?.SourceFilePath);
    public string SelectedNotes => Clean(Document?.Notes);
    public string SelectedCreatedUtc => FormatUtc(Document?.CreatedUtc);
    public string SelectedUpdatedUtc => FormatUtc(Document?.UpdatedUtc);

    partial void OnSelectedDocumentChanged(DocumentListItemViewModel? oldValue, DocumentListItemViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    [RelayCommand]
    private void MarkImportant()
    {
        StatusMessage = HasActiveCase
            ? "Important toggle will be wired in the next Documents pass."
            : "Open or create a case before marking documents important.";
    }

    [RelayCommand]
    private void OpenFile()
    {
        StatusMessage = HasSelectedDocument
            ? "Open file will be wired in the next Documents pass."
            : "Select a document first.";
    }

    private void LoadDocuments()
    {
        var result = _documentsService.LoadDocuments();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        Documents.Clear();

        var index = 0;
        foreach (var document in result.Documents)
        {
            Documents.Add(new DocumentListItemViewModel(document, index));
            index++;
        }

        SelectedDocument = Documents.FirstOrDefault();
    }

    private static string Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }

    private static string YesNo(bool? value)
    {
        return value == true ? "Yes" : "No";
    }

    private static string FormatUtc(DateTime? value)
    {
        if (value is null || value.Value == default)
            return "Not saved";

        return value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

public sealed partial class DocumentListItemViewModel : ObservableObject
{
    public DocumentListItemViewModel(DocumentRecord document, int index)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Index = index;
    }

    public DocumentRecord Document { get; }

    public int Index { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBackground))]
    [NotifyPropertyChangedFor(nameof(RowForeground))]
    [NotifyPropertyChangedFor(nameof(MutedForeground))]
    private bool _isSelected;

    public string Title => string.IsNullOrWhiteSpace(Document.Title) ? "Untitled Document" : Document.Title.Trim();
    public string Category => string.IsNullOrWhiteSpace(Document.Category) ? "Other" : Document.Category.Trim();
    public string Important => Document.ImportantText;
    public string Tags => string.IsNullOrWhiteSpace(Document.Tags) ? "None" : Document.Tags.Trim();
    public string LinkedDisplay => Document.LinkedDisplay;
    public string FileNameDisplay => Document.FileNameDisplay;
    public string Status => Document.StatusText;

    public bool IsInactive => !Document.IsActive;

    public string RowBackground
    {
        get
        {
            if (IsSelected)
                return "#2A6FA8";

            if (Document.IsImportant && Document.IsActive)
                return "#184E32";

            if (IsInactive)
                return "#1A1F29";

            return Index % 2 == 0 ? "#122238" : "#0F1B2A";
        }
    }

    public string RowForeground
    {
        get
        {
            if (IsSelected)
                return "White";

            return IsInactive ? "#7D8795" : "#DDE7F3";
        }
    }

    public string MutedForeground => IsSelected ? "White" : IsInactive ? "#707A88" : "#C8D4E2";
}
