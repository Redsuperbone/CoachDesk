using CoachDesk.Desktop.Models.Authentication;

namespace CoachDesk.Desktop.Services;

public interface IAuthenticationSession
{
    string? AccessToken { get; }
    CoachDto? CurrentCoach { get; }
    bool IsAuthenticated { get; }

    void Start(LoginResponseDto response);
    void Clear();
}
