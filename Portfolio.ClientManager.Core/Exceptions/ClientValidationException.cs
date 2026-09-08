namespace Portfolio.ClientManager.Core.Exceptions;

public sealed class ClientValidationException : Exception
{
    public ClientValidationException(IReadOnlyDictionary<string, string> errors)
        : base("Client data is invalid.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string> Errors { get; }
}
