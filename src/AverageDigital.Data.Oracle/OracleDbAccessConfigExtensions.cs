namespace AverageDigital.Data.Oracle
{
    public static class OracleDbAccessConfigExtensions
    {
        public static OracleDbConnectionFactory OnOracle(this RepositoryDbAccess _)
        {
            return new OracleDbConnectionFactory();
        }
    }
}
