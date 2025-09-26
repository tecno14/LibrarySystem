using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Models;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.DAL.Repositories;

/// <summary>
/// Repository for managing data access for the BookCopies table.
/// It handles operations related to individual, physical book copies.
/// </summary>
public class BookCopyRepository(IDbConnectionFactory connectionFactory, ILogger<BookCopyRepository> logger) : 
    BaseRepository(connectionFactory, logger), IBookCopyRepository
{
    #region Create
    /// <summary>
    /// Adds a new book copy to the database.
    /// </summary>
    public async Task<int> AddAsync(BookCopy copy)
    {
        const string sql = @"
        INSERT INTO dbo.BookCopies (ISBN, Status, Location)
        OUTPUT INSERTED.Id
        VALUES (@ISBN, @Status, @Location);";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ISBN", copy.ISBN);
            command.Parameters.AddWithValue("@Status", (int)copy.Status);
            command.Parameters.AddWithValue("@Location", (object?)copy.Location ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            if (result is int insertedId)
            {
                logger.LogInformation("Book copy added. Id={Id}, ISBN={ISBN}, Status={Status}", insertedId, copy.ISBN, copy.Status);
                return insertedId;
            }

            logger.LogWarning("Insert returned null or unexpected type. ISBN='{ISBN}', Status={Status}'", copy.ISBN, copy.Status);
            throw new InvalidOperationException("Failed to retrieve inserted bookCopy ID.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding book copy. ISBN={ISBN}", copy.ISBN);
            throw;
        }
    }
    #endregion

    #region Read
    /// <summary>
    /// Retrieves a book copy by its unique ID.
    /// </summary>
    public async Task<BookCopy?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM dbo.BookCopies WHERE Id = @Id;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                logger.LogInformation("Book copy retrieved. Id={Id}", id);
                return MapToBookCopy(reader);
            }

            logger.LogWarning("No book copy found. Id={Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving book copy. Id={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the first available copy of a book by ISBN.
    /// </summary>
    public async Task<BookCopy?> GetFirstAvailableCopyByIsbnAsync(string isbn)
    {
        const string sql = @"
            SELECT TOP 1 * FROM dbo.BookCopies
            WHERE ISBN = @ISBN AND Status = 0
            ORDER BY AddedDate ASC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ISBN", isbn);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                logger.LogInformation("First available copy retrieved for ISBN={ISBN}", isbn);
                return MapToBookCopy(reader);
            }

            logger.LogWarning("No available copy found for ISBN={ISBN}", isbn);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available copy for ISBN={ISBN}", isbn);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all copies of a book by ISBN.
    /// </summary>
    public async Task<IEnumerable<BookCopy>> GetCopiesByIsbnAsync(string isbn)
    {
        var copies = new List<BookCopy>();
        const string sql = "SELECT * FROM dbo.BookCopies WHERE ISBN = @ISBN ORDER BY AddedDate DESC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ISBN", isbn);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                copies.Add(MapToBookCopy(reader));

            logger.LogInformation("Retrieved {Count} copies for ISBN={ISBN}", copies.Count, isbn);
            return copies;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving copies for ISBN={ISBN}", isbn);
            throw;
        }
    }

    /// <summary>
    /// Gets the number of available copies for a specific ISBN.
    /// </summary>
    public async Task<int> GetAvailableCopyCountByIsbnAsync(string isbn)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.BookCopies WHERE ISBN = @ISBN AND Status = 0;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ISBN", isbn);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Available copy count for ISBN={ISBN}: {Count}", isbn, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available copy count for ISBN={ISBN}", isbn);
            throw;
        }
    }

    /// <summary>
    /// Gets the number of book copies with a specific status.
    /// </summary>
    public async Task<int> GetCountByStatusAsync(BookCopyStatus status)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.BookCopies WHERE Status = @Status;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", (int)status);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Copy count for status={Status}: {Count}", status, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving copy count for status={Status}", status);
            throw;
        }
    }
    
    /// <summary>
    /// Gets the total number of book copies.
    /// </summary>
    public async Task<int> GetTotalCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM dbo.BookCopies;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Total book copy count: {Count}", count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving total book copy count.");
            throw;
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Updates the status of a book copy.
    /// </summary>
    public async Task UpdateStatusAsync(int copyId, BookCopyStatus newStatus)
    {
        const string sql = "UPDATE dbo.BookCopies SET Status = @Status WHERE Id = @Id;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", (int)newStatus);
            command.Parameters.AddWithValue("@Id", copyId);

            var affected = await command.ExecuteNonQueryAsync();
            logger.LogInformation("Updated status for CopyId={CopyId} to {Status}. RowsAffected={Affected}", copyId, newStatus, affected);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating status for CopyId={CopyId}", copyId);
            throw;
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// A private helper method to map a row from the SqlDataReader to a BookCopy object.
    /// This reduces code duplication across methods.
    /// </summary>
    private static BookCopy MapToBookCopy(SqlDataReader reader)
    {
        return new BookCopy
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ISBN = reader.GetString(reader.GetOrdinal("ISBN")),
            // Cast the integer from the DB back to the enum
            Status = Enum.IsDefined(typeof(BookCopyStatus), reader.GetInt32(reader.GetOrdinal("Status")))
                ? (BookCopyStatus)reader.GetInt32(reader.GetOrdinal("Status"))
                : throw new InvalidOperationException($"Invalid BookCopyStatus value: {reader.GetInt32(reader.GetOrdinal("Status"))}"),

            Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
            AddedDate = reader.GetDateTime(reader.GetOrdinal("AddedDate"))
        };
    }
    #endregion
}
