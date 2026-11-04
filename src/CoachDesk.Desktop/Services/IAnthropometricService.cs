using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public interface IAnthropometricService
{
    Task<AnthropometricProfileDto> GetProfileAsync(Guid adviseeId, CancellationToken cancellationToken = default);

    Task SaveProfileAsync(AnthropometricProfileDto profile, CancellationToken cancellationToken = default);
}
