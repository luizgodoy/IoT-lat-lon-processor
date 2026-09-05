namespace IoT_lat_lon_processor.Infrastructure.FileExport;

/// <summary>
/// Contrato para geração do arquivo de saída enriquecido com a coluna de Endereço.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Verifica se o gravador é compatível com a extensão do arquivo de saída.
    /// </summary>
    bool CanHandle(string filePath);

    /// <summary>
    /// Escreve o arquivo de saída gerando ou atualizando a coluna de endereço sem corromper as demais colunas.
    /// </summary>
    Task WriteEnrichedFileAsync(
        string inputPath,
        string outputPath,
        string addressColumn,
        IReadOnlyDictionary<long, string?> rowAddresses,
        string? sheetName = null,
        CancellationToken ct = default);
}
