# Manual do Usuário — IoT Lat/Lon Processor

O **IoT Lat/Lon Processor** é um aplicativo desktop para Windows projetado para enriquecer arquivos tabulares (CSV e XLSX) que contenham coordenadas geográficas (Latitude e Longitude), convertendo-as automaticamente em endereços textuais legíveis por meio de geocodificação reversa.

---

## 1. Visão Geral

O aplicativo opera de forma predominantemente local:
* Seus arquivos, dados e históricos permanecem no seu computador.
* Apenas os pares numéricos de latitude e longitude são enviados ao provedor de geocodificação (OpenStreetMap Nominatim por padrão).
* Conta com cache inteligente e deduplicação automática: se o seu arquivo tiver 100.000 linhas mas apenas 500 coordenadas únicas, o sistema consultará a internet apenas para as 500 coordenadas diferentes, aproveitando o cache para todas as demais.

---

## 2. Requisitos e Instalação

* **Sistema Operacional:** Windows 10 (versão 1809 ou superior) ou Windows 11 (64 bits).
* **Dependências de Runtime:** Nenhuma. A versão de distribuição (`win-x64 self-contained`) já possui todos os componentes necessários embutidos em um único executável.
* **Conexão com a Internet:** Necessária apenas durante o processo de resolução de endereços.

### Como Executar
1. Navegue até a pasta de publicação (ex.: `artifacts/publish/win-x64` ou a pasta onde o executável foi instalado).
2. Certifique-se de que o arquivo `appsettings.json` esteja na mesma pasta que o executável `IoT-lat-lon-processor.exe`.
3. Dê um duplo clique em **`IoT-lat-lon-processor.exe`**.

---

## 3. Passo a Passo de Utilização

O fluxo de uso do aplicativo é dividido em quatro etapas principais:

```text
Início  ➡️  Mapeamento  ➡️  Processamento  ➡️  Resultado
```

---

### Etapa 1: Início (Seleção do Arquivo)

1. Na tela inicial, clique no botão **📁 Selecionar Arquivo CSV ou XLSX**.
2. Escolha o arquivo desejado em seu computador:
   * **Arquivos `.csv`:** O aplicativo detecta automaticamente o delimitador (vírgula, ponto e vírgula ou tabulação) e a codificação (UTF-8 ou codificações legadas como Windows-1252/Latin1).
   * **Arquivos `.xlsx`:** O aplicativo suporta pastas de trabalho do Excel, incluindo arquivos com múltiplas abas/planilhas.
3. As informações de tamanho e caminho do arquivo serão exibidas na tela.
4. Clique em **🔍 Inspecionar e Mapear Colunas** para prosseguir.

---

### Etapa 2: Mapeamento de Colunas e Diagnóstico

Nesta tela, você informa quais colunas do arquivo representam a **Latitude** e a **Longitude**.

1. **Seleção de Planilha (XLSX com múltiplas abas):**
   * Se o seu arquivo Excel tiver mais de uma aba, um seletor suspenso permitirá escolher qual planilha deseja processar.
2. **Seleção de Colunas:**
   * **Coluna de Latitude:** Selecione a coluna correspondente no menu suspenso.
   * **Coluna de Longitude:** Selecione a coluna correspondente no menu suspenso.
   * *Dica:* O sistema sugere automaticamente as colunas mais prováveis com base em nomes comuns (como `lat`, `latitude`, `lon`, `lng`, `longitude`), mas nunca seleciona sem a sua confirmação.
3. **Painel de Diagnóstico Inteligente:**
   O sistema analisa uma amostra dos dados e apresenta alertas visuais:
   * 🟢 **Coordenadas válidas:** Os valores estão nos intervalos aceitáveis (Latitude entre -90 e 90; Longitude entre -180 e 180).
   * 🔴 **Valores Invertidos Detectados:** Se o sistema identificar que os campos foram informados ao contrário (por exemplo, Latitude = 120 e Longitude = -45), uma caixa de aviso destacada aparecerá:
     > *"Os valores parecem estar invertidos. Deseja utilizar Longitude como Latitude e Latitude como Longitude?"*
     Basta marcar a opção de inversão para corrigir automaticamente durante o processamento.
   * 🟠 **Caso Ambíguo:** Se ambos os números forem válidos nos dois eixos (por exemplo, -23.55 e -46.63, comuns no Brasil onde ambos cabem em [-90, 90]), o sistema alertará para você conferir a ordem das colunas, **nunca invertendo silenciosamente**.
4. **Nome da Coluna de Endereço:**
   * Por padrão, a nova coluna se chamará **`Endereço`**.
   * Se o arquivo de entrada já contiver uma coluna com esse nome, o sistema perguntará se você deseja substituir a coluna existente ou criar uma nova coluna denominada **`Endereço_2`**.
5. Clique em **🚀 Iniciar Processamento Reverso**.

---

### Etapa 3: Processamento em Tempo Real

Durante o processamento, a interface exibe o andamento em tempo real sem travamentos:

* **Barra de Progresso:** Percentual concluído e quantidade de registros avaliados.
* **Métricas em Tempo Real:**
  * **Encontrados:** Endereços localizados com sucesso.
  * **Não Localizados:** Coordenadas válidas para as quais o mapa não possui endereço cadastrado (ex.: no oceano ou áreas remotas).
  * **Erros:** Falhas de rede ou coordenadas com formato corrompido.
  * **Cache / Deduplicação:** Quantidade de linhas que não precisaram gastar chamadas de rede por já estarem salvas localmente.
  * **Requisições HTTP:** Total de chamadas feitas ao servidor de mapas.
  * **Tempo Decorrido:** Cronômetro da execução.
* **Botão Cancelar:** A qualquer momento você pode clicar em **🛑 Cancelar Processamento**. O sistema interromperá o lote de forma segura, gravando um ponto de restauração (checkpoint) que permite retomar o processamento posteriormente sem perder o trabalho já feito.

---

### Etapa 4: Resumo e Arquivos de Saída

Ao concluir o processamento, a tela de resumo exibe:
* Estatísticas completas do lote (válidos, inválidos, encontrados, erros, tempo total).
* O caminho completo onde o arquivo processado foi salvo.

**Ações Disponíveis:**
* **📄 Abrir Arquivo:** Abre o arquivo CSV/XLSX gerado no seu programa padrão (como o Microsoft Excel).
* **📂 Abrir Pasta:** Abre a pasta de saída diretamente no Windows Explorer.
* **📋 Ver Log:** Abre o arquivo de log estruturado (`processamento.jsonl`) com as métricas técnicas da execução.
* **🔄 Iniciar Novo Processamento:** Retorna à tela inicial para processar outro arquivo.

---

## 4. Consulta ao Histórico

Acesse a aba **Histórico** na barra de navegação inferior/superior:
* **Pesquisa:** Digite o nome do arquivo original ou o código identificador do lote para filtrar.
* **Filtros por Status:** Visualize apenas os processamentos `Completed`, `Cancelled` ou `Failed`.
* **Ações Rápidas por Item:**
  * **Abrir Arquivo:** Abre o arquivo resultante.
  * **Abrir Pasta:** Abre a pasta de armazenamento daquele trabalho.
  * **Log:** Visualiza o arquivo de log daquele processamento.
  * **Retomar:** Se um processamento foi cancelado ou interrompido antes do fim, clique em Retomar para continuar exatamente das linhas pendentes.
  * **Excluir:** Remove o registro do histórico local mediante confirmação.

---

## 5. Configurações

Acesse a aba **Configurações** para personalizar o comportamento do aplicativo:

| Parâmetro | Padrão | Descrição |
|---|---|---|
| **Provedor** | `Nominatim` | Nome do serviço de mapas utilizado. |
| **URL Base da API** | `https://nominatim.openstreetmap.org` | Endereço do serviço de geocodificação. |
| **User-Agent** | Identificação da aplicação | Identificação obrigatória do cliente enviada nas requisições HTTP (deve conter um contato válido). |
| **Intervalo Mínimo (ms)** | `1100` | Tempo mínimo de espera entre duas requisições consecutivas (respeitando a política de no máximo 1 req/s do Nominatim). |
| **Concorrência Máxima** | `1` | Número de requisições paralelas simultâneas. |
| **Timeout (segundos)** | `30` | Tempo limite para aguardar resposta do servidor de mapas. |
| **Tentativas (Retries)** | `3` | Número de tentativas automáticas em caso de instabilidades transitórias de rede. |
| **Precisão do Cache** | `6` | Quantidade de casas decimais para chave de cache (6 casas decimais equivalem a uma precisão de ~10 centímetros). |
| **Checkpoint a Cada N Linhas** | `100` | Frequência de gravação de segurança do estado no banco de dados local. |
| **Manter Arquivos de Entrada** | `Ativado` | Guarda uma cópia de segurança do arquivo original na pasta do lote. |
| **Gerar Logs Estruturados** | `Ativado` | Salva o arquivo `processamento.jsonl` com os dados do processamento. |

> [!WARNING]
> **Aviso sobre a Política do OpenStreetMap Nominatim:**
> O serviço público gratuito do Nominatim exige que não seja feita mais de **1 requisição por segundo** e que um **User-Agent com contato válido** seja informado. Aumentar a concorrência ou reduzir o intervalo para menos de 1000ms fará com que o servidor bloqueie temporariamente o seu IP (código HTTP 429).

---

## 6. Solução de Dúvidas e Problemas Comuns

### 1. Meu arquivo tem coordenadas com vírgula (ex: `-23,550520`). O sistema aceita?
**Sim.** O sistema detecta e converte automaticamente números com vírgula ou ponto decimal.

### 2. O que acontece se uma linha contiver uma coordenada inválida ou corrompida?
O sistema não aborta o arquivo inteiro. A linha inválida é sinalizada no log e deixada com a coluna `Endereço` em branco, e o processamento continua normalmente para todos os demais registros válidos.

### 3. Onde ficam salvos os arquivos gerados e o banco de dados?
Por padrão, os dados e pastas de processamento ficam organizados em:
`C:\Users\<SeuUsuario>\AppData\Local\IoT-lat-lon-processor\Processamentos\<Ano>\<Mês>\<ID_do_Lote>\`
* Subpasta `input/`: Cópia do arquivo original.
* Subpasta `output/`: Arquivo processado enriquecido com a coluna `Endereço`.
* Subpasta `logs/`: Arquivo `processamento.jsonl`.
* Subpasta `state/`: Arquivo `checkpoint.json`.
