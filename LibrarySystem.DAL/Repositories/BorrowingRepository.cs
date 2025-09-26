using LibrarySystem.Common.Enums;
using LibrarySystem.Common.Models;
using LibrarySystem.DAL.Extensions;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.DAL.Repositories;

/// <summary>
/// Repository for managing data access for the Borrowings table.
/// It handles transactional operations for borrowing and returning books.
/// </summary>
public class BorrowingRepository(IDbConnectionFactory connectionFactory, ILogger<BorrowingRepository> logger) : 
    BaseRepository(connectionFactory, logger), IBorrowingRepository
{
    #region Create
    /// <summary>
    /// Creates a new borrowing record and updates the book copy's status within a single transaction.
    /// </summary>
    /// <param name="copyId">The ID of the book copy being borrowed.</param>
    /// <param name="userId">The ID of the user borrowing the book.</param>
    /// <param name="newStatus">The new status for the book copy (e.g., "Borrowed").</param>
    /// <returns>True if the transaction was successful; otherwise, false.</returns>
    public async Task<bool> CreateBorrowingAndUpdateCopyStatusAsync(int copyId, int userId, BookCopyStatus newStatus, int daysCount)
    {
        await using var connection = await GetOpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            const string insertBorrowingSql = @"
                INSERT INTO dbo.Borrowings (UserId, CopyId, BorrowDate, DueDate)
                VALUES (@UserId, @CopyId, GETUTCDATE(), DATEADD(day, @DaysCount, GETUTCDATE()));";

            await using var insertCommand = new SqlCommand(insertBorrowingSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("@UserId", userId);
            insertCommand.Parameters.AddWithValue("@CopyId", copyId);
            insertCommand.Parameters.AddWithValue("@DaysCount", daysCount);
            await insertCommand.ExecuteNonQueryAsync();

            const string updateCopySql = "UPDATE dbo.BookCopies SET Status = @Status WHERE Id = @Id;";
            await using var updateCommand = new SqlCommand(updateCopySql, connection, transaction);
            updateCommand.Parameters.AddWithValue("@Status", (int)newStatus);
            updateCommand.Parameters.AddWithValue("@Id", copyId);
            await updateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            logger.LogInformation("Borrowing created and copy status updated. CopyId={CopyId}, UserId={UserId}, Status='{Status}'", copyId, userId, newStatus);
            return true;
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "SQL error during borrowing creation. CopyId={CopyId}, UserId={UserId}", copyId, userId);
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Unexpected error during borrowing creation. CopyId={CopyId}, UserId={UserId}", copyId, userId);
            return false;
        }
    }
    #endregion

    #region Read
    /// <summary>
    /// Retrieves a specific borrowing record by its unique ID.
    /// </summary>
    public async Task<Borrowing?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM dbo.Borrowings WHERE Id = @Id;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                logger.LogInformation("Borrowing record retrieved. Id={BorrowingId}", id);
                return new Borrowing
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    CopyId = reader.GetInt32(reader.GetOrdinal("CopyId")),
                    BorrowDate = reader.GetDateTime(reader.GetOrdinal("BorrowDate")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReturnDate"))
                };
            }

            logger.LogWarning("No borrowing record found. Id={BorrowingId}", id);
            return null;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving borrowing record. Id={BorrowingId}", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving borrowing record. Id={BorrowingId}", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the complete borrowing history for a specific user, including details about the books.
    /// </summary>
    public async Task<IEnumerable<Borrowing>> GetBorrowingsByUserIdAsync(int userId)
    {
        var borrowings = new List<Borrowing>();
        const string sql = @"
        SELECT
            br.Id, br.UserId, br.CopyId, br.BorrowDate, br.DueDate, br.ReturnDate,
            b.Title AS BookTitle,
            b.Author AS BookAuthor,
            b.ISBN AS BookIsbn
        FROM dbo.Borrowings br
        JOIN dbo.BookCopies bc ON br.CopyId = bc.Id
        JOIN dbo.Books b ON bc.ISBN = b.ISBN
        WHERE br.UserId = @UserId
        ORDER BY br.BorrowDate DESC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                borrowings.Add(MapToBorrowingWithDetails(reader));
            }

            logger.LogInformation("Retrieved {Count} borrowings for UserId={UserId}", borrowings.Count, userId);
            return borrowings;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving borrowings for UserId={UserId}", userId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving borrowings for UserId={UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Gets a list of all books that are currently checked out and past their due date.
    /// </summary>
    public async Task<IEnumerable<Borrowing>> GetOverdueBorrowingsAsync()
    {
        var borrowings = new List<Borrowing>();
        const string sql = @"
        SELECT
            br.Id, br.UserId, br.CopyId, br.BorrowDate, br.DueDate, br.ReturnDate,
            b.Title AS BookTitle,
            b.Author AS BookAuthor,
            u.FullName AS UserFullName
        FROM dbo.Borrowings br
        JOIN dbo.BookCopies bc ON br.CopyId = bc.Id
        JOIN dbo.Books b ON bc.ISBN = b.ISBN
        JOIN dbo.Users u ON br.UserId = u.Id
        WHERE br.ReturnDate IS NULL AND br.DueDate < GETUTCDATE()
        ORDER BY br.DueDate ASC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                borrowings.Add(MapToBorrowingWithDetails(reader));
            }

            logger.LogInformation("Retrieved {Count} overdue borrowings.", borrowings.Count);
            return borrowings;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving overdue borrowings.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving overdue borrowings.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves all active borrowings where the book has not yet been returned.
    /// </summary>
    /// <returns>A list of active borrowing records.</returns>
    public async Task<IEnumerable<Borrowing>> GetActiveBorrowingsAsync()
    {
        var borrowings = new List<Borrowing>();
        const string sql = @"
            SELECT
                br.Id, br.UserId, br.CopyId, br.BorrowDate, br.DueDate, br.ReturnDate,
                b.Title AS BookTitle,
                b.Author AS BookAuthor,
                u.FullName AS UserFullName
            FROM dbo.Borrowings br
            JOIN dbo.BookCopies bc ON br.CopyId = bc.Id
            JOIN dbo.Books b ON bc.ISBN = b.ISBN
            JOIN dbo.Users u ON br.UserId = u.Id
            WHERE br.ReturnDate IS NULL
            ORDER BY br.DueDate ASC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                borrowings.Add(MapToBorrowingWithDetails(reader));
            }

            logger.LogInformation("Retrieved {Count} active borrowings.", borrowings.Count);
            return borrowings;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving active borrowings.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving active borrowings.");
            throw;
        }
    }

    /// <summary>
    /// Retrieves the borrowing history for a specific book copy.
    /// </summary>
    /// <param name="copyId">The ID of the book copy.</param>
    /// <returns>A list of borrowing records for the specified copy.</returns>
    public async Task<IEnumerable<Borrowing>> GetBorrowingsByCopyIdAsync(int copyId)
    {
        var borrowings = new List<Borrowing>();
        const string sql = @"
            SELECT
                br.Id, br.UserId, br.CopyId, br.BorrowDate, br.DueDate, br.ReturnDate,
                u.FullName AS UserFullName
            FROM dbo.Borrowings br
            JOIN dbo.Users u ON br.UserId = u.Id
            WHERE br.CopyId = @CopyId
            ORDER BY br.BorrowDate DESC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CopyId", copyId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                borrowings.Add(MapToBorrowingWithDetails(reader));
            }

            logger.LogInformation("Retrieved {Count} borrowings for CopyId={CopyId}.", borrowings.Count, copyId);
            return borrowings;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving borrowings for CopyId={CopyId}.", copyId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving borrowings for CopyId={CopyId}.", copyId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the total number of borrowing records.
    /// Optionally includes returned books based on the <paramref name="includeReturned"/> flag.
    /// </summary>
    /// <param name="includeReturned">Whether to include returned borrowings in the count.</param>
    /// <returns>The total number of borrowings.</returns>
    public async Task<int> GetTotalBorrowingsCountAsync(bool includeReturned)
    {
        var sql = "SELECT COUNT(*) FROM dbo.Borrowings";

        if (!includeReturned)
        {
            sql += " WHERE ReturnDate IS NULL";
        }

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Retrieved total borrowings count: {Count}. includeReturned={IncludeReturned}", count, includeReturned);
            return count;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving total borrowings count. includeReturned={IncludeReturned}", includeReturned);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving total borrowings count. includeReturned={IncludeReturned}", includeReturned);
            throw;
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Finalizes a return by setting the return date and updating the book copy's status in a transaction.
    /// </summary>
    /// <param name="borrowingId">The ID of the borrowing record.</param>
    /// <param name="copyId">The ID of the book copy being returned.</param>
    /// <param name="newStatus">The new status for the copy (e.g., "Available", "Damaged").</param>
    /// <returns>True if the transaction was successful; otherwise, false.</returns>
    public async Task<bool> FinalizeReturnAndUpdateCopyStatusAsync(int borrowingId, int copyId, BookCopyStatus newStatus)
    {
        await using var connection = await GetOpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            const string updateBorrowingSql = "UPDATE dbo.Borrowings SET ReturnDate = GETUTCDATE() WHERE Id = @Id;";
            await using var updateBorrowingCommand = new SqlCommand(updateBorrowingSql, connection, transaction);
            updateBorrowingCommand.Parameters.AddWithValue("@Id", borrowingId);
            await updateBorrowingCommand.ExecuteNonQueryAsync();

            const string updateCopySql = "UPDATE dbo.BookCopies SET Status = @Status WHERE Id = @Id;";
            await using var updateCopyCommand = new SqlCommand(updateCopySql, connection, transaction);
            updateCopyCommand.Parameters.AddWithValue("@Status", (int)newStatus);
            updateCopyCommand.Parameters.AddWithValue("@Id", copyId);
            await updateCopyCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            logger.LogInformation("Return finalized and copy status updated. BorrowingId={BorrowingId}, CopyId={CopyId}, Status='{Status}'", borrowingId, copyId, newStatus);
            return true;
        }
        catch (SqlException ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "SQL error during return finalization. BorrowingId={BorrowingId}, CopyId={CopyId}", borrowingId, copyId);
            return false;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Unexpected error during return finalization. BorrowingId={BorrowingId}, CopyId={CopyId}", borrowingId, copyId);
            return false;
        }
    }

    /// <summary>
    /// Extends the due date of a borrowing record.
    /// </summary>
    /// <param name="borrowingId">The ID of the borrowing record.</param>
    /// <param name="newDueDate">The new due date to set.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    public async Task<bool> ExtendDueDateAsync(int borrowingId, DateTime newDueDate)
    {
        const string sql = "UPDATE dbo.Borrowings SET DueDate = @NewDueDate WHERE Id = @Id AND ReturnDate IS NULL;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@NewDueDate", newDueDate);
            command.Parameters.AddWithValue("@Id", borrowingId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            var success = rowsAffected > 0;

            if (success)
                logger.LogInformation("Due date extended for BorrowingId={BorrowingId} to {NewDueDate}.", borrowingId, newDueDate);
            else
                logger.LogWarning("No rows affected while extending due date for BorrowingId={BorrowingId}.", borrowingId);

            return success;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while extending due date for BorrowingId={BorrowingId}.", borrowingId);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while extending due date for BorrowingId={BorrowingId}.", borrowingId);
            throw;
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Helper method to map a data row to a Borrowing object with JOINed details.
    /// </summary>
    private static Borrowing MapToBorrowingWithDetails(SqlDataReader reader)
    {
        var borrowing = new Borrowing
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            CopyId = reader.GetInt32(reader.GetOrdinal("CopyId")),
            BorrowDate = reader.GetDateTime(reader.GetOrdinal("BorrowDate")),
            DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
            ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ReturnDate"))
        };

        // Safely read optional JOINed columns
        if (reader.HasColumn("BookTitle"))
            borrowing.BookTitle = reader.GetString(reader.GetOrdinal("BookTitle"));
        if (reader.HasColumn("BookAuthor"))
            borrowing.BookAuthor = reader.GetString(reader.GetOrdinal("BookAuthor"));
        if (reader.HasColumn("BookIsbn"))
            borrowing.BookIsbn = reader.GetString(reader.GetOrdinal("BookIsbn"));
        if (reader.HasColumn("UserFullName"))
            borrowing.UserFullName = reader.GetString(reader.GetOrdinal("UserFullName"));

        return borrowing;
    }
    #endregion
}
