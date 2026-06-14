using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record AssetsLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<AssetItem> Assets);
