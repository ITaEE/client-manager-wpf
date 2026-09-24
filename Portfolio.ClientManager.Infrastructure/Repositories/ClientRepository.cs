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

    public async Task<IReadOnlyList<Client>> FindPotentialDuplicatesAsync(
        Guid? excludedClientId,
        string? phone,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizeComparisonValue(phone);
        var normalizedEmail = NormalizeComparisonValue(email);
        if (normalizedPhone is null && normalizedEmail is null)
        {
            return [];
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Clients.AsNoTracking().AsQueryable();
        if (excludedClientId is not null)
        {
            query = query.Where(client => client.Id != excludedClientId);
        }

        return await query
            .Where(client =>
                (normalizedPhone != null && client.Phone != null && client.Phone.ToLower() == normalizedPhone) ||
                (normalizedEmail != null && client.Email != null && client.Email.ToLower() == normalizedEmail))
            .OrderBy(client => client.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Clients.Add(client);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<Client> clients, CancellationToken cancellationToken = default)
    {
        if (clients.Count == 0)
        {
            return;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.Clients.AddRangeAsync(clients, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
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

    private static string? NormalizeComparisonValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
