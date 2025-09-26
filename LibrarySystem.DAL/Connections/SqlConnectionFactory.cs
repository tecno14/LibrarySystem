using LibrarySystem.Common.Exceptions;
using LibrarySystem.DAL.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.DAL.Connections;

public class SqlConnectionFactory(IConfiguration config, ILogger<SqlConnectionFactory> logger) : IDbConnectionFactory
{
    private readonly ILogger<SqlConnectionFactory> _logger = logger;

    /// <summary>
    /// The database connection string, accessible only to classes that inherit from BaseRepository.
    /// It is marked as 'readonly' because it should only be set once in the constructor.
    /// </summary>
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection") ??
        throw new DataAccessConnectionException("Database connection string cannot be null or empty, Connection string 'DefaultConnection' not found.");

    /// <summary>
    /// Creates and opens a new database connection with robust error handling.
    /// </summary>
    /// <returns>A new, open SqlConnection object.</returns>
    /// <exception cref="DataAccessConnectionException">Thrown if the connection to the database fails.</exception>
    public SqlConnection CreateConnection()
    {
        try
        {
            var connection = new SqlConnection(_connectionString);
            return connection;
        }
        catch (SqlException ex)
        {
            // 1. Log the detailed, low-level SQL exception for debugging.
            _logger.LogError(ex, "A database connection error occurred. ConnectionString: {ConnectionString}", _connectionString);

            // 2. Throw a new, high-level custom exception to the calling layer.
            // This hides the implementation details but clearly signals the problem.
            throw new DataAccessConnectionException("Could not connect to the database. Please check the connection string and ensure the server is available.", ex);
        }
    }
}
