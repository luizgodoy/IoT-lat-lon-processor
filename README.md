# IoT-lat-lon-processor

Documentação de especificação, skills e harness para uma aplicação desktop em .NET MAUI destinada a enriquecer arquivos CSV/XLSX com endereços obtidos a partir de coordenadas de latitude/longitude.

## Objetivo

O usuário seleciona um arquivo `.csv` ou `.xlsx`, o sistema identifica suas colunas, permite selecionar as colunas de latitude e longitude e processa cada registro para acrescentar a coluna `Endereço`.

O aplicativo deve funcionar **offline quanto à interface e armazenamento local**, mas precisa de conexão com a Internet durante a geocodificação reversa.

## Documentos

- `specs/01-visao-geral.spec.md` — requisitos funcionais e não funcionais.
- `specs/02-arquitetura.spec.md` — arquitetura e componentes.
- `specs/03-geocoding.spec.md` — validação de coordenadas, inversão e geocodificação.
- `specs/04-performance.spec.md` — concorrência, cache, rate limiting e retomada.
- `specs/05-historico.spec.md` — armazenamento e pesquisa dos processamentos.
- `specs/06-interface.spec.md` — fluxos e telas.
- `specs/07-build-release.spec.md` — publicação Windows em arquivo único.
- `skills/01-implementacao.skill.md` — skill principal do agente de desenvolvimento.
- `skills/02-geocoding.skill.md` — skill para resolver coordenadas com segurança.
- `skills/03-testes.skill.md` — skill de testes.
- `harness/README.md` — harness de execução e critérios de aceite.
- `harness/test-cases.md` — casos de teste manuais/automatizáveis.

## Premissas

1. Plataforma alvo inicial: Windows desktop.
2. Framework: .NET MAUI.
3. Nome do projeto: `IoT-lat-lon-processor`.
4. Persistência local: SQLite.
5. Arquivos de entrada e saída são armazenados em diretórios locais configuráveis.
6. Provedor padrão de geocodificação: OpenStreetMap Nominatim, respeitando suas políticas de uso, especialmente limites de requisições e identificação do cliente.
7. O design deve permitir trocar o provedor por outro serviço gratuito/freemium sem alterar a UI.
8. Nunca presumir que duas colunas são latitude/longitude apenas por nome; o usuário confirma a seleção.
