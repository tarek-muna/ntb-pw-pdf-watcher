using System.Text;

namespace NTBPW.PdfWatcher.Tests;

public sealed class OcrTests
{
    // Erzeugt eine einseitige PDF mit grossem Text in Helvetica (mit korrekter xref-Tabelle).
    private static byte[] CreateTextPdf(string text)
    {
        var content = $"BT /F1 48 Tf 60 700 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = pdf.Length;
        pdf.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) pdf.Append($"{offset:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    [Fact]
    public async Task Text_einer_PDF_wird_erkannt()
    {
        // Ohne installiertes OCR-Sprachpaket (z. B. auf manchen Build-Servern) ist kein Test moeglich.
        if (!WindowsOcr.IsAvailable) return;

        using var dir = new TempDirectory();
        var file = dir.File("rechnung.pdf");
        File.WriteAllBytes(file, CreateTextPdf("RECHNUNG 4711"));

        var text = await WindowsOcr.ExtractTextAsync(file);

        Assert.Contains("4711", text);
        Assert.Contains("RECHNUNG", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Defekte_PDF_ergibt_leeren_Text_ohne_Absturz()
    {
        using var dir = new TempDirectory();
        var file = dir.File("kaputt.pdf");
        File.WriteAllText(file, "keine PDF");

        var services = new ProcessingServices(new AppConfig
        {
            Rules = [new FilingRule { ContentContains = "Rechnung", DestinationFolder = dir.Path }]
        });

        Assert.True(services.NeedsOcr);
        Assert.Equal("", await services.RunOcrAsync(file));
    }

    [Fact]
    public async Task Ohne_Inhaltsregel_wird_keine_OCR_ausgefuehrt()
    {
        var services = new ProcessingServices(new AppConfig
        {
            Rules = [new FilingRule { FileNameContains = "Rechnung", DestinationFolder = "C:\\Ziel" }]
        });

        Assert.False(services.NeedsOcr);
        Assert.Equal("", await services.RunOcrAsync("C:\\gibt-es-nicht.pdf"));
    }
}
