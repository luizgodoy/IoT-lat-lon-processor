using System.Diagnostics;

namespace IoT_lat_lon_processor.Infrastructure.Storage;

public interface IFileLauncherService
{
    void OpenFile(string filePath);
    void OpenFolder(string folderPath);
}

public sealed class FileLauncherService : IFileLauncherService
{
    public void OpenFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch { }
    }

    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath)) return;

        if (!Directory.Exists(folderPath))
        {
            var dir = Path.GetDirectoryName(folderPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                folderPath = dir;
            }
            else
            {
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
