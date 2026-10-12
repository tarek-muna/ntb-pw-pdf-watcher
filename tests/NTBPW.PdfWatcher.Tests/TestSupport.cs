using System.Runtime.CompilerServices;
using System.Text;

namespace NTBPW.PdfWatcher.Tests;

internal static class TestSetup
{
    public static readonly string DataDirectory =
        Path.Combine(Path.GetTempPath(), "ntbpw-tests-" + Guid.NewGuid().ToString("N"));

    // Protokoll und Konfiguration der Tests nicht in den echten Benutzerordner schreiben.
    [ModuleInitializer]
    internal static void Init()
    {
        Directory.CreateDirectory(DataDirectory);
        Environment.SetEnvironmentVariable("NTBPW_DATA_DIR", DataDirectory);
    }
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ntbpw-" + Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, true); } catch { }
    }
}

internal static class TestPdf
{
    public static readonly byte[] Bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%Testdatei\n%%EOF\n");

    public static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < until)
        {
            if (condition()) return;
            await Task.Delay(100);
        }
        Assert.True(condition(), $"Bedingung wurde nicht innerhalb von {timeout.TotalSeconds} s erfüllt.");
    }
}
