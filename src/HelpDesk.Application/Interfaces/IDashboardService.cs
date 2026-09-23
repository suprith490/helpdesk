using HelpDesk.Application.DTOs.Dashboard;

namespace HelpDesk.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}
