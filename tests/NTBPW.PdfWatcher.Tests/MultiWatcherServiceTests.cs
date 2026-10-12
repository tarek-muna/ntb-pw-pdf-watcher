using System.Collections.Concurrent;

namespace NTBPW.PdfWatcher.Tests;

public sealed class MultiWatcherServiceTests
{
    private static AppConfig ConfigFor(string folder, int readyTimeoutSeconds = 5) => new()
    {
        Profiles =
        [
            new WatchProfile
            {
                Name = "Test",
                Folder = folder,
                ScanIntervalSeconds = 1,
                FileReadyTimeoutSeconds = readyTimeoutSeconds,
                OpenPdf = false,
                DesktopNotification = false,
                EmailNotification = false
            }
        ]
    };

    [Fact]
    public async Task Vorhandene_PDFs_werden_beim_Start_nicht_verarbeitet()
    {
        using var dir = new TempDirectory();
        var old = dir.File("alt.pdf");
        File.WriteAllBytes(old, TestPdf.Bytes);
        File.SetCreationTimeUtc(old, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddHours(-1));

        var processed = new ConcurrentQueue<string>();
        using var watcher = new MultiWatcherService(ConfigFor(dir.Path));
        watcher.Processed += r => processed.Enqueue(Path.GetFileName(r.SourceFile));
        watcher.Start();

        await Task.Delay(1500);
        File.WriteAllBytes(dir.File("neu.pdf"), TestPdf.Bytes);

        await TestPdf.WaitForAsync(() => processed.Contains("neu.pdf"), TimeSpan.FromSeconds(15));
        Assert.DoesNotContain("alt.pdf", processed);
        Assert.Single(processed);
    }

    [Fact]
    public async Task Gesperrte_PDF_wird_nach_Freigabe_doch_noch_verarbeitet()
    {
        using var dir = new TempDirectory();
        var processed = new ConcurrentQueue<string>();
        using var watcher = new MultiWatcherService(ConfigFor(dir.Path, readyTimeoutSeconds: 1));
        watcher.Processed += r => processed.Enqueue(Path.GetFileName(r.SourceFile));
        watcher.Start();

        // Simuliert einen Scanner, der die Datei laenger als das Timeout exklusiv geoeffnet haelt.
        var path = dir.File("scan.pdf");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.Write(TestPdf.Bytes);
            await Task.Delay(3500);
            Assert.Empty(processed);
        }

        await TestPdf.WaitForAsync(() => processed.Contains("scan.pdf"), TimeSpan.FromSeconds(15));
        Assert.Single(processed);
    }

    [Fact]
    public async Task Stop_beendet_die_Verarbeitung()
    {
        using var dir = new TempDirectory();
        var processed = new ConcurrentQueue<string>();
        using var watcher = new MultiWatcherService(ConfigFor(dir.Path));
        watcher.Processed += r => processed.Enqueue(Path.GetFileName(r.SourceFile));
        watcher.Start();
        Assert.True(watcher.IsRunning);

        watcher.Stop();
        Assert.False(watcher.IsRunning);

        File.WriteAllBytes(dir.File("nach-stop.pdf"), TestPdf.Bytes);
        await Task.Delay(3000);
        Assert.Empty(processed);
    }
}
