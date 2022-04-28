using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace C_3PO.Data.Context
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
                .AddJsonFile("appsettings.Development.json", false, true)
#else
                .AddJsonFile("appsettings.json", false, true)
#endif
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(
                    configuration["Database"],
                    new MySqlServerVersion(new Version(8, 0, 27)));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}