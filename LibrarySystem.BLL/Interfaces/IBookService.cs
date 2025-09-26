using LibrarySystem.BLL.DTOs;
using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Models;

namespace LibrarySystem.BLL.Interfaces;

public interface IBookService
{
    // --- Catalog Access ---
    Task<IEnumerable<Book>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<string>> GetAllGenresAsync();
    Task<Book?> GetBookTitleByIdAsync(int id);
    Task<IEnumerable<BookSearchResult>> SearchCatalogAsync(string? genre = null, string? query = null, int page = 1, int pageSize = 20);
    Task<int> GetTotalCountAsync();

    // --- Inventory Management ---
    Task AddTitleToCatalogAsync(Book book);
    Task UpdateTitleDetailsAsync(Book updatedBook);
    Task ArchiveTitleAsync(int bookId);
    Task AddNewCopyToInventoryAsync(string isbn, string? location);
    Task<IEnumerable<BookCopy>> GetCopiesByIsbnAsync(string isbn);
    Task<(bool Success, string Message)> UpdateCopyStatusAsync(int copyId, BookCopyStatus newStatus);

    // --- Borrowing Operations ---
    Task<(bool Success, string Message)> BorrowCopyByIsbnAsync(string isbn, int userId, int daysCount);
    Task<(bool Success, string Message)> ReturnCopyByBorrowingIdAsync(int borrowingId);
    Task<IEnumerable<Borrowing>> GetUserBorrowingHistoryAsync(int userId);
}
