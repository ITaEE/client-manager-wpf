using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Csv;

public sealed record CsvImportRecord(
    int RowNumber,
    ClientInput Client,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
