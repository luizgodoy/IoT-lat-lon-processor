namespace IoT_lat_lon_processor.Infrastructure.FileImport;

/// <summary>
/// Contrato para inspeção e leitura em streaming de arquivos tabulares (CSV e XLSX).
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Verifica se o leitor é compatível com a extensão do arquivo.
    /// </summary>
    bool CanHandle(string filePath);

    /// <summary>
    /// Inspeciona o cabeçalho, colunas e amostras sem carregar todo o arquivo na memória.
    /// </summary>
    Task<FileSchema> InspectAsync(string filePath, string? sheetName = null, CancellationToken ct = default);

    /// <summary>
    /// Lê as linhas do arquivo de forma assíncrona e em streaming.
    /// </summary>
    IAsyncEnumerable<DataRowItem> ReadRowsAsync(string filePath, string? sheetName = null, CancellationToken ct = default);
}
