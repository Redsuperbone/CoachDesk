using CoachDesk.Desktop.Models.Appointments;

namespace CoachDesk.Desktop.Services;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetPublishedAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<AppointmentDto> PublishAsync(CreateAvailabilityRequestDto request, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
