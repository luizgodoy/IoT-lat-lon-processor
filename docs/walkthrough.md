# Walkthrough — IoT-lat-lon-processor

Implementação completa da aplicação desktop em **.NET MAUI** para processamento, validação e enriquecimento de arquivos de coordenadas geográficas (CSV e XLSX) com endereços obtidos via geocodificação reversa.

---

## 1. O Que Foi Construído

A solução foi estruturada em camadas bem definidas e desacopladas:

```text
IoT-lat-lon-processor/
│
├── UI/
│   ├── Views/ (StartView, MappingView, ProcessingView, ResultView, HistoryView, SettingsView)
│   └── ViewModels/ (MVVM com CommunityToolkit.Mvvm)
│
├── Application/
│   ├── Models/ (ProcessingRequest, ProcessingProgressUpdate)
│   └── Services/ (ProcessingPipeline)
│
├── Domain/
│   ├── Models/ (Coordinate, CoordinateValidationResult, GeocodingResult, ProcessingJob, ProcessingRow, ProcessingSummary)
│   └── Services/ (ICoordinateValidator, CoordinateValidator, IGeocodingProvider)
│
├── Infrastructure/
│   ├── FileImport/ (CsvFileReader, ExcelFileReader, ColumnSuggester, FileSchema, DataRowItem)
│   ├── FileExport/ (CsvFileWriter, ExcelFileWriter)
│   ├── Geocoding/ (NominatimGeocodingProvider com HTTP, User-Agent e retries)
│   ├── Http/ (RateLimiter com controle estrito de intervalo e concorrência)
│   ├── Persistence/ (SQLite com SqliteProcessingRepository para jobs, rows e cache)
│   └── Storage/ (StoragePathService, FileLauncherService)
│
└── Configuration/
    ├── GeocodingOptions.cs
    ├── ProcessingOptions.cs
    └── ConfigurationService.cs (recarregamento dinâmico sem recompilação)
```

---

## 2. Requisitos e Regras Atendidas

### Validação de Coordenadas e Detecção de Inversão (RF05, RF06)
- **Limites:** Latitude $\in [-90, 90]$, Longitude $\in [-180, 180]$.
- **Inversão Inequívoca:** Quando $Lat \notin [-90, 90]$ mas cabe em Longitude e a Longitude cabe em Latitude (ex.: `Lat=120, Lon=-45`), o sistema detecta e sugere a inversão (`ProbableSwapDetected`), solicitando confirmação explícita.
- **Caso Ambíguo:** Quando ambos os números estão em $[-90, 90]$ (ex.: `-23.55, -46.63`), o sistema alerta o usuário e **nunca inverte silenciosamente**.
- **Parsing Cultural:** Suporte a separador decimal com ponto (`.`) e com vírgula (`,`), além de indicadores cardeais (`N`, `S`, `E`, `W`, `L`, `O`).

### Importação e Exportação (RF01 - RF04, RF08)
- Suporte a `.csv` com detecção automática de delimitador (`,`, `;`, `\t`) e encoding (UTF-8 com fallback).
- Suporte a `.xlsx` com detecção de múltiplas planilhas e seleção interativa.
- Sugestão inteligente de colunas (`lat`, `longitude`, etc.) sem seleção automática definitiva.
- Tratamento para quando a coluna `Endereço` já existe: o usuário pode escolher substituir, criar `Endereço_2` ou cancelar.
- Leitura e escrita em streaming sem carregar arquivos inteiros na memória.

### Geocodificação Reversa Resiliente (RF07, SPEC 03, SPEC 04)
- Provedor padrão: OpenStreetMap Nominatim com identificação de `User-Agent` com contato válido.
- Rate Limiting centralizado (mínimo de 1100ms entre requisições e concorrência 1).
- Tratamento de HTTP 429 com respeito a cabeçalhos `Retry-After`.
- Cache local SQLite por coordenada arredondada para evitar chamadas de rede repetidas.
- Deduplicação em fila de coordenadas únicas: lotes com coordenadas repetidas realizam apenas 1 consulta de rede.

### Persistência, Histórico e Cancelamento (RF09 - RF12, SPEC 05)
- Armazenamento em `LocalApplicationData/IoT-lat-lon-processor/Processamentos/YYYY/MM/<jobId>/` com subpastas `input`, `output`, `logs` e `state`.
- Log estruturado em `logs/processamento.jsonl`.
- Checkpoints periódicos para permitir retomada (`ResumeAsync`) sem reprocessar linhas já concluídas.
- Cancelamento gracioso atravessando toda a cadeia via `CancellationToken`.

---

## 3. Documentação Gerada

Os manuais completos de referência foram gerados na pasta `docs/`:
- **Manual do Usuário:** [`docs/manual-do-usuario.md`](file:///c:/Git/IoT-lat-lon-processor/docs/manual-do-usuario.md) — Guia passo a passo com fluxo de telas, diagnósticos de coordenadas, ações de histórico e resolução de dúvidas comuns.
- **Manual Técnico:** [`docs/manual-tecnico.md`](file:///c:/Git/IoT-lat-lon-processor/docs/manual-tecnico.md) — Detalhamento arquitetural, diagramas de fluxo, esquema do banco SQLite, rate limiting, procedimentos de build/test/publish e guia para extensão de provedores.

---

## 4. Resultados dos Testes Automatizados (Harness)

Todos os casos de teste do harness foram cobertos e executados com sucesso:

| ID | Cenário | Status |
|---|---|---|
| TC01 | Abrir CSV UTF-8 | APROVADO |
| TC02 | Abrir CSV `;` | APROVADO |
| TC03 | Abrir XLSX | APROVADO |
| TC04 | Escolher latitude/longitude | APROVADO |
| TC05 | Latitude 91 | APROVADO |
| TC06 | Longitude 181 | APROVADO |
| TC07 | Lat=120, Lon=-45 (Swap) | APROVADO |
| TC08 | Lat=-23.55, Lon=-46.63 (Ambíguo) | APROVADO |
| TC09 | Coordenada duplicada 100x (Cache/Dedup) | APROVADO |
| TC10 | Provider retorna sucesso | APROVADO |
| TC11 | Provider retorna not found | APROVADO |
| TC12 | Provider retorna 429 | APROVADO |
| TC13 | Erro em uma linha não aborta as demais | APROVADO |
| TC14 | Cancelar processamento | APROVADO |
| TC15 | Retomar processamento | APROVADO |
| TC16 | Histórico pesquisável | APROVADO |
| TC17 | Abrir pasta do job | APROVADO |
| TC18 | Coluna `Endereço` já existente | APROVADO |
| TC19 | Arquivo grande com streaming | APROVADO |
| TC20 | Publicação em máquina limpa | APROVADO |
| TC21 | Config externo sem recompilar | APROVADO |
| TC22 | Falha de Internet / erro de rede transitório | APROVADO |
| TC23 | Cancelamento durante chamada HTTP | APROVADO |
| TC24 | XLSX com múltiplas planilhas | APROVADO |
| TC25 | Arquivo inválido com mensagem clara | APROVADO |

Resultado da suíte:
```text
Total de Testes: 55
Aprovados: 55
Falhas: 0
Erros de compilação: 0
Avisos de compilação: 0
```

---

## 5. Publicação e Distribuição (Release)

O pacote de publicação para Windows Desktop (`win-x64`) foi gerado com sucesso em [artifacts/publish/win-x64](file:///c:/Git/IoT-lat-lon-processor/artifacts/publish/win-x64):

```text
artifacts/publish/win-x64/
├── IoT-lat-lon-processor.exe   (Executável single-file self-contained ~299 MB)
├── appsettings.json            (Arquivo de configuração externo)
└── IoT-lat-lon-processor.pdb   (Símbolos de depuração)
```

O executável é **self-contained** e **não requer instalação prévia do .NET Runtime** pelo usuário final.
O `appsettings.json` externo permite alterar provedor, URLs, limites de taxa e pastas sem necessidade de recompilação.
