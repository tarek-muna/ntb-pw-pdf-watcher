using System.Text.Json;
using Microsoft.Win32;

namespace NTBPW.PdfWatcher;

internal static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static AppConfig Load()
    {
        try
        {
            Directory.CreateDirectory(AppConfig.DataDirectory);
            if (!File.Exists(AppConfig.ConfigPath)) return new AppConfig();
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(AppConfig.ConfigPath), Options) ?? new AppConfig();
        }
        catch { return new AppConfig(); }
    }

    public static void Save(AppConfig config)
    {
        Directory.CreateDirectory(AppConfig.DataDirectory);
        File.WriteAllText(AppConfig.ConfigPath, JsonSerializer.Serialize(config, Options));
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (key is null) return;
        if (config.AutoStart) key.SetValue("NTB-PW PDF-Watcher 3", $"\"{Application.ExecutablePath}\" --minimized");
        else key.DeleteValue("NTB-PW PDF-Watcher 3", false);
    }

    public static List<ProcessingRecord> LoadHistory()
    {
        try
        {
            if (!File.Exists(AppConfig.HistoryPath)) return [];
            return JsonSerializer.Deserialize<List<ProcessingRecord>>(File.ReadAllText(AppConfig.HistoryPath), Options) ?? [];
        }
        catch { return []; }
    }

    public static void SaveHistory(List<ProcessingRecord> history)
    {
        Directory.CreateDirectory(AppConfig.DataDirectory);
        File.WriteAllText(AppConfig.HistoryPath, JsonSerializer.Serialize(history.TakeLast(10000), Options));
    }
}
