using LibrarySystem.BLL.DTOs;

namespace LibrarySystem.BLL.Interfaces;

public interface IDashboardService
{
    Task<DashboardStats> GetDashboardStatsAsync();
}
