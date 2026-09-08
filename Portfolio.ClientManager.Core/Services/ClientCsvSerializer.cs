using System.Globalization;
using System.Text;
using Portfolio.ClientManager.Core.Csv;
using Portfolio.ClientManager.Core.Interfaces;
using Portfolio.ClientManager.Core.Models;

namespace Portfolio.ClientManager.Core.Services;

public sealed class ClientCsvSerializer : IClientCsvSerializer
{
    private static readonly string[] RequiredHeaders =
    [
        "FullName",
        "Phone",
        "Email",
        "Status",
        "Notes",
        "CreatedAt",
        "UpdatedAt"
    ];

    public string Serialize(IEnumerable<Client> clients)
    {
        var builder = new StringBuilder();
        WriteRow(builder, RequiredHeaders);

        foreach (var client in clients)
        {
            WriteRow(
                builder,
                client.FullName,
                client.Phone,
                client.Email,
                client.Status.ToString(),
                client.Notes,
                client.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                client.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public CsvReadResult Parse(string csv)
    {
        var errors = new List<CsvImportError>();
        var rows = ReadRows(csv, errors);
        if (rows.Count == 0)
        {
            errors.Add(new CsvImportError(1, "The CSV file does not contain a header row."));
            return new CsvReadResult([], errors);
        }

        var headerIndexes = BuildHeaderIndexes(rows[0], errors);
        if (headerIndexes is null)
        {
            return new CsvReadResult([], errors);
        }

        var records = new List<CsvImportRecord>();
        foreach (var row in rows.Skip(1))
        {
            if (row.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (row.Values.Count != rows[0].Values.Count)
            {
                errors.Add(new CsvImportError(row.RowNumber, "The row has a different number of columns than the header."));
                continue;
            }

            var statusText = GetValue(row.Values, headerIndexes, "Status");
            if (!Enum.TryParse<ClientStatus>(statusText, ignoreCase: true, out var status))
            {
                errors.Add(new CsvImportError(row.RowNumber, $"Unknown status '{statusText}'."));
                continue;
            }

            if (!TryParseDate(GetValue(row.Values, headerIndexes, "CreatedAt"), out var createdAt) ||
                !TryParseDate(GetValue(row.Values, headerIndexes, "UpdatedAt"), out var updatedAt))
            {
                errors.Add(new CsvImportError(row.RowNumber, "CreatedAt and UpdatedAt must be valid ISO 8601 timestamps."));
                continue;
            }

            records.Add(new CsvImportRecord(
                row.RowNumber,
                new ClientInput(
                    GetValue(row.Values, headerIndexes, "FullName"),
                    GetValue(row.Values, headerIndexes, "Phone"),
                    GetValue(row.Values, headerIndexes, "Email"),
                    status,
                    GetValue(row.Values, headerIndexes, "Notes")),
                createdAt,
                updatedAt));
        }

        return new CsvReadResult(records, errors);
    }

    private static List<CsvRow> ReadRows(string csv, ICollection<CsvImportError> errors)
    {
        var rows = new List<CsvRow>();
        var values = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var rowNumber = 1;

        for (var index = 0; index < csv.Length; index++)
        {
            var character = csv[index];
            if (inQuotes)
            {
                if (character == '"')
                {
                    if (index + 1 < csv.Length && csv[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(character);
                    if (character == '\n')
                    {
                        rowNumber++;
                    }
                }

                continue;
            }

            switch (character)
            {
                case '"' when field.Length == 0:
                    inQuotes = true;
                    break;
                case ',':
                    values.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (index + 1 < csv.Length && csv[index + 1] == '\n')
                    {
                        index++;
                    }

                    AddRow(rows, values, field, rowNumber);
                    rowNumber++;
                    break;
                case '\n':
                    AddRow(rows, values, field, rowNumber);
                    rowNumber++;
                    break;
                default:
                    field.Append(character);
                    break;
            }
        }

        if (inQuotes)
        {
            errors.Add(new CsvImportError(rowNumber, "A quoted field is not closed."));
            return rows;
        }

        if (field.Length > 0 || values.Count > 0)
        {
            AddRow(rows, values, field, rowNumber);
        }

        return rows;
    }

    private static Dictionary<string, int>? BuildHeaderIndexes(CsvRow header, ICollection<CsvImportError> errors)
    {
        var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < header.Values.Count; index++)
        {
            indexes.TryAdd(header.Values[index].Trim().TrimStart('\uFEFF'), index);
        }

        var missingHeaders = RequiredHeaders.Where(headerName => !indexes.ContainsKey(headerName)).ToArray();
        if (missingHeaders.Length > 0)
        {
            errors.Add(new CsvImportError(
                header.RowNumber,
                $"Missing required column(s): {string.Join(", ", missingHeaders)}."));
            return null;
        }

        return indexes;
    }

    private static string GetValue(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> indexes, string header) =>
        values[indexes[header]];

    private static bool TryParseDate(string value, out DateTimeOffset dateTime) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out dateTime);

    private static void AddRow(List<CsvRow> rows, List<string> values, StringBuilder field, int rowNumber)
    {
        values.Add(field.ToString());
        rows.Add(new CsvRow(rowNumber, [.. values]));
        values.Clear();
        field.Clear();
    }

    private static void WriteRow(StringBuilder builder, params string?[] values)
    {
        builder.AppendJoin(',', values.Select(Escape));
        builder.Append("\r\n");
    }

    private static string Escape(string? value)
    {
        var text = value ?? string.Empty;
        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }

    private sealed record CsvRow(int RowNumber, IReadOnlyList<string> Values);
}
