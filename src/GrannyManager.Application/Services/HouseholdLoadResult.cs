using GrannyManager.Core.Models;

namespace GrannyManager.Application.Services;

public sealed record HouseholdLoadResult(
    bool HasActiveCase,
    string StatusMessage,
    IReadOnlyList<HouseholdPerson> People);
