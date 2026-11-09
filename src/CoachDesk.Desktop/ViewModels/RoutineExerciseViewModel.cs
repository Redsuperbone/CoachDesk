namespace CoachDesk.Desktop.ViewModels;

public sealed class RoutineExerciseViewModel : ViewModelBase
{
    private string _exerciseName = string.Empty;
    private string _sets = "3";
    private string _repetitions = "8-12";
    private string _rest = "90 seg";
    private string _suggestedLoad = string.Empty;

    public string ExerciseName { get => _exerciseName; set => SetProperty(ref _exerciseName, value); }
    public string Sets { get => _sets; set => SetProperty(ref _sets, value); }
    public string Repetitions { get => _repetitions; set => SetProperty(ref _repetitions, value); }
    public string Rest { get => _rest; set => SetProperty(ref _rest, value); }
    public string SuggestedLoad { get => _suggestedLoad; set => SetProperty(ref _suggestedLoad, value); }
}
