namespace Portfolio.ClientManager.Core.Csv;

public sealed record CsvImportError(int RowNumber, string Message);
