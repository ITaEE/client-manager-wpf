using Microsoft.EntityFrameworkCore;

namespace Portfolio.ClientManager.Infrastructure.Data;

public sealed class DatabaseInitializer(IDbContextFactory<ClientManagerDbContext> contextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
