using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record IncomeLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<IncomeSource> Sources);
