using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Models;

namespace LibrarySystem.DAL.Interfaces;

public interface IBorrowingRepository
{
    // Create
    Task<bool> CreateBorrowingAndUpdateCopyStatusAsync(int copyId, int userId, BookCopyStatus newStatus, int daysCount);

    // Read
    Task<Borrowing?> GetByIdAsync(int id);
    Task<IEnumerable<Borrowing>> GetBorrowingsByUserIdAsync(int userId);
    Task<IEnumerable<Borrowing>> GetOverdueBorrowingsAsync();
    Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync();
    Task<IEnumerable<Borrowing>> GetBorrowingsByCopyIdAsync(int copyId);
    Task<int> GetTotalBorrowingsCountAsync(bool includeReturned);

    // Update
    Task<bool> FinalizeReturnAndUpdateCopyStatusAsync(int borrowingId, int copyId, BookCopyStatus newStatus);
    Task<bool> ExtendDueDateAsync(int borrowingId, DateTime newDueDate);
}
