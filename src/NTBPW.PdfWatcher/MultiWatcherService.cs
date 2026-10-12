using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed class MultiWatcherService : IDisposable
{
    // Toleranz fuer leicht abweichende Uhren (z. B. \\tsclient-Pfade vom RDP-Client).
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromSeconds(5);
    private const int MaxStoredTextLength = 4000;

    private AppConfig _config;
    private readonly List<CancellationTokenSource> _tokens = [];
    private readonly Dictionary<string, string> _profileStates = [];
    private string _lastReportedState = "";
    public event Action<string>? StatusChanged;
    // Status eines einzelnen Profils ("active" / "disconnected"); nur bei Aenderung.
    public event Action<string, string>? ProfileStatusChanged;
    public event Action<ProcessingRecord>? Processed;
    public bool IsRunning => _tokens.Count > 0;

    public MultiWatcherService(AppConfig config) => _config = config;
    public void UpdateConfig(AppConfig config) { var running = IsRunning; Stop(); _config = config; if (running) Start(); }

    public void Start()
    {
        if (IsRunning) return;
        var config = _config;
        foreach (var profile in config.Profiles.Where(p => p.Enabled))
        {
            var cts = new CancellationTokenSource();
            _tokens.Add(cts);
            var token = cts.Token;
            // Eigener Thread-Pool-Task, damit langsame Netzwerkpfade die Oberflaeche nicht blockieren.
            _ = Task.Run(() => WatchLoop(config, profile, token), token);
        }
        if (_tokens.Count == 0) ReportState("stopped", force: true);
    }

    public void Stop()
    {
        foreach (var t in _tokens) t.Cancel();
        _tokens.Clear();
        lock (_profileStates) _profileStates.Clear();
        ReportState("stopped", force: true);
    }

    private async Task WatchLoop(AppConfig config, WatchProfile profile, CancellationToken token)
    {
        // Nur Dateien verarbeiten, die nach dem Start der Ueberwachung entstanden sind.
        // So werden beim Programmstart nicht alle bereits vorhandenen PDFs erneut geoeffnet.
        var startedUtc = DateTime.UtcNow - ClockTolerance;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedExisting = 0;

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!Directory.Exists(profile.Folder))
                {
                    SetProfileState(profile, "disconnected", token);
                }
                else
                {
                    SetProfileState(profile, "active", token);
                    var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var files = Directory.EnumerateFiles(profile.Folder, "*.pdf", SearchOption.TopDirectoryOnly)
                        .Select(f => new FileInfo(f))
                        .Where(f => f.Exists)
                        .OrderBy(f => f.CreationTimeUtc)
                        .ToList();

                    foreach (var info in files)
                    {
                        token.ThrowIfCancellationRequested();
                        // Der Erstellungszeitpunkt gehoert zum Schluessel, damit ein neuer Scan
                        // mit gleichem Dateinamen wieder verarbeitet wird.
                        var key = info.FullName + "|" + info.CreationTimeUtc.Ticks;
                        present.Add(key);
                        if (seen.Contains(key)) continue;

                        var stamp = info.CreationTimeUtc > info.LastWriteTimeUtc ? info.CreationTimeUtc : info.LastWriteTimeUtc;
                        if (stamp < startedUtc)
                        {
                            seen.Add(key);
                            skippedExisting++;
                            continue;
                        }

                        // Erst als erledigt markieren, wenn die Datei fertig geschrieben ist,
                        // sonst wird sie im naechsten Durchlauf erneut geprueft.
                        if (!await WaitUntilReady(info.FullName, profile.FileReadyTimeoutSeconds, token)) continue;
                        seen.Add(key);
                        await ProcessFile(config, profile, info.FullName, token);
                    }

                    if (skippedExisting > 0)
                    {
                        AppLogger.Write($"{profile.Name}: {skippedExisting} bereits vorhandene PDF(s) beim Start übersprungen.");
                        skippedExisting = 0;
                    }

                    // Verschwundene Dateien vergessen, damit die Liste nicht unbegrenzt waechst.
                    seen.IntersectWith(present);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { AppLogger.Write($"Profil {profile.Name}: {ex.Message}"); }
            try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, profile.ScanIntervalSeconds)), token); } catch { break; }
        }
    }

    private async Task ProcessFile(AppConfig config, WatchProfile profile, string file, CancellationToken token)
    {
        var services = new ProcessingServices(config);
        var ocr = await services.RunOcrAsync(file, token);
        var classification = await services.ClassifyAsync(Path.GetFileName(file), ocr);
        token.ThrowIfCancellationRequested();
        var final = services.ApplyRules(file, classification, config.Rules, ocr);
        services.CloudSync(final);
        var result = "OK";
        if (profile.OpenPdf && File.Exists(final))
        {
            try { Process.Start(new ProcessStartInfo(final) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                result = "Öffnen fehlgeschlagen";
                AppLogger.Write($"{profile.Name}: PDF konnte nicht geöffnet werden: {ex.Message}");
            }
        }
        if (profile.EmailNotification) await services.SendEmailAsync("Neue PDF: " + Path.GetFileName(final), $"Profil: {profile.Name}\nDatei: {final}\nKlassifizierung: {classification}");
        // Erkannten Text gekuerzt in der Historie ablegen (fuer die Suche), damit history.json klein bleibt.
        var storedText = ocr.Length > MaxStoredTextLength ? ocr[..MaxStoredTextLength] : ocr;
        var record = new ProcessingRecord { Profile = profile.Name, SourceFile = final, Classification = classification, OcrText = storedText, Result = result };
        AppLogger.Write($"{profile.Name}: PDF verarbeitet: {final}");
        Processed?.Invoke(record);
    }

    private void SetProfileState(WatchProfile profile, string state, CancellationToken token)
    {
        string aggregate;
        bool changed;
        lock (_profileStates)
        {
            if (token.IsCancellationRequested) return;
            changed = !_profileStates.TryGetValue(profile.Id, out var previous) || previous != state;
            _profileStates[profile.Id] = state;
            aggregate = _profileStates.Values.Any(s => s == "disconnected") ? "disconnected" : "active";
        }
        if (changed) ProfileStatusChanged?.Invoke(profile.Id, state);
        ReportState(aggregate);
    }

    private void ReportState(string state, bool force = false)
    {
        lock (_profileStates)
        {
            if (!force && state == _lastReportedState) return;
            _lastReportedState = state;
        }
        StatusChanged?.Invoke(state);
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
