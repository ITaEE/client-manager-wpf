namespace Portfolio.ClientManager.Core.Csv;

public sealed record CsvImportResult(
    int ImportedCount,
    int DuplicateCount,
    IReadOnlyList<CsvImportError> Errors)
{
    public int SkippedCount => DuplicateCount + Errors.Count;
}
