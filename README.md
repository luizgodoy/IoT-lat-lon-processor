# 📍 IoT Lat/Lon Processor

> **Solução desktop de alta performance para enriquecimento geográfico em lote.**  
> Converta milhões de coordenadas brutas (Latitude/Longitude) de dispositivos IoT, frotas e telemetria em endereços completos e legíveis em arquivos CSV e Excel.

---

## 🎯 O Problema que Esta Solução Resolve

Dispositivos de IoT, rastreadores veiculares, sensores agrícolas e registros de telemetria geram continuamente grandes volumes de coordenadas geográficas numéricas. No entanto, para análise operacional, auditoria, logística e relatórios gerenciais, **números como `-23.550520, -46.633308` precisam se tornar endereços reais** (ex.: *Praça da Sé, Centro, São Paulo - SP, Brasil*).

Soluções convencionais ou scripts improvisados frequentemente falham devido a:
* 🚫 **Bloqueio de IP por excesso de requisições:** Servidores públicos de mapas possuem políticas rígidas de taxa de acesso (*rate limit*).
* 🔄 **Coordenadas Invertidas:** Arquivos de campo frequentemente trocam Latitude por Longitude, jogando pontos no oceano ou fora do país.
* 🇧🇷 **Inconsistências de Formato Decimal:** Mistura de vírgulas e pontos decimais (`-23,55` vs `-23.55`) e indicadores cardeais (`S`, `W`, `O`).
* 💥 **Estouro de Memória (RAM):** Carregar arquivos gigantescos trava as aplicações.
* 🛑 **Perda de Trabalho em Falhas de Rede:** Se a internet oscilar ou o computador reiniciar, o processamento geralmente precisa recomeçar do zero.

---

## 💡 A Solução: IoT Lat/Lon Processor

O **IoT Lat/Lon Processor** foi desenvolvido para solucionar esses gargalos em ambiente desktop Windows com uma arquitetura **Local-First**, segura e resiliente:

```text
[Arquivo CSV ou XLSX] 
       ⬇️
[Diagnóstico Inteligente] ➡️ Valida limites, formatações e alerta sobre inversões
       ⬇️
[Pipeline em Streaming]   ➡️ Leitura linha por linha sem consumir toda a memória RAM
       ⬇️
[Cache & Deduplicação]   ➡️ Coordenadas repetidas não consom chamadas de rede
       ⬇️
[Rate Limiter Rígido]    ➡️ Respeita a regra de ≤ 1 req/s com retries automáticos
       ⬇️
[Arquivo Enriquecido]    ➡️ Salva com a nova coluna "Endereço" e mantém histórico local
```

---

## ✨ Principais Funcionalidades

### 🔍 Diagnóstico e Validação Prévia de Coordenadas
* **Detecção Automática de Inversão:** Identifica quando colunas de Latitude e Longitude foram trocadas (ex.: Latitude = 120 e Longitude = -45) e oferece correção em um clique.
* **Alerta de Ambiguidade:** Em regiões onde ambos os valores são válidos nos dois eixos (como no Brasil, onde ambos cabem em $[-90, 90]$), o sistema alerta o usuário e **nunca inverte silenciosamente**.
* **Parser Cultural Universal:** Trata automaticamente vírgulas decimais, pontos, espaços e notações cardeais (`N`, `S`, `E`, `W`, `L`, `O`).

### ⚡ Eficiência e Resiliência Operacional
* **Deduplicação & Cache Hierárquico:** Se um arquivo tiver 100.000 linhas mas apenas 2.000 coordenadas distintas, o sistema resolve apenas as únicas e preenche as demais instantaneamente via cache.
* **Rate Limiting em Conformidade:** Opera estritamente dentro da política de uso do OpenStreetMap Nominatim ($\le 1$ requisição/segundo, concorrência = 1 e User-Agent rastreável).
* **Tratamento Inteligente de HTTP 429:** Respeita o cabeçalho `Retry-After` e aplica *exponential backoff* em instabilidades transitórias de rede.
* **Checkpoints & Retomada Sem Perdas:** Gravação periódica de pontos de restauração no SQLite local. Se cancelado ou interrompido, pode ser retomado exatamente da linha onde parou.

### 📊 Experiência e Gestão de Dados
* **Suporte Completo a CSV e Excel:** Detecta delimitadores de CSV (`,`, `;`, `\t`) e suporta pastas de trabalho `.xlsx` com múltiplas planilhas.
* **Tratamento de Conflito de Colunas:** Se a coluna `Endereço` já existir, permite substituir ou criar `Endereço_2`.
* **Histórico Centralizado:** Pesquise trabalhos anteriores por nome ou ID, abra o arquivo resultante ou a pasta no Windows Explorer com um clique.
* **Logs Estruturados (`processamento.jsonl`):** Registro detalhado de auditoria de cada lote pronto para ingestão em ferramentas de observabilidade.
* **Privacidade Absoluta:** Todos os arquivos de entrada e saída permanecem exclusivamente no seu computador.

---

## 🚀 Como Executar

A aplicação é distribuída como um executável único e independente (*self-contained*), **sem necessidade de instalar o .NET ou qualquer outro pré-requisito**:

1. Acesse a pasta de publicação (ex.: `artifacts/publish/win-x64/` ou o diretório da versão instalada).
2. Certifique-se de que o arquivo de configuração `appsettings.json` esteja na mesma pasta do executável.
3. Dê um duplo clique em **`IoT-lat-lon-processor.exe`**.

---

## 🖥️ Como Usar (Fluxo em 4 Passos)

1. **📁 Selecionar Arquivo:** Escolha o arquivo `.csv` ou `.xlsx` no seu computador.
2. **🗺️ Mapear Colunas:** Confirme quais colunas contêm a Latitude e a Longitude. O sistema sugere as colunas automaticamente e alerta se houver suspeita de coordenadas invertidas.
3. **⏳ Acompanhar o Processamento:** Visualize o progresso em tempo real (registros concluídos, acertos em cache, erros e tempo estimado). Pause ou cancele quando quiser com garantia de checkpoint.
4. **✅ Acessar o Resultado:** Abra o arquivo enriquecido com a nova coluna `Endereço` ou visualize a pasta de armazenamento diretamente no Windows Explorer.

---

## ⚙️ Configuração Personalizada (`appsettings.json`)

Você pode ajustar parâmetros operacionais diretamente no arquivo `appsettings.json` localizado junto ao executável, sem precisar recompilar a aplicação:

```json
{
  "Geocoding": {
    "Provider": "Nominatim",
    "BaseUrl": "https://nominatim.openstreetmap.org",
    "UserAgent": "IoT-lat-lon-processor/1.0 (contact: seu-email@exemplo.com)",
    "RateLimitMs": 1100,
    "MaxConcurrency": 1,
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "CoordinatePrecisionDigits": 6
  },
  "Pipeline": {
    "BatchSize": 100,
    "EnableMemoryDeduplication": true,
    "EnablePersistentCache": true
  }
}
```

---

## 📚 Documentação Complementar

Para aprofundamento operacional e arquitetural, consulte os manuais detalhados na pasta [`docs/`](docs/):

* 📖 **[Manual do Usuário](docs/manual-do-usuario.md):** Guia detalhado de uso tela a tela, dicas práticas e solução de dúvidas comuns.
* 🛠️ **[Manual Técnico](docs/manual-tecnico.md):** Diagramas de arquitetura, esquema do banco SQLite, ciclo de vida do streaming, suíte de testes e instruções para extensão de provedores.

---

## 💻 Desenvolvimento e Testes

Para desenvolvedores que desejam compilar o projeto a partir do código-fonte:

```powershell
# Compilar em modo Debug
dotnet build IoT-lat-lon-processor.slnx

# Executar a suíte de 55 testes automatizados
dotnet test tests/IoT-lat-lon-processor.Tests/IoT-lat-lon-processor.Tests.csproj

# Publicar executável único para Windows x64
dotnet publish src/IoT-lat-lon-processor/IoT-lat-lon-processor.csproj `
  -c Release `
  -r win-x64 `
  -f net10.0-windows10.0.19041.0 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o artifacts/publish/win-x64
```
