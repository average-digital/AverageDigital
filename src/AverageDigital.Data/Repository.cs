namespace AverageDigital.Data
{
    public abstract class Repository
    {
        public RepositoryDataAccess Data { get; } = new RepositoryDataAccess();
    }
}
