# SPEC 01 — Visão geral

## 1. Objetivo

Criar um aplicativo Windows em .NET MAUI que leia CSV/XLSX, identifique latitude e longitude, valide e eventualmente corrija coordenadas invertidas, execute geocodificação reversa e gere um novo arquivo com a coluna `Endereço`.

## 2. Fluxo principal

1. Abrir tela inicial.
2. Selecionar arquivo `.csv` ou `.xlsx`.
3. Validar extensão e acessibilidade.
4. Ler cabeçalho e identificar colunas.
5. Exibir amostra dos dados.
6. Usuário escolhe coluna de latitude e coluna de longitude.
7. Sistema analisa valores e sinaliza:
   - válidos;
   - inválidos;
   - vazios;
   - potencialmente invertidos.
8. Usuário confirma o processamento.
9. Sistema resolve os endereços.
10. Sistema salva arquivo de saída.
11. Sistema salva metadados, estatísticas e log.
12. UI apresenta resumo.
13. Usuário pode abrir a pasta, abrir o arquivo ou consultar o processamento no histórico.

## 3. Requisitos funcionais

### RF01 — Seleção
Aceitar `.csv` e `.xlsx`.

### RF02 — CSV
Detectar UTF-8 preferencialmente, com fallback configurável. Detectar delimitador entre `,`, `;`, tabulação e permitir configuração manual quando a detecção for ambígua.

### RF03 — XLSX
Ler a primeira planilha por padrão e permitir seleção de outra planilha quando houver múltiplas.

### RF04 — Identificação
Exibir todas as colunas detectadas e sugerir nomes prováveis como `lat`, `latitude`, `lon`, `lng`, `longitude`, sem seleção automática definitiva.

### RF05 — Validação
Latitude válida: `-90 <= lat <= 90`.
Longitude válida: `-180 <= lon <= 180`.
Aceitar decimal com ponto e, mediante configuração/parse seguro, decimal com vírgula.

### RF06 — Inversão
Se os valores selecionados forem incompatíveis com os respectivos intervalos, testar a inversão. Se apenas a inversão for válida, sinalizar e pedir confirmação.
Se ambos forem numericamente válidos, não inverter silenciosamente. Calcular uma heurística opcional e exigir confirmação explícita quando a confiança não for alta.

### RF07 — Geocodificação
Para cada coordenada válida, executar reverse geocoding e retornar endereço textual completo quando disponível.

### RF08 — Coluna
Criar exatamente uma coluna chamada `Endereço`. Se ela já existir, não sobrescrever silenciosamente: perguntar se deseja substituir, criar `Endereço_2` ou cancelar.

### RF09 — Erros por linha
Falhas individuais não devem abortar o arquivo inteiro. Registrar status por registro.

### RF10 — Resumo
Exibir:
- total de registros;
- coordenadas válidas;
- inválidas;
- corrigidas por inversão;
- endereços encontrados;
- não encontrados;
- erros de rede/provedor;
- duração;
- arquivo de saída;
- quantidade de requisições;
- cache hits;
- pausas por rate limit.

### RF11 — Histórico
Permitir pesquisar processamentos anteriores por nome do arquivo, data, status e texto.

### RF12 — Recuperação
Permitir abrir detalhes de um processamento e acessar seus arquivos de entrada, saída e log.

## 4. Requisitos não funcionais

- UI responsiva durante processamento.
- Cancelamento seguro.
- Retomada de processamento interrompido quando possível.
- Não bloquear a thread de UI.
- Logs estruturados.
- Dados permanecem locais, exceto coordenadas enviadas ao provedor de geocodificação.
- Configuração sem recompilar.
- Código testável sem depender de UI.
- Arquitetura preparada para novos provedores.

## 5. Fora de escopo

- Geocodificação sem Internet.
- Navegação/mapa interativo.
- Edição manual massiva dos endereços.
- Serviço próprio de geocodificação.
