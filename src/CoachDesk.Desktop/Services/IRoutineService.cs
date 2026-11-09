using CoachDesk.Desktop.Models.Routines;

namespace CoachDesk.Desktop.Services;

public interface IRoutineService
{
    Task<IReadOnlyList<TrainingPlanSummaryDto>> GetPlansAsync(int adviseeId, CancellationToken cancellationToken = default);
    Task<TrainingPlanDetailDto> GetPlanAsync(int adviseeId, int planId, CancellationToken cancellationToken = default);
    Task<TrainingPlanDto> SaveAndAssignAsync(int adviseeId, CreateTrainingPlanRequestDto plan, CancellationToken cancellationToken = default);
}
