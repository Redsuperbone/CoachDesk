namespace CoachDesk.Desktop.Models.Nutrition;

public sealed class NutritionItemDto
{
    public int Id { get; init; }
    public string FoodName { get; init; } = string.Empty;
    public string Portion { get; init; } = string.Empty;
    public decimal Calories { get; init; }
    public decimal ProteinGrams { get; init; }
    public decimal CarbohydratesGrams { get; init; }
    public decimal FatGrams { get; init; }
}
