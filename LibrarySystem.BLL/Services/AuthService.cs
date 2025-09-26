using LibrarySystem.BLL.Enums;
using LibrarySystem.BLL.Interfaces;
using LibrarySystem.Common.Models;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.BLL.Services;

public class AuthService(IUserRepository userRepository, ILogger<AuthService> logger) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<AuthService> _logger = logger;

    /// <summary>
    /// Registers a new user if the username is available.
    /// </summary>
    public async Task<User?> RegisterAsync(string username, string password, string fullName)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(fullName))
        {
            _logger.LogWarning("Registration failed due to missing fields. Username='{Username}', FullName='{FullName}'", username, fullName);
            return null;
        }

        try
        {
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed. Username already exists: '{Username}'", username);
                return null;
            }

            var newUser = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FullName = fullName,
                Role = UserRole.Member.ToString(),
            };

            await _userRepository.AddAsync(newUser);
            _logger.LogInformation("User registered successfully. Username='{Username}', FullName='{FullName}'", username, fullName);
            return newUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration. Username='{Username}'", username);
            throw;
        }
    }

    /// <summary>
    /// Authenticates a user by verifying credentials.
    /// </summary>
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Authentication failed due to missing credentials.");
            return null;
        }

        try
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed. Username not found: '{Username}'", username);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Authentication failed. Invalid password for Username='{Username}'", username);
                return null;
            }

            _logger.LogInformation("User authenticated successfully. Username='{Username}'", username);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during authentication. Username='{Username}'", username);
            throw;
        }
    }
}
