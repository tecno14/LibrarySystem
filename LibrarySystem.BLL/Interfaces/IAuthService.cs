using LibrarySystem.Common.Models;

namespace LibrarySystem.BLL.Interfaces;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> RegisterAsync(string username, string password, string fullName);
}
