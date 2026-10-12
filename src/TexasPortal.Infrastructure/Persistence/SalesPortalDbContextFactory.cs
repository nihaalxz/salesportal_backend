using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TexasPortal.Infrastructure.Persistence;

public class SalesPortalDbContextFactory : IDesignTimeDbContextFactory<SalesPortalDbContext>
{
    public SalesPortalDbContext CreateDbContext(string[] args)
    {
        var apiPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TexasPortal.Api");
        if (!Directory.Exists(apiPath))
        {
            apiPath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=db67449.public.databaseasp.net; Database=db67449; User Id=db67449; Password=n#7YD2o%5?Qy; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;";

        var optionsBuilder = new DbContextOptionsBuilder<SalesPortalDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new SalesPortalDbContext(optionsBuilder.Options);
    }
}
