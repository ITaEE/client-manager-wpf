using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Exceptions;

public sealed class ClientDuplicateException(IReadOnlyList<Client> potentialDuplicates)
    : Exception("A client with the same email address or phone number may already exist.")
{
    public IReadOnlyList<Client> PotentialDuplicates { get; } = potentialDuplicates;
}
