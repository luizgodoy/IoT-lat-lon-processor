# SPEC 05 — Histórico e armazenamento

## Estrutura de pastas

Usar uma pasta raiz configurável, por padrão dentro de `LocalApplicationData`:

```text
IoT-lat-lon-processor/
  Processamentos/
    2026/
      09/
        20260904-082100_ABC123/
          input/
          output/
          logs/
          state/
```

Nunca depender do diretório do executável para dados mutáveis.

## Arquivos

- `input/original.ext` — opcional conforme configuração.
- `output/<nome>_processado.ext`
- `logs/processamento.jsonl`
- `state/checkpoint.json`

## Histórico

Tela com:
- pesquisa textual;
- período;
- status;
- arquivo de origem;
- provedor;
- total de registros.

Colunas:
- data;
- arquivo;
- registros;
- sucesso;
- erros;
- duração;
- status.

Ações:
- abrir detalhes;
- abrir pasta;
- abrir arquivo de saída;
- repetir processamento;
- excluir histórico/arquivos, com confirmação.

## Integridade

Gerar identificador único por job e evitar sobrescrita acidental. Nome de saída deve ser determinístico e seguro.
