using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NTBPW.PdfWatcher;

internal sealed record ToolCommand(string Name, string FileName, string Arguments, bool RequiresAdmin = false);

internal static class SystemToolsService
{
    public static IReadOnlyList<ToolCommand> RepairCommands { get; } = new[]
    {
        new ToolCommand("Systemdateien prüfen (SFC)", "sfc.exe", "/scannow", true),
        new ToolCommand("Windows-Abbild prüfen (DISM CheckHealth)", "dism.exe", "/Online /Cleanup-Image /CheckHealth", true),
        new ToolCommand("Windows-Abbild reparieren (DISM RestoreHealth)", "dism.exe", "/Online /Cleanup-Image /RestoreHealth", true),
        new ToolCommand("Datenträger prüfen (CHKDSK)", "chkdsk.exe", "C: /scan", true),
        new ToolCommand("Druckwarteschlange neu starten", "powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"Restart-Service Spooler -Force\"", true),
        new ToolCommand("Explorer neu starten", "cmd.exe", "/c taskkill /f /im explorer.exe & start explorer.exe"),
        new ToolCommand("Temporäre Dateien öffnen", "explorer.exe", "%TEMP%")
    };

    public static async Task<CommandResult> RunAsync(ToolCommand command, CancellationToken token, Action<string> output)
    {
        var psi = new ProcessStartInfo(command.FileName, command.Arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) output(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) output(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(token);
        return new CommandResult(process.ExitCode == 0, process.ExitCode, string.Empty, string.Empty);
    }

    public static string BuildSystemReport()
    {
        var os = RuntimeInformation.OSDescription;
        var arch = RuntimeInformation.OSArchitecture;
        var machine = Environment.MachineName;
        var user = Environment.UserName;
        var processors = Environment.ProcessorCount;
        var clr = Environment.Version;
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => $"{d.Name}  {FormatBytes(d.AvailableFreeSpace)} frei von {FormatBytes(d.TotalSize)}  ({d.DriveFormat})");

        return $"Computer: {machine}\r\nBenutzer: {user}\r\nWindows: {os}\r\nArchitektur: {arch}\r\nLogische Prozessoren: {processors}\r\n.NET Runtime: {clr}\r\n\r\nLaufwerke:\r\n{string.Join("\r\n", drives)}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var index = 0;
        while (value >= 1024 && index < units.Length - 1) { value /= 1024; index++; }
        return $"{value:0.##} {units[index]}";
    }
}
