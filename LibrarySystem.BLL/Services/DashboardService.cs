using LibrarySystem.BLL.DTOs;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.BLL.Services;

public class DashboardService(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IUserRepository userRepository,
    IBorrowingRepository borrowingRepository,
    ICurrentUserService currentUser,
    ILogger<DashboardService> logger) : IDashboardService
{
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly IBookCopyRepository _bookCopyRepository = bookCopyRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IBorrowingRepository _borrowingRepository = borrowingRepository;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<DashboardService> _logger = logger;

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        if (!_currentUser.IsPowerUser)
        {
            throw new UnauthorizedAccessException("Only moderators can do this action.");
        }

        var totalTitlesTask = _bookRepository.GetTotalCountAsync(true);
        var totalCopiesTask = _bookCopyRepository.GetTotalCountAsync();
        var totalUsersTask = _userRepository.GetTotalCountAsync(true);
        // Books checked out is the count of copies with the status 'Borrowed'
        var booksCheckedOutTask = _bookCopyRepository.GetCountByStatusAsync(Common.Enums.BookCopyStatus.Borrowed);
        var overdueBooksTask = _borrowingRepository.GetOverdueBorrowingsAsync();
        var recentlyAddedBooksTask = _bookRepository.GetRecentlyAddedAsync(5);

        // Run all data-fetching tasks in parallel for efficiency.
        await Task.WhenAll(
            totalTitlesTask,
            totalCopiesTask,
            totalUsersTask,
            booksCheckedOutTask,
            overdueBooksTask,
            recentlyAddedBooksTask);

        return new DashboardStats
        {
            TotalBookTitles = await totalTitlesTask,
            TotalBookCopies = await totalCopiesTask,
            TotalUsers = await totalUsersTask,
            BooksCheckedOut = await booksCheckedOutTask,
            OverdueBooks = await overdueBooksTask,
            RecentlyAddedBooks = await recentlyAddedBooksTask
        };
    }
}
