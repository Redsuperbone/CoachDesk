using System.Collections.ObjectModel;
using CoachDesk.Desktop.Commands;
using CoachDesk.Desktop.Models.Nutrition;

namespace CoachDesk.Desktop.ViewModels;

public sealed class MealViewModel : ViewModelBase
{
    public MealViewModel(MealType type, string title)
    {
        Type = type;
        Title = title;
        AddItemCommand = new RelayCommand(_ => AddItem());
        RemoveItemCommand = new RelayCommand(item => RemoveItem(item as NutritionItemViewModel));
    }

    public event EventHandler? TotalsChanged;
    public MealType Type { get; }
    public string Title { get; }
    public ObservableCollection<NutritionItemViewModel> Items { get; } = [];
    public RelayCommand AddItemCommand { get; }
    public RelayCommand RemoveItemCommand { get; }
    public decimal Calories => Items.Sum(item => item.Calories);
    public decimal ProteinGrams => Items.Sum(item => item.ProteinGrams);
    public decimal CarbohydratesGrams => Items.Sum(item => item.CarbohydratesGrams);
    public decimal FatGrams => Items.Sum(item => item.FatGrams);

    public void AddItem(NutritionItemViewModel? item = null)
    {
        var newItem = item ?? new NutritionItemViewModel();
        newItem.NutrientsChanged += (_, _) => NotifyTotalsChanged();
        Items.Add(newItem);
        NotifyTotalsChanged();
    }

    public void ClearItems()
    {
        Items.Clear();
        NotifyTotalsChanged();
    }

    private void RemoveItem(NutritionItemViewModel? item)
    {
        if (item is not null && Items.Remove(item)) NotifyTotalsChanged();
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(Calories));
        OnPropertyChanged(nameof(ProteinGrams));
        OnPropertyChanged(nameof(CarbohydratesGrams));
        OnPropertyChanged(nameof(FatGrams));
        TotalsChanged?.Invoke(this, EventArgs.Empty);
    }
}
