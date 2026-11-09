using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public interface IPhotoEvidenceService
{
    Task<IReadOnlyList<PhotoEvidenceDto>> GetAsync(int adviseeId, CancellationToken cancellationToken = default);
    Task<byte[]> GetContentAsync(int adviseeId, int photoId, CancellationToken cancellationToken = default);
}
