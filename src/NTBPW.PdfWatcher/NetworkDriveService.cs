using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal static class NetworkDriveService
{
    public static async Task<ProcessResult> ConnectAsync(string driveLetter, string uncPath, bool persistent, string? userName = null, string? password = null, CancellationToken cancellationToken = default)
    {
        var letter = NormalizeDriveLetter(driveLetter);
        if (string.IsNullOrWhiteSpace(uncPath) || !uncPath.StartsWith(@"\\", StringComparison.Ordinal))
            return new ProcessResult(-1, string.Empty, "Der Netzwerkpfad muss mit \\\\ beginnen.");

        var args = $"use {letter} \"{uncPath.Trim()}\"";
        if (!string.IsNullOrWhiteSpace(password)) args += $" \"{password.Replace("\"", "\\\"")}\"";
        if (!string.IsNullOrWhiteSpace(userName)) args += $" /user:\"{userName.Trim()}\"";
        args += persistent ? " /persistent:yes" : " /persistent:no";
        return await RunNetAsync(args, cancellationToken);
    }

    public static Task<ProcessResult> DisconnectAsync(string driveLetter, CancellationToken cancellationToken = default)
        => RunNetAsync($"use {NormalizeDriveLetter(driveLetter)} /delete /yes", cancellationToken);

    public static async Task<(bool Success, string Message)> TestPathAsync(string uncPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uncPath) || !uncPath.StartsWith(@"\\", StringComparison.Ordinal))
            return (false, "Ungültiger UNC-Pfad.");

        try
        {
            var task = Task.Run(() => Directory.Exists(uncPath), cancellationToken);
            var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(8), cancellationToken));
            if (completed != task) return (false, "Zeitüberschreitung beim Verbindungstest.");
            return await task ? (true, "Netzwerkpfad ist erreichbar.") : (false, "Netzwerkpfad ist nicht erreichbar oder es fehlen Berechtigungen.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string NormalizeDriveLetter(string value)
    {
        var trimmed = value.Trim().TrimEnd(':');
        if (trimmed.Length != 1 || !char.IsLetter(trimmed[0]))
            throw new ArgumentException("Ungültiger Laufwerksbuchstabe.", nameof(value));
        return char.ToUpperInvariant(trimmed[0]) + ":";
    }

    private static async Task<ProcessResult> RunNetAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return new ProcessResult(process.ExitCode, await stdout, await stderr);
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }
    }
}
