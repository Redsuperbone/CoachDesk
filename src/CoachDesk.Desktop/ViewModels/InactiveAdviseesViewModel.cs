using System.Collections.ObjectModel;
using System.Net.Http;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class InactiveAdviseesViewModel : ViewModelBase
{
    private readonly IAdviseeService _service;
    private readonly IConfirmationDialogService _confirmation;
    private AdviseeSummaryDto? _selectedAdvisee;
    private string? _message;
    private bool _isLoading;

    public InactiveAdviseesViewModel(IAdviseeService service, IConfirmationDialogService confirmation)
    {
        _service = service; _confirmation = confirmation;
        ReactivateCommand = new AsyncRelayCommand(_ => ReactivateAsync(), _ => SelectedAdvisee is not null && !IsLoading);
    }

    public ObservableCollection<AdviseeSummaryDto> Advisees { get; } = [];
    public AdviseeSummaryDto? SelectedAdvisee { get => _selectedAdvisee; set { if (SetProperty(ref _selectedAdvisee, value)) ReactivateCommand.RaiseCanExecuteChanged(); } }
    public string? Message { get => _message; private set { if (SetProperty(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool IsLoading { get => _isLoading; private set { if (SetProperty(ref _isLoading, value)) ReactivateCommand.RaiseCanExecuteChanged(); } }
    public AsyncRelayCommand ReactivateCommand { get; }

    public async Task LoadAsync()
    {
        IsLoading = true; Message = null;
        try { var items = await _service.GetInactiveAsync(); Advisees.Clear(); foreach (var item in items) Advisees.Add(item); SelectedAdvisee = Advisees.FirstOrDefault(); }
        catch (ApiException exception) { Message = exception.Message; }
        catch (HttpRequestException) { Message = "No fue posible cargar los expedientes inactivos."; }
        finally { IsLoading = false; }
    }

    private async Task ReactivateAsync()
    {
        var advisee = SelectedAdvisee;
        if (advisee is null || !_confirmation.Confirm($"¿Reactivar el servicio de {advisee.FullName}? Volverá a aparecer entre los asesorados activos.", "Reactivar servicio")) return;
        try { await _service.ReactivateAsync(advisee.Id); await LoadAsync(); Message = $"{advisee.FullName} fue reactivado correctamente."; }
        catch (ApiException exception) { Message = exception.Message; }
        catch (HttpRequestException) { Message = "No fue posible reactivar el servicio."; }
    }
}
