using System.Diagnostics;

namespace NTBPW.PdfWatcher;

internal sealed record SoftwarePackage(string Name, string Id, string Category);

internal static class WingetService
{
    public static IReadOnlyList<SoftwarePackage> DefaultPackages { get; } =
    [
        new("Adobe Acrobat Reader", "Adobe.Acrobat.Reader.64-bit", "PDF"),
        new("7-Zip", "7zip.7zip", "System"),
        new("Google Chrome", "Google.Chrome", "Browser"),
        new("Mozilla Firefox", "Mozilla.Firefox", "Browser"),
        new("Notepad++", "Notepad++.Notepad++", "Tools"),
        new("VLC media player", "VideoLAN.VLC", "Tools"),
        new("PowerShell 7", "Microsoft.PowerShell", "Administration"),
        new("Visual Studio Code", "Microsoft.VisualStudioCode", "Administration"),
        new("Git", "Git.Git", "Administration"),
        new("PuTTY", "PuTTY.PuTTY", "Netzwerk"),
        new("WinSCP", "WinSCP.WinSCP", "Netzwerk"),
        new("Wireshark", "WiresharkFoundation.Wireshark", "Netzwerk"),
        new("RustDesk", "RustDesk.RustDesk", "Fernwartung"),
        new("AnyDesk", "AnyDeskSoftwareGmbH.AnyDesk", "Fernwartung")
    ];

    public static async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync("--version", cancellationToken);
        return result.ExitCode == 0;
    }

    public static Task<ProcessResult> InstallAsync(string packageId, CancellationToken cancellationToken = default)
        => RunAsync($"install --id \"{packageId}\" --exact --silent --accept-package-agreements --accept-source-agreements --disable-interactivity", cancellationToken);

    public static Task<ProcessResult> UpgradeAllAsync(CancellationToken cancellationToken = default)
        => RunAsync("upgrade --all --silent --accept-package-agreements --accept-source-agreements --disable-interactivity", cancellationToken);

    private static async Task<ProcessResult> RunAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "winget.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
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

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}
