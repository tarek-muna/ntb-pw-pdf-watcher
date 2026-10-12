namespace NTBPW.PdfWatcher.Tests;

public sealed class SignatureVerifierTests
{
    [Fact]
    public void Unsignierte_Datei_hat_keinen_Signierer()
    {
        using var dir = new TempDirectory();
        var file = dir.File("unsigniert.exe");
        File.WriteAllBytes(file, [0x4D, 0x5A, 0x00, 0x00]);

        Assert.Null(SignatureVerifier.GetTrustedSigner(file));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C:\\gibt-es-nicht\\setup.exe")]
    public void Fehlende_Datei_hat_keinen_Signierer(string? path)
    {
        Assert.Null(SignatureVerifier.GetTrustedSigner(path));
    }

    [Fact]
    public void Signierte_Microsoft_Datei_wird_erkannt()
    {
        // dotnet.exe ist auf Entwicklungsrechnern und Build-Servern vorhanden und von Microsoft signiert.
        var dotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        if (!File.Exists(dotnet)) return;

        var signer = SignatureVerifier.GetTrustedSigner(dotnet);

        // Microsoft signiert dotnet.exe mit dem Namen ".NET"; entscheidend ist eine gueltige Signatur mit Namen.
        Assert.False(string.IsNullOrWhiteSpace(signer));
    }

    [Fact]
    public void Veraenderte_signierte_Datei_ist_ungueltig()
    {
        var dotnet = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        if (!File.Exists(dotnet)) return;

        using var dir = new TempDirectory();
        var copy = dir.File("manipuliert.exe");
        var bytes = File.ReadAllBytes(dotnet);
        bytes[bytes.Length / 2] ^= 0xFF; // ein Byte im signierten Bereich kippen
        File.WriteAllBytes(copy, bytes);

        Assert.Null(SignatureVerifier.GetTrustedSigner(copy));
    }
}
