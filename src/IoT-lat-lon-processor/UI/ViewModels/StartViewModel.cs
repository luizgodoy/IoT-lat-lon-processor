using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IoT_lat_lon_processor.Infrastructure.FileImport;

namespace IoT_lat_lon_processor.UI.ViewModels;

public partial class StartViewModel : ObservableObject
{
    private readonly IEnumerable<IFileReader> _fileReaders;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private string? _selectedFileName;

    [ObservableProperty]
    private string? _fileSizeFormatted;

    [ObservableProperty]
    private bool _isFileSelected;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public StartViewModel(IEnumerable<IFileReader> fileReaders)
    {
        _fileReaders = fileReaders;
    }

    [RelayCommand]
    public async Task PickFileAsync()
    {
        ErrorMessage = null;
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".csv", ".xlsx"] }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione o arquivo CSV ou XLSX com coordenadas",
                FileTypes = customFileType
            });

            if (result != null)
            {
                SelectedFilePath = result.FullPath;
                SelectedFileName = result.FileName;
                var fileInfo = new FileInfo(result.FullPath);
                FileSizeFormatted = FormatBytes(fileInfo.Length);
                IsFileSelected = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao selecionar arquivo: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task AnalyzeAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            ErrorMessage = "Selecione um arquivo válido para analisar.";
            return;
        }

        var reader = _fileReaders.FirstOrDefault(r => r.CanHandle(SelectedFilePath));
        if (reader == null)
        {
            ErrorMessage = "Formato de arquivo não suportado. Utilize arquivos .csv ou .xlsx.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var schema = await reader.InspectAsync(SelectedFilePath);

            if (schema.Columns.Count < 2)
            {
                ErrorMessage = "O arquivo deve conter pelo menos duas colunas para latitude e longitude.";
                return;
            }

            var navParam = new Dictionary<string, object>
            {
                { "FileSchema", schema }
            };

            await Shell.Current.GoToAsync("MappingView", navParam);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao inspecionar o arquivo: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task NavigateToHistoryAsync()
    {
        await Shell.Current.GoToAsync("//HistoryView");
    }

    [RelayCommand]
    public async Task NavigateToSettingsAsync()
    {
        await Shell.Current.GoToAsync("//SettingsView");
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {suffixes[order]}";
    }
}
