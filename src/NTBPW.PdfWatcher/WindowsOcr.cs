using System.Text;
using Windows.Data.Pdf;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace NTBPW.PdfWatcher;

// Texterkennung mit den in Windows eingebauten Komponenten (Windows.Data.Pdf + Windows.Media.Ocr).
// Es wird keine Zusatzsoftware benoetigt; erkannt wird in der Sprache des Windows-Benutzerprofils,
// ersatzweise Deutsch oder Englisch, sofern das jeweilige Sprachpaket installiert ist.
internal static class WindowsOcr
{
    public const int DefaultMaxPages = 3;

    public static bool IsAvailable => CreateEngine() is not null;

    public static async Task<string> ExtractTextAsync(string pdfPath, int maxPages = DefaultMaxPages, CancellationToken token = default)
    {
        var engine = CreateEngine();
        if (engine is null)
        {
            AppLogger.Write("OCR nicht verfügbar: Für keine installierte Windows-Sprache ist Texterkennung vorhanden.");
            return "";
        }

        // Ueber einen Stream laden, damit auch UNC- und \\tsclient-Pfade funktionieren.
        await using var file = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var input = file.AsRandomAccessStream();
        var document = await PdfDocument.LoadFromStreamAsync(input);

        var text = new StringBuilder();
        var pages = Math.Min(document.PageCount, (uint)Math.Max(1, maxPages));
        for (uint i = 0; i < pages; i++)
        {
            token.ThrowIfCancellationRequested();
            using var page = document.GetPage(i);
            using var image = new InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(image, RenderOptionsFor(page));

            var decoder = await BitmapDecoder.CreateAsync(image);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var result = await engine.RecognizeAsync(bitmap);
            text.AppendLine(result.Text);
        }
        return text.ToString().Trim();
    }

    private static PdfPageRenderOptions RenderOptionsFor(PdfPage page)
    {
        // Fuer gute Erkennung hoch skalieren, aber die maximale Bildgroesse der OCR-Engine einhalten.
        var max = (double)OcrEngine.MaxImageDimension;
        var width = Math.Max(1, page.Size.Width);
        var height = Math.Max(1, page.Size.Height);
        var scale = Math.Min(3.0, Math.Min(max / width, max / height));
        return new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Floor(width * scale),
            DestinationHeight = (uint)Math.Floor(height * scale)
        };
    }

    private static OcrEngine? CreateEngine()
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is not null) return engine;
        foreach (var tag in new[] { "de-DE", "en-US" })
        {
            var language = new Language(tag);
            if (OcrEngine.IsLanguageSupported(language)) return OcrEngine.TryCreateFromLanguage(language);
        }
        return null;
    }
}
