# SPEC 07 — Build e release

## Plataforma alvo

Windows x64.

## Publicação

Gerar publicação self-contained, single-file, sem exigir instalação prévia do .NET Runtime.

Exemplo:

```powershell
dotnet publish .\src\IoT-lat-lon-processor\IoT-lat-lon-processor.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -o .\artifacts\publish\win-x64
```

Para MAUI, validar no ambiente real quais dependências nativas precisam permanecer externas. O requisito de distribuição é: **um único `.exe` da aplicação mais arquivos de configuração necessários**, sem runtime .NET separado.

## Configuração

Preferir `appsettings.json` externo ao executável para permitir ajuste de provedor, limites e pastas sem recompilação.

Exemplo:

```text
release/
  IoT-lat-lon-processor.exe
  appsettings.json
```

## Checklist

- Windows x64 limpo;
- executar sem SDK instalado;
- executar sem runtime .NET instalado;
- abrir CSV;
- abrir XLSX;
- processar arquivo pequeno;
- processar arquivo grande;
- cancelar;
- retomar;
- consultar histórico;
- validar que configuração externa é carregada;
- verificar logs;
- verificar que dados continuam locais exceto coordenadas enviadas ao provedor.
