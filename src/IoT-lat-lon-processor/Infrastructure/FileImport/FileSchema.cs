namespace IoT_lat_lon_processor.Infrastructure.FileImport;

/// <summary>
/// Representa o esquema inspecionado de um arquivo CSV ou XLSX, incluindo colunas, amostras e sugestões de mapeamento.
/// </summary>
public sealed class FileSchema
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;

    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> SampleRows { get; init; } = [];
    public long ApproximateRowCount { get; init; }

    public string? SuggestedLatitudeColumn { get; init; }
    public string? SuggestedLongitudeColumn { get; init; }

    public IReadOnlyList<string> SheetNames { get; init; } = [];
    public string? SelectedSheet { get; init; }
    public string? DetectedDelimiter { get; init; }
}

/// <summary>
/// Representa uma linha de dados lida de um arquivo.
/// </summary>
public sealed class DataRowItem
{
    public long RowNumber { get; init; }
    public IReadOnlyList<string> Values { get; init; } = [];

    public string? GetValue(int columnIndex)
    {
        if (columnIndex >= 0 && columnIndex < Values.Count)
        {
            return Values[columnIndex];
        }
        return null;
    }
}
