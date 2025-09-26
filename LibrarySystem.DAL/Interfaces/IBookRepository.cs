using LibrarySystem.Common.Models;

namespace LibrarySystem.DAL.Interfaces;

public interface IBookRepository
{
    // Create
    Task<int> AddAsync(Book book);

    // Read
    Task<Book?> GetByIdAsync(int id, bool includeHidden);
    Task<Book?> GetByIsbnAsync(string isbn, bool includeHidden);
    Task<IEnumerable<Book>> GetPagedAsync(int page, int pageSize, bool includeHidden);
    Task<IEnumerable<Book>> SearchAsync(string keyword, int page, int pageSize, bool includeHidden);
    Task<IEnumerable<Book>> GetBooksByGenreAsync(string genre, int page, int pageSize, bool includeHidden);
    Task<IEnumerable<Book>> GetRecentlyAddedAsync(int count);
    Task<IEnumerable<string>> GetAllGenresAsync(bool includeHidden);
    Task<int> GetGenreCountAsync(string genre);    
    Task<int> GetTotalCountAsync(bool includeHidden);

    // Update
    Task UpdateAsync(Book book);
    Task SetHiddenStatusAsync(int id, bool includeHidden);
}
