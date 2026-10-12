using System.Text.Json.Serialization;

namespace NTBPW.PdfWatcher;

internal sealed class WatchProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Scanner 1";
    public string Folder { get; set; } = @"\\tsclient\C\Scan";
    public bool Enabled { get; set; } = true;
    public int ScanIntervalSeconds { get; set; } = 1;
    public int FileReadyTimeoutSeconds { get; set; } = 5;
    public bool OpenPdf { get; set; } = true;
    public bool DesktopNotification { get; set; } = true;
    public bool EmailNotification { get; set; }
}

internal sealed class FilingRule
{
    public string Name { get; set; } = "Neue Regel";
    public bool Enabled { get; set; } = true;
    public string FileNameContains { get; set; } = "";
    // Suchbegriffe im erkannten PDF-Text (Windows-OCR); leer = Inhalt wird nicht geprueft.
    public string ContentContains { get; set; } = "";
    public string ClassificationEquals { get; set; } = "";
    public string DestinationFolder { get; set; } = "";
    public bool CopyInsteadOfMove { get; set; }
}

internal sealed class ProcessingRecord
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Profile { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string Classification { get; set; } = "";
    public string OcrText { get; set; } = "";
    public string Result { get; set; } = "";
}

internal sealed class AppConfig
{
    public List<WatchProfile> Profiles { get; set; } = [new WatchProfile()];
    public List<FilingRule> Rules { get; set; } = [];
    public bool AutoStart { get; set; } = true;
    public bool StartMinimized { get; set; } = false;
    // Design, Sprache, OCR und KI sind seit 5.1 nicht mehr in der Oberflaeche, bis sie fertig umgesetzt sind.
    // Die Werte bleiben erhalten, damit bestehende Konfigurationen beim Speichern nicht verloren gehen.
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "de";
    public bool EnableOcr { get; set; }
    public string TesseractExe { get; set; } = "";
    public string TesseractLanguage { get; set; } = "deu+eng";
    public bool EnableAiClassification { get; set; }
    public string AiEndpoint { get; set; } = "";
    public bool EnableEmail { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string EmailFrom { get; set; } = "";
    public string EmailTo { get; set; } = "";
    public bool EnableCloudSync { get; set; }
    public string CloudSyncFolder { get; set; } = "";
    public bool CheckForUpdates { get; set; }
    public string GitHubRepository { get; set; } = "tarek-muna/ntb-pw-pdf-watcher";
    // Nur zur Abwaertskompatibilitaet mit aelteren Konfigurationen.
    public string UpdateManifestUrl { get; set; } = "";

    // Zugangsdaten werden nur verschluesselt (DPAPI, aktueller Windows-Benutzer) gespeichert.
    [JsonIgnore]
    public string SmtpPassword
    {
        get => SecretProtector.Unprotect(SmtpPasswordProtected);
        set => SmtpPasswordProtected = SecretProtector.Protect(value);
    }
    public string SmtpPasswordProtected { get; set; } = "";

    [JsonIgnore]
    public string AiApiKey
    {
        get => SecretProtector.Unprotect(AiApiKeyProtected);
        set => AiApiKeyProtected = SecretProtector.Protect(value);
    }
    public string AiApiKeyProtected { get; set; } = "";

    // Bis 5.0.x standen diese Werte im Klartext in der config.json. Sie werden beim Laden
    // verschluesselt uebernommen und nie wieder im Klartext geschrieben.
    [JsonPropertyName("SmtpPassword"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacySmtpPassword
    {
        get => null;
        set { if (!string.IsNullOrEmpty(value)) { SmtpPassword = value; HasLegacySecrets = true; } }
    }

    [JsonPropertyName("AiApiKey"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyAiApiKey
    {
        get => null;
        set { if (!string.IsNullOrEmpty(value)) { AiApiKey = value; HasLegacySecrets = true; } }
    }

    [JsonIgnore] public bool HasLegacySecrets { get; private set; }

    // NTBPW_DATA_DIR erlaubt einen abweichenden Datenordner (z. B. fuer automatische Tests).
    [JsonIgnore]
    public static string DataDirectory =>
        Environment.GetEnvironmentVariable("NTBPW_DATA_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTB-PW", "PDF-Watcher-3");
    [JsonIgnore] public static string ConfigPath => Path.Combine(DataDirectory, "config.json");
    [JsonIgnore] public static string HistoryPath => Path.Combine(DataDirectory, "history.json");
    [JsonIgnore] public static string LogPath => Path.Combine(DataDirectory, "PDF-Watcher.log");
}
