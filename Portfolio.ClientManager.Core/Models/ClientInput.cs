namespace Portfolio.ClientManager.Core.Models;

public sealed record ClientInput(
    string FullName,
    string? Phone,
    string? Email,
    ClientStatus Status,
    string? Notes);
