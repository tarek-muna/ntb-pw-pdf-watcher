using System.Text.Json;

namespace NTBPW.PdfWatcher.Tests;

public sealed class SecretTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    [Fact]
    public void Verschluesseln_und_Entschluesseln()
    {
        var stored = SecretProtector.Protect("Geheim-123");

        Assert.StartsWith("dpapi:", stored);
        Assert.DoesNotContain("Geheim-123", stored);
        Assert.Equal("Geheim-123", SecretProtector.Unprotect(stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("klartext")]
    [InlineData("dpapi:kein-base64!")]
    [InlineData("dpapi:AAAA")]
    public void Ungueltige_Werte_ergeben_leeren_Text(string? stored)
    {
        Assert.Equal("", SecretProtector.Unprotect(stored));
    }

    [Fact]
    public void Passwoerter_werden_nicht_im_Klartext_gespeichert()
    {
        var config = new AppConfig { SmtpPassword = "Geheim-123", AiApiKey = "sk-test" };

        var json = JsonSerializer.Serialize(config, Options);

        Assert.DoesNotContain("Geheim-123", json);
        Assert.DoesNotContain("sk-test", json);
        var restored = JsonSerializer.Deserialize<AppConfig>(json, Options)!;
        Assert.Equal("Geheim-123", restored.SmtpPassword);
        Assert.Equal("sk-test", restored.AiApiKey);
        Assert.False(restored.HasLegacySecrets);
    }

    [Fact]
    public void Klartext_aus_Version_5_0_wird_uebernommen_und_verschluesselt()
    {
        const string legacy = """{ "SmtpPassword": "Alt-Passwort", "AiApiKey": "alt-key", "SmtpHost": "smtp.example.org" }""";

        var config = JsonSerializer.Deserialize<AppConfig>(legacy, Options)!;

        Assert.True(config.HasLegacySecrets);
        Assert.Equal("Alt-Passwort", config.SmtpPassword);
        Assert.Equal("alt-key", config.AiApiKey);
        Assert.Equal("smtp.example.org", config.SmtpHost);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(config, Options));
        Assert.False(doc.RootElement.TryGetProperty("SmtpPassword", out _));
        Assert.False(doc.RootElement.TryGetProperty("AiApiKey", out _));
        Assert.StartsWith("dpapi:", doc.RootElement.GetProperty("SmtpPasswordProtected").GetString());
    }

    [Fact]
    public void ConfigStore_schreibt_alte_Konfiguration_verschluesselt_zurueck()
    {
        File.WriteAllText(AppConfig.ConfigPath, """{ "SmtpPassword": "Alt-Passwort" }""");

        var config = ConfigStore.Load();

        Assert.Equal("Alt-Passwort", config.SmtpPassword);
        var onDisk = File.ReadAllText(AppConfig.ConfigPath);
        Assert.DoesNotContain("Alt-Passwort", onDisk);
        Assert.Contains("SmtpPasswordProtected", onDisk);
    }
}
