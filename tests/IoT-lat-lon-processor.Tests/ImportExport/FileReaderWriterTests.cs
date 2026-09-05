using System.Text;
using ClosedXML.Excel;
using IoT_lat_lon_processor.Infrastructure.FileExport;
using IoT_lat_lon_processor.Infrastructure.FileImport;
using Xunit;

namespace IoT_lat_lon_processor.Tests.ImportExport;

public class FileReaderWriterTests : IDisposable
{
    private readonly string _testDir;

    public FileReaderWriterTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "iot_processor_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignora falhas em exclusão de temporários
        }
    }

    [Fact]
    public async Task TC01_Inspect_Csv_Utf8_Returns_Headers_And_Samples()
    {
        var csvPath = Path.Combine(_testDir, "utf8_sample.csv");
        var content = "id,dispositivo,latitude,longitude\n1,Sensor-A,-23.550520,-46.633308\n2,Sensor-B,-22.906847,-43.172896\n";
        await File.WriteAllTextAsync(csvPath, content, Encoding.UTF8);

        var reader = new CsvFileReader();
        var schema = await reader.InspectAsync(csvPath);

        Assert.Equal(4, schema.Columns.Count);
        Assert.Equal("id", schema.Columns[0]);
        Assert.Equal("latitude", schema.Columns[2]);
        Assert.Equal("longitude", schema.Columns[3]);
        Assert.Equal(2, schema.ApproximateRowCount);
        Assert.Equal("latitude", schema.SuggestedLatitudeColumn);
        Assert.Equal("longitude", schema.SuggestedLongitudeColumn);
        Assert.Equal(",", schema.DetectedDelimiter);
    }

    [Fact]
    public async Task TC02_Inspect_Csv_Semicolon_Detects_Delimiter()
    {
        var csvPath = Path.Combine(_testDir, "semicolon_sample.csv");
        var content = "codigo;equipamento;lat;lon\n101;Tracker-1;-23,55;-46,63\n102;Tracker-2;-22,90;-43,17\n";
        await File.WriteAllTextAsync(csvPath, content, Encoding.Latin1);

        var reader = new CsvFileReader();
        var schema = await reader.InspectAsync(csvPath);

        Assert.Equal(";", schema.DetectedDelimiter);
        Assert.Equal(4, schema.Columns.Count);
        Assert.Equal("lat", schema.SuggestedLatitudeColumn);
        Assert.Equal("lon", schema.SuggestedLongitudeColumn);
    }

    [Fact]
    public async Task TC03_TC24_Xlsx_Multiple_Sheets_Inspects_And_Reads_Selected_Sheet()
    {
        var xlsxPath = Path.Combine(_testDir, "multi_sheet.xlsx");

        using (var wb = new XLWorkbook())
        {
            var ws1 = wb.Worksheets.Add("Dados Primarios");
            ws1.Cell(1, 1).Value = "device_id";
            ws1.Cell(1, 2).Value = "latitude";
            ws1.Cell(1, 3).Value = "longitude";
            ws1.Cell(2, 1).Value = "DEV-01";
            ws1.Cell(2, 2).Value = -23.55;
            ws1.Cell(2, 3).Value = -46.63;

            var ws2 = wb.Worksheets.Add("Secundaria");
            ws2.Cell(1, 1).Value = "tag";
            ws2.Cell(1, 2).Value = "lat_gps";
            ws2.Cell(1, 3).Value = "lon_gps";
            ws2.Cell(2, 1).Value = "TAG-99";
            ws2.Cell(2, 2).Value = -22.90;
            ws2.Cell(2, 3).Value = -43.17;

            wb.SaveAs(xlsxPath);
        }

        var reader = new ExcelFileReader();
        var schema1 = await reader.InspectAsync(xlsxPath);

        Assert.Equal(2, schema1.SheetNames.Count);
        Assert.Equal("Dados Primarios", schema1.SelectedSheet);
        Assert.Equal("latitude", schema1.SuggestedLatitudeColumn);
        Assert.Equal("longitude", schema1.SuggestedLongitudeColumn);

        // Inspeciona a segunda planilha (TC24)
        var schema2 = await reader.InspectAsync(xlsxPath, sheetName: "Secundaria");
        Assert.Equal("Secundaria", schema2.SelectedSheet);
        Assert.Equal("lat_gps", schema2.SuggestedLatitudeColumn);
        Assert.Equal("lon_gps", schema2.SuggestedLongitudeColumn);

        // Lê linhas da segunda planilha
        var rows = new List<DataRowItem>();
        await foreach (var row in reader.ReadRowsAsync(xlsxPath, sheetName: "Secundaria"))
        {
            rows.Add(row);
        }

        Assert.Single(rows);
        Assert.Equal("TAG-99", rows[0].Values[0]);
    }

    [Fact]
    public async Task TC18_Csv_Writer_Appends_Or_Updates_Address_Column()
    {
        var inCsv = Path.Combine(_testDir, "input_for_write.csv");
        var outCsv = Path.Combine(_testDir, "output_enriched.csv");

        var content = "id,latitude,longitude\n1,-23.55,-46.63\n2,-22.90,-43.17\n";
        await File.WriteAllTextAsync(inCsv, content, Encoding.UTF8);

        var writer = new CsvFileWriter();
        var addresses = new Dictionary<long, string?>
        {
            [1] = "Praça da Sé, São Paulo, SP",
            [2] = "Copacabana, Rio de Janeiro, RJ"
        };

        await writer.WriteEnrichedFileAsync(inCsv, outCsv, "Endereço", addresses);

        Assert.True(File.Exists(outCsv));
        var reader = new CsvFileReader();
        var outSchema = await reader.InspectAsync(outCsv);

        Assert.Contains("Endereço", outSchema.Columns);

        var rows = new List<DataRowItem>();
        await foreach (var r in reader.ReadRowsAsync(outCsv))
        {
            rows.Add(r);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal("Praça da Sé, São Paulo, SP", rows[0].Values[^1]);
        Assert.Equal("Copacabana, Rio de Janeiro, RJ", rows[1].Values[^1]);
    }

    [Fact]
    public async Task Xlsx_Writer_Appends_Address_Column_Correctly()
    {
        var inXlsx = Path.Combine(_testDir, "input_excel.xlsx");
        var outXlsx = Path.Combine(_testDir, "output_excel.xlsx");

        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("Planilha1");
            ws.Cell(1, 1).Value = "ID";
            ws.Cell(1, 2).Value = "Lat";
            ws.Cell(1, 3).Value = "Lon";
            ws.Cell(2, 1).Value = 10;
            ws.Cell(2, 2).Value = -23.55;
            ws.Cell(2, 3).Value = -46.63;
            wb.SaveAs(inXlsx);
        }

        var writer = new ExcelFileWriter();
        var addresses = new Dictionary<long, string?>
        {
            [1] = "Avenida Paulista, São Paulo"
        };

        await writer.WriteEnrichedFileAsync(inXlsx, outXlsx, "Endereço", addresses);

        Assert.True(File.Exists(outXlsx));
        var reader = new ExcelFileReader();
        var schema = await reader.InspectAsync(outXlsx);

        Assert.Contains("Endereço", schema.Columns);

        var rows = new List<DataRowItem>();
        await foreach (var r in reader.ReadRowsAsync(outXlsx))
        {
            rows.Add(r);
        }

        Assert.Single(rows);
        Assert.Equal("Avenida Paulista, São Paulo", rows[0].Values[^1]);
    }
}
