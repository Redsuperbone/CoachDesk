using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public interface IPhotoEvidenceService
{
    Task<IReadOnlyList<PhotoEvidenceDto>> GetAsync(int adviseeId, CancellationToken cancellationToken = default);
    Task<PhotoEvidenceDto> UploadAsync(int adviseeId, int month, string type, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default);
}
