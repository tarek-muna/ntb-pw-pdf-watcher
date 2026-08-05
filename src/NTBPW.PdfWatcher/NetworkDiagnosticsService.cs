using System.Diagnostics;
using System.Text;

namespace NTBPW.PdfWatcher;

internal sealed record DiagnosticCommand(string Name, string FileName, string Arguments, bool RequiresTarget = false);
internal sealed record DiagnosticResult(bool Success, int ExitCode, string Output, string Error);

internal static class NetworkDiagnosticsService
{
    public static IReadOnlyList<DiagnosticCommand> Commands { get; } =
    [
        new("IP-Konfiguration", "ipconfig.exe", "/all"),
        new("ARP-Tabelle", "arp.exe", "-a"),
        new("Routingtabelle", "route.exe", "print"),
        new("Netzwerkverbindungen", "netstat.exe", "-ano"),
        new("DNS-Abfrage", "nslookup.exe", "{target}", true),
        new("Ping", "ping.exe", "-n 4 {target}", true),
        new("Traceroute", "tracert.exe", "-d {target}", true),
        new("PathPing", "pathping.exe", "-n {target}", true),
        new("Freigaben", "net.exe", "share"),
        new("Netzlaufwerke", "net.exe", "use")
    ];

    public static async Task<DiagnosticResult> RunAsync(
        DiagnosticCommand command,
        string? target,
        CancellationToken cancellationToken,
        Action<string>? onOutput = null)
    {
        if (command.RequiresTarget && string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("Bitte zuerst ein Ziel eingeben.");

        var safeTarget = (target ?? string.Empty).Trim();
        if (safeTarget.Contains('"'))
            throw new InvalidOperationException("Das Ziel enthält ungültige Zeichen.");

        var arguments = command.Arguments.Replace("{target}", Quote(safeTarget), StringComparison.Ordinal);
        var info = new ProcessStartInfo
        {
            FileName = command.FileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default
        };

        using var process = new Process { StartInfo = info, EnableRaisingEvents = true };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            output.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            error.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };

        if (!process.Start())
            throw new InvalidOperationException($"{command.FileName} konnte nicht gestartet werden.");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(true);
            }
            catch
            {
                // Der Prozess wurde möglicherweise bereits beendet.
            }
            throw;
        }

        return new DiagnosticResult(process.ExitCode == 0, process.ExitCode, output.ToString(), error.ToString());
    }

    public static async Task<DiagnosticResult> FlushDnsAsync(CancellationToken cancellationToken, Action<string>? onOutput = null) =>
        await RunAsync(new DiagnosticCommand("DNS-Cache leeren", "ipconfig.exe", "/flushdns"), null, cancellationToken, onOutput);

    private static string Quote(string value) => $"\"{value}\"";
}
