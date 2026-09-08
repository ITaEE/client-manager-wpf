using System.IO;
using System.Text;

namespace Portfolio.ClientManager.App.Services;

public sealed class CsvFileService : ICsvFileService
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, Utf8WithoutBom, cancellationToken);

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) =>
        File.WriteAllTextAsync(path, contents, Utf8WithoutBom, cancellationToken);
}
