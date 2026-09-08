using Microsoft.EntityFrameworkCore;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Infrastructure.Data;

namespace Portfolio.ClientManager.Infrastructure.Repositories;

public sealed class ClientRepository(IDbContextFactory<ClientManagerDbContext> contextFactory)
    : IClientRepository
{
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Clients.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> SearchAsync(
        string? searchText,
        ClientStatus? status,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Clients.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            query = query.Where(client => client.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim().ToLower();
            query = query.Where(client =>
                client.FullName.ToLower().Contains(term) ||
                (client.Phone != null && client.Phone.ToLower().Contains(term)) ||
                (client.Email != null && client.Email.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(client => client.FullName)
            .ThenBy(client => client.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Clients.AsNoTracking()
            .SingleOrDefaultAsync(client => client.Id == id, cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Clients.Add(client);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Clients.Update(client);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var deleted = await context.Clients
            .Where(client => client.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            throw new KeyNotFoundException("The client no longer exists.");
        }
    }
}
