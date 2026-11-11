using CoachDesk.Desktop.Models.Dashboard;

namespace CoachDesk.Desktop.Services;

public sealed class DashboardService(IApiService apiService) : IDashboardService
{
    public Task<DashboardReviewSummaryDto> GetReviewSummaryAsync(CancellationToken cancellationToken = default) =>
        apiService.GetAsync<DashboardReviewSummaryDto>("coach/dashboard/reviews", cancellationToken);
}
