using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Interfaces;

public interface IClientService
{
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> SearchAsync(
        string? searchText,
        ClientStatus? status,
        CancellationToken cancellationToken = default);

    Task<Client> CreateAsync(
        ClientInput input,
        bool allowPotentialDuplicates = false,
        CancellationToken cancellationToken = default);

    Task<Client> UpdateAsync(
        Guid id,
        ClientInput input,
        bool allowPotentialDuplicates = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
