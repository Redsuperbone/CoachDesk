using System.Net;
using System.Net.Http;
using System.IO;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Advisees;
using CoachDesk.Desktop.Models.Nutrition;
using CoachDesk.Desktop.Services;

namespace CoachDesk.Desktop.ViewModels;

public sealed class DietPlanViewModel : ViewModelBase
{
    private readonly INutritionService _nutritionService;
    private readonly IMealPlanPdfExporter _pdfExporter;
    private readonly IFileDialogService _fileDialogService;
    private AdviseeSummaryDto? _advisee;
    private string _name = "Plan nutricional";
    private string? _notes;
    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;
    private string? _successMessage;

    public DietPlanViewModel(INutritionService nutritionService, IMealPlanPdfExporter pdfExporter, IFileDialogService fileDialogService)
    {
        _nutritionService = nutritionService;
        _pdfExporter = pdfExporter;
        _fileDialogService = fileDialogService;
        Meals =
        [
            new MealViewModel(MealType.Breakfast, "Desayuno"),
            new MealViewModel(MealType.Snack, "Colación"),
            new MealViewModel(MealType.Lunch, "Comida"),
            new MealViewModel(MealType.Dinner, "Cena")
        ];
        foreach (var meal in Meals) meal.TotalsChanged += (_, _) => NotifyTotalsChanged();
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync(), _ => CanUsePlan);
        ExportPdfCommand = new AsyncRelayCommand(_ => ExportPdfAsync(), _ => CanUsePlan);
    }

    public IReadOnlyList<MealViewModel> Meals { get; }
    public AdviseeSummaryDto? Advisee { get => _advisee; private set => SetProperty(ref _advisee, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string? Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public string? ErrorMessage { get => _errorMessage; private set => SetMessage(ref _errorMessage, value, nameof(HasError)); }
    public string? SuccessMessage { get => _successMessage; private set => SetMessage(ref _successMessage, value, nameof(HasSuccess)); }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);
    public bool CanUsePlan => Advisee is not null && !IsLoading && !IsSaving;
    public decimal TotalCalories => Meals.Sum(meal => meal.Calories);
    public decimal TotalProteinGrams => Meals.Sum(meal => meal.ProteinGrams);
    public decimal TotalCarbohydratesGrams => Meals.Sum(meal => meal.CarbohydratesGrams);
    public decimal TotalFatGrams => Meals.Sum(meal => meal.FatGrams);
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand ExportPdfCommand { get; }

    public async Task LoadForAsync(AdviseeSummaryDto advisee)
    {
        Advisee = advisee;
        ErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;
        NotifyStateChanged();
        try
        {
            var plan = await _nutritionService.GetCurrentPlanAsync(advisee.Id);
            Populate(plan);
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            CreateEmptyPlan();
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible cargar el plan nutricional.";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private async Task SaveAsync()
    {
        if (!CanUsePlan) return;
        IsSaving = true;
        ErrorMessage = null;
        SuccessMessage = null;
        NotifyStateChanged();
        try
        {
            await _nutritionService.SavePlanAsync(BuildPlan());
            SuccessMessage = "Plan nutricional guardado correctamente.";
        }
        catch (ApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No fue posible guardar el plan. Intenta nuevamente.";
        }
        finally
        {
            IsSaving = false;
            NotifyStateChanged();
        }
    }

    private async Task ExportPdfAsync()
    {
        if (Advisee is null) return;
        var safeName = string.Concat(Advisee.FullName.Where(character => !Path.GetInvalidFileNameChars().Contains(character)));
        var outputPath = _fileDialogService.GetPdfSavePath($"Plan nutricional - {safeName}.pdf");
        if (string.IsNullOrWhiteSpace(outputPath)) return;
        try
        {
            await _pdfExporter.ExportAsync(BuildPlan(), Advisee.FullName, outputPath);
            SuccessMessage = "PDF exportado correctamente.";
        }
        catch (IOException)
        {
            ErrorMessage = "No fue posible escribir el archivo PDF. Verifica que no esté abierto.";
        }
    }

    private MealPlanDto BuildPlan() => new()
    {
        AdviseeId = Advisee!.Id,
        Name = string.IsNullOrWhiteSpace(Name) ? "Plan nutricional" : Name.Trim(),
        EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
        Notes = Notes,
        Meals = Meals.Select(meal => new MealDto
        {
            Type = meal.Type,
            Items = meal.Items.Select(item => new NutritionItemDto
            {
                FoodName = item.FoodName,
                Portion = item.Portion,
                Calories = item.Calories,
                ProteinGrams = item.ProteinGrams,
                CarbohydratesGrams = item.CarbohydratesGrams,
                FatGrams = item.FatGrams
            }).ToArray()
        }).ToArray()
    };

    private void Populate(MealPlanDto plan)
    {
        Name = plan.Name;
        Notes = plan.Notes;
        foreach (var meal in Meals)
        {
            meal.ClearItems();
            var source = plan.Meals.FirstOrDefault(item => item.Type == meal.Type);
            if (source is null) continue;
            foreach (var item in source.Items)
            {
                meal.AddItem(new NutritionItemViewModel
                {
                    FoodName = item.FoodName,
                    Portion = item.Portion,
                    Calories = item.Calories,
                    ProteinGrams = item.ProteinGrams,
                    CarbohydratesGrams = item.CarbohydratesGrams,
                    FatGrams = item.FatGrams
                });
            }
        }
    }

    private void CreateEmptyPlan()
    {
        Name = "Plan nutricional";
        Notes = null;
        foreach (var meal in Meals) meal.ClearItems();
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(TotalCalories));
        OnPropertyChanged(nameof(TotalProteinGrams));
        OnPropertyChanged(nameof(TotalCarbohydratesGrams));
        OnPropertyChanged(nameof(TotalFatGrams));
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CanUsePlan));
        SaveCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();
    }

    private void SetMessage(ref string? field, string? value, string dependentProperty)
    {
        if (SetProperty(ref field, value)) OnPropertyChanged(dependentProperty);
    }
}
