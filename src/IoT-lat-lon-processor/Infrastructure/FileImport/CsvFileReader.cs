using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace IoT_lat_lon_processor.Infrastructure.FileImport;

public sealed class CsvFileReader : IFileReader
{
    static CsvFileReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public bool CanHandle(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<FileSchema> InspectAsync(string filePath, string? sheetName = null, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Arquivo CSV não encontrado.", filePath);
        }

        var encoding = DetectEncoding(filePath);
        var delimiter = DetectDelimiter(filePath, encoding);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            Encoding = encoding,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream, encoding);
        using var csv = new CsvReader(reader, config);

        if (!await csv.ReadAsync())
        {
            return new FileSchema
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Extension = Path.GetExtension(filePath),
                Columns = [],
                SampleRows = [],
                ApproximateRowCount = 0,
                DetectedDelimiter = delimiter
            };
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord?.Select(h => h.Trim()).ToList() ?? [];

        var sampleRows = new List<IReadOnlyList<string>>();
        long rowCount = 0;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowCount++;

            if (sampleRows.Count < 5)
            {
                var rowValues = new List<string>();
                for (var i = 0; i < (csv.Parser.Record?.Length ?? 0); i++)
                {
                    rowValues.Add(csv.GetField(i) ?? string.Empty);
                }
                sampleRows.Add(rowValues);
            }
        }

        return new FileSchema
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Extension = Path.GetExtension(filePath),
            Columns = headers,
            SampleRows = sampleRows,
            ApproximateRowCount = rowCount,
            SuggestedLatitudeColumn = ColumnSuggester.SuggestLatitude(headers),
            SuggestedLongitudeColumn = ColumnSuggester.SuggestLongitude(headers),
            DetectedDelimiter = delimiter
        };
    }

    public async IAsyncEnumerable<DataRowItem> ReadRowsAsync(
        string filePath,
        string? sheetName = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Arquivo CSV não encontrado.", filePath);
        }

        var encoding = DetectEncoding(filePath);
        var delimiter = DetectDelimiter(filePath, encoding);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            Encoding = encoding,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream, encoding);
        using var csv = new CsvReader(reader, config);

        if (!await csv.ReadAsync())
        {
            yield break;
        }

        csv.ReadHeader();
        long rowNumber = 0;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            var count = csv.Parser.Record?.Length ?? 0;
            var values = new string[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = csv.GetField(i) ?? string.Empty;
            }

            yield return new DataRowItem
            {
                RowNumber = rowNumber,
                Values = values
            };
        }
    }

    public static string DetectDelimiter(string filePath, Encoding? encoding = null)
    {
        encoding ??= DetectEncoding(filePath);

        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream, encoding);

        var lines = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var line = reader.ReadLine();
            if (line == null) break;
            if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
        }

        if (lines.Count == 0) return ",";

        char[] candidates = [';', ',', '\t'];
        var candidateScores = new Dictionary<char, int>();

        foreach (var candidate in candidates)
        {
            var counts = lines.Select(l => l.Count(c => c == candidate)).ToList();
            var firstCount = counts[0];

            if (firstCount > 0 && counts.All(c => c == firstCount))
            {
                // Consistência perfeita entre linhas
                candidateScores[candidate] = firstCount * 10;
            }
            else
            {
                candidateScores[candidate] = counts.Sum();
            }
        }

        var best = candidateScores.OrderByDescending(kvp => kvp.Value).First();
        return best.Value > 0 ? best.Key.ToString() : ",";
    }

    public static Encoding DetectEncoding(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        var bom = new byte[4];
        var read = fs.Read(bom, 0, 4);

        if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        // Tenta ler como UTF-8 estrito
        fs.Position = 0;
        try
        {
            using var reader = new StreamReader(fs, new UTF8Encoding(false, true), true, 1024, leaveOpen: true);
            var buffer = new char[4096];
            while (reader.Read(buffer, 0, buffer.Length) > 0) { }
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            // Fallback para Windows-1252 / Latin1
            try
            {
                return Encoding.GetEncoding(1252);
            }
            catch
            {
                return Encoding.Latin1;
            }
        }
    }
}
