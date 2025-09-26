using LibrarySystem.BLL.Enums;
using LibrarySystem.Common.Models;
using LibrarySystem.Common.Settings;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.BLL.Initialization;

public class AdminAccountInitializer(
    IUserRepository userRepository,
    ILogger<AdminAccountInitializer> logger,
    IConfiguration configuration)
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<AdminAccountInitializer> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task SeedAdminUserAsync()
    {
        try
        {
            // Check if there is any user already exists
            if ((await _userRepository.GetTotalCountAsync(true)) > 0)
            {
                _logger.LogInformation("A user already exists. Seeding not required.");
                return;
            }

            // Get Admin settings from configuration
            var adminSettings = new AdminUserSettings();
            _configuration.GetSection("AdminUser").Bind(adminSettings);

            // If not, create the new admin user
            _logger.LogInformation("Admin user not found. Creating a new one...");
            var adminUser = new User
            {
                Username = adminSettings.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminSettings.Password), // Use the same hashing logic
                FullName = adminSettings.FullName,
                Role = UserRole.Manager.ToString() // Assign the Manager role
            };

            await _userRepository.AddAsync(adminUser);
            _logger.LogInformation("Admin user created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding admin user.");
        }
    }
}
