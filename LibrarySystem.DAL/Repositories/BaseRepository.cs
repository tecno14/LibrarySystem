using LibrarySystem.Common.Exceptions;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.DAL.Repositories;

/// <summary>
/// An abstract base class for all repositories in the Data Access Layer.
/// Its primary responsibility is to manage the database connection string
/// and provide a common foundation for concrete repository implementations.
/// </summary>
/// <remarks>
/// Initializes a new instance of a repository class.
/// This constructor is called by the constructors of derived classes.
/// </remarks>
public abstract class BaseRepository(IDbConnectionFactory connectionFactory, ILogger<BaseRepository> logger)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<BaseRepository> _logger = logger;

    protected async Task<SqlConnection> GetOpenConnectionAsync()
    {
        var connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch (SqlException ex)
        {
            // 1. Log the detailed, low-level SQL exception for debugging.
            _logger.LogError(ex, "A database connection error occurred.");

            // 2. Throw a new, high-level custom exception to the calling layer.
            // This hides the implementation details but clearly signals the problem.
            throw new DataAccessConnectionException("Could not connect to the database. Please check the connection string and ensure the server is available.", ex);
        }
    }
}
