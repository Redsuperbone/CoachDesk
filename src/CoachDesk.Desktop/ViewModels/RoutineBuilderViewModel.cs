using System.Collections.ObjectModel;
using System.Net.Http;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class RoutineBuilderViewModel : ViewModelBase
{
    private readonly IAdviseeService _adviseeService;
    private AdviseeSummaryDto? _selectedAdvisee;
    private string? _message;
    private bool _isLoading;

    public RoutineBuilderViewModel(IAdviseeService adviseeService) => _adviseeService = adviseeService;

    public ObservableCollection<AdviseeSummaryDto> Advisees { get; } = [];
    public AdviseeSummaryDto? SelectedAdvisee { get => _selectedAdvisee; set => SetProperty(ref _selectedAdvisee, value); }
    public string? Message { get => _message; private set { if (SetProperty(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Message = null;
        var previousId = SelectedAdvisee?.Id;
        try
        {
            var advisees = await _adviseeService.GetActiveAsync();
            Advisees.Clear();
            foreach (var advisee in advisees) Advisees.Add(advisee);
            SelectedAdvisee = Advisees.FirstOrDefault(item => item.Id == previousId) ?? Advisees.FirstOrDefault();
        }
        catch (ApiException exception) { Message = exception.Message; }
        catch (HttpRequestException) { Message = "No fue posible cargar los asesorados activos."; }
        finally { IsLoading = false; }
    }
}
