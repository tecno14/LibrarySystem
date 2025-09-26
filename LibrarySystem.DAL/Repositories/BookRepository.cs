using LibrarySystem.Common.Models;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LibrarySystem.DAL.Repositories;

/// <summary>
/// Repository for managing data access for the Books table.
/// This class handles operations related to book titles and their metadata.
/// </summary>
public class BookRepository(IDbConnectionFactory connectionFactory, ILogger<BookRepository> logger) :
    BaseRepository(connectionFactory, logger), IBookRepository
{
    #region Create
    /// <summary>
    /// Adds a new book title to the database.
    /// </summary>
    /// <param name="book">The Book object to add.</param>
    /// <returns>The ID of the newly created book.</returns>
    public async Task<int> AddAsync(Book book)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(book.ISBN) || string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author))
        {
            logger.LogWarning("Attempted to add book with missing required fields. ISBN='{ISBN}', Title='{Title}', Author='{Author}'",
                book.ISBN, book.Title, book.Author);
            throw new ArgumentException("ISBN, Title, and Author are required.");
        }

        const string sql = @"
            INSERT INTO dbo.Books (ISBN, Title, Author, Publisher, PublicationYear, Genre, CoverImageUrl)
            OUTPUT INSERTED.Id
            VALUES (@ISBN, @Title, @Author, @Publisher, @PublicationYear, @Genre, @CoverImageUrl);";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ISBN", book.ISBN);
            command.Parameters.AddWithValue("@Title", book.Title);
            command.Parameters.AddWithValue("@Author", book.Author);
            command.Parameters.AddWithValue("@Publisher", (object?)book.Publisher ?? DBNull.Value);
            command.Parameters.AddWithValue("@PublicationYear", (object?)book.PublicationYear ?? DBNull.Value);
            command.Parameters.AddWithValue("@Genre", (object?)book.Genre ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverImageUrl", (object?)book.CoverImageUrl ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            if (result is int insertedId)
            {
                logger.LogInformation("Book added successfully. Id={Id}, ISBN='{ISBN}', Title='{Title}', Author='{Author}', Genre='{Genre}'",
                    insertedId, book.ISBN, book.Title, book.Author, book.Genre ?? "NULL");
                return insertedId;
            }

            logger.LogWarning("Insert returned null or unexpected type. ISBN='{ISBN}', Title='{Title}'", book.ISBN, book.Title);
            throw new InvalidOperationException("Failed to retrieve inserted book ID.");
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while adding book. ISBN='{ISBN}', Title='{Title}'", book.ISBN, book.Title);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while adding book. ISBN='{ISBN}', Title='{Title}'", book.ISBN, book.Title);
            throw;
        }
    }

    #endregion

    #region Read
    /// <summary>
    /// Retrieves a specific book by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the book to retrieve.</param>
    /// <returns>The Book object if found; otherwise, null.</returns>
    public async Task<Book?> GetByIdAsync(int id, bool includeHidden)
    {
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT * FROM dbo.Books WHERE Id = @Id");

        if (!includeHidden)
        {
            sqlBuilder.Append(" AND IsHidden = 0");
        }

        sqlBuilder.Append(";");

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var book = MapToBook(reader);
                logger.LogInformation("Book retrieved successfully. Id={Id}, Title='{Title}', ISBN='{ISBN}', IncludeHidden={IncludeHidden}",
                    book.Id, book.Title, book.ISBN, includeHidden);
                return book;
            }

            logger.LogWarning("No book found with Id={Id}. IncludeHidden={IncludeHidden}", id, includeHidden);
            return null;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving book by Id={Id}. IncludeHidden={IncludeHidden}", id, includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving book by Id={Id}. IncludeHidden={IncludeHidden}", id, includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a book by its ISBN.
    /// </summary>
    public async Task<Book?> GetByIsbnAsync(string isbn, bool includeHidden)
    {
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT * FROM dbo.Books WHERE ISBN = @ISBN");

        if (!includeHidden)
        {
            sqlBuilder.Append(" AND IsHidden = 0");
        }

        sqlBuilder.Append(";");

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sqlBuilder.ToString(), connection);
            command.Parameters.AddWithValue("@ISBN", isbn);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                logger.LogInformation("Book retrieved by ISBN: {ISBN}. IncludeHidden={IncludeHidden}", isbn, includeHidden);
                return MapToBook(reader);
            }

            logger.LogWarning("No book found for ISBN: {ISBN}. IncludeHidden={IncludeHidden}", isbn, includeHidden);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving book by ISBN: {ISBN}. IncludeHidden={IncludeHidden}", isbn, includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a paginated list of books, optionally including hidden ones.
    /// </summary>
    public async Task<IEnumerable<Book>> GetPagedAsync(int page, int pageSize, bool includeHidden)
    {
        var books = new List<Book>();
        var offset = (page - 1) * pageSize;

        var sql = @"
        SELECT * FROM dbo.Books
        WHERE 1 = 1";

        if (!includeHidden)
            sql += " AND IsHidden = 0";

        sql += @"
        ORDER BY Title
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                books.Add(MapToBook(reader));

            logger.LogInformation("Retrieved {Count} books for page {Page}. includeHidden={IncludeHidden}", books.Count, page, includeHidden);
            return books;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged books. Page={Page}, includeHidden={IncludeHidden}", page, includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Searches books by title or author with paging and optional hidden inclusion.
    /// </summary>
    public async Task<IEnumerable<Book>> SearchAsync(string keyword, int page, int pageSize, bool includeHidden)
    {
        var books = new List<Book>();
        var offset = (page - 1) * pageSize;

        var sql = @"
            SELECT * FROM dbo.Books
            WHERE (Title LIKE @Keyword OR Author LIKE @Keyword)";

        if (!includeHidden)
            sql += " AND IsHidden = 0";

        sql += @"
        ORDER BY Title
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                books.Add(MapToBook(reader));

            logger.LogInformation("Search completed for keyword '{Keyword}'. Matches={Count}, Page={Page}", keyword, books.Count, page);
            return books;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during search for keyword '{Keyword}'. Page={Page}", keyword, page);
            throw;
        }
    }

    /// <summary>
    /// Retrieves books by genre with paging and optional hidden inclusion.
    /// </summary>
    public async Task<IEnumerable<Book>> GetBooksByGenreAsync(string genre, int page, int pageSize, bool includeHidden)
    {
        var books = new List<Book>();
        var offset = (page - 1) * pageSize;

        var sql = @"
            SELECT * FROM dbo.Books
            WHERE Genre = @Genre";

        if (!includeHidden)
            sql += " AND IsHidden = 0";

        sql += @"
            ORDER BY Title
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Genre", genre);
            command.Parameters.AddWithValue("@Offset", offset);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                books.Add(MapToBook(reader));

            logger.LogInformation("Retrieved {Count} books for genre '{Genre}' on page {Page}. includeHidden={IncludeHidden}", books.Count, genre, page, includeHidden);
            return books;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving books by genre '{Genre}'. Page={Page}", genre, page);
            throw;
        }
    }

    /// <summary>
    /// Gets a list of the most recently added books. For the dashboard.
    /// </summary>
    /// <param name="count">The maximum number of books to retrieve.</param>
    /// <returns>A list of recently added books.</returns>
    public async Task<IEnumerable<Book>> GetRecentlyAddedAsync(int count)
    {
        var books = new List<Book>();

        if (count <= 0)
        {
            logger.LogWarning("Attempted to retrieve recently added books with non-positive count: {Count}", count);
            return books;
        }

        const string sql = "SELECT TOP (@Count) * FROM dbo.Books WHERE IsHidden = 0 ORDER BY CreatedAt DESC;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Count", count);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                books.Add(MapToBook(reader));
            }

            logger.LogInformation("Retrieved {Count} recently added books.", books.Count);
            return books;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving recently added books. Requested count={Count}", count);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving recently added books. Requested count={Count}", count);
            throw;
        }
    }

    /// <summary>
    /// Gets a distinct list of all genres from the Books table.
    /// </summary>
    /// <returns>A list of genre names.</returns>
    public async Task<IEnumerable<string>> GetAllGenresAsync(bool includeHidden)
    {
        var genres = new List<string>();

        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT DISTINCT Genre FROM dbo.Books WHERE Genre IS NOT NULL");

        if (!includeHidden)
        {
            sqlBuilder.Append(" AND IsHidden = 0");
        }

        sqlBuilder.Append(" ORDER BY Genre;");
        var sql = sqlBuilder.ToString();

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                genres.Add(reader.GetString(0));
            }

            logger.LogInformation("Retrieved {Count} distinct genres. IncludeHidden={IncludeHidden}", genres.Count, includeHidden);
            return genres;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving genres. IncludeHidden={IncludeHidden}", includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving genres. IncludeHidden={IncludeHidden}", includeHidden);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the total count of books in a specific genre.
    /// </summary>
    public async Task<int> GetGenreCountAsync(string genre)
    {
        const string sql = "SELECT COUNT(*) FROM dbo.Books WHERE Genre = @Genre AND IsHidden = 0;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Genre", genre);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Genre count retrieved for '{Genre}': {Count}", genre, count);
            return count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving genre count for '{Genre}'", genre);
            throw;
        }
    }

    /// <summary>
    /// Gets the total count of non-hidden book titles. For the dashboard.
    /// </summary>
    /// <returns>The number of visible books.</returns>
    public async Task<int> GetTotalCountAsync(bool includeHidden)
    {
        var sqlBuilder = new StringBuilder();
        sqlBuilder.Append("SELECT COUNT(*) FROM dbo.Books");

        if (!includeHidden)
        {
            sqlBuilder.Append(" WHERE IsHidden = 0");
        }

        sqlBuilder.Append(";");

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sqlBuilder.ToString(), connection);

            var result = await command.ExecuteScalarAsync();
            var count = result is int value ? value : 0;

            logger.LogInformation("Retrieved total book count: {Count}. IncludeHidden={IncludeHidden}", count, includeHidden);
            return count;
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while retrieving total book count. IncludeHidden={IncludeHidden}", includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while retrieving total book count. IncludeHidden={IncludeHidden}", includeHidden);
            throw;
        }
    }
    #endregion

    #region Update
    /// <summary>
    /// Updates the details of an existing book title.
    /// </summary>
    /// <param name="book">The Book object containing updated data.</param>
    public async Task UpdateAsync(Book book)
    {
        // Validate required fields
        if (book.Id <= 0 || string.IsNullOrWhiteSpace(book.ISBN) || string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author))
        {
            logger.LogWarning("Attempted to update book with invalid or missing fields. Id={Id}, ISBN='{ISBN}', Title='{Title}', Author='{Author}'",
                book.Id, book.ISBN, book.Title, book.Author);
            throw new ArgumentException("Id, ISBN, Title, and Author are required.");
        }

        const string sql = @"
        UPDATE dbo.Books SET
            ISBN = @ISBN,
            Title = @Title,
            Author = @Author,
            Publisher = @Publisher,
            PublicationYear = @PublicationYear,
            Genre = @Genre,
            CoverImageUrl = @CoverImageUrl
        WHERE Id = @Id;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Id", book.Id);
            command.Parameters.AddWithValue("@ISBN", book.ISBN);
            command.Parameters.AddWithValue("@Title", book.Title);
            command.Parameters.AddWithValue("@Author", book.Author);
            command.Parameters.AddWithValue("@Publisher", (object?)book.Publisher ?? DBNull.Value);
            command.Parameters.AddWithValue("@PublicationYear", (object?)book.PublicationYear ?? DBNull.Value);
            command.Parameters.AddWithValue("@Genre", (object?)book.Genre ?? DBNull.Value);
            command.Parameters.AddWithValue("@CoverImageUrl", (object?)book.CoverImageUrl ?? DBNull.Value);

            var affected = await command.ExecuteNonQueryAsync();

            if (affected > 0)
            {
                logger.LogInformation("Book updated successfully. Id={Id}, Title='{Title}', ISBN='{ISBN}'", book.Id, book.Title, book.ISBN);
            }
            else
            {
                logger.LogWarning("No book record updated. Id={Id}", book.Id);
            }
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while updating book. Id={Id}, ISBN='{ISBN}'", book.Id, book.ISBN);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while updating book. Id={Id}, ISBN='{ISBN}'", book.Id, book.ISBN);
            throw;
        }
    }

    /// <summary>
    /// Performs a soft delete by setting the IsHidden flag for a book title.
    /// </summary>
    /// <param name="id">The ID of the book to update.</param>
    /// <param name="includeHidden">The new hidden status to apply.</param>
    public async Task SetHiddenStatusAsync(int id, bool includeHidden)
    {
        if (id <= 0)
        {
            logger.LogWarning("Attempted to set hidden status with invalid book Id={Id}", id);
            throw new ArgumentException("Book ID must be greater than zero.");
        }

        const string sql = "UPDATE dbo.Books SET IsHidden = @IsHidden WHERE Id = @Id;";

        try
        {
            await using var connection = await GetOpenConnectionAsync();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IsHidden", includeHidden);

            var affected = await command.ExecuteNonQueryAsync();

            if (affected > 0)
            {
                logger.LogInformation("Book hidden status updated. Id={Id}, IsHidden={IsHidden}", id, includeHidden);
            }
            else
            {
                logger.LogWarning("No book record updated. Id={Id}", id);
            }
        }
        catch (SqlException ex)
        {
            logger.LogError(ex, "SQL error while updating hidden status. Id={Id}, IsHidden={IsHidden}", id, includeHidden);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while updating hidden status. Id={Id}, IsHidden={IsHidden}", id, includeHidden);
            throw;
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// A private helper method to map a row from the SqlDataReader to a Book object.
    /// </summary>
    private static Book MapToBook(SqlDataReader reader)
    {
        return new Book
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            ISBN = reader.GetString(reader.GetOrdinal("ISBN")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Author = reader.GetString(reader.GetOrdinal("Author")),
            Publisher = reader.IsDBNull(reader.GetOrdinal("Publisher")) ? null : reader.GetString(reader.GetOrdinal("Publisher")),
            PublicationYear = reader.IsDBNull(reader.GetOrdinal("PublicationYear")) ? null : reader.GetInt32(reader.GetOrdinal("PublicationYear")),
            Genre = reader.IsDBNull(reader.GetOrdinal("Genre")) ? null : reader.GetString(reader.GetOrdinal("Genre")),
            CoverImageUrl = reader.IsDBNull(reader.GetOrdinal("CoverImageUrl")) ? null : reader.GetString(reader.GetOrdinal("CoverImageUrl")),
            IsHidden = reader.GetBoolean(reader.GetOrdinal("IsHidden")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
    #endregion
}
