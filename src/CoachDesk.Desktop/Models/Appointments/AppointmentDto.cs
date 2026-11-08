namespace CoachDesk.Desktop.Models.Appointments;

public sealed class AppointmentDto
{
    public Guid Id { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public int DurationMinutes { get; init; } = 60;
    public string Detail { get; init; } = string.Empty;
    public string? AdviseeName { get; init; }
    public AppointmentStatus Status { get; init; }
}
