using System.Runtime.CompilerServices;
using System.Text;

namespace OnleyiciBakim.Services;

public sealed record CsvImportRow(
    long RowNumber,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyList<string> Errors);

public interface ICsvImportService
{
    IAsyncEnumerable<CsvImportRow> ReadAsync(
        Stream stream,
        char delimiter = ',',
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);
}

public sealed class CsvImportService : ICsvImportService
{
    public async IAsyncEnumerable<CsvImportRow> ReadAsync(
        Stream stream,
        char delimiter = ',',
        Encoding? encoding = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(
            stream, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 8192, leaveOpen: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
            yield break;
        var headers = ParseLine(headerLine, delimiter).Select(x => x.Trim()).ToArray();
        var duplicateHeaders = headers.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1).Select(x => x.Key).ToArray();
        if (duplicateHeaders.Length > 0)
            throw new InvalidDataException(
                $"CSV başlıkları benzersiz olmalıdır: {string.Join(", ", duplicateHeaders)}");

        long rowNumber = 1;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;
            var values = ParseLine(line, delimiter);
            var errors = new List<string>();
            if (values.Count != headers.Length)
                errors.Add($"Beklenen {headers.Length}, bulunan {values.Count} kolon.");
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
                row[headers[index]] = index < values.Count && values[index].Length > 0
                    ? values[index]
                    : null;
            yield return new CsvImportRow(rowNumber, row, errors);
        }
    }

    private static IReadOnlyList<string> ParseLine(string line, char delimiter)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == delimiter && !quoted)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }
        values.Add(current.ToString());
        return values;
    }
}
