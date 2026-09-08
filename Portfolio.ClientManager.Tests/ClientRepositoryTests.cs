using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Infrastructure.Data;
using Portfolio.ClientManager.Infrastructure.Repositories;

namespace Portfolio.ClientManager.Tests;

public sealed class ClientRepositoryTests
{
    [Fact]
    public async Task Repository_PersistsAddUpdateAndDeleteWithSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ClientManagerDbContext>()
            .UseSqlite(connection)
            .Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = factory.CreateDbContext())
        {
            await context.Database.EnsureCreatedAsync();
        }

        var repository = new ClientRepository(factory);
        var client = CreateClient("Ada Lovelace", "ada@example.com", ClientStatus.New);

        await repository.AddAsync(client);
        var saved = await repository.GetByIdAsync(client.Id);
        Assert.NotNull(saved);
        Assert.Equal("Ada Lovelace", saved.FullName);

        saved.Status = ClientStatus.Active;
        saved.Notes = "Follow up next week";
        await repository.UpdateAsync(saved);

        var updated = await repository.GetByIdAsync(client.Id);
        Assert.NotNull(updated);
        Assert.Equal(ClientStatus.Active, updated.Status);
        Assert.Equal("Follow up next week", updated.Notes);

        await repository.DeleteAsync(client.Id);
        Assert.Null(await repository.GetByIdAsync(client.Id));
    }

    [Fact]
    public async Task SearchAsync_TranslatesSearchAndStatusFilterForSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ClientManagerDbContext>()
            .UseSqlite(connection)
            .Options;
        var factory = new TestDbContextFactory(options);

        await using (var context = factory.CreateDbContext())
        {
            await context.Database.EnsureCreatedAsync();
            context.Clients.AddRange(
                CreateClient("Ada Lovelace", "ada@example.com", ClientStatus.Active),
                CreateClient("Grace Hopper", "grace@example.com", ClientStatus.Inactive),
                CreateClient("Alan Turing", "alan@example.com", ClientStatus.Active));
            await context.SaveChangesAsync();
        }

        var repository = new ClientRepository(factory);

        var result = await repository.SearchAsync("a", ClientStatus.Active);

        Assert.Equal(2, result.Count);
        Assert.All(result, client => Assert.Equal(ClientStatus.Active, client.Status));
    }

    private static Client CreateClient(string fullName, string email, ClientStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        return new Client
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private sealed class TestDbContextFactory(DbContextOptions<ClientManagerDbContext> options)
        : IDbContextFactory<ClientManagerDbContext>
    {
        public ClientManagerDbContext CreateDbContext() => new(options);
    }
}
