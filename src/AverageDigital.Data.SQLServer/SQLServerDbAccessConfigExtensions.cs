namespace AverageDigital.Data.SqlServer
{
    public static class SQLServerDbAccessConfigExtensions
    {
        public static SQLServerDbConnectionFactory OnSQLServer(this RepositoryDbAccess _)
        {
            return new SQLServerDbConnectionFactory();
        }
    }
}
