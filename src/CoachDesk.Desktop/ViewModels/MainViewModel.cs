using System.Net.Http;
using CoachDesk.Desktop.Services;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Commands;

namespace CoachDesk.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private string _title = "Panel del Coach";
    private MainPage _currentPage = MainPage.Dashboard;
    private string? _managementMessage;
    private readonly IAdviseeService _adviseeService;
    private readonly IConfirmationDialogService _confirmationDialogService;

    public MainViewModel(
        IAuthenticationSession session,
        IAdviseeService adviseeService,
        IAnthropometricService anthropometricService,
        IPhotoEvidenceService photoEvidenceService,
        INutritionService nutritionService,
        IAppointmentService appointmentService,
        IRoutineService routineService,
        IConfirmationDialogService confirmationDialogService,
        IMealPlanPdfExporter pdfExporter,
        IFileDialogService fileDialogService)
    {
        _adviseeService = adviseeService;
        _confirmationDialogService = confirmationDialogService;
        CoachName = session.CurrentCoach?.FullName ?? "Coach";
        AdviseeSelector = new AdviseeSelectorViewModel(adviseeService);
        AnthropometricRecord = new AnthropometricRecordViewModel(anthropometricService, photoEvidenceService);
        DietPlan = new DietPlanViewModel(nutritionService, pdfExporter, fileDialogService);
        AppointmentManager = new AppointmentsViewModel(appointmentService);
        RoutineBuilder = new RoutineBuilderViewModel(adviseeService, routineService);
        NewAdvisee = new AdviseeRegistrationViewModel(adviseeService, OnAdviseeCreatedAsync);
        InactiveAdvisees = new InactiveAdviseesViewModel(adviseeService, confirmationDialogService);
        OpenAnthropometricCommand = new AsyncRelayCommand(_ => OpenAnthropometricAsync(), _ => SelectedAdvisee is not null);
        OpenNutritionCommand = new AsyncRelayCommand(_ => OpenNutritionAsync(), _ => SelectedAdvisee is not null);
        BackToDashboardCommand = new RelayCommand(_ => CurrentPage = MainPage.Dashboard);
        OpenAppointmentsCommand = new AsyncRelayCommand(_ => OpenAppointmentsAsync());
        OpenRoutinesCommand = new AsyncRelayCommand(_ => OpenRoutinesAsync());
        OpenRegistrationCommand = new RelayCommand(_ => CurrentPage = MainPage.Registration);
        OpenInactiveAdviseesCommand = new AsyncRelayCommand(_ => OpenInactiveAdviseesAsync());
        PauseServiceCommand = new AsyncRelayCommand(_ => PauseServiceAsync(), _ => SelectedAdvisee is not null);
        DeleteAdviseeCommand = new AsyncRelayCommand(_ => DeleteAdviseeAsync(), _ => SelectedAdvisee is not null);
        AdviseeSelector.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(AdviseeSelectorViewModel.SelectedAdvisee))
            {
                OnPropertyChanged(nameof(SelectedAdvisee));
                OnPropertyChanged(nameof(HasSelectedAdvisee));
                OpenAnthropometricCommand.RaiseCanExecuteChanged();
                OpenNutritionCommand.RaiseCanExecuteChanged();
                PauseServiceCommand.RaiseCanExecuteChanged();
                DeleteAdviseeCommand.RaiseCanExecuteChanged();
            }
        };
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string CoachName { get; }

    public MainPage CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (SetProperty(ref _currentPage, value))
            {
                Title = value switch
                {
                    MainPage.Anthropometry => "Expediente antropométrico",
                    MainPage.Registration => "Alta de asesorado",
                    MainPage.InactiveAdvisees => "Asesorados suspendidos y bajas",
                    MainPage.Nutrition => "Plan nutricional",
                    MainPage.Appointments => "Gestión de Citas y Horarios Disponibles",
                    MainPage.Routines => "Constructor y Planificador de Rutinas",
                    _ => "Panel del Coach"
                };
            }
        }
    }

    public AdviseeSelectorViewModel AdviseeSelector { get; }

    public AdviseeSummaryDto? SelectedAdvisee => AdviseeSelector.SelectedAdvisee;

    public bool HasSelectedAdvisee => SelectedAdvisee is not null;

    public string? ManagementMessage
    {
        get => _managementMessage;
        private set
        {
            if (SetProperty(ref _managementMessage, value)) OnPropertyChanged(nameof(HasManagementMessage));
        }
    }

    public bool HasManagementMessage => !string.IsNullOrWhiteSpace(ManagementMessage);

    public AnthropometricRecordViewModel AnthropometricRecord { get; }
    public DietPlanViewModel DietPlan { get; }
    public AppointmentsViewModel AppointmentManager { get; }
    public AdviseeRegistrationViewModel NewAdvisee { get; }
    public RoutineBuilderViewModel RoutineBuilder { get; }
    public InactiveAdviseesViewModel InactiveAdvisees { get; }

    public AsyncRelayCommand OpenAnthropometricCommand { get; }
    public AsyncRelayCommand OpenNutritionCommand { get; }

    public RelayCommand BackToDashboardCommand { get; }
    public AsyncRelayCommand OpenAppointmentsCommand { get; }
    public AsyncRelayCommand OpenRoutinesCommand { get; }
    public RelayCommand OpenRegistrationCommand { get; }
    public AsyncRelayCommand OpenInactiveAdviseesCommand { get; }
    public AsyncRelayCommand PauseServiceCommand { get; }
    public AsyncRelayCommand DeleteAdviseeCommand { get; }

    public Task InitializeAsync() => AdviseeSelector.LoadAsync();

    private async Task OpenAnthropometricAsync()
    {
        if (SelectedAdvisee is null)
        {
            return;
        }

        CurrentPage = MainPage.Anthropometry;
        await AnthropometricRecord.LoadForAsync(SelectedAdvisee);
    }

    private async Task OpenNutritionAsync()
    {
        if (SelectedAdvisee is null)
        {
            return;
        }

        CurrentPage = MainPage.Nutrition;
        await DietPlan.LoadForAsync(SelectedAdvisee);
    }

    private async Task OpenAppointmentsAsync()
    {
        CurrentPage = MainPage.Appointments;
        await AppointmentManager.LoadAsync();
    }

    private async Task OpenRoutinesAsync()
    {
        CurrentPage = MainPage.Routines;
        await RoutineBuilder.LoadAsync();
    }

    private async Task OpenInactiveAdviseesAsync()
    {
        CurrentPage = MainPage.InactiveAdvisees;
        await InactiveAdvisees.LoadAsync();
    }

    private async Task PauseServiceAsync()
    {
        var advisee = SelectedAdvisee;
        if (advisee is null || !_confirmationDialogService.Confirm($"¿Pausar el servicio de {advisee.FullName}? El expediente se conservará, pero dejará de aparecer entre los asesorados activos.", "Pausar servicio")) return;

        await ManageAdviseeAsync(() => _adviseeService.PauseAsync(advisee.Id), $"El servicio de {advisee.FullName} fue pausado.");
    }

    private async Task DeleteAdviseeAsync()
    {
        var advisee = SelectedAdvisee;
        if (advisee is null || !_confirmationDialogService.Confirm($"¿Dar de baja a {advisee.FullName}? El expediente se conservará por historial, pero dejará de estar activo en el sistema.", "Dar de baja")) return;

        await ManageAdviseeAsync(() => _adviseeService.DeleteAsync(advisee.Id), $"El expediente de {advisee.FullName} fue dado de baja.");
    }

    private async Task ManageAdviseeAsync(Func<Task> operation, string successMessage)
    {
        ManagementMessage = null;
        try
        {
            await operation();
            await AdviseeSelector.LoadAsync();
            ManagementMessage = successMessage;
        }
        catch (ApiException exception)
        {
            ManagementMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ManagementMessage = "No fue posible actualizar el estado del expediente. Revisa la conexión con el servidor.";
        }
    }

    private async Task OnAdviseeCreatedAsync(AdviseeSummaryDto advisee)
    {
        await AdviseeSelector.LoadAsync();
        AdviseeSelector.SelectedAdvisee = AdviseeSelector.Advisees.FirstOrDefault(item => item.Id == advisee.Id);
        CurrentPage = MainPage.Dashboard;
        ManagementMessage = $"El expediente de {advisee.FullName} fue registrado correctamente.";
    }

}
