using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public sealed class AnthropometricService : IAnthropometricService
{
    private readonly IApiService _apiService;

    public AnthropometricService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<AnthropometricProfileDto> GetProfileAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        _apiService.GetAsync<AnthropometricProfileDto>(
            $"coach/advisees/{adviseeId}/anthropometry",
            cancellationToken);

    public Task SaveProfileAsync(AnthropometricProfileDto profile, CancellationToken cancellationToken = default) =>
        _apiService.PutAsync(
            $"coach/advisees/{profile.AdviseeId}/anthropometry",
            profile,
            cancellationToken);
}
