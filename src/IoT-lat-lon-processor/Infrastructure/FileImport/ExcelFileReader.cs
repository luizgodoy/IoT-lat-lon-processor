using System.Runtime.CompilerServices;
using ClosedXML.Excel;

namespace IoT_lat_lon_processor.Infrastructure.FileImport;

public sealed class ExcelFileReader : IFileReader
{
    public bool CanHandle(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<FileSchema> InspectAsync(string filePath, string? sheetName = null, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Arquivo XLSX não encontrado.", filePath);
        }

        ct.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook(filePath);
        var sheetNames = workbook.Worksheets.Select(w => w.Name).ToList();

        if (sheetNames.Count == 0)
        {
            return Task.FromResult(new FileSchema
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Extension = Path.GetExtension(filePath),
                Columns = [],
                SampleRows = [],
                ApproximateRowCount = 0,
                SheetNames = []
            });
        }

        var worksheet = !string.IsNullOrEmpty(sheetName) && workbook.Worksheets.Any(w => w.Name == sheetName)
            ? workbook.Worksheet(sheetName)
            : workbook.Worksheets.First();

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null)
        {
            return Task.FromResult(new FileSchema
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Extension = Path.GetExtension(filePath),
                Columns = [],
                SampleRows = [],
                ApproximateRowCount = 0,
                SheetNames = sheetNames,
                SelectedSheet = worksheet.Name
            });
        }

        var lastColumnNumber = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var headers = new List<string>();
        for (var col = 1; col <= lastColumnNumber; col++)
        {
            headers.Add(firstRow.Cell(col).GetString().Trim());
        }

        var sampleRows = new List<IReadOnlyList<string>>();
        var totalRows = (long)Math.Max(0, (worksheet.LastRowUsed()?.RowNumber() ?? 1) - firstRow.RowNumber());

        var rowCursor = firstRow.RowBelow();
        var samplesTaken = 0;

        while (rowCursor != null && !rowCursor.IsEmpty() && samplesTaken < 5)
        {
            ct.ThrowIfCancellationRequested();
            var rowVals = new List<string>();
            for (var col = 1; col <= lastColumnNumber; col++)
            {
                rowVals.Add(rowCursor.Cell(col).GetString().Trim());
            }
            sampleRows.Add(rowVals);
            samplesTaken++;
            rowCursor = rowCursor.RowBelow();
        }

        var schema = new FileSchema
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Extension = Path.GetExtension(filePath),
            Columns = headers,
            SampleRows = sampleRows,
            ApproximateRowCount = totalRows,
            SuggestedLatitudeColumn = ColumnSuggester.SuggestLatitude(headers),
            SuggestedLongitudeColumn = ColumnSuggester.SuggestLongitude(headers),
            SheetNames = sheetNames,
            SelectedSheet = worksheet.Name
        };

        return Task.FromResult(schema);
    }

    public async IAsyncEnumerable<DataRowItem> ReadRowsAsync(
        string filePath,
        string? sheetName = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Arquivo XLSX não encontrado.", filePath);
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = !string.IsNullOrEmpty(sheetName) && workbook.Worksheets.Any(w => w.Name == sheetName)
            ? workbook.Worksheet(sheetName)
            : workbook.Worksheets.First();

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null) yield break;

        var lastColumnNumber = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var rowCursor = firstRow.RowBelow();
        long rowNumber = 0;

        while (rowCursor != null && rowCursor.RowNumber() <= (worksheet.LastRowUsed()?.RowNumber() ?? 0))
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            var values = new string[lastColumnNumber];
            for (var col = 1; col <= lastColumnNumber; col++)
            {
                values[col - 1] = rowCursor.Cell(col).GetString().Trim();
            }

            yield return new DataRowItem
            {
                RowNumber = rowNumber,
                Values = values
            };

            rowCursor = rowCursor.RowBelow();
            await Task.Yield();
        }
    }
}
