using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Validation;

public static class ClientInputNormalizer
{
    public static ClientInput Normalize(ClientInput input) =>
        new(
            input.FullName.Trim(),
            NormalizeOptional(input.Phone),
            NormalizeOptional(input.Email),
            input.Status,
            NormalizeOptional(input.Notes));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
