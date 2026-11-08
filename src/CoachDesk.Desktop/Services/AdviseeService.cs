using CoachDesk.Desktop.Models.Advisees;

namespace CoachDesk.Desktop.Services;

public sealed class AdviseeService : IAdviseeService
{
    private readonly IApiService _apiService;

    public AdviseeService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<AdviseeSummaryDto> CreateAsync(CreateAdviseeRequestDto request, CancellationToken cancellationToken = default) =>
        _apiService.PostAsync<CreateAdviseeRequestDto, AdviseeSummaryDto>("coach/advisees", request, cancellationToken);

    public Task PauseAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        _apiService.PutAsync($"coach/advisees/{adviseeId}/service-status", new UpdateAdviseeServiceStatusRequestDto(false), cancellationToken);

    public Task DeleteAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        _apiService.DeleteAsync($"coach/advisees/{adviseeId}", cancellationToken);

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
