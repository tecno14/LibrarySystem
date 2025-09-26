using LibrarySystem.Common.Models;

namespace LibrarySystem.DAL.Interfaces;

public interface IUserRepository
{
    // Create
    Task<int> AddAsync(User user);

    // Read
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, bool includeHidden);
    Task<IEnumerable<User>> SearchAsync(string keyword, int page, int pageSize, bool includeHidden);
    Task<int> GetTotalCountAsync(bool includeHidden);
    Task<bool> ExistsByUsernameAsync(string username);

    // Update
    Task<bool> UpdateAsync(User user);

    // Delete (soft)
    Task<bool> SoftDeleteAsync(int id);
}
