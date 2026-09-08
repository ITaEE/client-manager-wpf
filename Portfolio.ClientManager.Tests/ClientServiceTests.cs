using Portfolio.ClientManager.Core.Exceptions;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Core.Services;

namespace Portfolio.ClientManager.Tests;

public sealed class ClientServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_CreatesNormalizedClient()
    {
        var repository = new InMemoryClientRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            new ClientInput("  Ada Lovelace  ", "  +44 20 1234  ", "  ada@example.com  ", ClientStatus.New, "  First contact  "));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Ada Lovelace", result.FullName);
        Assert.Equal("+44 20 1234", result.Phone);
        Assert.Equal("ada@example.com", result.Email);
        Assert.Equal("First contact", result.Notes);
        Assert.Equal(Now, result.CreatedAt);
        Assert.Equal(Now, result.UpdatedAt);
        Assert.Single(repository.Clients);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndPreservesCreationDate()
    {
        var originalCreatedAt = Now.AddDays(-5);
        var client = new Client
        {
            Id = Guid.NewGuid(),
            FullName = "Old Name",
            Status = ClientStatus.New,
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };
        var repository = new InMemoryClientRepository(client);
        var service = CreateService(repository);

        var result = await service.UpdateAsync(
            client.Id,
            new ClientInput("New Name", null, "new@example.com", ClientStatus.Active, null));

        Assert.Equal("New Name", result.FullName);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal(ClientStatus.Active, result.Status);
        Assert.Equal(originalCreatedAt, result.CreatedAt);
        Assert.Equal(Now, result.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_RejectsMissingFullName(string fullName)
    {
        var service = CreateService(new InMemoryClientRepository());

        var exception = await Assert.ThrowsAsync<ClientValidationException>(() =>
            service.CreateAsync(new ClientInput(fullName, null, null, ClientStatus.New, null)));

        Assert.Contains(nameof(ClientInput.FullName), exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidEmail()
    {
        var service = CreateService(new InMemoryClientRepository());

        var exception = await Assert.ThrowsAsync<ClientValidationException>(() =>
            service.CreateAsync(new ClientInput("Grace Hopper", null, "not-an-email", ClientStatus.Active, null)));

        Assert.Contains(nameof(ClientInput.Email), exception.Errors.Keys);
    }

    [Fact]
    public async Task SearchAsync_SearchesContactFieldsAndFiltersStatus()
    {
        var repository = new InMemoryClientRepository(
            CreateClient("Ada Lovelace", "+44 100", "ada@example.com", ClientStatus.Active),
            CreateClient("Grace Hopper", "+1 200", "grace@example.com", ClientStatus.Inactive),
            CreateClient("Alan Turing", "+44 300", "alan@example.com", ClientStatus.Active));
        var service = CreateService(repository);

        var byEmail = await service.SearchAsync("GRACE@EXAMPLE.COM", null);
        var activeByPhone = await service.SearchAsync("+44", ClientStatus.Active);

        Assert.Single(byEmail);
        Assert.Equal("Grace Hopper", byEmail[0].FullName);
        Assert.Equal(2, activeByPhone.Count);
        Assert.All(activeByPhone, client => Assert.Equal(ClientStatus.Active, client.Status));
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        var client = CreateClient("Katherine Johnson", null, null, ClientStatus.New);
        var repository = new InMemoryClientRepository(client);
        var service = CreateService(repository);

        await service.DeleteAsync(client.Id);

        Assert.Empty(repository.Clients);
    }

    private static ClientService CreateService(InMemoryClientRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static Client CreateClient(
        string name,
        string? phone,
        string? email,
        ClientStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            FullName = name,
            Phone = phone,
            Email = email,
            Status = status,
            CreatedAt = Now,
            UpdatedAt = Now
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class InMemoryClientRepository(params Client[] clients) : IClientRepository
    {
        public List<Client> Clients { get; } = [.. clients];

        public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Clients.Count);

        public Task<IReadOnlyList<Client>> SearchAsync(
            string? searchText,
            ClientStatus? status,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Client> query = Clients;
            if (status is not null)
            {
                query = query.Where(client => client.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var term = searchText.Trim();
                query = query.Where(client =>
                    client.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (client.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (client.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return Task.FromResult<IReadOnlyList<Client>>(query.OrderBy(client => client.FullName).ToList());
        }

        public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Clients.SingleOrDefault(client => client.Id == id));

        public Task AddAsync(Client client, CancellationToken cancellationToken = default)
        {
            Clients.Add(client);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Client client, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var client = Clients.SingleOrDefault(item => item.Id == id)
                ?? throw new KeyNotFoundException();
            Clients.Remove(client);
            return Task.CompletedTask;
        }
    }
}
