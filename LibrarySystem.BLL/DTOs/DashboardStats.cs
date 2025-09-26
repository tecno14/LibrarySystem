using LibrarySystem.Common.Models;

namespace LibrarySystem.BLL.DTOs;

// This class acts as a Data Transfer Object (DTO) for the dashboard UI.
public class DashboardStats
{
    public int TotalBookTitles { get; set; } // Count from Books table
    public int TotalBookCopies { get; set; } // Count from BookCopies table
    public int TotalUsers { get; set; }
    public int BooksCheckedOut { get; set; }
    public IEnumerable<Borrowing> OverdueBooks { get; set; } = [];
    public IEnumerable<Book> RecentlyAddedBooks { get; set; } = [];
}
