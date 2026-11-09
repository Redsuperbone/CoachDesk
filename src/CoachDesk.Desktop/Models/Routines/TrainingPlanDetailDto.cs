namespace CoachDesk.Desktop.Models.Routines;

public sealed record TrainingPlanSummaryDto(int Id, string Name, DateOnly StartDate, DateOnly? EndDate, bool IsActive, int Version);

public sealed record TrainingPlanExerciseDto(string Name, short Sets, short RepsMin, short RepsMax, short RestSeconds, decimal? SuggestedLoadKg);

public sealed record TrainingPlanDayDto(short DayOfWeek, string Title, IReadOnlyList<TrainingPlanExerciseDto> Exercises);

public sealed record TrainingPlanDetailDto(int Id, int AdviseeId, string Name, DateOnly StartDate, int Version, IReadOnlyList<TrainingPlanDayDto> Days);
