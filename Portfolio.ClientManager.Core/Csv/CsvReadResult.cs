namespace Portfolio.ClientManager.Core.Csv;

public sealed record CsvReadResult(
    IReadOnlyList<CsvImportRecord> Records,
    IReadOnlyList<CsvImportError> Errors);
