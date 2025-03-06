using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LMPWebService.Extensions
{
    public static class DataBaseExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //var connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL_CONNECTION_STRING is not set.");
            }

            serviceCollection.AddDbContext<AstraDbContext>(options =>
            {
                options.UseSqlServer(connectionString); 
            });
            return serviceCollection;
        }
    }
}

