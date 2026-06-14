using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record DocumentsLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<DocumentRecord> Documents);
