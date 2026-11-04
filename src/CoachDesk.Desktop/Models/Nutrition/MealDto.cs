namespace CoachDesk.Desktop.Models.Nutrition;

public sealed class MealDto
{
    public MealType Type { get; init; }
    public IReadOnlyList<NutritionItemDto> Items { get; init; } = [];
}
