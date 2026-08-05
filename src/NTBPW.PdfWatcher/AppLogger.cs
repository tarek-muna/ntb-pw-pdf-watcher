namespace NTBPW.PdfWatcher;

internal static class AppLogger
{
    public static event Action<string>? Message;
    public static void Write(string text)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {text}";
        try { Directory.CreateDirectory(AppConfig.DataDirectory); File.AppendAllText(AppConfig.LogPath, line + Environment.NewLine); } catch { }
        Message?.Invoke(line);
    }
}
