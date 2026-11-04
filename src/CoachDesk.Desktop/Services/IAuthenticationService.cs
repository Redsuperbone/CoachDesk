using CoachDesk.Desktop.Models.Authentication;

namespace CoachDesk.Desktop.Services;

public interface IAuthenticationService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
