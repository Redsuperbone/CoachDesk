namespace CoachDesk.Desktop.Models.Advisees;

public sealed class AdviseeSummaryDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? BirthDate { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastAssessmentAt { get; init; }

    public int? Age => BirthDate is null
        ? null
        : DateTime.Today.Year - BirthDate.Value.Year - (DateTime.Today.DayOfYear < BirthDate.Value.DayOfYear ? 1 : 0);
}
