using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace AverageDigital.Data.Oracle
{
    public class OracleDbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection GetDbConnection(string currentConnectionString, RepositoryDbAccess dbAccess)
        {
            return new OracleConnection(dbAccess.GetConnectionString(currentConnectionString));
        }
    }
}
