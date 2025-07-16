using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace HealthcareApp.Identity.API.Infrastructure.Data;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // Recursively search for appsettings.json up the directory tree
        string? FindConfig(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                var configPath = Path.Combine(dir.FullName, "appsettings.json");
                if (File.Exists(configPath))
                    return configPath;
                dir = dir.Parent;
            }
            return null;
        }

        var basePath = Directory.GetCurrentDirectory();
        var configPath = FindConfig(basePath);
        if (configPath == null)
            throw new FileNotFoundException("Could not find appsettings.json in any parent directory.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(configPath) ?? basePath)
            .AddJsonFile(Path.GetFileName(configPath), optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlServer(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
