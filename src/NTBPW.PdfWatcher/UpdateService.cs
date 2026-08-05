using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace NTBPW.PdfWatcher;

internal sealed record GitHubAsset(string Name, string DownloadUrl, long Size);

internal sealed record GitHubRelease(
    string TagName,
    string Name,
    string Notes,
    string WebUrl,
    bool IsPrerelease,
    GitHubAsset? Installer,
    GitHubAsset? Checksum)
{
    public Version? Version => UpdateService.ParseVersion(TagName);
}

internal static class UpdateService
{
    private static readonly HttpClient Client = CreateClient();

    public static string CurrentVersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    public static async Task<GitHubRelease?> GetLatestReleaseAsync(
        string repository,
        CancellationToken cancellationToken = default)
    {
        repository = NormalizeRepository(repository);
        if (string.IsNullOrWhiteSpace(repository))
            throw new InvalidOperationException("Das GitHub-Repository ist nicht eingetragen.");

        var url = $"https://api.github.com/repos/{repository}/releases/latest";
        using var response = await Client.GetAsync(url, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var tag = GetString(root, "tag_name");
        var name = GetString(root, "name");
        var notes = GetString(root, "body");
        var webUrl = GetString(root, "html_url");
        var prerelease = root.TryGetProperty("prerelease", out var pre) && pre.GetBoolean();

        var assets = new List<GitHubAsset>();
        if (root.TryGetProperty("assets", out var assetArray))
        {
            foreach (var asset in assetArray.EnumerateArray())
            {
                var assetName = GetString(asset, "name");
                var downloadUrl = GetString(asset, "browser_download_url");
                var size = asset.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt64() : 0;
                if (!string.IsNullOrWhiteSpace(assetName) && !string.IsNullOrWhiteSpace(downloadUrl))
                    assets.Add(new GitHubAsset(assetName, downloadUrl, size));
            }
        }

        var installer = assets
            .Where(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        GitHubAsset? checksum = null;
        if (installer is not null)
        {
            checksum = assets.FirstOrDefault(a =>
                a.Name.Equals(installer.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
        }
        checksum ??= assets.FirstOrDefault(a => a.Name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));

        return new GitHubRelease(tag, name, notes, webUrl, prerelease, installer, checksum);
    }

    public static bool IsNewer(GitHubRelease release)
    {
        var available = release.Version;
        return available is not null && available > CurrentVersion;
    }

    public static async Task<string> DownloadAndVerifyAsync(
        GitHubRelease release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (release.Installer is null)
            throw new InvalidOperationException("Der GitHub-Release enthält keinen Windows-Installer.");
        if (release.Checksum is null)
            throw new InvalidOperationException("Zum Installer fehlt die SHA-256-Prüfsummendatei.");

        var updateDirectory = Path.Combine(Path.GetTempPath(), "NTB-PW", "Updates", release.TagName);
        Directory.CreateDirectory(updateDirectory);
        var installerPath = Path.Combine(updateDirectory, SanitizeFileName(release.Installer.Name));

        using (var response = await Client.GetAsync(
                   release.Installer.DownloadUrl,
                   HttpCompletionOption.ResponseHeadersRead,
                   cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? release.Installer.Size;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long written = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                written += read;
                if (total > 0)
                    progress?.Report((int)Math.Clamp(written * 100L / total, 0, 100));
            }
        }

        var expectedText = await Client.GetStringAsync(release.Checksum.DownloadUrl, cancellationToken);
        var expectedHash = expectedText
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(expectedHash) || expectedHash.Length != 64)
            throw new InvalidOperationException("Die SHA-256-Prüfsummendatei hat ein ungültiges Format.");

        await using var file = File.OpenRead(installerPath);
        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(installerPath);
            throw new InvalidOperationException("Die SHA-256-Prüfung ist fehlgeschlagen. Das Update wurde verworfen.");
        }

        progress?.Report(100);
        return installerPath;
    }

    public static void StartInstaller(string installerPath)
    {
        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
    }

    public static Version? ParseVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Trim();
        if (cleaned.StartsWith('v') || cleaned.StartsWith('V')) cleaned = cleaned[1..];
        var separator = cleaned.IndexOfAny(['-', '+']);
        if (separator >= 0) cleaned = cleaned[..separator];
        return Version.TryParse(cleaned, out var version) ? version : null;
    }

    public static string NormalizeRepository(string? repository)
    {
        if (string.IsNullOrWhiteSpace(repository)) return "";
        var value = repository.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            value = uri.AbsolutePath.Trim('/');
        }
        if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            value = value[..^4];
        return value.Trim('/');
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NTB-PW-PDF-Watcher", CurrentVersionText));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static string GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(invalid, '_');
        return fileName;
    }
}
