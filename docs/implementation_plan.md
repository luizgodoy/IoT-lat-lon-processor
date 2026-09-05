# Plano de Implementação — IoT-lat-lon-processor

Aplicação desktop Windows em **.NET MAUI** para processamento, validação e enriquecimento de arquivos de coordenadas geográficas (CSV/XLSX) com endereços obtidos via geocodificação reversa (Nominatim/OpenStreetMap), com suporte a histórico, persistência local SQLite, resiliência, cancelamento e publicação self-contained single-file.

---

## 1. Diagnóstico do Ambiente e Bootstrap Inicial

### Inspeção do Ambiente
- **Sistema Operacional:** Windows 11 (build 10.0.26200, win-x64)
- **.NET SDKs Instalados:** 10.0.301 (padrão) e 8.0.418
- **Workloads Instalados:** `maui` (versão de manifesto 10.0.20/10.0.100 no SDK 10.0.300)
- **Código Prévio:** Nenhum código C# existia na raiz; apenas especificações (`specs/`), skills (`skills/`) e o harness de testes (`harness/`).

### Bootstrap Concluído (Fase 1)
- [x] Solution criada: `IoT-lat-lon-processor.sln`
- [x] Projeto MAUI criado: [IoT-lat-lon-processor.csproj](file:///c:/Git/IoT-lat-lon-processor/src/IoT-lat-lon-processor/IoT-lat-lon-processor.csproj)
  - Configurado para destino específico de Windows Desktop (`net10.0-windows10.0.19041.0`).
- [x] Projeto de Testes xUnit criado: [IoT-lat-lon-processor.Tests.csproj](file:///c:/Git/IoT-lat-lon-processor/tests/IoT-lat-lon-processor.Tests/IoT-lat-lon-processor.Tests.csproj)
  - Referência para o projeto principal configurada e ajustada para isolamento dos targets de resizetizer de UI.
- [x] Configuração base criada: [appsettings.json](file:///c:/Git/IoT-lat-lon-processor/src/IoT-lat-lon-processor/appsettings.json) com cópia para o diretório de saída (`PreserveNewest`).
- [x] Verificações de build e teste bem-sucedidas:
  - `dotnet restore`: OK
  - `dotnet build`: OK (0 Avisos, 0 Erros)
  - `dotnet test`: OK (1/1 aprovado)

---

## 2. Roteiro de Implementação por Fases

### Fase 2 — Domínio e Modelos Puros
Criar a camada de domínio livre de dependências de UI e de frameworks externos.

#### Arquivos a Criar:
- `src/IoT-lat-lon-processor/Domain/Models/Coordinate.cs`:
  - Representa uma coordenada com `Latitude` e `Longitude` (tipo `decimal`), normalização e método para round/chave de cache.
- `src/IoT-lat-lon-processor/Domain/Models/CoordinateValidationResult.cs`:
  - Enum de status: `Valid`, `InvalidLatitude`, `InvalidLongitude`, `BothInvalid`, `Empty`, `ProbableSwapDetected`, `Ambiguous`.
  - Mensagens amigáveis e indicação se a inversão é viável.
- `src/IoT-lat-lon-processor/Domain/Models/GeocodingResult.cs`:
  - `Success`, `DisplayAddress`, `Provider`, `ErrorCode`, `ErrorMessage`, `HttpStatus`, `RetryAfter`.
- `src/IoT-lat-lon-processor/Domain/Models/ProcessingJob.cs`:
  - Entidade do trabalho de processamento (Id, datas, caminhos de arquivo, colunas, status, contadores estatísticos).
- `src/IoT-lat-lon-processor/Domain/Models/ProcessingRow.cs`:
  - Entidade de cada linha analisada e resolvida.
- `src/IoT-lat-lon-processor/Domain/Models/ProcessingSummary.cs`:
  - Resumo estatístico final gerado pós-processamento.
- Testes unitários em `tests/IoT-lat-lon-processor.Tests/Domain/`:
  - Testes de imutabilidade, cálculos de intervalo e formatação de coordenadas.

---

### Fase 3 — Regras de Validação de Coordenadas
Implementar e testar rigorosamente o analisador de coordenadas conforme as specs 01 e 03.

#### Arquivos a Criar:
- `src/IoT-lat-lon-processor/Domain/Services/ICoordinateValidator.cs`
- `src/IoT-lat-lon-processor/Domain/Services/CoordinateValidator.cs`:
  - `Validate(decimal latitude, decimal longitude)`:
    - Latitude ∈ `[-90, 90]`
    - Longitude ∈ `[-180, 180]`
  - `DetectPossibleSwap(decimal first, decimal second)`:
    - Se `first ∉ [-90, 90]` e `first ∈ [-180, 180]`, e `second ∈ [-90, 90]`: sinalizar `ProbableSwapDetected` (ex.: Lat=120, Lon=-45).
    - Se ambos estiverem em `[-90, 90]` (ex.: -23.55 e -46.63): marcar como válido, mas registrar alerta de caso ambíguo para confirmação explícita do usuário (NUNCA inverter silenciosamente).
  - Normalização de strings e parsing cultural (ponto vs vírgula decimal, remoção de caracteres espúrios).
- Testes unitários em `tests/IoT-lat-lon-processor.Tests/Validation/CoordinateValidatorTests.cs`:
  - Casos TC05 (lat 91), TC06 (lon 181), TC07 (swap inequívoco 120, -45), TC08 (ambíguo -23.55, -46.63), valores vazios, formatos inválidos.

---

### Fase 4 — Importação e Exportação de Arquivos (CSV / XLSX)
Leitura em streaming e escrita enriquecida com a nova coluna `Endereço`.

#### Dependências:
- `CsvHelper` (leitura e escrita eficiente de CSV com detecção de delimitador e encoding UTF-8).
- `ClosedXML` ou `ExcelDataReader` (leitura em streaming/chunks de XLSX e seleção de planilha).

#### Arquivos a Criar:
- `src/IoT-lat-lon-processor/Infrastructure/FileImport/IFileReader.cs`:
  - `Task<FileSchema> InspectAsync(string path, CancellationToken ct)`
  - `IAsyncEnumerable<DataRowItem> ReadAsync(string path, FileReadOptions options, CancellationToken ct)`
- `src/IoT-lat-lon-processor/Infrastructure/FileImport/CsvFileReader.cs` (detecta delimitadores `,`, `;`, `\t` e encoding).
- `src/IoT-lat-lon-processor/Infrastructure/FileImport/ExcelFileReader.cs` (suporte a múltiplas planilhas).
- `src/IoT-lat-lon-processor/Infrastructure/FileExport/IFileWriter.cs`
- `src/IoT-lat-lon-processor/Infrastructure/FileExport/CsvFileWriter.cs` e `ExcelFileWriter.cs`:
  - Adiciona a coluna `Endereço` (ou `Endereço_2` se o usuário solicitar).
- Testes unitários para TC01, TC02, TC03, TC18, TC24, TC25.

---

### Fase 5 — Persistência Local (SQLite) e Estado
Armazenamento seguro em `LocalApplicationData` para jobs, linhas, checkpoints e cache de coordenadas.

#### Dependências:
- `sqlite-net-pcl` ou `Microsoft.Data.Sqlite`.

#### Arquivos a Criar:
- `src/IoT-lat-lon-processor/Infrastructure/Persistence/IProcessingRepository.cs`
- `src/IoT-lat-lon-processor/Infrastructure/Persistence/SqliteProcessingRepository.cs`
- `src/IoT-lat-lon-processor/Infrastructure/Persistence/ICoordinateCacheRepository.cs`:
  - Chave: `round(lat, precision) + ":" + round(lon, precision) + ":" + provider`
- `src/IoT-lat-lon-processor/Infrastructure/Storage/StoragePathService.cs`:
  - Estrutura: `LocalApplicationData/IoT-lat-lon-processor/Processamentos/YYYY/MM/<jobId>/` com subpastas `input`, `output`, `logs`, `state`.

---

### Fase 6 — Geocodificação Reversa (Nominatim) com Resiliência
Cliente HTTP configurável, respeitando as políticas do OpenStreetMap Nominatim.

#### Arquivos a Criar:
- `src/IoT-lat-lon-processor/Domain/Services/IGeocodingProvider.cs`
- `src/IoT-lat-lon-processor/Infrastructure/Geocoding/NominatimGeocodingProvider.cs`:
  - User-Agent obrigatório configurável.
  - Timeout e tratamento de status HTTP (429 Too Many Requests, Retry-After, erros 5xx transitórios).
- `src/IoT-lat-lon-processor/Infrastructure/Http/RateLimiter.cs`:
  - Intervalo mínimo entre chamadas (padrão 1100ms) e semáforo de concorrência (padrão 1 para Nominatim).
- Testes unitários com mock HTTP (TC09, TC10, TC11, TC12, TC22, TC23).

---

### Fase 7 — Pipeline de Processamento e Cancelamento
Orquestração em streaming do arquivo com suporte a CancellationToken, deduplicação e checkpoint incremental.

#### Arquivos a Criar:
- `src/IoT-lat-lon-processor/Application/Services/IProcessingPipeline.cs`
- `src/IoT-lat-lon-processor/Application/Services/ProcessingPipeline.cs`:
  - Orquestra: Inspeção → Validação → Deduplicação em fila de coordenadas únicas → Consulta de Cache → Chamada de Rede sob Rate Limiting → Persistência de Checkpoint → Geração do Arquivo de Saída → Resumo Final.
  - Cancelamento limpo com preservação de estado.
- Testes unitários para TC13, TC14, TC15.

---

### Fase 8 — Interface do Usuário (MVVM em .NET MAUI)
Construção das telas responsivas sem bloquear a thread principal.

#### Estrutura de Telas e ViewModels:
1. **Início (`StartView` / `StartViewModel`):**
   - Seleção de arquivo (`.csv` ou `.xlsx`), exibição do tipo/tamanho, botão "Analisar", atalhos para Histórico e Configurações.
2. **Mapeamento (`MappingView` / `MappingViewModel`):**
   - Lista de colunas, seletores de Latitude e Longitude (com sugestões automáticas sem seleção precipitada), amostra tabular dos primeiros registros, painel de diagnóstico de coordenadas (detecção de swap e alerta de caso ambíguo).
3. **Processamento (`ProcessingView` / `ProcessingViewModel`):**
   - Barra de progresso, contadores em tempo real (total, processados, encontrados, não encontrados, falhas, cache hits, requisições), botão Cancelar.
4. **Resultado (`ResultView` / `ResultViewModel`):**
   - Resumo completo das estatísticas, botões "Abrir Arquivo", "Abrir Pasta", "Ver Log", "Novo Processamento".
5. **Histórico (`HistoryView` / `HistoryViewModel`):**
   - Listagem, pesquisa textual, filtros por data e status, abertura de arquivos de jobs anteriores.
6. **Configurações (`SettingsView` / `SettingsViewModel`):**
   - Edição de URLs, User-Agent, intervalos de rate limit, retenção e diretórios.

---

### Fase 9 — Histórico e Detalhes
Implementar abertura de pastas, arquivos e retomada quando aplicável.

---

### Fase 10 — Execução do Harness Completo
Execução automatizada e guiada de todos os 25 casos de teste de `harness/test-cases.md`.

---

### Fase 11 — Publicação e Release
- Publicação `win-x64`, self-contained, single-file.
- Verificação do executável final ao lado de `appsettings.json`.

---

## 3. Plano de Verificação

### Testes Automatizados
- Executar a cada fase:
  ```powershell
  dotnet build -c Debug
  dotnet test -c Debug
  ```
- Cobertura direcionada para os casos do harness (`TC01` a `TC25`).

### Verificação Manual e Interface
- Execução do aplicativo no Windows desktop para validar renderização XAML, fluxo de navegação entre as telas e responsividade da interface gráfica.
