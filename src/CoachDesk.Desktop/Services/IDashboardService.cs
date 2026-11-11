using CoachDesk.Desktop.Models.Dashboard;

namespace CoachDesk.Desktop.Services;

public interface IDashboardService
{
    Task<DashboardReviewSummaryDto> GetReviewSummaryAsync(CancellationToken cancellationToken = default);
}
