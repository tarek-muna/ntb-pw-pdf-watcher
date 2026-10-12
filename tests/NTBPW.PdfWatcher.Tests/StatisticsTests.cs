namespace NTBPW.PdfWatcher.Tests;

public sealed class StatisticsTests
{
    private static readonly DateTime Today = new(2026, 10, 12);

    private static ProcessingRecord Record(string profile, int daysAgo, string result = "OK") =>
        new() { Profile = profile, Timestamp = Today.AddDays(-daysAgo).AddHours(9), Result = result };

    [Fact]
    public void Auswertung_je_Profil()
    {
        var records = new[]
        {
            Record("Empfang", 0), Record("Empfang", 3), Record("Empfang", 20), Record("Empfang", 40),
            Record("Buchhaltung", 0, "Öffnen fehlgeschlagen"), Record("Buchhaltung", 1)
        };

        var stats = Statistics.ByProfile(records, Today);

        var empfang = Assert.Single(stats, s => s.Profile == "Empfang");
        Assert.Equal((1, 2, 3, 4, 0), (empfang.Today, empfang.Last7Days, empfang.Last30Days, empfang.Total, empfang.Errors));
        var buchhaltung = Assert.Single(stats, s => s.Profile == "Buchhaltung");
        Assert.Equal((1, 2, 2, 2, 1), (buchhaltung.Today, buchhaltung.Last7Days, buchhaltung.Last30Days, buchhaltung.Total, buchhaltung.Errors));
        Assert.Equal("Empfang", stats[0].Profile); // nach 30-Tage-Menge sortiert
    }

    [Fact]
    public void Tagesverlauf_enthaelt_auch_leere_Tage()
    {
        var records = new[] { Record("A", 0), Record("A", 0), Record("A", 2), Record("A", 30) };

        var days = Statistics.ByDay(records, Today, 7);

        Assert.Equal(7, days.Count);
        Assert.Equal(Today, days[0].Day);
        Assert.Equal(2, days[0].Count);
        Assert.Equal(0, days[1].Count);
        Assert.Equal(1, days[2].Count);
        Assert.Equal(1 + 2, days.Sum(d => d.Count));
    }

    [Theory]
    [InlineData("OK", false)]
    [InlineData("ok", false)]
    [InlineData("", false)]
    [InlineData("Öffnen fehlgeschlagen", true)]
    public void Fehler_werden_am_Ergebnis_erkannt(string result, bool isError)
    {
        Assert.Equal(isError, Statistics.IsError(new ProcessingRecord { Result = result }));
    }
}
