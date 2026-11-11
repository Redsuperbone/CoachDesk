using System.Net.Http;
using CoachDesk.Desktop.Services;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Models.Appointments;
using CoachDesk.Desktop.Commands;

namespace CoachDesk.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private string _title = "Panel del Coach";
    private MainPage _currentPage = MainPage.Dashboard;
    private string? _managementMessage;
    private readonly IAdviseeService _adviseeService;
    private readonly IAppointmentService _appointmentService;
    private readonly IDashboardService _dashboardService;
    private readonly IConfirmationDialogService _confirmationDialogService;
    private readonly Action _onLogout;
    private int _scheduledAppointmentsThisWeek;
    private int _pendingReviews;
    private string? _dashboardMessage;

    public MainViewModel(
        IAuthenticationSession session,
        IAdviseeService adviseeService,
        IAnthropometricService anthropometricService,
        IPhotoEvidenceService photoEvidenceService,
        INutritionService nutritionService,
        IAppointmentService appointmentService,
        IRoutineService routineService,
        IDashboardService dashboardService,
        IConfirmationDialogService confirmationDialogService,
        IMealPlanPdfExporter pdfExporter,
        IFileDialogService fileDialogService,
        Action onLogout)
    {
        _adviseeService = adviseeService;
        _appointmentService = appointmentService;
        _dashboardService = dashboardService;
        _confirmationDialogService = confirmationDialogService;
        _onLogout = onLogout ?? throw new ArgumentNullException(nameof(onLogout));
        CoachName = session.CurrentCoach?.FullName ?? "Coach";
        AdviseeSelector = new AdviseeSelectorViewModel(adviseeService);
        AnthropometricRecord = new AnthropometricRecordViewModel(anthropometricService, photoEvidenceService);
        DietPlan = new DietPlanViewModel(nutritionService, pdfExporter, fileDialogService);
        AppointmentManager = new AppointmentsViewModel(appointmentService);
        RoutineBuilder = new RoutineBuilderViewModel(adviseeService, routineService);
        NewAdvisee = new AdviseeRegistrationViewModel(adviseeService, OnAdviseeCreatedAsync);
        InactiveAdvisees = new InactiveAdviseesViewModel(adviseeService, confirmationDialogService);
        OpenAnthropometricCommand = new AsyncRelayCommand(_ => OpenAnthropometricAsync());
        OpenNutritionCommand = new AsyncRelayCommand(_ => OpenNutritionAsync());
        BackToDashboardCommand = new AsyncRelayCommand(_ => OpenDashboardAsync());
        OpenAppointmentsCommand = new AsyncRelayCommand(_ => OpenAppointmentsAsync());
        OpenRoutinesCommand = new AsyncRelayCommand(_ => OpenRoutinesAsync());
        OpenRegistrationCommand = new RelayCommand(_ => CurrentPage = MainPage.Registration);
        OpenInactiveAdviseesCommand = new AsyncRelayCommand(_ => OpenInactiveAdviseesAsync());
        PauseServiceCommand = new AsyncRelayCommand(_ => PauseServiceAsync(), _ => SelectedAdvisee is not null);
        DeleteAdviseeCommand = new AsyncRelayCommand(_ => DeleteAdviseeAsync(), _ => SelectedAdvisee is not null);
        LogoutCommand = new RelayCommand(_ => Logout());
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
                if (CurrentPage == MainPage.Anthropometry && SelectedAdvisee is not null) _ = AnthropometricRecord.LoadForAsync(SelectedAdvisee);
                if (CurrentPage == MainPage.Nutrition && SelectedAdvisee is not null) _ = DietPlan.LoadForAsync(SelectedAdvisee);
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
    public int ScheduledAppointmentsThisWeek { get => _scheduledAppointmentsThisWeek; private set => SetProperty(ref _scheduledAppointmentsThisWeek, value); }
    public int PendingReviews { get => _pendingReviews; private set => SetProperty(ref _pendingReviews, value); }
    public string? DashboardMessage { get => _dashboardMessage; private set { if (SetProperty(ref _dashboardMessage, value)) OnPropertyChanged(nameof(HasDashboardMessage)); } }
    public bool HasDashboardMessage => !string.IsNullOrWhiteSpace(DashboardMessage);

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

    public AsyncRelayCommand BackToDashboardCommand { get; }
    public AsyncRelayCommand OpenAppointmentsCommand { get; }
    public AsyncRelayCommand OpenRoutinesCommand { get; }
    public RelayCommand OpenRegistrationCommand { get; }
    public AsyncRelayCommand OpenInactiveAdviseesCommand { get; }
    public AsyncRelayCommand PauseServiceCommand { get; }
    public AsyncRelayCommand DeleteAdviseeCommand { get; }
    public RelayCommand LogoutCommand { get; }

    public async Task InitializeAsync()
    {
        await AdviseeSelector.LoadAsync();
        await LoadDashboardMetricsAsync();
    }

    private async Task OpenDashboardAsync()
    {
        CurrentPage = MainPage.Dashboard;
        await LoadDashboardMetricsAsync();
    }

    private async Task LoadDashboardMetricsAsync()
    {
        DashboardMessage = null;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var weekStart = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
            var appointmentsTask = _appointmentService.GetPublishedAsync(weekStart, weekStart.AddDays(6));
            var reviewsTask = _dashboardService.GetReviewSummaryAsync();
            await Task.WhenAll(appointmentsTask, reviewsTask);
            ScheduledAppointmentsThisWeek = appointmentsTask.Result.Count(item => item.Status != AppointmentStatus.Cancelled);
            PendingReviews = reviewsTask.Result.PendingReviews;
        }
        catch (ApiException exception) { DashboardMessage = exception.Message; }
        catch (HttpRequestException) { DashboardMessage = "No fue posible actualizar los indicadores del dashboard."; }
    }

    private async Task OpenAnthropometricAsync()
    {
        CurrentPage = MainPage.Anthropometry;
        if (SelectedAdvisee is not null) await AnthropometricRecord.LoadForAsync(SelectedAdvisee);
    }

    private async Task OpenNutritionAsync()
    {
        CurrentPage = MainPage.Nutrition;
        if (SelectedAdvisee is not null) await DietPlan.LoadForAsync(SelectedAdvisee);
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

    private void Logout()
    {
        if (!_confirmationDialogService.Confirm("¿Deseas cerrar la sesión actual?", "Cerrar sesión")) return;
        _onLogout();
    }

    private async Task OnAdviseeCreatedAsync(AdviseeSummaryDto advisee)
    {
        await AdviseeSelector.LoadAsync();
        AdviseeSelector.SelectedAdvisee = AdviseeSelector.Advisees.FirstOrDefault(item => item.Id == advisee.Id);
        CurrentPage = MainPage.Dashboard;
        ManagementMessage = $"El expediente de {advisee.FullName} fue registrado correctamente.";
    }

}
