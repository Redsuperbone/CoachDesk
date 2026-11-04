using CoachDesk.Desktop.Models.Authentication;

namespace CoachDesk.Desktop.Services;

public sealed class AuthenticationSession : IAuthenticationSession
{
    public string? AccessToken { get; private set; }
    public CoachDto? CurrentCoach { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public void Start(LoginResponseDto response)
    {
        ArgumentNullException.ThrowIfNull(response);

        AccessToken = response.AccessToken;
        CurrentCoach = response.Coach;
    }

    public void Clear()
    {
        AccessToken = null;
        CurrentCoach = null;
    }
}
