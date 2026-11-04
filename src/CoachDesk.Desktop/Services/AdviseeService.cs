using CoachDesk.Desktop.Models.Advisees;

namespace CoachDesk.Desktop.Services;

public sealed class AdviseeService : IAdviseeService
{
    private readonly IApiService _apiService;

    public AdviseeService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<IReadOnlyList<AdviseeSummaryDto>> GetActiveAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = "coach/advisees?active=true";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            endpoint += $"&search={Uri.EscapeDataString(searchTerm.Trim())}";
        }

        return _apiService.GetAsync<IReadOnlyList<AdviseeSummaryDto>>(endpoint, cancellationToken);
    }
}
