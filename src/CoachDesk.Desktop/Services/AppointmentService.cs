using CoachDesk.Desktop.Models.Appointments;

namespace CoachDesk.Desktop.Services;

public sealed class AppointmentService : IAppointmentService
{
    private readonly IApiService _apiService;

    public AppointmentService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<IReadOnlyList<AppointmentDto>> GetPublishedAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        _apiService.GetAsync<IReadOnlyList<AppointmentDto>>(
            $"coach/appointments?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    public Task<AppointmentDto> PublishAsync(CreateAvailabilityRequestDto request, CancellationToken cancellationToken = default) =>
        _apiService.PostAsync<CreateAvailabilityRequestDto, AppointmentDto>("coach/appointments", request, cancellationToken);

    public async Task CancelAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        await _apiService.PostAsync<object, object>($"coach/appointments/{appointmentId}/cancel", new { }, cancellationToken);
    }

    public Task DeleteAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        _apiService.DeleteAsync($"coach/appointments/{appointmentId}", cancellationToken);
}
