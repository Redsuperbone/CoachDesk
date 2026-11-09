using CoachDesk.Desktop.Models.Routines;

namespace CoachDesk.Desktop.Services;

public sealed class RoutineService(IApiService apiService) : IRoutineService
{
    public Task<IReadOnlyList<TrainingPlanSummaryDto>> GetPlansAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        apiService.GetAsync<IReadOnlyList<TrainingPlanSummaryDto>>($"coach/advisees/{adviseeId}/training-plans", cancellationToken);

    public Task<TrainingPlanDetailDto> GetPlanAsync(int adviseeId, int planId, CancellationToken cancellationToken = default) =>
        apiService.GetAsync<TrainingPlanDetailDto>($"coach/advisees/{adviseeId}/training-plans/{planId}", cancellationToken);

    public Task<TrainingPlanDto> SaveAndAssignAsync(int adviseeId, CreateTrainingPlanRequestDto plan, CancellationToken cancellationToken = default) =>
        apiService.PostAsync<CreateTrainingPlanRequestDto, TrainingPlanDto>($"coach/advisees/{adviseeId}/training-plans", plan, cancellationToken);
}
