namespace AverageDigital.Data.Oracle
{
    public static class RepositoryDataAccessExtensions
    {
        public static RepositoryDbAccess Db(this RepositoryDataAccess _)
        {
            return new RepositoryDbAccess
            {
                DbConnectionFactory = new OracleDbConnectionFactory()
            };
        }
    }
}
