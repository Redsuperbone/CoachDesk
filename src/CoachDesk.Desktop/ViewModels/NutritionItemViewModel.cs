namespace CoachDesk.Desktop.ViewModels;

public sealed class NutritionItemViewModel : ViewModelBase
{
    private string _foodName = string.Empty;
    private string _portion = string.Empty;
    private decimal _calories;
    private decimal _proteinGrams;
    private decimal _carbohydratesGrams;
    private decimal _fatGrams;

    public event EventHandler? NutrientsChanged;

    public string FoodName { get => _foodName; set => SetProperty(ref _foodName, value); }
    public string Portion { get => _portion; set => SetProperty(ref _portion, value); }
    public decimal Calories { get => _calories; set => SetNutrient(ref _calories, value); }
    public decimal ProteinGrams { get => _proteinGrams; set => SetNutrient(ref _proteinGrams, value); }
    public decimal CarbohydratesGrams { get => _carbohydratesGrams; set => SetNutrient(ref _carbohydratesGrams, value); }
    public decimal FatGrams { get => _fatGrams; set => SetNutrient(ref _fatGrams, value); }

    private void SetNutrient(ref decimal field, decimal value)
    {
        if (SetProperty(ref field, Math.Max(0, value))) NutrientsChanged?.Invoke(this, EventArgs.Empty);
    }
}
