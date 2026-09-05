# SKILL — Testes

Criar testes para:

## Coordenadas
- latitude -90, 90;
- longitude -180, 180;
- limites excedidos;
- vazio;
- decimal com ponto;
- decimal com vírgula quando habilitado;
- latitude/longitude invertidas de forma inequívoca;
- caso ambíguo.

## Arquivos
- CSV vírgula;
- CSV ponto-e-vírgula;
- UTF-8;
- XLSX simples;
- XLSX com múltiplas planilhas;
- coluna inexistente;
- `Endereço` já existente.

## Geocoding
Mock do provider para:
- sucesso;
- not found;
- timeout;
- 429;
- erro 5xx;
- cancelamento;
- cache hit.

## Processamento
- arquivo inteiro processado;
- erro em uma linha não aborta as demais;
- cancelamento;
- checkpoint;
- retomada;
- resumo correto.

## UI
Testar ViewModels sem dependência direta de controles visuais.
