using Portfolio.ClientManager.Core.Csv;

namespace Portfolio.ClientManager.Core.Interfaces;

public interface IClientImportService
{
    Task<CsvImportResult> ImportAsync(
        IEnumerable<CsvImportRecord> records,
        CancellationToken cancellationToken = default);
}
