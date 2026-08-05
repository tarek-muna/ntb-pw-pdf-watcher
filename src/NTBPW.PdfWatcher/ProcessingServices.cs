using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NTBPW.PdfWatcher;

internal sealed class ProcessingServices
{
    private readonly AppConfig _config;
    public ProcessingServices(AppConfig config) => _config = config;

    public async Task<string> RunOcrAsync(string pdf)
    {
        if (!_config.EnableOcr || string.IsNullOrWhiteSpace(_config.TesseractExe) || !File.Exists(_config.TesseractExe)) return "";
        var tempBase = Path.Combine(Path.GetTempPath(), "ntbpw_" + Guid.NewGuid().ToString("N"));
        try
        {
            var psi = new ProcessStartInfo(_config.TesseractExe, $"\"{pdf}\" \"{tempBase}\" -l {_config.TesseractLanguage} pdf")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            using var p = Process.Start(psi);
            if (p is null) return "";
            await p.WaitForExitAsync();
            var txt = tempBase + ".txt";
            return File.Exists(txt) ? await File.ReadAllTextAsync(txt) : "";
        }
        catch (Exception ex) { AppLogger.Write("OCR-Fehler: " + ex.Message); return ""; }
        finally { try { File.Delete(tempBase + ".txt"); } catch { } }
    }

    public async Task<string> ClassifyAsync(string fileName, string ocrText)
    {
        if (!_config.EnableAiClassification || string.IsNullOrWhiteSpace(_config.AiEndpoint)) return "";
        try
        {
            using var http = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_config.AiApiKey)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.AiApiKey);
            var payload = JsonSerializer.Serialize(new { fileName, text = ocrText.Length > 12000 ? ocrText[..12000] : ocrText });
            using var response = await http.PostAsync(_config.AiEndpoint, new StringContent(payload, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("classification", out var c) ? c.GetString() ?? "" : json.Trim();
        }
        catch (Exception ex) { AppLogger.Write("KI-Klassifizierung fehlgeschlagen: " + ex.Message); return ""; }
    }

    public async Task SendEmailAsync(string subject, string body)
    {
        if (!_config.EnableEmail || string.IsNullOrWhiteSpace(_config.SmtpHost) || string.IsNullOrWhiteSpace(_config.EmailTo)) return;
        try
        {
            using var client = new SmtpClient(_config.SmtpHost, _config.SmtpPort) { EnableSsl = true };
            if (!string.IsNullOrWhiteSpace(_config.SmtpUser)) client.Credentials = new NetworkCredential(_config.SmtpUser, _config.SmtpPassword);
            using var msg = new MailMessage(_config.EmailFrom, _config.EmailTo, subject, body);
            await client.SendMailAsync(msg);
        }
        catch (Exception ex) { AppLogger.Write("E-Mail konnte nicht gesendet werden: " + ex.Message); }
    }

    public string ApplyRules(string source, string classification, IEnumerable<FilingRule> rules)
    {
        foreach (var rule in rules.Where(r => r.Enabled))
        {
            if (!string.IsNullOrWhiteSpace(rule.FileNameContains) && !Path.GetFileName(source).Contains(rule.FileNameContains, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrWhiteSpace(rule.ClassificationEquals) && !classification.Equals(rule.ClassificationEquals, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(rule.DestinationFolder)) continue;
            try
            {
                Directory.CreateDirectory(rule.DestinationFolder);
                var target = UniquePath(Path.Combine(rule.DestinationFolder, Path.GetFileName(source)));
                if (rule.CopyInsteadOfMove) File.Copy(source, target); else File.Move(source, target);
                return target;
            }
            catch (Exception ex) { AppLogger.Write($"Ablageregel '{rule.Name}' fehlgeschlagen: {ex.Message}"); }
        }
        return source;
    }

    public void CloudSync(string source)
    {
        if (!_config.EnableCloudSync || string.IsNullOrWhiteSpace(_config.CloudSyncFolder) || !File.Exists(source)) return;
        try
        {
            Directory.CreateDirectory(_config.CloudSyncFolder);
            File.Copy(source, UniquePath(Path.Combine(_config.CloudSyncFolder, Path.GetFileName(source))));
        }
        catch (Exception ex) { AppLogger.Write("Cloud-Synchronisation fehlgeschlagen: " + ex.Message); }
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!; var name = Path.GetFileNameWithoutExtension(path); var ext = Path.GetExtension(path);
        for (var i = 1; ; i++) { var p = Path.Combine(dir, $"{name}_{i}{ext}"); if (!File.Exists(p)) return p; }
    }
}
