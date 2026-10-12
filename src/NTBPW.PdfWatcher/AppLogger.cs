namespace NTBPW.PdfWatcher;

internal static class AppLogger
{
    // Ab dieser Groesse wird das Protokoll nach PDF-Watcher.log.1 verschoben (eine Generation wird behalten).
    internal const long MaxLogBytes = 5 * 1024 * 1024;
    private static readonly object FileLock = new();
    public static event Action<string>? Message;
    public static void Write(string text)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {text}";
        // Mehrere Ueberwachungsprofile schreiben parallel; ohne Sperre gehen Zeilen verloren.
        lock (FileLock)
        {
            try
            {
                Directory.CreateDirectory(AppConfig.DataDirectory);
                RotateIfNeeded();
                File.AppendAllText(AppConfig.LogPath, line + Environment.NewLine);
            }
            catch { }
        }
        Message?.Invoke(line);
    }

    private static void RotateIfNeeded()
    {
        var log = new FileInfo(AppConfig.LogPath);
        if (!log.Exists || log.Length < MaxLogBytes) return;
        File.Move(log.FullName, AppConfig.LogPath + ".1", overwrite: true);
    }
}
