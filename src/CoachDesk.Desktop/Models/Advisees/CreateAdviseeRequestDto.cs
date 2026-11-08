namespace CoachDesk.Desktop.Models.Advisees;

public sealed class CreateAdviseeRequestDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Sex { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Occupation { get; init; }
    public string? Goal { get; init; }
    public DateOnly? CoachingStartDate { get; init; }
}
