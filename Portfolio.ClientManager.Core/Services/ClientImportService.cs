using Portfolio.ClientManager.Core.Csv;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Core.Validation;

namespace Portfolio.ClientManager.Core.Services;

public sealed class ClientImportService(IClientRepository repository, TimeProvider timeProvider) : IClientImportService
{
    public async Task<CsvImportResult> ImportAsync(
        IEnumerable<CsvImportRecord> records,
        CancellationToken cancellationToken = default)
    {
        var existingClients = await repository.SearchAsync(null, null, cancellationToken);
        var fingerprints = new HashSet<ClientFingerprint>(existingClients.Select(ClientFingerprint.From));
        var errors = new List<CsvImportError>();
        var clientsToAdd = new List<Client>();
        var duplicateCount = 0;

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var validationErrors = ClientValidator.Validate(record.Client);
            if (validationErrors.Count > 0)
            {
                errors.Add(new CsvImportError(record.RowNumber, string.Join(" ", validationErrors.Values)));
                continue;
            }

            var input = ClientInputNormalizer.Normalize(record.Client);
            var fingerprint = ClientFingerprint.From(input);
            if (!fingerprints.Add(fingerprint))
            {
                duplicateCount++;
                continue;
            }

            var now = timeProvider.GetUtcNow();
            var createdAt = record.CreatedAt == default ? now : record.CreatedAt;
            var updatedAt = record.UpdatedAt < createdAt ? createdAt : record.UpdatedAt;
            clientsToAdd.Add(new Client
            {
                Id = Guid.NewGuid(),
                FullName = input.FullName,
                Phone = input.Phone,
                Email = input.Email,
                Status = input.Status,
                Notes = input.Notes,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            });
        }

        await repository.AddRangeAsync(clientsToAdd, cancellationToken);
        return new CsvImportResult(clientsToAdd.Count, duplicateCount, errors);
    }

    private sealed record ClientFingerprint(
        string FullName,
        string Phone,
        string Email,
        ClientStatus Status,
        string Notes)
    {
        public static ClientFingerprint From(Client client) =>
            new(
                NormalizeKey(client.FullName),
                NormalizeKey(client.Phone),
                NormalizeKey(client.Email),
                client.Status,
                NormalizeKey(client.Notes));

        public static ClientFingerprint From(ClientInput input) =>
            new(
                NormalizeKey(input.FullName),
                NormalizeKey(input.Phone),
                NormalizeKey(input.Email),
                input.Status,
                NormalizeKey(input.Notes));

        private static string NormalizeKey(string? value) => value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
