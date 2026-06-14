using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record DebtsLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<Debt> Debts);
