namespace NTBPW.PdfWatcher.Tests;

public sealed class UpdateServiceTests
{
    [Theory]
    [InlineData("v5.0.1", "5.0.1")]
    [InlineData("5.1.0", "5.1.0")]
    [InlineData("V5.1.0-beta.1", "5.1.0")]
    [InlineData("5.1.0+build.7", "5.1.0")]
    public void Versionen_werden_erkannt(string tag, string expected)
    {
        Assert.Equal(Version.Parse(expected), UpdateService.ParseVersion(tag));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    public void Ungueltige_Versionen_ergeben_null(string? tag)
    {
        Assert.Null(UpdateService.ParseVersion(tag));
    }

    [Theory]
    [InlineData("tarek-muna/ntb-pw-pdf-watcher")]
    [InlineData("https://github.com/tarek-muna/ntb-pw-pdf-watcher")]
    [InlineData("https://github.com/tarek-muna/ntb-pw-pdf-watcher.git")]
    [InlineData(" tarek-muna/ntb-pw-pdf-watcher/ ")]
    public void Repository_wird_normalisiert(string input)
    {
        Assert.Equal("tarek-muna/ntb-pw-pdf-watcher", UpdateService.NormalizeRepository(input));
    }

    [Fact]
    public void Nur_hoehere_Versionen_gelten_als_Update()
    {
        var current = UpdateService.CurrentVersion;
        var newer = new GitHubRelease($"v{current.Major + 1}.0.0", "", "", "", false, null, null);
        var same = new GitHubRelease($"v{current.ToString(3)}", "", "", "", false, null, null);

        Assert.True(UpdateService.IsNewer(newer));
        Assert.False(UpdateService.IsNewer(same));
    }
}
