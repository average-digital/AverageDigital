namespace AverageDigital.Data.SqlServer
{
    public static class RepositoryDataAccessExtensions
    {
        public static RepositoryDbAccess Db(this RepositoryDataAccess _)
        {
            return new RepositoryDbAccess
            {
                DbConnectionFactory = new SQLServerDbConnectionFactory()
            };
        }
    }
}
