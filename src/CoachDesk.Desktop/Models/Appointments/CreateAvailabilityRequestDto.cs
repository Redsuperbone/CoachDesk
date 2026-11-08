namespace CoachDesk.Desktop.Models.Appointments;

public sealed class CreateAvailabilityRequestDto
{
    public DateTimeOffset StartsAt { get; init; }
    public int DurationMinutes { get; init; } = 60;
    public string Detail { get; init; } = string.Empty;
}
