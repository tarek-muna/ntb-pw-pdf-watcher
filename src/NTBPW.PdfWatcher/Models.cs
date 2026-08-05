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
    public string Theme { get; set; } = "System";
    public string Language { get; set; } = "de";
    public bool EnableOcr { get; set; }
    public string TesseractExe { get; set; } = "";
    public string TesseractLanguage { get; set; } = "deu+eng";
    public bool EnableAiClassification { get; set; }
    public string AiEndpoint { get; set; } = "";
    public string AiApiKey { get; set; } = "";
    public bool EnableEmail { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string EmailFrom { get; set; } = "";
    public string EmailTo { get; set; } = "";
    public bool EnableCloudSync { get; set; }
    public string CloudSyncFolder { get; set; } = "";
    public bool CheckForUpdates { get; set; }
    public string GitHubRepository { get; set; } = "tarek-muna/ntb-pw-pdf-watcher";
    // Nur zur Abwaertskompatibilitaet mit aelteren Konfigurationen.
    public string UpdateManifestUrl { get; set; } = "";

    [JsonIgnore] public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTB-PW", "PDF-Watcher-3");
    [JsonIgnore] public static string ConfigPath => Path.Combine(DataDirectory, "config.json");
    [JsonIgnore] public static string HistoryPath => Path.Combine(DataDirectory, "history.json");
    [JsonIgnore] public static string LogPath => Path.Combine(DataDirectory, "PDF-Watcher.log");
}
