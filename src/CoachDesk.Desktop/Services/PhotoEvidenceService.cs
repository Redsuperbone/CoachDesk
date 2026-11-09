using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public sealed class PhotoEvidenceService(IApiService apiService) : IPhotoEvidenceService
{
    public Task<IReadOnlyList<PhotoEvidenceDto>> GetAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        apiService.GetAsync<IReadOnlyList<PhotoEvidenceDto>>($"coach/advisees/{adviseeId}/photos", cancellationToken);

    public Task<byte[]> GetContentAsync(int adviseeId, int photoId, CancellationToken cancellationToken = default) =>
        apiService.GetBytesAsync($"coach/advisees/{adviseeId}/photos/{photoId}/content", cancellationToken);

}
