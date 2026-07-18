namespace GrannyManager.Application.Services;

public sealed class LiveSearchResult
{
    public string Section { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string NavigateSection { get; init; } = string.Empty;
    public long TargetId { get; init; }
}
