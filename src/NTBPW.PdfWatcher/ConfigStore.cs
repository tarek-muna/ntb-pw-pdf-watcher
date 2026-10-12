using System.Text.Json;
using Microsoft.Win32;

namespace NTBPW.PdfWatcher;

internal static class ConfigStore
{
    public const int MaxHistoryEntries = 10000;
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "NTB-PW PDF-Watcher 3";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static AppConfig Load()
    {
        try
        {
            Directory.CreateDirectory(AppConfig.DataDirectory);
            if (!File.Exists(AppConfig.ConfigPath)) return new AppConfig();
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(AppConfig.ConfigPath), Options) ?? new AppConfig();
            if (config.HasLegacySecrets)
            {
                // Klartext-Passwoerter aus 5.0.x sofort verschluesselt zurueckschreiben.
                try
                {
                    WriteConfig(config);
                    AppLogger.Write("Gespeicherte Zugangsdaten wurden verschlüsselt.");
                }
                catch (Exception ex) { AppLogger.Write("Zugangsdaten konnten nicht verschlüsselt werden: " + ex.Message); }
            }
            return config;
        }
        catch (Exception ex)
        {
            // Defekte Konfiguration sichern, bevor sie beim naechsten Speichern ueberschrieben wird.
            try
            {
                var backup = AppConfig.ConfigPath + $".defekt-{DateTime.Now:yyyyMMdd-HHmmss}";
                File.Copy(AppConfig.ConfigPath, backup, true);
                AppLogger.Write($"Konfiguration konnte nicht gelesen werden ({ex.Message}). Sicherung: {backup}");
            }
            catch { }
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        WriteConfig(config);
        ApplyAutoStart(config);
    }

    private static void WriteConfig(AppConfig config)
    {
        Directory.CreateDirectory(AppConfig.DataDirectory);
        File.WriteAllText(AppConfig.ConfigPath, JsonSerializer.Serialize(config, Options));
    }

    public static void ApplyAutoStart(AppConfig config)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key is null) return;
            if (config.AutoStart)
            {
                var arguments = config.StartMinimized ? " --minimized" : "";
                key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"{arguments}");
            }
            else
            {
                key.DeleteValue(RunValueName, false);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Write("Autostart konnte nicht gesetzt werden: " + ex.Message);
        }
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
        File.WriteAllText(AppConfig.HistoryPath, JsonSerializer.Serialize(history.TakeLast(MaxHistoryEntries), Options));
    }

    // Export fuer Sicherung oder Uebertragung auf einen anderen PC. Zugangsdaten werden nicht
    // exportiert: Sie sind per DPAPI an den Windows-Benutzer gebunden und anderswo unbrauchbar.
    public static void Export(AppConfig config, string path)
    {
        var copy = JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config, Options), Options) ?? new AppConfig();
        copy.SmtpPassword = "";
        copy.AiApiKey = "";
        File.WriteAllText(path, JsonSerializer.Serialize(copy, Options));
    }

    // Liest eine exportierte Konfiguration und uebernimmt die Zugangsdaten aus der aktuellen Konfiguration.
    public static AppConfig Import(string path, AppConfig current)
    {
        AppConfig? imported;
        try
        {
            imported = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Die Datei ist keine gültige Konfiguration: " + ex.Message, ex);
        }
        if (imported is null || imported.Profiles.Count == 0)
            throw new InvalidDataException("Die Datei enthält keine Scanner-Profile.");

        imported.Profiles.RemoveAll(p => p is null);
        imported.Rules.RemoveAll(r => r is null);
        if (string.IsNullOrEmpty(imported.SmtpPasswordProtected)) imported.SmtpPasswordProtected = current.SmtpPasswordProtected;
        if (string.IsNullOrEmpty(imported.AiApiKeyProtected)) imported.AiApiKeyProtected = current.AiApiKeyProtected;
        return imported;
    }
}
