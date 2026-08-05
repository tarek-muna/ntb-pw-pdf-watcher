using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed class MultiWatcherService : IDisposable
{
    private AppConfig _config;
    private readonly List<CancellationTokenSource> _tokens = [];
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
    public event Action<string>? StatusChanged;
    public event Action<ProcessingRecord>? Processed;
    public bool IsRunning => _tokens.Count > 0;

    public MultiWatcherService(AppConfig config) => _config = config;
    public void UpdateConfig(AppConfig config) { var running = IsRunning; Stop(); _config = config; if (running) Start(); }

    public void Start()
    {
        if (IsRunning) return;
        foreach (var profile in _config.Profiles.Where(p => p.Enabled))
        {
            var cts = new CancellationTokenSource(); _tokens.Add(cts); _ = WatchLoop(profile, cts.Token);
        }
        StatusChanged?.Invoke(_tokens.Count > 0 ? "active" : "stopped");
    }

    public void Stop()
    {
        foreach (var t in _tokens) t.Cancel();
        foreach (var t in _tokens) t.Dispose();
        _tokens.Clear();
        StatusChanged?.Invoke("stopped");
    }

    private async Task WatchLoop(WatchProfile profile, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!Directory.Exists(profile.Folder)) { StatusChanged?.Invoke("disconnected"); await Task.Delay(1000, token); continue; }
                StatusChanged?.Invoke("active");
                foreach (var file in Directory.EnumerateFiles(profile.Folder, "*.pdf", SearchOption.TopDirectoryOnly).OrderBy(File.GetCreationTimeUtc))
                {
                    var key = profile.Id + "|" + file;
                    lock (_seen) if (!_seen.Add(key)) continue;
                    await ProcessFile(profile, file, token);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { AppLogger.Write($"Profil {profile.Name}: {ex.Message}"); }
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, profile.ScanIntervalSeconds)), token); } catch { break; }
        }
    }

    private async Task ProcessFile(WatchProfile profile, string file, CancellationToken token)
    {
        if (!await WaitUntilReady(file, profile.FileReadyTimeoutSeconds, token)) return;
        var services = new ProcessingServices(_config);
        var ocr = await services.RunOcrAsync(file);
        var classification = await services.ClassifyAsync(Path.GetFileName(file), ocr);
        var final = services.ApplyRules(file, classification, _config.Rules);
        services.CloudSync(final);
        if (profile.OpenPdf && File.Exists(final)) Process.Start(new ProcessStartInfo(final) { UseShellExecute = true });
        if (profile.EmailNotification) await services.SendEmailAsync("Neue PDF: " + Path.GetFileName(final), $"Profil: {profile.Name}\nDatei: {final}\nKlassifizierung: {classification}");
        var record = new ProcessingRecord { Profile = profile.Name, SourceFile = final, Classification = classification, OcrText = ocr, Result = "OK" };
        AppLogger.Write($"{profile.Name}: PDF verarbeitet: {final}");
        Processed?.Invoke(record);
    }

    private static async Task<bool> WaitUntilReady(string file, int timeoutSeconds, CancellationToken token)
    {
        var until = DateTime.UtcNow.AddSeconds(Math.Max(1, timeoutSeconds));
        long last = -1; var stable = 0;
        while (DateTime.UtcNow < until && !token.IsCancellationRequested)
        {
            try
            {
                var info = new FileInfo(file); var length = info.Length;
                using var s = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                stable = length == last && length > 0 ? stable + 1 : 0; last = length;
                if (stable >= 1) return true;
            }
            catch { }
            await Task.Delay(350, token);
        }
        return false;
    }

    public void Dispose() => Stop();
}
