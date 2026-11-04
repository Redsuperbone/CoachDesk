using CoachDesk.Desktop.Models.Advisees;

namespace CoachDesk.Desktop.Services;

public interface IAdviseeService
{
    Task<IReadOnlyList<AdviseeSummaryDto>> GetActiveAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
}
