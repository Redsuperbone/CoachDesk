using CoachDesk.Desktop.Models.Appointments;

namespace CoachDesk.Desktop.ViewModels;

public sealed class AppointmentRowViewModel(AppointmentDto appointment) : ViewModelBase
{
    public Guid Id { get; } = appointment.Id;
    public DateTimeOffset StartsAt { get; } = appointment.StartsAt;
    public int DurationMinutes { get; } = appointment.DurationMinutes;
    public string Date => StartsAt.LocalDateTime.ToString("dddd dd MMM, yyyy");
    public string Time => StartsAt.LocalDateTime.ToString("hh:mm tt");
    public string Detail { get; } = appointment.Detail;
    public string AdviseeName => string.IsNullOrWhiteSpace(appointment.AdviseeName) ? "-- Libre --" : appointment.AdviseeName;
    public AppointmentStatus Status { get; } = appointment.Status;
    public string StatusLabel => Status switch
    {
        AppointmentStatus.Booked => "Ocupado",
        AppointmentStatus.Available => "Disponible",
        _ => "Cancelado"
    };
    public string ActionLabel => Status == AppointmentStatus.Booked ? "Cancelar" : "Eliminar";
}
