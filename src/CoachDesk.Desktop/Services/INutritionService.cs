using CoachDesk.Desktop.Models.Nutrition;

namespace CoachDesk.Desktop.Services;

public interface INutritionService
{
    Task<MealPlanDto> GetCurrentPlanAsync(int adviseeId, CancellationToken cancellationToken = default);
    Task SavePlanAsync(MealPlanDto plan, CancellationToken cancellationToken = default);
}
