using Microsoft.Data.SqlClient;

namespace LibrarySystem.DAL.Interfaces;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
