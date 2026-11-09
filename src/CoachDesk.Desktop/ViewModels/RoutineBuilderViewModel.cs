using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Models.Routines;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class RoutineBuilderViewModel : ViewModelBase
{
    private readonly IAdviseeService _adviseeService;
    private readonly IRoutineService _routineService;
    private AdviseeSummaryDto? _selectedAdvisee;
    private TrainingPlanSummaryDto? _selectedSavedPlan;
    private RoutineDayViewModel? _selectedDay;
    private WeekDayOptionViewModel? _dayToAdd;
    private string _planName = "Nueva rutina";
    private DateTime _startDate = DateTime.Today;
    private string? _message;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isLoadingPlans;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isPlanPanelVisible = true;

    public RoutineBuilderViewModel(IAdviseeService adviseeService, IRoutineService routineService)
    {
        _adviseeService = adviseeService;
        _routineService = routineService;
        WeekDays = [new(1, "Lunes"), new(2, "Martes"), new(3, "Miércoles"), new(4, "Jueves"), new(5, "Viernes"), new(6, "Sábado"), new(7, "Domingo")];
        AddExerciseCommand = new RelayCommand(_ => AddExercise());
        RemoveExerciseCommand = new RelayCommand(item => RemoveExercise(item as RoutineExerciseViewModel));
        AddDayCommand = new RelayCommand(_ => AddDay());
        RemoveDayCommand = new RelayCommand(_ => RemoveSelectedDay());
        NewPlanCommand = new RelayCommand(_ => CreateNewPlan());
        TogglePlanPanelCommand = new RelayCommand(_ => IsPlanPanelVisible = !IsPlanPanelVisible);
        LoadPlansCommand = new AsyncRelayCommand(_ => LoadPlansAsync(), _ => SelectedAdvisee is not null && !IsLoadingPlans);
        EditSavedPlanCommand = new AsyncRelayCommand(_ => LoadSelectedPlanAsync(), _ => SelectedAdvisee is not null && SelectedSavedPlan is not null && !IsLoadingPlans);
        SaveAndAssignCommand = new AsyncRelayCommand(_ => SaveAndAssignAsync(), _ => !IsSaving);
        CreateNewPlan();
    }

    public ObservableCollection<AdviseeSummaryDto> Advisees { get; } = [];
    public ObservableCollection<RoutineDayViewModel> Days { get; } = [];
    public IReadOnlyList<WeekDayOptionViewModel> WeekDays { get; }
    public AdviseeSummaryDto? SelectedAdvisee
    {
        get => _selectedAdvisee;
        set
        {
            if (!SetProperty(ref _selectedAdvisee, value)) return;
            SavedPlans.Clear();
            SelectedSavedPlan = null;
            LoadPlansCommand.RaiseCanExecuteChanged();
            EditSavedPlanCommand.RaiseCanExecuteChanged();
        }
    }
    public ObservableCollection<TrainingPlanSummaryDto> SavedPlans { get; } = [];
    public TrainingPlanSummaryDto? SelectedSavedPlan
    {
        get => _selectedSavedPlan;
        set { if (SetProperty(ref _selectedSavedPlan, value)) EditSavedPlanCommand.RaiseCanExecuteChanged(); }
    }
    public RoutineDayViewModel? SelectedDay
    {
        get => _selectedDay;
        set { if (SetProperty(ref _selectedDay, value)) { OnPropertyChanged(nameof(SelectedDayExercises)); OnPropertyChanged(nameof(HasSelectedDay)); } }
    }
    public WeekDayOptionViewModel? DayToAdd { get => _dayToAdd; set => SetProperty(ref _dayToAdd, value); }
    public string PlanName { get => _planName; set => SetProperty(ref _planName, value); }
    public DateTime StartDate { get => _startDate; set => SetProperty(ref _startDate, value); }
    public IReadOnlyList<RoutineExerciseViewModel> SelectedDayExercises => SelectedDay?.Exercises ?? [];
    public bool HasSelectedDay => SelectedDay is not null;
    public string? Message { get => _message; private set { if (SetProperty(ref _message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set { if (SetProperty(ref _isSaving, value)) SaveAndAssignCommand.RaiseCanExecuteChanged(); } }
    public bool IsLoadingPlans { get => _isLoadingPlans; private set { if (SetProperty(ref _isLoadingPlans, value)) { LoadPlansCommand.RaiseCanExecuteChanged(); EditSavedPlanCommand.RaiseCanExecuteChanged(); } } }
    public string? ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public string? SuccessMessage { get => _successMessage; private set { if (SetProperty(ref _successMessage, value)) OnPropertyChanged(nameof(HasSuccess)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool IsPlanPanelVisible
    {
        get => _isPlanPanelVisible;
        private set
        {
            if (!SetProperty(ref _isPlanPanelVisible, value)) return;
            OnPropertyChanged(nameof(PlanPanelWidth));
            OnPropertyChanged(nameof(PlanPanelGapWidth));
        }
    }
    public GridLength PlanPanelWidth => IsPlanPanelVisible ? new GridLength(275) : new GridLength(0);
    public GridLength PlanPanelGapWidth => IsPlanPanelVisible ? new GridLength(18) : new GridLength(0);
    public RelayCommand AddExerciseCommand { get; }
    public RelayCommand RemoveExerciseCommand { get; }
    public RelayCommand AddDayCommand { get; }
    public RelayCommand RemoveDayCommand { get; }
    public RelayCommand NewPlanCommand { get; }
    public RelayCommand TogglePlanPanelCommand { get; }
    public AsyncRelayCommand LoadPlansCommand { get; }
    public AsyncRelayCommand EditSavedPlanCommand { get; }
    public AsyncRelayCommand SaveAndAssignCommand { get; }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Message = ErrorMessage = SuccessMessage = null;
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

    private void CreateNewPlan()
    {
        PlanName = "Nueva rutina";
        StartDate = DateTime.Today;
        Days.Clear();
        SelectedDay = null;
        DayToAdd = null;
        ErrorMessage = SuccessMessage = null;
    }

    private async Task LoadPlansAsync()
    {
        if (SelectedAdvisee is null) return;
        ErrorMessage = SuccessMessage = null;
        IsLoadingPlans = true;
        try
        {
            var plans = await _routineService.GetPlansAsync(SelectedAdvisee.Id);
            SavedPlans.Clear();
            foreach (var plan in plans) SavedPlans.Add(plan);
            SelectedSavedPlan = SavedPlans.FirstOrDefault(plan => plan.IsActive) ?? SavedPlans.FirstOrDefault();
            if (SavedPlans.Count == 0) SuccessMessage = $"{SelectedAdvisee.FullName} todavía no tiene rutinas guardadas.";
        }
        catch (ApiException exception) { ErrorMessage = exception.Message; }
        catch (HttpRequestException) { ErrorMessage = "No fue posible consultar las rutinas del asesorado."; }
        finally { IsLoadingPlans = false; }
    }

    private async Task LoadSelectedPlanAsync()
    {
        if (SelectedAdvisee is null || SelectedSavedPlan is null) return;
        ErrorMessage = SuccessMessage = null;
        IsLoadingPlans = true;
        try
        {
            var plan = await _routineService.GetPlanAsync(SelectedAdvisee.Id, SelectedSavedPlan.Id);
            PlanName = plan.Name;
            StartDate = plan.StartDate.ToDateTime(TimeOnly.MinValue);
            Days.Clear();
            foreach (var sourceDay in plan.Days)
            {
                var option = WeekDays.FirstOrDefault(day => day.Value == sourceDay.DayOfWeek);
                var day = new RoutineDayViewModel(sourceDay.DayOfWeek, option?.Name ?? $"Día {sourceDay.DayOfWeek}", sourceDay.Title);
                foreach (var sourceExercise in sourceDay.Exercises)
                    day.Exercises.Add(new RoutineExerciseViewModel { ExerciseName = sourceExercise.Name, Sets = sourceExercise.Sets.ToString(), Repetitions = sourceExercise.RepsMin == sourceExercise.RepsMax ? sourceExercise.RepsMin.ToString() : $"{sourceExercise.RepsMin}-{sourceExercise.RepsMax}", Rest = $"{sourceExercise.RestSeconds} seg", SuggestedLoad = sourceExercise.SuggestedLoadKg?.ToString("0.##", CultureInfo.InvariantCulture) + (sourceExercise.SuggestedLoadKg is null ? string.Empty : " kg") });
                Days.Add(day);
            }
            SelectedDay = Days.FirstOrDefault();
            SuccessMessage = $"Rutina \"{plan.Name}\" cargada. Al guardarla se publicará una nueva versión.";
        }
        catch (ApiException exception) { ErrorMessage = exception.Message; }
        catch (HttpRequestException) { ErrorMessage = "No fue posible cargar la rutina seleccionada."; }
        finally { IsLoadingPlans = false; }
    }

    private void AddDay()
    {
        ErrorMessage = SuccessMessage = null;
        if (DayToAdd is null) { ErrorMessage = "Selecciona un día para agregarlo al plan."; return; }
        if (Days.Any(day => day.DayOfWeek == DayToAdd.Value)) { ErrorMessage = $"{DayToAdd.Name} ya forma parte de esta rutina."; return; }
        var day = new RoutineDayViewModel(DayToAdd.Value, DayToAdd.Name);
        Days.Add(day);
        SelectedDay = day;
    }

    private void RemoveSelectedDay()
    {
        ErrorMessage = SuccessMessage = null;
        if (SelectedDay is null) return;
        if (Days.Count == 1) { ErrorMessage = "Una rutina debe contener al menos un día."; return; }
        var index = Days.IndexOf(SelectedDay);
        Days.Remove(SelectedDay);
        SelectedDay = Days[Math.Max(0, index - 1)];
    }

    private void AddExercise()
    {
        if (SelectedDay is null) { ErrorMessage = "Selecciona un día antes de agregar un ejercicio."; return; }
        SelectedDay.Exercises.Add(new RoutineExerciseViewModel());
    }

    private void RemoveExercise(RoutineExerciseViewModel? exercise)
    {
        if (exercise is not null) SelectedDay?.Exercises.Remove(exercise);
    }

    private async Task SaveAndAssignAsync()
    {
        ErrorMessage = SuccessMessage = null;
        if (SelectedAdvisee is null) { ErrorMessage = "Selecciona un asesorado activo antes de asignar la rutina."; return; }
        if (!TryBuildPlan(out var plan, out var error)) { ErrorMessage = error; return; }
        IsSaving = true;
        try
        {
            var saved = await _routineService.SaveAndAssignAsync(SelectedAdvisee.Id, plan);
            SuccessMessage = $"La rutina \"{saved.Name}\" (versión {saved.Version}) fue asignada a {SelectedAdvisee.FullName}.";
        }
        catch (ApiException exception) { ErrorMessage = exception.Message; }
        catch (HttpRequestException) { ErrorMessage = "No fue posible guardar la rutina. Revisa la conexión con el servidor."; }
        finally { IsSaving = false; }
    }

    private bool TryBuildPlan(out CreateTrainingPlanRequestDto plan, out string error)
    {
        plan = default!;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(PlanName) || Days.Count == 0) { error = "Indica el nombre del plan y al menos un día de entrenamiento."; return false; }
        var requestDays = new List<TrainingDayRequestDto>();
        foreach (var day in Days)
        {
            if (string.IsNullOrWhiteSpace(day.Title) || day.Exercises.Count == 0) { error = $"{day.DayName} necesita un título y al menos un ejercicio."; return false; }
            var exercises = new List<TrainingExerciseRequestDto>();
            foreach (var item in day.Exercises)
            {
                if (string.IsNullOrWhiteSpace(item.ExerciseName) || !TryNumber(item.Sets, 1, 20, out var sets) || !TryRepetitions(item.Repetitions, out var repsMin, out var repsMax) || !TryNumberFromText(item.Rest, 0, 1800, out var rest)) { error = $"Revisa los datos del ejercicio en {day.DayName}."; return false; }
                if (!TryLoad(item.SuggestedLoad, out var load)) { error = $"La carga de \"{item.ExerciseName}\" debe ser un número válido en kg."; return false; }
                exercises.Add(new TrainingExerciseRequestDto(item.ExerciseName.Trim(), (short)sets, (short)repsMin, (short)repsMax, (short)rest, load));
            }
            requestDays.Add(new TrainingDayRequestDto(day.DayOfWeek, day.Title.Trim(), exercises));
        }
        plan = new CreateTrainingPlanRequestDto(PlanName.Trim(), DateOnly.FromDateTime(StartDate), requestDays);
        return true;
    }

    private static bool TryNumber(string value, int min, int max, out int number) => int.TryParse(value?.Trim(), out number) && number >= min && number <= max;
    private static bool TryNumberFromText(string value, int min, int max, out int number) { number = 0; var match = Regex.Match(value ?? string.Empty, @"\d+"); return match.Success && int.TryParse(match.Value, out number) && number >= min && number <= max; }
    private static bool TryRepetitions(string value, out int min, out int max)
    {
        var parts = (value ?? string.Empty).Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        min = max = 0;
        if (parts.Length is < 1 or > 2 || !int.TryParse(parts[0], out min)) return false;
        if (parts.Length == 1) max = min; else if (!int.TryParse(parts[1], out max)) return false;
        return min >= 1 && max >= min && max <= 1000;
    }
    private static bool TryLoad(string value, out decimal? load)
    {
        load = null; if (string.IsNullOrWhiteSpace(value)) return true;
        var match = Regex.Match(value, @"\d+(?:[\.,]\d+)?");
        if (!match.Success || !decimal.TryParse(match.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed is < 0 or > 1000) return false;
        load = parsed; return true;
    }
}
