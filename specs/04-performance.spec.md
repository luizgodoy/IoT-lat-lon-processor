# SPEC 04 — Performance

## Objetivo

Processar arquivos grandes sem consumir memória desnecessariamente e sem violar limites do serviço de geocodificação.

## Estratégia

1. Ler em streaming/chunks.
2. Não carregar o XLSX inteiro em memória quando a biblioteca permitir leitura incremental.
3. Criar uma fila de coordenadas únicas.
4. Consultar cache antes da rede.
5. Deduplicar coordenadas.
6. Processar requisições com concorrência configurável, iniciando em 1 para Nominatim.
7. Atualizar saída e checkpoint periodicamente.
8. Atualizar progresso em intervalos, não a cada registro, para evitar overhead de UI.

## Configuração

```json
{
  "Geocoding": {
    "Provider": "Nominatim",
    "BaseUrl": "https://nominatim.openstreetmap.org",
    "RequestIntervalMs": 1100,
    "MaxConcurrency": 1,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "CachePrecision": 6
  },
  "Processing": {
    "ProgressUpdateIntervalMs": 250,
    "CheckpointEveryRows": 100,
    "KeepInputFiles": true,
    "KeepLogs": true
  }
}
```

Os valores são exemplos e devem ser revisados contra as políticas atuais do provedor.

## Cancelamento

`CancellationToken` deve atravessar toda a cadeia:
UI → aplicação → leitura → geocoding → escrita.

Ao cancelar:
- salvar checkpoint;
- atualizar status `Cancelled`;
- manter arquivos temporários seguros;
- permitir retomada.

## Retomada

Um job interrompido deve conseguir identificar linhas já concluídas e não repetir requisições quando houver resultado persistido/cache.

## Critério

A UI nunca deve congelar durante processamento. Para arquivos grandes, memória deve crescer aproximadamente com o tamanho do chunk/cache configurado, não linearmente com o número total de linhas.
