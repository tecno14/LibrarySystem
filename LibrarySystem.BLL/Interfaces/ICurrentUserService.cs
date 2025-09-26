using LibrarySystem.BLL.Enums;

namespace LibrarySystem.BLL.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
    bool IsPowerUser { get; }
}
