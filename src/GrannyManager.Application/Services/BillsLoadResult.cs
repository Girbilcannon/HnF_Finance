using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record BillsLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<Bill> Bills);
