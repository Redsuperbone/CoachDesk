using System.Collections.ObjectModel;

namespace CoachDesk.Desktop.ViewModels;

public sealed class RoutineDayViewModel : ViewModelBase
{
    private string _title;

    public RoutineDayViewModel(short dayOfWeek, string dayName, string? title = null)
    {
        DayOfWeek = dayOfWeek;
        DayName = dayName;
        _title = title ?? dayName;
    }

    public short DayOfWeek { get; }
    public string DayName { get; }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public ObservableCollection<RoutineExerciseViewModel> Exercises { get; } = [];
    public string DisplayName => string.IsNullOrWhiteSpace(Title) || Title == DayName ? DayName : $"{DayName} · {Title}";
}
