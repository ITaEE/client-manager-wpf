using Portfolio.ClientManager.Core.Exceptions;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;
using Portfolio.ClientManager.Core.Validation;

namespace Portfolio.ClientManager.Core.Services;

public sealed class ClientService(IClientRepository repository, TimeProvider timeProvider) : IClientService
{
    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default) =>
        repository.CountAsync(cancellationToken);

    public Task<IReadOnlyList<Client>> SearchAsync(
        string? searchText,
        ClientStatus? status,
        CancellationToken cancellationToken = default)
    {
        return repository.SearchAsync(searchText?.Trim(), status, cancellationToken);
    }

    public async Task<Client> CreateAsync(ClientInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        var normalizedInput = ClientInputNormalizer.Normalize(input);
        var now = timeProvider.GetUtcNow();
        var client = new Client
        {
            Id = Guid.NewGuid(),
            FullName = normalizedInput.FullName,
            Phone = normalizedInput.Phone,
            Email = normalizedInput.Email,
            Status = input.Status,
            Notes = normalizedInput.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(client, cancellationToken);
        return client;
    }

    public async Task<Client> UpdateAsync(
        Guid id,
        ClientInput input,
        CancellationToken cancellationToken = default)
    {
        Validate(input);
        var client = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("The client no longer exists.");

        var normalizedInput = ClientInputNormalizer.Normalize(input);
        client.FullName = normalizedInput.FullName;
        client.Phone = normalizedInput.Phone;
        client.Email = normalizedInput.Email;
        client.Status = input.Status;
        client.Notes = normalizedInput.Notes;
        client.UpdatedAt = timeProvider.GetUtcNow();

        await repository.UpdateAsync(client, cancellationToken);
        return client;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return repository.DeleteAsync(id, cancellationToken);
    }

    private static void Validate(ClientInput input)
    {
        var errors = ClientValidator.Validate(input);
        if (errors.Count > 0)
        {
            throw new ClientValidationException(errors);
        }
    }
}
