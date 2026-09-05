# Harness de execução

O harness valida o produto por etapas, preferencialmente em máquina Windows.

## Pré-requisitos

- Windows 10/11 x64.
- Visual Studio 2022+ com workload .NET MAUI ou SDK equivalente.
- Acesso à Internet para os testes de geocoding.
- Uma pasta temporária de testes.

## Execução

```powershell
dotnet restore
dotnet build -c Debug
dotnet test -c Debug
dotnet publish .\src\IoT-lat-lon-processor\IoT-lat-lon-processor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o .\artifacts\publish\win-x64
```

Depois execute o `.exe` gerado em uma máquina Windows sem .NET Runtime instalado.

## Dados de teste

Criar:
- CSV com coordenadas válidas;
- CSV com lat/lon invertidos de forma inequívoca;
- CSV com coordenadas inválidas;
- CSV com coordenadas duplicadas;
- XLSX com as mesmas situações.

Não utilizar dados pessoais reais.

## Critérios de aceite

Todos os casos em `test-cases.md` devem passar.
