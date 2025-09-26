using LibrarySystem.Common.Models;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.DAL.Repositories;

/// <summary>
/// Repository for managing data access for the Users table.
/// It handles all operations related to user accounts.
/// </summary>
public class UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger) :
    BaseRepository(connectionFactory, logger), IUserRepository
{
    #region Create
    /// <summary>
    /// Adds a new user to the database.
    /// </summary>
    /// <param name="user">The User object to add.</param>
    /// <returns>The ID of the newly created user.</returns>
    public async Task<int> AddAsync(User user)
    {
        const string sql = @"
            INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role)
            OUTPUT INSERTED.Id
            VALUES (@Username, @PasswordHash, @FullName, @Role);";

        user.Username = user.Username.Trim();
        user.FullName = user.FullName.Trim();

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Role", user.Role);

            var result = await command.ExecuteScalarAsync();
            return result is int id && id >= 0
                ? id
                : throw new InvalidOperationException("Insert failed: no ID returned.");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error occurred while adding user '{Username}'.", user.Username);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while adding user '{Username}'.", user.Username);
            throw;
        }
    }
    #endregion

    #region Read
    /// <summary>
    /// Retrieves a specific user by their unique ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>User or null if not found</returns>
    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM dbo.Users WHERE Id = @Id;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToUser(reader);
            }
            return null;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error occurred while looking for user by id '{Id}'.", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while looking for user by id '{Id}'.", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a specific user by their unique username. Case-insensitive.
    /// This is the primary method used for authentication lookups.
    /// </summary>
    /// <param name="username"></param>
    /// <returns>User or null if not found</returns>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        // Using a case-insensitive collation (COLLATE SQL_Latin1_General_CP1_CI_AS) is robust,
        // but for simplicity and broader compatibility, we can use LOWER().
        const string sql = "SELECT * FROM dbo.Users WHERE LOWER(Username) = LOWER(@Username);";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToUser(reader);
            }
            return null;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error occurred while looking for user by username '{Username}'.", username);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while looking for user by username '{Username}'.", username);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a paginated list of users, optionally including hidden (archived) ones.
    /// </summary>
    /// <param name="page">The page number (starting from 1).</param>
    /// <param name="pageSize">The number of users per page.</param>
    /// <param name="includeHidden">Whether to include hidden users in the result.</param>
    /// <returns>A list of users for the specified page.</returns>
    public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, bool includeHidden)
    {
        var users = new List<User>();
        var offset = (page - 1) * pageSize;

        var sql = @"
            SELECT * FROM dbo.Users
            WHERE 1 = 1"; // base condition for dynamic filtering

        if (!includeHidden)
        {
            sql += " AND IsHidden = 0";
        }

        sql += @"
            ORDER BY FullName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapToUser(reader));
            }

            logger.LogInformation("Retrieved page {Page} with {Count} users. includeHidden={IncludeHidden}.",
                page, users.Count, includeHidden);

            return users;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving page {Page}. includeHidden={IncludeHidden}.", page, includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error retrieving page {Page}. includeHidden={IncludeHidden}.", page, includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Searches for users whose username or full name contains the specified keyword.
    /// Supports paging and optionally includes hidden users based on the <paramref name="includeHidden"/> flag.
    /// </summary>
    /// <param name="keyword">The search keyword (case-insensitive).</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of results per page.</param>
    /// <param name="includeHidden">Whether to include hidden (archived) users in the results.</param>
    /// <returns>A paged list of matching users.</returns>
    public async Task<IEnumerable<User>> SearchAsync(string keyword, int page, int pageSize, bool includeHidden)
    {
        var users = new List<User>();

        var sql = @"
        SELECT * FROM dbo.Users
        WHERE (Username LIKE @Keyword COLLATE Latin1_General_CI_AS
               OR FullName LIKE @Keyword COLLATE Latin1_General_CI_AS)";

        if (!includeHidden)
        {
            sql += " AND IsHidden = 0";
        }

        sql += @"
        ORDER BY FullName
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
            command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapToUser(reader));
            }

            logger.LogInformation("Paged search completed for keyword '{Keyword}' (Page={Page}, Size={PageSize}) with includeHidden={IncludeHidden}. Matches returned: {Count}.",
                keyword, page, pageSize, includeHidden, users.Count);

            return users;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error during paged search for keyword '{Keyword}' (Page={Page}, Size={PageSize}) with includeHidden={IncludeHidden}.",
                keyword, page, pageSize, includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during paged search for keyword '{Keyword}' (Page={Page}, Size={PageSize}) with includeHidden={IncludeHidden}.",
                keyword, page, pageSize, includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the total count of users in the system.
    /// Optionally includes hidden (archived) users based on the <paramref name="includeHidden"/> flag.
    /// </summary>
    /// <param name="includeHidden">Whether to include hidden users in the count.</param>
    /// <returns>The total number of users.</returns>
    public async Task<int> GetTotalCountAsync(bool includeHidden)
    {
        var sql = "SELECT COUNT(*) FROM dbo.Users";

        if (!includeHidden)
        {
            sql += " WHERE IsHidden = 0";
        }

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Retrieved total user count: {Count}. includeHidden={IncludeHidden}.", count, includeHidden);
            return count;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving total user count. includeHidden={IncludeHidden}.", includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving total user count. includeHidden={IncludeHidden}.", includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Checks whether a user exists in the database by their username.
    /// </summary>
    /// <param name="username">The username to check for existence.</param>
    /// <returns>True if the user exists; otherwise, false.</returns>
    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        const string sql = "SELECT 1 FROM dbo.Users WHERE Username = @Username;";

        username = username.Trim();

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            var result = await command.ExecuteScalarAsync();
            var exists = result != null;

            logger.LogInformation("Checked existence for username '{Username}': {Exists}.", username, exists);
            return exists;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error occurred while checking if username exist '{Username}'.", username);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking existence for username '{Username}'.", username);
            throw;
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Updates an existing user's details in the database.
    /// </summary>
    /// <param name="user">The user object containing updated information.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE dbo.Users
            SET Username = @Username,
                PasswordHash = @PasswordHash,
                FullName = @FullName,
                Role = @Role
            WHERE Id = @Id;";

        user.Username = user.Username.Trim();
        user.FullName = user.FullName.Trim();

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Role", user.Role);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                logger.LogInformation("User ID {UserId} updated successfully.", user.Id);
                return true;
            }

            throw new InvalidOperationException("Update failed: no user found");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error occurred while updating user '{Username}'.", user.Username);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating User ID {UserId}.", user.Id);
            throw;
        }
    }
    #endregion

    #region Delete (soft)
    /// <summary>
    /// Marks a user as deleted by setting IsHidden = 1.
    /// </summary>
    /// <param name="id">The ID of the user to soft delete.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public async Task<bool> SoftDeleteAsync(int id)
    {
        const string sql = "UPDATE dbo.Users SET IsHidden = 1 WHERE Id = @Id AND IsHidden = 0;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected > 0)
            {
                logger.LogInformation("User ID {UserId} soft-deleted successfully.", id);
                return true;
            }

            logger.LogWarning("Soft delete failed: no user found with ID {UserId}.", id);
            return false;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error occurred while soft delete user id '{UserId}'.", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error soft-deleting User ID {UserId}.", id);
            throw;
        }
    }
    #endregion

    #region Helper
    /// <summary>
    /// A private helper method to map a row from the SqlDataReader to a User object.
    /// </summary>
    /// <param name="reader">The SqlDataReader containing user data.</param>
    /// <returns>A fully populated User object.</returns>
    private static User MapToUser(SqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            IsHidden = reader.GetBoolean(reader.GetOrdinal("IsHidden")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
    #endregion
}
