using CommunityToolkit.Mvvm.ComponentModel;
using GrannyManager.Application.Services;
using GrannyManager.Application.State;
using GrannyManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GrannyManager.App.Avalonia.ViewModels.Sections;

public sealed partial class DocumentsViewModel : ViewModelBase
{
    private readonly DocumentsService _documentsService;
    private readonly ObservableCollection<DocumentRowViewModel> _allDocuments = new();

    public DocumentsViewModel(ActiveCaseState activeCaseState, DocumentsService documentsService)
    {
        _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));

        if (activeCaseState is not null)
            activeCaseState.ActiveCaseChanged += (_, _) => LoadDocuments();

        AppDataChangeNotifier.DocumentsChanged += (_, _) => LoadDocuments();

        LoadDocuments();
    }

    public ObservableCollection<DocumentRowViewModel> Documents { get; } = new();
    public ObservableCollection<string> People { get; } = new();
    public ObservableCollection<string> Sections { get; } = new();
    public ObservableCollection<string> Tags { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDocument))]
    [NotifyPropertyChangedFor(nameof(CanEditDocument))]
    [NotifyPropertyChangedFor(nameof(CanRemoveDocument))]
    [NotifyPropertyChangedFor(nameof(CanOpenDocument))]
    [NotifyPropertyChangedFor(nameof(SelectedDisplayName))]
    [NotifyPropertyChangedFor(nameof(SelectedOriginalFileName))]
    [NotifyPropertyChangedFor(nameof(SelectedFolder))]
    [NotifyPropertyChangedFor(nameof(SelectedLinkedDisplay))]
    [NotifyPropertyChangedFor(nameof(SelectedTags))]
    [NotifyPropertyChangedFor(nameof(SelectedNotes))]
    [NotifyPropertyChangedFor(nameof(SelectedImportedUtc))]
    [NotifyPropertyChangedFor(nameof(SelectedFullPath))]
    private DocumentRowViewModel? _selectedDocument;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImportDocuments))]
    private bool _hasActiveCase;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _selectedPersonFilter = "All";

    [ObservableProperty]
    private string _selectedSectionFilter = "All";

    [ObservableProperty]
    private string _selectedTagFilter = "All";

    [ObservableProperty]
    private string _searchText = string.Empty;

    public bool HasSelectedDocument => SelectedDocument is not null;
    public bool CanImportDocuments => HasActiveCase;
    public bool CanEditDocument => HasActiveCase && HasSelectedDocument;
    public bool CanRemoveDocument => HasActiveCase && HasSelectedDocument;
    public bool CanOpenDocument => HasActiveCase && HasSelectedDocument;

    public string SelectedDisplayName => SelectedDocument?.Document.DisplayName ?? "No document selected";
    public string SelectedOriginalFileName => Clean(SelectedDocument?.Document.OriginalFileName);
    public string SelectedFolder => Clean(SelectedDocument?.Document.FolderDisplay);
    public string SelectedLinkedDisplay => Clean(SelectedDocument?.Document.LinkedDisplay);
    public string SelectedTags => Clean(SelectedDocument?.Document.TagDisplay);
    public string SelectedNotes => Clean(SelectedDocument?.Document.Notes);
    public string SelectedImportedUtc => FormatDate(SelectedDocument?.Document.ImportedUtc);
    public string SelectedFullPath => Clean(SelectedDocument?.Document.FullPath);

    partial void OnSelectedDocumentChanged(DocumentRowViewModel? oldValue, DocumentRowViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
            newValue.IsSelected = true;
    }

    partial void OnSelectedPersonFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedSectionFilterChanged(string value) => ApplyFilters();
    partial void OnSelectedTagFilterChanged(string value) => ApplyFilters();
    partial void OnSearchTextChanged(string value) => ApplyFilters();

    public void RefreshFromNavigation()
    {
        LoadDocuments();
    }

    public IReadOnlyList<string> GetPeopleForFolders()
    {
        return _documentsService.LoadPeopleForFolders();
    }

    public IReadOnlyList<DocumentConnectionOption> GetConnectionOptions(string section)
    {
        return _documentsService.LoadConnectionOptions(section);
    }

    public bool FolderExists(DocumentImportRequest request)
    {
        return _documentsService.FolderExists(request);
    }

    public bool ImportDocuments(DocumentImportRequest request)
    {
        if (!_documentsService.ImportDocuments(request, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadDocuments();
        StatusMessage = message;
        return true;
    }

    public DocumentRecord? CreateEditableCopyOfSelectedDocument()
    {
        var document = SelectedDocument?.Document;
        if (document is null)
            return null;

        return new DocumentRecord
        {
            Id = document.Id,
            DisplayName = document.DisplayName,
            OriginalFileName = document.OriginalFileName,
            StoredFileName = document.StoredFileName,
            RelativePath = document.RelativePath,
            FullPath = document.FullPath,
            PersonName = document.PersonName,
            Category = document.Category,
            LinkedSection = document.LinkedSection,
            LinkedRecordId = document.LinkedRecordId,
            LinkedRecordName = document.LinkedRecordName,
            CustomFolder = document.CustomFolder,
            Tags = document.Tags,
            Notes = document.Notes,
            IsMergedFile = document.IsMergedFile,
            PasswordProtectedRequested = document.PasswordProtectedRequested,
            ImportBatchId = document.ImportBatchId,
            IsActive = document.IsActive,
            ImportedUtc = document.ImportedUtc,
            UpdatedUtc = document.UpdatedUtc
        };
    }

    public bool SaveDocumentMetadata(DocumentRecord document, DocumentEditRequest request)
    {
        if (!_documentsService.SaveDocumentMetadata(document, request, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadDocuments();
        SelectedDocument = Documents.FirstOrDefault(row => row.Document.Id == document.Id) ?? Documents.FirstOrDefault();
        StatusMessage = message;
        return true;
    }

    public bool RemoveSelectedDocument(bool deleteFile)
    {
        var selectedId = SelectedDocument?.Document.Id ?? 0;
        if (!_documentsService.RemoveDocument(selectedId, deleteFile, out var message))
        {
            StatusMessage = message;
            return false;
        }

        LoadDocuments();
        StatusMessage = message;
        return true;
    }

    public void OpenSelectedDocument()
    {
        if (SelectedDocument is null)
            return;

        _documentsService.OpenDocument(SelectedDocument.Document, out var message);
        StatusMessage = message;
    }

    public void ShowSelectedDocumentInFileBrowser()
    {
        if (SelectedDocument is null)
            return;

        _documentsService.ShowInFileBrowser(SelectedDocument.Document, out var message);
        StatusMessage = message;
    }

    private void LoadDocuments()
    {
        var selectedId = SelectedDocument?.Document.Id ?? 0;
        var result = _documentsService.LoadDocuments();

        HasActiveCase = result.HasActiveCase;
        StatusMessage = result.StatusMessage;

        _allDocuments.Clear();
        var index = 0;
        foreach (var document in result.Documents)
        {
            _allDocuments.Add(new DocumentRowViewModel(document, index));
            index++;
        }

        RefreshFilterLists();
        ApplyFilters();

        SelectedDocument = Documents.FirstOrDefault(row => row.Document.Id == selectedId) ?? Documents.FirstOrDefault();
    }

    private void RefreshFilterLists()
    {
        var currentPerson = SelectedPersonFilter;
        var currentSection = SelectedSectionFilter;
        var currentTag = SelectedTagFilter;

        People.Clear();
        People.Add("All");
        foreach (var person in _allDocuments.Select(row => row.Document.PersonName).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
            People.Add(person);

        Sections.Clear();
        Sections.Add("All");
        foreach (var section in _allDocuments.Select(row => row.Document.LinkedSection).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
            Sections.Add(section);

        Tags.Clear();
        Tags.Add("All");
        foreach (var tag in _allDocuments.SelectMany(row => SplitTags(row.Document.Tags)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(tag => tag))
            Tags.Add(tag);

        SelectedPersonFilter = People.Contains(currentPerson) ? currentPerson : "All";
        SelectedSectionFilter = Sections.Contains(currentSection) ? currentSection : "All";
        SelectedTagFilter = Tags.Contains(currentTag) ? currentTag : "All";
    }

    private void ApplyFilters()
    {
        var selectedId = SelectedDocument?.Document.Id ?? 0;
        var query = SearchText?.Trim() ?? string.Empty;

        var filtered = _allDocuments.AsEnumerable();

        if (SelectedPersonFilter != "All")
            filtered = filtered.Where(row => string.Equals(row.Document.PersonName, SelectedPersonFilter, StringComparison.OrdinalIgnoreCase));

        if (SelectedSectionFilter != "All")
            filtered = filtered.Where(row => string.Equals(row.Document.LinkedSection, SelectedSectionFilter, StringComparison.OrdinalIgnoreCase));

        if (SelectedTagFilter != "All")
            filtered = filtered.Where(row => SplitTags(row.Document.Tags).Any(tag => string.Equals(tag, SelectedTagFilter, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(row =>
                Contains(row.Document.DisplayName, query) ||
                Contains(row.Document.OriginalFileName, query) ||
                Contains(row.Document.Tags, query) ||
                Contains(row.Document.LinkedRecordName, query) ||
                Contains(row.Document.PersonName, query) ||
                Contains(row.Document.Category, query));
        }

        Documents.Clear();
        foreach (var row in filtered)
            Documents.Add(row);

        SelectedDocument = Documents.FirstOrDefault(row => row.Document.Id == selectedId) ?? Documents.FirstOrDefault();
    }

    private static bool Contains(string? value, string query)
    {
        return (value ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SplitTags(string tags)
    {
        return (tags ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag));
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    private static string FormatDate(DateTime? value) => value is null || value.Value == default ? "Not saved" : value.Value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
}

public sealed partial class DocumentRowViewModel : ObservableObject
{
    public DocumentRowViewModel(DocumentRecord document, int index)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Index = index;
    }

    public DocumentRecord Document { get; }
    public int Index { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayName => string.IsNullOrWhiteSpace(Document.DisplayName) ? "Unnamed Document" : Document.DisplayName.Trim();
    public string PersonCategory => Document.FolderDisplay;
    public string LinkedDisplay => Document.LinkedDisplay;
    public string Tags => Document.TagDisplay;
    public string NameForeground => Document.IsActive ? "White" : "#7D8795";
    public string DetailForeground => Document.IsActive ? "#C8D4E2" : "#707A88";
}
