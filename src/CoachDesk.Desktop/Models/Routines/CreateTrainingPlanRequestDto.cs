namespace CoachDesk.Desktop.Models.Routines;

public sealed record CreateTrainingPlanRequestDto(string Name, DateOnly StartDate, IReadOnlyList<TrainingDayRequestDto> Days);

public sealed record TrainingDayRequestDto(short DayOfWeek, string Title, IReadOnlyList<TrainingExerciseRequestDto> Exercises);

public sealed record TrainingExerciseRequestDto(string Name, short Sets, short RepsMin, short RepsMax, short RestSeconds, decimal? SuggestedLoadKg);

public sealed record TrainingPlanDto(int Id, int AdviseeId, string Name, DateOnly StartDate, int Version);
