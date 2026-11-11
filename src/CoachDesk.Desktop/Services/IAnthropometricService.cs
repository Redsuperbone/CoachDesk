using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public interface IAnthropometricService
{
    Task<AnthropometricProfileDto> GetProfileAsync(int adviseeId, CancellationToken cancellationToken = default);

    Task SaveProfileAsync(AnthropometricProfileDto profile, CancellationToken cancellationToken = default);

    Task ConfirmReviewAsync(int adviseeId, int month, CancellationToken cancellationToken = default);
}
