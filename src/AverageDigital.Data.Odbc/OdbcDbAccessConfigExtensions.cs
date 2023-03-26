namespace AverageDigital.Data.Odbc
{
    public static class OdbcDbAccessConfigExtensions
    {
        public static OdbcDbConnectionFactory OnOdbc(this RepositoryDbAccess _)
        {
            return new OdbcDbConnectionFactory();
        }
    }
}
