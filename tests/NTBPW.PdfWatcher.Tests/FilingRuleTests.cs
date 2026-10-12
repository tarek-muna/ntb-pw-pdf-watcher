namespace NTBPW.PdfWatcher.Tests;

public sealed class FilingRuleTests
{
    private static readonly ProcessingServices Services = new(new AppConfig());

    [Theory]
    [InlineData("Rechnung-4711.pdf", "", true)]
    [InlineData("Rechnung-4711.pdf", "rechnung", true)]
    [InlineData("Invoice-12.pdf", "Rechnung; Invoice", true)]
    [InlineData("Lieferschein.pdf", "Rechnung;Invoice", false)]
    [InlineData("Lieferschein.pdf", " ; ", true)]
    public void Dateinamenfilter(string fileName, string filter, bool expected)
    {
        Assert.Equal(expected, ProcessingServices.MatchesFileName(fileName, filter));
    }

    [Fact]
    public void Erste_passende_Regel_gewinnt()
    {
        using var source = new TempDirectory();
        using var first = new TempDirectory();
        using var second = new TempDirectory();
        var file = source.File("Rechnung.pdf");
        File.WriteAllBytes(file, TestPdf.Bytes);

        var result = Services.ApplyRules(file, "",
        [
            new FilingRule { FileNameContains = "Rechnung", DestinationFolder = first.Path },
            new FilingRule { DestinationFolder = second.Path }
        ]);

        Assert.Equal(first.File("Rechnung.pdf"), result);
    }

    [Fact]
    public void Passende_Regel_verschiebt_die_Datei()
    {
        using var source = new TempDirectory();
        using var target = new TempDirectory();
        var file = source.File("Rechnung-4711.pdf");
        File.WriteAllBytes(file, TestPdf.Bytes);

        var result = Services.ApplyRules(file, "", [new FilingRule { FileNameContains = "rechnung", DestinationFolder = target.Path }]);

        Assert.Equal(target.File("Rechnung-4711.pdf"), result);
        Assert.True(File.Exists(result));
        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Vorhandene_Zieldatei_wird_nicht_ueberschrieben()
    {
        using var source = new TempDirectory();
        using var target = new TempDirectory();
        File.WriteAllBytes(target.File("scan.pdf"), TestPdf.Bytes);
        var file = source.File("scan.pdf");
        File.WriteAllBytes(file, TestPdf.Bytes);

        var result = Services.ApplyRules(file, "", [new FilingRule { DestinationFolder = target.Path }]);

        Assert.Equal(target.File("scan_1.pdf"), result);
    }

    [Fact]
    public void Kopieren_laesst_das_Original_stehen()
    {
        using var source = new TempDirectory();
        using var target = new TempDirectory();
        var file = source.File("scan.pdf");
        File.WriteAllBytes(file, TestPdf.Bytes);

        var result = Services.ApplyRules(file, "", [new FilingRule { DestinationFolder = target.Path, CopyInsteadOfMove = true }]);

        Assert.True(File.Exists(file));
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void Ohne_passende_Regel_bleibt_die_Datei_liegen()
    {
        using var source = new TempDirectory();
        using var target = new TempDirectory();
        var file = source.File("scan.pdf");
        File.WriteAllBytes(file, TestPdf.Bytes);

        var rules = new[]
        {
            new FilingRule { FileNameContains = "Rechnung", DestinationFolder = target.Path },
            new FilingRule { Enabled = false, DestinationFolder = target.Path }
        };
        var result = Services.ApplyRules(file, "", rules);

        Assert.Equal(file, result);
        Assert.True(File.Exists(file));
    }
}
