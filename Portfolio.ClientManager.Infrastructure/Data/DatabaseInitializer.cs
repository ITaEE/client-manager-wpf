using Microsoft.EntityFrameworkCore;

namespace Portfolio.ClientManager.Infrastructure.Data;

public sealed class DatabaseInitializer(IDbContextFactory<ClientManagerDbContext> contextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Clients_Phone ON Clients (Phone);",
            cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_Clients_Email ON Clients (Email);",
            cancellationToken);
    }
}
