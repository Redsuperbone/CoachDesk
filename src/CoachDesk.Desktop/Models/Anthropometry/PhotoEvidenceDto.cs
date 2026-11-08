namespace CoachDesk.Desktop.Models.Anthropometry;

public sealed class PhotoEvidenceDto
{
    public int Id { get; init; }
    public int Month { get; init; }
    public string Type { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
