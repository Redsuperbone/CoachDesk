using CoachDesk.Desktop.Models.Advisees;

namespace CoachDesk.Desktop.Services;

public interface IAdviseeService
{
    Task<AdviseeSummaryDto> CreateAsync(CreateAdviseeRequestDto request, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int adviseeId, CancellationToken cancellationToken = default);

    Task PauseAsync(int adviseeId, CancellationToken cancellationToken = default);

    Task DeleteAsync(int adviseeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdviseeSummaryDto>> GetActiveAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdviseeSummaryDto>> GetInactiveAsync(string? searchTerm = null, CancellationToken cancellationToken = default);
}
