using CoachDesk.Desktop.Models.Nutrition;

namespace CoachDesk.Desktop.Services;

public interface IMealPlanPdfExporter
{
    Task ExportAsync(MealPlanDto plan, string adviseeName, string outputPath, CancellationToken cancellationToken = default);
}
