namespace NTBPW.PdfWatcher.Tests;

public sealed class AppLoggerTests
{
    [Fact]
    public void Grosses_Protokoll_wird_rotiert()
    {
        File.WriteAllText(AppConfig.LogPath, new string('x', (int)AppLogger.MaxLogBytes + 10));

        AppLogger.Write("Nach der Rotation");

        Assert.True(File.Exists(AppConfig.LogPath + ".1"));
        Assert.True(new FileInfo(AppConfig.LogPath).Length < AppLogger.MaxLogBytes);
        Assert.Contains("Nach der Rotation", File.ReadAllText(AppConfig.LogPath));
    }
}
