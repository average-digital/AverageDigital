using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AverageDigital.Data
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAverageDataService(this IServiceCollection _, IConfiguration configuration)
        {
            RepositoryDbAccess.Configuration = configuration;
        }
        public static void AddAverageConfiguration(this IConfiguration configuration)
        {
            RepositoryDbAccess.Configuration = configuration;
        }

        public static void AddAverageConnectionString(string connection)
        {
            RepositoryDbAccess.ForcedConnectionString = connection;
        }
    }
}
