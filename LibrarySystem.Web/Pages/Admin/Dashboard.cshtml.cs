using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibrarySystem.Web.Pages.Admin;

[Authorize(Policy = "ManagerOnly")] // Ensures only users with the 'Manager' role can access.
public class DashboardModel(IDashboardService dashboardService, ISampleDataSeeder dataSeeder) : PageModel
{
    private readonly IDashboardService _dashboardService = dashboardService;
    private readonly ISampleDataSeeder _dataSeeder = dataSeeder;

    // --- Properties ---
    public DashboardViewModel DashboardStats { get; set; } = new DashboardViewModel();

    /// <summary>
    /// Handles the GET request to fetch and display dashboard statistics.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var stats = await _dashboardService.GetDashboardStatsAsync();

        // Map the stats from the BLL model to the ViewModel.
        DashboardStats = new DashboardViewModel
        {
            TotalBookTitles = stats.TotalBookTitles,
            TotalBookCopies = stats.TotalBookCopies,
            TotalUsers = stats.TotalUsers,
            BooksCheckedOut = stats.BooksCheckedOut,
            OverdueBooks = stats.OverdueBooks,
            RecentlyAddedBooks = stats.RecentlyAddedBooks
        };

        return Page();
    }

    /// <summary>
    /// Handles the POST request to seed the database with sample book titles.
    /// </summary>
    public async Task<IActionResult> OnPostSeedBooksAsync()
    {
        if (await _dataSeeder.SeedCatalogAsync(50, 5))  // Seed 50 titles, max 5 copies each
        {
            TempData["StatusMessage"] = "Successfully seeded 50 new book titles with copies.";
            TempData["Success"] = true;
        }
        else
        {
            TempData["StatusMessage"] = "Seeding skipped: The system already contains book titles.";
            TempData["Success"] = false;
        }
        return RedirectToPage();
    }
}