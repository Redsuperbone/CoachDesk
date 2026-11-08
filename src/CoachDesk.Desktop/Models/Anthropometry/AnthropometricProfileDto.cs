namespace CoachDesk.Desktop.Models.Anthropometry;

public sealed class AnthropometricProfileDto
{
    public int AdviseeId { get; init; }
    public IReadOnlyList<AnthropometricAssessmentDto> Assessments { get; init; } = [];
}
