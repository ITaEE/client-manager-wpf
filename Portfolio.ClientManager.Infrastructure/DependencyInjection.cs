using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Infrastructure.Data;
using Portfolio.ClientManager.Infrastructure.Repositories;

namespace Portfolio.ClientManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Portfolio.ClientManager");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "client-manager.db");
        services.AddDbContextFactory<ClientManagerDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<IClientRepository, ClientRepository>();

        return services;
    }
}
