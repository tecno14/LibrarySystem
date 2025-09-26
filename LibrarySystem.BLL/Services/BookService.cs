using LibrarySystem.BLL.Constants;
using LibrarySystem.BLL.DTOs;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Helper;
using LibrarySystem.Common.Models;
using LibrarySystem.Common.Validation;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.BLL.Services;

public class BookService(
    IBookRepository bookRepository,
    IBookCopyRepository bookCopyRepository,
    IBorrowingRepository borrowingRepository,
    ICurrentUserService currentUser,
    ICacheService cache,
    ILogger<BookService> logger) : IBookService
{
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly IBookCopyRepository _bookCopyRepository = bookCopyRepository;
    private readonly IBorrowingRepository _borrowingRepository = borrowingRepository;
    private readonly ICacheService _cache = cache;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<BookService> _logger = logger;

    #region Catalog Access    
    /// <summary>
    /// Gets a list of all non-hidden book titles.
    /// </summary>
    public Task<IEnumerable<Book>> GetPagedAsync(int page = 1, int pageSize = 20) =>
        _bookRepository.GetPagedAsync(1, 1000, _currentUser.IsPowerUser);

    /// <summary>
    /// Gets a distinct list of all genres in the catalog.
    /// </summary>
    public Task<IEnumerable<string>> GetAllGenresAsync() =>
        _bookRepository.GetAllGenresAsync(_currentUser.IsPowerUser);

    /// <summary>
    /// Gets a book title's metadata by its unique ID.
    /// </summary>
    public async Task<Book?> GetBookTitleByIdAsync(int id)
    {
        var book = await _cache.GetOrSetAsync(CacheKeys.BookById(id), () => _bookRepository.GetByIdAsync(id, true));
        if (book?.IsHidden == true && !_currentUser.IsPowerUser)
        {
            _logger.LogWarning("User {UserId} attempted to access hidden book Id={Id}", _currentUser.UserId, id);
            return null;
        }
        return book;
    }

    /// <summary>
    /// Searches the catalog from a single master cache of all books.
    /// </summary>
    /// <param name="genre">An optional genre to filter by.</param>
    /// <param name="query">An optional text query to filter by.</param>
    public async Task<IEnumerable<BookSearchResult>> SearchCatalogAsync(string? genre = null, string? query = null, int page = 1, int pageSize = 20)
    {
        var booksToSearch =
            await _cache.GetOrSetAsync(CacheKeys.MasterBookList, () => _bookRepository.GetPagedAsync(1, int.MaxValue, includeHidden: true));

        if (!_currentUser.IsPowerUser)
        {
            booksToSearch = booksToSearch.Where(b => !b.IsHidden);
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            booksToSearch = booksToSearch.Where(b => b.Genre?.Equals(genre, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            booksToSearch = booksToSearch.Where(b =>
                b.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                b.Author.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                b.ISBN.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var pagedBooks = booksToSearch
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var results = new List<BookSearchResult>();
        foreach (var book in pagedBooks)
        {
            var availableCount = await _bookCopyRepository.GetAvailableCopyCountByIsbnAsync(book.ISBN);
            results.Add(new BookSearchResult
            {
                BookId = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                Genre = book.Genre,
                Publisher = book.Publisher,
                CoverImageUrl = book.CoverImageUrl,
                AvailableCopies = availableCount,
            });
        }

        _logger.LogInformation("SearchCatalogAsync: genre='{Genre}', query='{Query}', page={Page}, pageSize={PageSize}, results={Count}",
            genre, query, page, pageSize, results.Count);

        return results;
    }
    #endregion

    #region Inventory Management
    /// <summary>
    /// Adds a new book title and invalidates the master cache.
    /// </summary>
    public async Task AddTitleToCatalogAsync(Book book)
    {
        if (!_currentUser.IsPowerUser)
        {
            throw new UnauthorizedAccessException("Only moderators can do this action.");
        }

        ValidationHelper.ValidateObject(book);

        var existing = await _bookRepository.GetByIsbnAsync(book.ISBN, true);
        if (existing != null)
        {
            throw new InvalidOperationException($"A book with ISBN '{book.ISBN}' already exists.");
        }

        await _bookRepository.AddAsync(book);
        _cache.Invalidate(CacheKeys.MasterBookList);
    }

    /// <summary>
    /// Updates a book's details and invalidates the master cache.
    /// </summary>
    public async Task UpdateTitleDetailsAsync(Book updatedBook)
    {
        if (!_currentUser.IsPowerUser)
        {
            throw new UnauthorizedAccessException("Only moderators can do this action.");
        }

        ValidationHelper.ValidateObject(updatedBook);

        await _bookRepository.UpdateAsync(updatedBook);
        _cache.Invalidate(CacheKeys.MasterBookList, CacheKeys.BookById(updatedBook.Id));
    }

    /// <summary>
    /// Archives a book title and invalidates the master cache.
    /// </summary>
    public async Task ArchiveTitleAsync(int bookId)
    {
        if (!_currentUser.IsPowerUser)
        {
            throw new UnauthorizedAccessException("Only moderators can do this action.");
        }
        await _bookRepository.SetHiddenStatusAsync(bookId, true);
        _cache.Invalidate(CacheKeys.MasterBookList, CacheKeys.BookById(bookId));
    }

    /// <summary>
    /// Creates a new BookCopy object and adds it to the inventory via the repository.
    /// </summary>
    public async Task AddNewCopyToInventoryAsync(string isbn, string? location)
    {
        if (!_currentUser.IsPowerUser)
        {
            throw new UnauthorizedAccessException("Only moderators can do this action.");
        }

        if (string.IsNullOrWhiteSpace(isbn))
        {
            _logger.LogWarning("Attempted to add copy with empty ISBN.");
            throw new ArgumentException("ISBN is required.");
        }

        var newCopy = new BookCopy
        {
            ISBN = isbn,
            Status = BookCopyStatus.Available,
            Location = location
        };
        await _bookCopyRepository.AddAsync(newCopy);
        _cache.Invalidate(CacheKeys.MasterBookList, CacheKeys.CopiesByIsbn(isbn));
    }

    /// <summary>
    /// Gets all physical copies for a given book ISBN.
    /// </summary>
    /// <param name="isbn">The ISBN of the book title to find copies for.</param>
    /// <returns>A collection of BookCopy objects.</returns>
    public async Task<IEnumerable<BookCopy>> GetCopiesByIsbnAsync(string isbn)
    {
        var bookcopies =
            await _cache.GetOrSetAsync(CacheKeys.CopiesByIsbn(isbn), () => _bookCopyRepository.GetCopiesByIsbnAsync(isbn));
        if (!bookcopies.Any())
            return bookcopies;
        if (!_currentUser.IsPowerUser)
        {
            var book = await _bookRepository.GetByIsbnAsync(isbn, false);
            if (book == null)// hidden
                return [];
        }
        return bookcopies;
    }

    // Allows a manager to change the status of a specific copy.
    public async Task<(bool Success, string Message)> UpdateCopyStatusAsync(int copyId, BookCopyStatus newStatus)
    {
        if (!_currentUser.IsPowerUser)
        {
            throw new UnauthorizedAccessException("Only moderators can do this action.");
        }

        // Add validation here to ensure newStatus is a valid one.
        if (!Enum.IsDefined(typeof(BookCopyStatus), newStatus))
        {
            return (false, "Invalid status provided.");
        }

        await _bookCopyRepository.UpdateStatusAsync(copyId, newStatus);
        var copy = await _bookCopyRepository.GetByIdAsync(copyId);

        // Invalidate cache, as this could affect availability counts.
        _cache.Invalidate(CacheKeys.MasterBookList, CacheKeys.CopiesByIsbn(copy.ISBN));
        return (true, "Book copy status updated successfully.");
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _bookRepository.GetTotalCountAsync(_currentUser.IsPowerUser);
    }
    #endregion

    #region Borrowing Operations
    /// <summary>
    /// Borrows the first available copy of a book specified by its ISBN.
    /// This operation should be transactional in the DAL.
    /// </summary>
    public async Task<(bool Success, string Message)> BorrowCopyByIsbnAsync(string isbn, int userId, int daysCount)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Only logged in users can do this action.");
        }

        // 1. Find an available copy of the book.
        var availableCopy = await _bookCopyRepository.GetFirstAvailableCopyByIsbnAsync(isbn);
        if (availableCopy == null)
        {
            _logger.LogWarning("Borrow failed. No available copy for ISBN='{ISBN}'", isbn);
            return (false, "No available copies of this book were found.");
        }

        // 2. The DAL should handle creating a borrowing record AND updating the copy's status
        // within a single database transaction to ensure data integrity.
        var success = await _borrowingRepository.CreateBorrowingAndUpdateCopyStatusAsync(availableCopy.Id, userId, BookCopyStatus.Borrowed, daysCount);
        if (!success)
        {
            _logger.LogError("Borrowing transaction failed for ISBN='{ISBN}', UserId={UserId}", isbn, userId);
            return (false, "An error occurred during the borrowing process.");
        }

        _cache.Invalidate(CacheKeys.MasterBookList, CacheKeys.CopiesByIsbn(isbn));
        _logger.LogInformation("Book borrowed successfully. ISBN='{ISBN}', CopyId={CopyId}, UserId={UserId}", isbn, availableCopy.Id, userId);
        return (true, "Book successfully borrowed.");
    }

    /// <summary>
    /// Returns a borrowed book, identified by its borrowing record ID.
    /// This operation should be transactional in the DAL.
    /// </summary>
    public async Task<(bool Success, string Message)> ReturnCopyByBorrowingIdAsync(int borrowingId)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Only logged in users can do this action.");
        }

        var borrowing = await _borrowingRepository.GetByIdAsync(borrowingId);

        if (!_currentUser.IsPowerUser && borrowing?.UserId != _currentUser.UserId)
        {
            throw new UnauthorizedAccessException("You can only return your own borrowed books.");
        }

        if (borrowing == null || borrowing.ReturnDate.HasValue)
        {
            _logger.LogWarning("Return failed. Invalid or already returned borrowingId={BorrowingId}", borrowingId);
            return (false, "This borrowing record is invalid or the book has already been returned.");
        }

        // The DAL should handle updating the borrowing record AND updating the copy's status to "Available"
        // within a single database transaction.
        var success = await _borrowingRepository.FinalizeReturnAndUpdateCopyStatusAsync(borrowingId, borrowing.CopyId, BookCopyStatus.Available);
        if (!success)
        {
            _logger.LogError("Return transaction failed. BorrowingId={BorrowingId}", borrowingId);
            return (false, "An error occurred during the return process.");
        }

        _cache.Invalidate(CacheKeys.MasterBookList, CacheKeys.CopiesByIsbn(borrowing.BookIsbn ?? (await _bookCopyRepository.GetByIdAsync(borrowing.CopyId)).ISBN));
        _logger.LogInformation("Book returned successfully. BorrowingId={BorrowingId}, CopyId={CopyId}", borrowingId, borrowing.CopyId);
        return (true, "Book successfully returned.");
    }

    public Task<IEnumerable<Borrowing>> GetUserBorrowingHistoryAsync(int userId)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Only logged in users can do this action.");
        }

        if (!_currentUser.IsPowerUser && userId != _currentUser.UserId)
        {
            throw new UnauthorizedAccessException("You can only view your own borrowing history.");
        }

        return _borrowingRepository.GetBorrowingsByUserIdAsync(userId);
    }
    #endregion
}