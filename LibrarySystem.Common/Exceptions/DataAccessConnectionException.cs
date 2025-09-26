namespace LibrarySystem.Common.Exceptions;

/// <summary>
/// Custom exception for failures related to establishing a database connection.
/// This allows higher layers to catch a specific, meaningful exception type.
/// </summary>
public class DataAccessConnectionException : Exception
{
    public DataAccessConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DataAccessConnectionException(string message)
        : base(message)
    {
    }
}
