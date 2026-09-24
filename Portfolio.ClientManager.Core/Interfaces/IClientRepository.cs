using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Interfaces;

public interface IClientRepository
{
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> SearchAsync(
        string? searchText,
        ClientStatus? status,
        CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> FindPotentialDuplicatesAsync(
        Guid? excludedClientId,
        string? phone,
        string? email,
        CancellationToken cancellationToken = default);

    Task AddAsync(Client client, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IReadOnlyCollection<Client> clients, CancellationToken cancellationToken = default);

    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
