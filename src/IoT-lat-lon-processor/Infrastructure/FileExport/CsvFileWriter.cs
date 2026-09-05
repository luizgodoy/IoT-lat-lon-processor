using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using IoT_lat_lon_processor.Infrastructure.FileImport;

namespace IoT_lat_lon_processor.Infrastructure.FileExport;

public sealed class CsvFileWriter : IFileWriter
{
    public bool CanHandle(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase);
    }

    public async Task WriteEnrichedFileAsync(
        string inputPath,
        string outputPath,
        string addressColumn,
        IReadOnlyDictionary<long, string?> rowAddresses,
        string? sheetName = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Arquivo de entrada não encontrado.", inputPath);
        }

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var encoding = CsvFileReader.DetectEncoding(inputPath);
        var delimiter = CsvFileReader.DetectDelimiter(inputPath, encoding);

        var inConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            Encoding = encoding,
            BadDataFound = null,
            MissingFieldFound = null,
            HeaderValidated = null
        };

        var outConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter,
            Encoding = new UTF8Encoding(false), // Saída sempre em UTF-8 limpo
            ShouldQuote = _ => true // Protege campos com delimitadores ou quebras de linha
        };

        using var inStream = File.OpenRead(inputPath);
        using var inReader = new StreamReader(inStream, encoding);
        using var inCsv = new CsvReader(inReader, inConfig);

        using var outStream = File.Create(outputPath);
        using var outWriter = new StreamWriter(outStream, new UTF8Encoding(false));
        using var outCsv = new CsvWriter(outWriter, outConfig);

        if (!await inCsv.ReadAsync())
        {
            return;
        }

        inCsv.ReadHeader();
        var headers = inCsv.HeaderRecord?.Select(h => h.Trim()).ToList() ?? [];

        var addressColIndex = headers.FindIndex(h => string.Equals(h, addressColumn, StringComparison.OrdinalIgnoreCase));
        var appendNewColumn = addressColIndex < 0;

        // Escreve cabeçalho
        foreach (var header in headers)
        {
            outCsv.WriteField(header);
        }
        if (appendNewColumn)
        {
            outCsv.WriteField(addressColumn);
        }
        await outCsv.NextRecordAsync();

        long rowNumber = 0;
        while (await inCsv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            rowAddresses.TryGetValue(rowNumber, out var addressValue);
            var recordCount = inCsv.Parser.Record?.Length ?? 0;

            for (var col = 0; col < recordCount; col++)
            {
                if (col == addressColIndex)
                {
                    outCsv.WriteField(addressValue ?? string.Empty);
                }
                else
                {
                    outCsv.WriteField(inCsv.GetField(col) ?? string.Empty);
                }
            }

            if (appendNewColumn)
            {
                outCsv.WriteField(addressValue ?? string.Empty);
            }

            await outCsv.NextRecordAsync();
        }

        await outWriter.FlushAsync(ct);
    }
}
