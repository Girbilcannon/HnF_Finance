using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record AllowanceSavingsLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<AllowanceSavingsItem> Items);
