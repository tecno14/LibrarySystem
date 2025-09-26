using LibrarySystem.Common.Models;

namespace LibrarySystem.Web.ViewModels;

/// <summary>
/// Represents all the data needed to display the manager's dashboard.
/// This is populated by the DashboardService.
/// </summary>
public class DashboardViewModel
{
    public int TotalBookTitles { get; set; }
    public int TotalBookCopies { get; set; }
    public int TotalUsers { get; set; }
    public int BooksCheckedOut { get; set; }
    public IEnumerable<Borrowing> OverdueBooks { get; set; } = [];
    public IEnumerable<Book> RecentlyAddedBooks { get; set; } = [];
}
