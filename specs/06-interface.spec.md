# SPEC 06 — Interface

## Tela 1 — Início

Componentes:
- botão `Selecionar arquivo`;
- arquivo selecionado;
- tipo/tamanho;
- botão `Analisar`;
- acesso ao `Histórico`;
- acesso a `Configurações`.

## Tela 2 — Mapeamento

Mostrar:
- nome do arquivo;
- quantidade aproximada de registros;
- lista de colunas;
- seletor `Latitude`;
- seletor `Longitude`;
- prévia das primeiras linhas;
- painel de diagnóstico.

Mensagens:
- `Coordenadas válidas`;
- `Foram encontradas coordenadas inválidas`;
- `Os campos parecem estar invertidos`;
- `Atenção: ambos os campos são matematicamente válidos; confirme a ordem`.

Botão `Processar`.

## Tela 3 — Processamento

Mostrar:
- barra de progresso;
- registros processados/total;
- encontrados;
- não encontrados;
- erros;
- cache hits;
- requisições;
- tempo decorrido;
- estimativa restante quando confiável.

Botão `Cancelar`.

## Tela 4 — Resultado

Resumo visual:
- sucesso;
- total;
- endereços encontrados;
- falhas;
- inválidos;
- invertidos;
- duração.

Ações:
- `Abrir arquivo`;
- `Abrir pasta`;
- `Ver log`;
- `Voltar ao início`.

## Tela 5 — Histórico

Pesquisar e filtrar processamentos anteriores.

## Tela 6 — Configurações

- provedor;
- URL;
- User-Agent;
- intervalo entre requisições;
- concorrência;
- timeout;
- retries;
- precisão do cache;
- diretório de processamento;
- manter arquivos de entrada;
- retenção de histórico.

Alterações que possam aumentar tráfego devem exibir aviso.

## UX

- textos em português;
- acessibilidade básica;
- estados vazios claros;
- confirmação para ações destrutivas;
- nenhuma operação de rede no thread de UI.
