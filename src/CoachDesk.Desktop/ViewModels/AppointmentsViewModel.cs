using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Appointments;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class AppointmentsViewModel : ViewModelBase
{
    private readonly IAppointmentService _appointmentService;
    private DateTime? _selectedDate = DateTime.Today;
    private string _timeText = "18:00";
    private string _detail = string.Empty;
    private int _durationMinutes = 60;
    private AppointmentRowViewModel? _selectedAppointment;
    private bool _isLoading;
    private bool _isPublishing;
    private string? _errorMessage;
    private string? _successMessage;

    public AppointmentsViewModel(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
        PublishCommand = new AsyncRelayCommand(_ => PublishAsync(), _ => CanPublish);
        CancelOrDeleteCommand = new AsyncRelayCommand(_ => CancelOrDeleteAsync(), _ => SelectedAppointment is not null && !IsLoading);
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync(), _ => !IsLoading);
    }

    public ObservableCollection<AppointmentRowViewModel> Appointments { get; } = [];
    public DateTime? SelectedDate { get => _selectedDate; set { if (SetProperty(ref _selectedDate, value)) RefreshCommands(); } }
    public string TimeText { get => _timeText; set { if (SetProperty(ref _timeText, value)) RefreshCommands(); } }
    public string Detail { get => _detail; set { if (SetProperty(ref _detail, value)) RefreshCommands(); } }
    public int DurationMinutes { get => _durationMinutes; set { if (SetProperty(ref _durationMinutes, Math.Clamp(value, 15, 240))) RefreshCommands(); } }
    public AppointmentRowViewModel? SelectedAppointment { get => _selectedAppointment; set { if (SetProperty(ref _selectedAppointment, value)) CancelOrDeleteCommand.RaiseCanExecuteChanged(); } }
    public bool IsLoading { get => _isLoading; private set { if (SetProperty(ref _isLoading, value)) RefreshCommands(); } }
    public bool IsPublishing { get => _isPublishing; private set { if (SetProperty(ref _isPublishing, value)) RefreshCommands(); } }
    public string? ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public string? SuccessMessage { get => _successMessage; private set { if (SetProperty(ref _successMessage, value)) OnPropertyChanged(nameof(HasSuccess)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool CanPublish => !IsLoading && !IsPublishing && SelectedDate is not null && !string.IsNullOrWhiteSpace(Detail) && TryGetStart(out _);
    public AsyncRelayCommand PublishCommand { get; }
    public AsyncRelayCommand CancelOrDeleteCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var appointments = await _appointmentService.GetPublishedAsync(today, today.AddDays(30));
            Appointments.Clear();
            foreach (var appointment in appointments.OrderBy(item => item.StartsAt)) Appointments.Add(new AppointmentRowViewModel(appointment));
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible cargar los horarios publicados.";
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PublishAsync()
    {
        if (!TryGetStart(out var startsAt))
        {
            ErrorMessage = "Indica una hora válida con formato 24 horas, por ejemplo 18:00.";
            return;
        }

        if (HasOverlap(startsAt))
        {
            ErrorMessage = "El horario se traslapa con una cita publicada.";
            return;
        }

        IsPublishing = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var appointment = await _appointmentService.PublishAsync(new CreateAvailabilityRequestDto
            {
                StartsAt = startsAt,
                DurationMinutes = DurationMinutes,
                Detail = Detail.Trim()
            });
            Appointments.Add(new AppointmentRowViewModel(appointment));
            Detail = string.Empty;
            SuccessMessage = "Horario publicado correctamente.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible publicar el horario. Intenta nuevamente.";
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsPublishing = false;
        }
    }

    private async Task CancelOrDeleteAsync()
    {
        if (SelectedAppointment is null) return;
        ErrorMessage = null;
        try
        {
            if (SelectedAppointment.Status == AppointmentStatus.Booked) await _appointmentService.CancelAsync(SelectedAppointment.Id);
            else await _appointmentService.DeleteAsync(SelectedAppointment.Id);
            Appointments.Remove(SelectedAppointment);
            SelectedAppointment = null;
            SuccessMessage = "Horario actualizado correctamente.";
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private bool HasOverlap(DateTimeOffset startsAt)
    {
        var endsAt = startsAt.AddMinutes(DurationMinutes);
        return Appointments.Any(item => startsAt < item.StartsAt.AddMinutes(item.DurationMinutes) && endsAt > item.StartsAt);
    }

    private bool TryGetStart(out DateTimeOffset start)
    {
        start = default;
        if (SelectedDate is null || !TimeOnly.TryParseExact(TimeText.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) return false;
        var dateTime = SelectedDate.Value.Date.Add(time.ToTimeSpan());
        start = new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
        return true;
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(CanPublish));
        PublishCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
    }
}
