using LibrarySystem.BLL.Enums;
using LibrarySystem.BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace LibrarySystem.BLL.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public int? UserId =>
        int.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id
            : null;
    public string? Username => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    public UserRole? Role
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var parsedRole))
                return parsedRole;

            return null; // or default to UserRole.Guest if you prefer
        }
    }
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public bool IsPowerUser => Role is UserRole.Manager;
}
