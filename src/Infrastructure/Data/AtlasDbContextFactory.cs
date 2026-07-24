using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CrmAtlas.Infrastructure.Data;

public sealed class AtlasDbContextFactory : IDesignTimeDbContextFactory<AtlasDbContext>
{
    public AtlasDbContext CreateDbContext(string[] args)
    {
        var settingsPath = FindWebSettings();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(settingsPath, optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection não foi configurada.");
        }

        var options = new DbContextOptionsBuilder<AtlasDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AtlasDbContext).Assembly.FullName))
            .Options;

        return new AtlasDbContext(options);
    }

    private static string FindWebSettings()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentDirectory, "src", "Web", "appsettings.json"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "Web", "appsettings.json")),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "Não foi possível localizar src/Web/appsettings.json para o EF Core.");
    }
}
