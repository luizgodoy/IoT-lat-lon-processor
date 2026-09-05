using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IoT_lat_lon_processor.Application.Models;
using IoT_lat_lon_processor.Domain.Models;
using IoT_lat_lon_processor.Domain.Services;
using IoT_lat_lon_processor.Infrastructure.FileImport;

namespace IoT_lat_lon_processor.UI.ViewModels;

public partial class MappingViewModel : ObservableObject, IQueryAttributable
{
    private readonly ICoordinateValidator _validator;
    private readonly IEnumerable<IFileReader> _fileReaders;

    [ObservableProperty]
    private FileSchema? _schema;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private long _approximateRowCount;

    [ObservableProperty]
    private ObservableCollection<string> _columns = [];

    [ObservableProperty]
    private ObservableCollection<string> _sheetNames = [];

    [ObservableProperty]
    private string? _selectedSheet;

    [ObservableProperty]
    private bool _hasMultipleSheets;

    [ObservableProperty]
    private string? _selectedLatitudeColumn;

    [ObservableProperty]
    private string? _selectedLongitudeColumn;

    [ObservableProperty]
    private string _addressColumnName = "Endereço";

    [ObservableProperty]
    private bool _swapAxes;

    [ObservableProperty]
    private bool _showSwapOption;

    [ObservableProperty]
    private bool _showAmbiguousAlert;

    [ObservableProperty]
    private string _diagnosticMessage = string.Empty;

    [ObservableProperty]
    private string _diagnosticColor = "Gray";

    [ObservableProperty]
    private ObservableCollection<IReadOnlyList<string>> _sampleRows = [];

    [ObservableProperty]
    private string? _errorMessage;

    public MappingViewModel(ICoordinateValidator validator, IEnumerable<IFileReader> fileReaders)
    {
        _validator = validator;
        _fileReaders = fileReaders;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("FileSchema", out var schemaObj) && schemaObj is FileSchema s)
        {
            LoadSchema(s);
        }
    }

    public void LoadSchema(FileSchema s)
    {
        Schema = s;
        FileName = s.FileName;
        FilePath = s.FilePath;
        ApproximateRowCount = s.ApproximateRowCount;

        Columns.Clear();
        foreach (var col in s.Columns) Columns.Add(col);

        SheetNames.Clear();
        foreach (var sheet in s.SheetNames) SheetNames.Add(sheet);
        HasMultipleSheets = s.SheetNames.Count > 1;
        SelectedSheet = s.SelectedSheet;

        SampleRows.Clear();
        foreach (var r in s.SampleRows) SampleRows.Add(r);

        SelectedLatitudeColumn = s.SuggestedLatitudeColumn;
        SelectedLongitudeColumn = s.SuggestedLongitudeColumn;

        UpdateDiagnostics();
    }

    async partial void OnSelectedSheetChanged(string? value)
    {
        if (string.IsNullOrEmpty(value) || Schema == null || string.IsNullOrEmpty(FilePath)) return;

        var reader = _fileReaders.FirstOrDefault(r => r.CanHandle(FilePath));
        if (reader != null)
        {
            var newSchema = await reader.InspectAsync(FilePath, value);
            LoadSchema(newSchema);
        }
    }

    partial void OnSelectedLatitudeColumnChanged(string? value) => UpdateDiagnostics();
    partial void OnSelectedLongitudeColumnChanged(string? value) => UpdateDiagnostics();
    partial void OnSwapAxesChanged(bool value) => UpdateDiagnostics();

    private void UpdateDiagnostics()
    {
        ErrorMessage = null;
        ShowSwapOption = false;
        ShowAmbiguousAlert = false;

        if (string.IsNullOrEmpty(SelectedLatitudeColumn) || string.IsNullOrEmpty(SelectedLongitudeColumn))
        {
            DiagnosticMessage = "Selecione as colunas de Latitude e Longitude para análise prévia.";
            DiagnosticColor = "Gray";
            return;
        }

        if (string.Equals(SelectedLatitudeColumn, SelectedLongitudeColumn, StringComparison.OrdinalIgnoreCase))
        {
            DiagnosticMessage = "Atenção: A mesma coluna foi selecionada para Latitude e Longitude.";
            DiagnosticColor = "#E65100";
            return;
        }

        if (Schema == null || Schema.SampleRows.Count == 0)
        {
            DiagnosticMessage = "Colunas selecionadas. Pronto para processar.";
            DiagnosticColor = "Green";
            return;
        }

        var latColIdx = Schema.Columns.ToList().FindIndex(c => string.Equals(c, SelectedLatitudeColumn, StringComparison.OrdinalIgnoreCase));
        var lonColIdx = Schema.Columns.ToList().FindIndex(c => string.Equals(c, SelectedLongitudeColumn, StringComparison.OrdinalIgnoreCase));

        if (latColIdx < 0 || lonColIdx < 0) return;

        var sampleValidCount = 0;
        var sampleSwapCount = 0;
        var sampleAmbiguousCount = 0;
        var sampleInvalidCount = 0;

        foreach (var row in Schema.SampleRows)
        {
            var rawLat = latColIdx < row.Count ? row[latColIdx] : null;
            var rawLon = lonColIdx < row.Count ? row[lonColIdx] : null;

            if (SwapAxes)
            {
                (rawLat, rawLon) = (rawLon, rawLat);
            }

            var result = _validator.ParseAndValidate(rawLat, rawLon);

            if (result.Status == CoordinateStatus.ProbableSwapDetected)
            {
                sampleSwapCount++;
            }
            else if (result.Status == CoordinateStatus.Ambiguous)
            {
                sampleAmbiguousCount++;
            }
            else if (result.IsValid)
            {
                sampleValidCount++;
            }
            else
            {
                sampleInvalidCount++;
            }
        }

        if (sampleSwapCount > 0 && !SwapAxes)
        {
            ShowSwapOption = true;
            DiagnosticMessage = "Os valores parecem estar invertidos. Deseja utilizar Longitude como Latitude e Latitude como Longitude?";
            DiagnosticColor = "#C2185B";
        }
        else if (sampleAmbiguousCount > 0)
        {
            ShowAmbiguousAlert = true;
            DiagnosticMessage = "Atenção: ambos os campos são matematicamente válidos nos dois eixos; confirme a ordem antes de iniciar.";
            DiagnosticColor = "#F57C00";
        }
        else if (sampleInvalidCount > 0)
        {
            DiagnosticMessage = $"Foram encontradas coordenadas inválidas na amostra ({sampleInvalidCount} registro(s)). O processamento continuará para os registros válidos.";
            DiagnosticColor = "#C62828";
        }
        else
        {
            DiagnosticMessage = "Coordenadas válidas e analisadas com sucesso.";
            DiagnosticColor = "#2E7D32";
        }
    }

    [RelayCommand]
    public async Task StartProcessingAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrEmpty(SelectedLatitudeColumn) || string.IsNullOrEmpty(SelectedLongitudeColumn))
        {
            ErrorMessage = "Selecione as colunas de Latitude e Longitude.";
            return;
        }

        if (string.Equals(SelectedLatitudeColumn, SelectedLongitudeColumn, StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Latitude e Longitude não podem ser a mesma coluna.";
            return;
        }

        // RF08 / TC18: Verifica se a coluna de endereço já existe
        var targetColName = AddressColumnName;
        if (Schema != null && Schema.Columns.Any(c => string.Equals(c.Trim(), targetColName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            var action = await Microsoft.Maui.Controls.Application.Current?.Windows[0].Page?.DisplayActionSheetAsync(
                $"A coluna '{targetColName}' já existe no arquivo.",
                "Cancelar",
                null,
                "Substituir existente",
                $"Criar {targetColName}_2")!;

            if (action == "Cancelar" || string.IsNullOrEmpty(action))
            {
                return;
            }

            if (action.StartsWith("Criar"))
            {
                targetColName = $"{targetColName}_2";
                AddressColumnName = targetColName;
            }
        }

        var request = new ProcessingRequest
        {
            InputFilePath = FilePath,
            LatitudeColumn = SelectedLatitudeColumn,
            LongitudeColumn = SelectedLongitudeColumn,
            SwapAxes = SwapAxes,
            AddressColumnName = targetColName,
            SheetName = SelectedSheet
        };

        var navParam = new Dictionary<string, object>
        {
            { "ProcessingRequest", request }
        };

        await Shell.Current.GoToAsync("ProcessingView", navParam);
    }
}
