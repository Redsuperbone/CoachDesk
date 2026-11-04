using CoachDesk.Desktop.Models.Authentication;

namespace CoachDesk.Desktop.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private const string LoginEndpoint = "auth/login";
    private readonly IApiService _apiService;
    private readonly IAuthenticationSession _session;

    public AuthenticationService(IApiService apiService, IAuthenticationSession session)
    {
        _apiService = apiService;
        _session = session;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _apiService.PostAsync<LoginRequestDto, LoginResponseDto>(
            LoginEndpoint,
            request,
            cancellationToken);

        if (!response.Coach.IsCoach)
        {
            _session.Clear();
            throw new UnauthorizedAccessException("Esta aplicación solo está disponible para usuarios con rol Coach.");
        }

        _session.Start(response);
        return response;
    }
}
