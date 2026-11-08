using CoachDesk.Desktop.Services;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Commands;

namespace CoachDesk.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private string _title = "Panel del Coach";
    private MainPage _currentPage = MainPage.Dashboard;

    public MainViewModel(
        IAuthenticationSession session,
        IAdviseeService adviseeService,
        IAnthropometricService anthropometricService,
        INutritionService nutritionService,
        IAppointmentService appointmentService,
        IMealPlanPdfExporter pdfExporter,
        IFileDialogService fileDialogService)
    {
        CoachName = session.CurrentCoach?.FullName ?? "Coach";
        AdviseeSelector = new AdviseeSelectorViewModel(adviseeService);
        AnthropometricRecord = new AnthropometricRecordViewModel(anthropometricService);
        DietPlan = new DietPlanViewModel(nutritionService, pdfExporter, fileDialogService);
        AppointmentManager = new AppointmentsViewModel(appointmentService);
        OpenAnthropometricCommand = new AsyncRelayCommand(_ => OpenAnthropometricAsync(), _ => SelectedAdvisee is not null);
        OpenNutritionCommand = new AsyncRelayCommand(_ => OpenNutritionAsync(), _ => SelectedAdvisee is not null);
        BackToDashboardCommand = new RelayCommand(_ => CurrentPage = MainPage.Dashboard);
        OpenAppointmentsCommand = new AsyncRelayCommand(_ => OpenAppointmentsAsync());
        OpenRoutinesCommand = new RelayCommand(_ => CurrentPage = MainPage.Routines);
        AdviseeSelector.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(AdviseeSelectorViewModel.SelectedAdvisee))
            {
                OnPropertyChanged(nameof(SelectedAdvisee));
                OnPropertyChanged(nameof(HasSelectedAdvisee));
                OpenAnthropometricCommand.RaiseCanExecuteChanged();
                OpenNutritionCommand.RaiseCanExecuteChanged();
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

    public AnthropometricRecordViewModel AnthropometricRecord { get; }
    public DietPlanViewModel DietPlan { get; }
    public AppointmentsViewModel AppointmentManager { get; }

    public AsyncRelayCommand OpenAnthropometricCommand { get; }
    public AsyncRelayCommand OpenNutritionCommand { get; }

    public RelayCommand BackToDashboardCommand { get; }
    public AsyncRelayCommand OpenAppointmentsCommand { get; }
    public RelayCommand OpenRoutinesCommand { get; }

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
}
