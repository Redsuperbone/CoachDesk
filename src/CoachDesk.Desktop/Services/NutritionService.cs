using CoachDesk.Desktop.Models.Nutrition;

namespace CoachDesk.Desktop.Services;

public sealed class NutritionService : INutritionService
{
    private readonly IApiService _apiService;

    public NutritionService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public Task<MealPlanDto> GetCurrentPlanAsync(int adviseeId, CancellationToken cancellationToken = default) =>
        _apiService.GetAsync<MealPlanDto>($"coach/advisees/{adviseeId}/nutrition-plan", cancellationToken);

    public Task SavePlanAsync(MealPlanDto plan, CancellationToken cancellationToken = default) =>
        _apiService.PutAsync($"coach/advisees/{plan.AdviseeId}/nutrition-plan", plan, cancellationToken);
}
