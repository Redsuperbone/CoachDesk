namespace CoachDesk.Desktop.Models.Nutrition;

public sealed class MealPlanDto
{
    public int? Id { get; init; }
    public int AdviseeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly EffectiveDate { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<MealDto> Meals { get; init; } = [];
}
