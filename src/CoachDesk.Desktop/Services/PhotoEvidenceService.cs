using CoachDesk.Desktop.Models.Anthropometry;

namespace CoachDesk.Desktop.Services;

public sealed class PhotoEvidenceService(IApiService apiService) : IPhotoEvidenceService
{
    public Task<IReadOnlyList<PhotoEvidenceDto>> GetAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        apiService.GetAsync<IReadOnlyList<PhotoEvidenceDto>>($"coach/advisees/{adviseeId}/photos", cancellationToken);

    public Task<PhotoEvidenceDto> UploadAsync(int adviseeId, int period, int month, string type, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default) =>
        apiService.PostMultipartAsync<PhotoEvidenceDto>($"coach/advisees/{adviseeId}/photos", new Dictionary<string, string> { ["period"] = period.ToString(), ["month"] = month.ToString(), ["type"] = type }, "file", fileName, contentType, content, cancellationToken);
}
