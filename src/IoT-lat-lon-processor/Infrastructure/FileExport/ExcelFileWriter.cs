using ClosedXML.Excel;

namespace IoT_lat_lon_processor.Infrastructure.FileExport;

public sealed class ExcelFileWriter : IFileWriter
{
    public bool CanHandle(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase);
    }

    public Task WriteEnrichedFileAsync(
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

        ct.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook(inputPath);
        var worksheet = !string.IsNullOrEmpty(sheetName) && workbook.Worksheets.Any(w => w.Name == sheetName)
            ? workbook.Worksheet(sheetName)
            : workbook.Worksheets.First();

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null)
        {
            workbook.SaveAs(outputPath);
            return Task.CompletedTask;
        }

        var lastCol = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var addressColIndex = -1;

        for (var col = 1; col <= lastCol; col++)
        {
            var headerName = firstRow.Cell(col).GetString().Trim();
            if (string.Equals(headerName, addressColumn, StringComparison.OrdinalIgnoreCase))
            {
                addressColIndex = col;
                break;
            }
        }

        if (addressColIndex < 0)
        {
            addressColIndex = lastCol + 1;
            firstRow.Cell(addressColIndex).Value = addressColumn;
        }

        var rowCursor = firstRow.RowBelow();
        long rowNumber = 0;

        while (rowCursor != null && rowCursor.RowNumber() <= (worksheet.LastRowUsed()?.RowNumber() ?? 0))
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            if (rowAddresses.TryGetValue(rowNumber, out var addressValue))
            {
                rowCursor.Cell(addressColIndex).Value = addressValue ?? string.Empty;
            }

            rowCursor = rowCursor.RowBelow();
        }

        workbook.SaveAs(outputPath);
        return Task.CompletedTask;
    }
}
