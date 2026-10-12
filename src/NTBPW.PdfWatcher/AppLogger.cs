namespace NTBPW.PdfWatcher;

internal static class AppLogger
{
    private static readonly object FileLock = new();
    public static event Action<string>? Message;
    public static void Write(string text)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {text}";
        // Mehrere Ueberwachungsprofile schreiben parallel; ohne Sperre gehen Zeilen verloren.
        lock (FileLock)
        {
            try { Directory.CreateDirectory(AppConfig.DataDirectory); File.AppendAllText(AppConfig.LogPath, line + Environment.NewLine); } catch { }
        }
        Message?.Invoke(line);
    }
}
