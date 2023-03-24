using System.Data;

namespace AverageDigital.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection GetDbConnection(string currentConnectionString, RepositoryDbAccess dbAccess);
    }
}
