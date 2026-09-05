# SPEC 02 — Arquitetura

## 1. Estrutura recomendada

```text
IoT-lat-lon-processor/
  src/
    IoT-lat-lon-processor/
      App.xaml
      AppShell.xaml
      MauiProgram.cs
      Models/
      ViewModels/
      Views/
      Services/
        Import/
        Geocoding/
        Processing/
        History/
        Storage/
      Infrastructure/
        Persistence/
        Http/
        Logging/
      Configuration/
      Resources/
  tests/
    IoT-lat-lon-processor.Tests/
  docs/
```

## 2. Camadas

### UI
.NET MAUI + MVVM. Responsável por navegação, seleção de arquivos, progresso, mensagens e comandos.

### Application
Orquestra casos de uso:
- `ImportFile`
- `AnalyzeCoordinates`
- `ProcessFile`
- `CancelProcessing`
- `ResumeProcessing`
- `SearchHistory`
- `OpenProcessing`

### Domain
Entidades e regras puras:
- `ProcessingJob`
- `ProcessingRow`
- `Coordinate`
- `CoordinateValidationResult`
- `GeocodingResult`
- `ProcessingSummary`

### Infrastructure
Implementações:
- CSV/XLSX;
- SQLite;
- HTTP;
- Nominatim;
- filesystem;
- logs.

## 3. Interfaces essenciais

```csharp
public interface IFileReader
{
    Task<FileSchema> InspectAsync(string path, CancellationToken ct);
    IAsyncEnumerable<DataRow> ReadAsync(string path, CancellationToken ct);
}

public interface IFileWriter
{
    Task WriteAsync(string inputPath, string outputPath,
        string addressColumn, IReadOnlyDictionary<long,string> addresses,
        CancellationToken ct);
}

public interface IGeocodingProvider
{
    string Name { get; }
    Task<GeocodingResult> ReverseAsync(
        Coordinate coordinate, CancellationToken ct);
}

public interface IProcessingRepository
{
    Task SaveAsync(ProcessingJob job, CancellationToken ct);
    Task<IReadOnlyList<ProcessingJob>> SearchAsync(
        string? text, DateTime? from, DateTime? to, CancellationToken ct);
}

public interface ICoordinateValidator
{
    CoordinateValidationResult Validate(decimal latitude, decimal longitude);
    CoordinateValidationResult DetectPossibleSwap(
        decimal first, decimal second);
}
```

## 4. Injeção de dependência

Registrar todas as interfaces no `MauiProgram`. Evitar instâncias estáticas globais.

## 5. Persistência

SQLite local com tabelas:

`ProcessingJob`
- Id
- StartedAt
- FinishedAt
- InputFileName
- InputFilePath
- OutputFilePath
- LatitudeColumn
- LongitudeColumn
- Provider
- Status
- TotalRows
- ValidRows
- InvalidRows
- SwappedRows
- SuccessRows
- NotFoundRows
- ErrorRows
- CacheHits
- RequestCount
- DurationMs

`ProcessingRow`
- Id
- JobId
- SourceRowNumber
- Latitude
- Longitude
- NormalizedLatitude
- NormalizedLongitude
- Address
- Status
- ErrorMessage
- Attempts
- UpdatedAt

Índices: `ProcessingJob.StartedAt`, `ProcessingJob.InputFileName`, `ProcessingRow.JobId`, e uma chave de cache por coordenada normalizada.

## 6. Segurança e privacidade

Não enviar o arquivo inteiro ao provedor. Enviar apenas pares de coordenadas necessários ao reverse geocoding. Não persistir conteúdo de colunas não necessárias para o processamento.
