using System.Data;
using System.Data.Odbc;

namespace AverageDigital.Data.Odbc
{
    public class OdbcDbConnectionFactory : IDbConnectionFactory
    {
        public IDbConnection GetDbConnection(string currentConnectionString, RepositoryDbAccess dbAccess)
        {
            return new OdbcConnection(dbAccess.GetConnectionString(currentConnectionString));
        }
    }
}
