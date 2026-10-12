namespace NTBPW.PdfWatcher.Tests;

public sealed class ConfigTransferTests
{
    [Fact]
    public void Export_enthaelt_Profile_und_Regeln_aber_keine_Passwoerter()
    {
        using var dir = new TempDirectory();
        var config = new AppConfig
        {
            Profiles = [new WatchProfile { Name = "Empfang", Folder = @"\\server\scan" }],
            Rules = [new FilingRule { Name = "Rechnungen", FileNameContains = "Rechnung", DestinationFolder = @"\\server\buchhaltung" }],
            SmtpHost = "smtp.example.org",
            SmtpPassword = "Geheim-123"
        };
        var file = dir.File("export.json");

        ConfigStore.Export(config, file);

        var json = File.ReadAllText(file);
        Assert.Contains("Empfang", json);
        Assert.Contains("Rechnungen", json);
        Assert.Contains("smtp.example.org", json);
        Assert.DoesNotContain("dpapi:", json);
        Assert.Equal("Geheim-123", config.SmtpPassword); // Original bleibt unveraendert
    }

    [Fact]
    public void Import_uebernimmt_Daten_und_behaelt_vorhandene_Passwoerter()
    {
        using var dir = new TempDirectory();
        var source = new AppConfig
        {
            Profiles = [new WatchProfile { Name = "Empfang", Folder = @"\\server\scan" }],
            Rules = [new FilingRule { Name = "Rechnungen", DestinationFolder = @"\\server\buchhaltung" }],
            SmtpHost = "smtp.example.org"
        };
        var file = dir.File("export.json");
        ConfigStore.Export(source, file);
        var current = new AppConfig { SmtpPassword = "Lokal-Passwort" };

        var imported = ConfigStore.Import(file, current);

        Assert.Equal("Empfang", Assert.Single(imported.Profiles).Name);
        Assert.Equal(source.Profiles[0].Id, imported.Profiles[0].Id);
        Assert.Equal("Rechnungen", Assert.Single(imported.Rules).Name);
        Assert.Equal("smtp.example.org", imported.SmtpHost);
        Assert.Equal("Lokal-Passwort", imported.SmtpPassword);
    }

    [Theory]
    [InlineData("keine json")]
    [InlineData("""{ "Profiles": [] }""")]
    public void Ungueltige_Dateien_werden_abgelehnt(string content)
    {
        using var dir = new TempDirectory();
        var file = dir.File("kaputt.json");
        File.WriteAllText(file, content);

        Assert.Throws<InvalidDataException>(() => ConfigStore.Import(file, new AppConfig()));
    }
}
