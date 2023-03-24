using System.Data;
using Microsoft.Data.SqlClient;

namespace AverageDigital.Data.SqlServer
{
    public class SQLServerDbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection GetDbConnection(string currentConnectionString, RepositoryDbAccess dbAccess)
        {
            return new SqlConnection(dbAccess.GetConnectionString(currentConnectionString));
        }
    }
}
