namespace Portfolio.ClientManager.App.Services;

public interface ICsvFileService
{
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
}
