using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Models;

namespace LibrarySystem.DAL.Interfaces;

public interface IBookCopyRepository
{
    // Create
    Task<int> AddAsync(BookCopy copy);

    // Read
    Task<BookCopy?> GetByIdAsync(int id);
    Task<BookCopy?> GetFirstAvailableCopyByIsbnAsync(string isbn);
    Task<IEnumerable<BookCopy>> GetCopiesByIsbnAsync(string isbn);
    Task<int> GetAvailableCopyCountByIsbnAsync(string isbn);
    Task<int> GetCountByStatusAsync(BookCopyStatus status);
    Task<int> GetTotalCountAsync();

    // Update
    Task UpdateStatusAsync(int copyId, BookCopyStatus newStatus);
}
