using Portfolio.ClientManager.Core.Csv;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Core.Services;

namespace Portfolio.ClientManager.Tests;

public sealed class CsvProcessingTests
{
    private readonly ClientCsvSerializer _serializer = new();
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 7, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void SerializeAndParse_RoundTripsExportFields()
    {
        var client = CreateClient("Ada Lovelace", "+44 20 100", "ada@example.com", ClientStatus.Active, "First customer");

        var csv = _serializer.Serialize([client]);
        var result = _serializer.Parse(csv);

        var record = Assert.Single(result.Records);
        Assert.Empty(result.Errors);
        Assert.Equal(client.FullName, record.Client.FullName);
        Assert.Equal(client.Phone, record.Client.Phone);
        Assert.Equal(client.Email, record.Client.Email);
        Assert.Equal(client.Status, record.Client.Status);
        Assert.Equal(client.Notes, record.Client.Notes);
        Assert.Equal(client.CreatedAt, record.CreatedAt);
        Assert.Equal(client.UpdatedAt, record.UpdatedAt);
    }

    [Fact]
    public void Parse_HandlesQuotedCommaQuotesAndNewLines()
    {
        var client = CreateClient(
            "Ada, \"Countess\" Lovelace",
            null,
            "ada@example.com",
            ClientStatus.New,
            "First line\nSecond line, with a comma");

        var result = _serializer.Parse(_serializer.Serialize([client]));

        var record = Assert.Single(result.Records);
        Assert.Empty(result.Errors);
        Assert.Equal("Ada, \"Countess\" Lovelace", record.Client.FullName);
        Assert.Equal("First line\nSecond line, with a comma", record.Client.Notes);
    }

    [Fact]
    public void Parse_ReportsMalformedQuotedField()
    {
        const string csv = "FullName,Phone,Email,Status,Notes,CreatedAt,UpdatedAt\r\n\"Ada,123";

        var result = _serializer.Parse(csv);

        Assert.Empty(result.Records);
        Assert.Contains(result.Errors, error => error.Message.Contains("not closed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_AcceptsUtf8BomOnHeader()
    {
        var client = CreateClient("Ada Lovelace", null, "ada@example.com", ClientStatus.New, null);
        var csvWithBom = $"\uFEFF{_serializer.Serialize([client])}";

        var result = _serializer.Parse(csvWithBom);

        Assert.Empty(result.Errors);
        Assert.Single(result.Records);
    }

    [Fact]
    public async Task ImportAsync_SkipsInvalidClientAndContinues()
    {
        var repository = new InMemoryClientRepository();
        var service = new ClientImportService(repository, TimeProvider.System);
        var records = new[]
        {
            new CsvImportRecord(2, new ClientInput("   ", null, null, ClientStatus.New, null), Timestamp, Timestamp),
            new CsvImportRecord(3, new ClientInput("Grace Hopper", null, "grace@example.com", ClientStatus.Active, null), Timestamp, Timestamp)
        };

        var result = await service.ImportAsync(records);

        Assert.Equal(1, result.ImportedCount);
        Assert.Single(result.Errors);
        Assert.Single(repository.Clients);
        Assert.Equal("Grace Hopper", repository.Clients[0].FullName);
    }

    [Fact]
    public async Task ImportAsync_SkipsExistingAndRepeatedDuplicates()
    {
        var existing = CreateClient("Ada Lovelace", null, "ada@example.com", ClientStatus.Active, "First customer");
        var repository = new InMemoryClientRepository(existing);
        var service = new ClientImportService(repository, TimeProvider.System);
        var ada = new CsvImportRecord(
            2,
            new ClientInput(" ada lovelace ", null, "ADA@EXAMPLE.COM", ClientStatus.Active, "first customer"),
            Timestamp,
            Timestamp);
        var grace = new CsvImportRecord(
            3,
            new ClientInput("Grace Hopper", null, "grace@example.com", ClientStatus.New, null),
            Timestamp,
            Timestamp);

        var result = await service.ImportAsync([ada, grace, grace]);

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(2, result.DuplicateCount);
        Assert.Empty(result.Errors);
        Assert.Equal(2, repository.Clients.Count);
    }

    [Fact]
    public async Task ExportedCsv_CanBeImportedAndThenSkippedOnRepeat()
    {
        var source = CreateClient(
            "Ada, Lovelace",
            "+44 20 100",
            "ada@example.com",
            ClientStatus.Active,
            "Imported from an exported CSV");
        var parsed = _serializer.Parse(_serializer.Serialize([source]));
        var repository = new InMemoryClientRepository();
        var service = new ClientImportService(repository, TimeProvider.System);

        var firstImport = await service.ImportAsync(parsed.Records);
        var repeatedImport = await service.ImportAsync(parsed.Records);

        Assert.Empty(parsed.Errors);
        Assert.Equal(1, firstImport.ImportedCount);
        Assert.Equal(0, firstImport.DuplicateCount);
        Assert.Equal(0, repeatedImport.ImportedCount);
        Assert.Equal(1, repeatedImport.DuplicateCount);
        Assert.Single(repository.Clients);
    }

    private static Client CreateClient(string fullName, string? phone, string email, ClientStatus status, string? notes) =>
        new()
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Phone = phone,
            Email = email,
            Status = status,
            Notes = notes,
            CreatedAt = Timestamp,
            UpdatedAt = Timestamp
        };

    private sealed class InMemoryClientRepository(params Client[] clients) : IClientRepository
    {
        public List<Client> Clients { get; } = [.. clients];

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Clients.Count);

        public Task<IReadOnlyList<Client>> SearchAsync(
            string? searchText,
            ClientStatus? status,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Client>>(Clients.ToList());

        public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Clients.SingleOrDefault(client => client.Id == id));

        public Task AddAsync(Client client, CancellationToken cancellationToken = default)
        {
            Clients.Add(client);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Client client, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var client = Clients.Single(item => item.Id == id);
            Clients.Remove(client);
            return Task.CompletedTask;
        }
    }
}
