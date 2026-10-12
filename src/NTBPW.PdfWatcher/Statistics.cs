namespace NTBPW.PdfWatcher;

internal sealed record ProfileStatistics(string Profile, int Today, int Last7Days, int Last30Days, int Total, int Errors);
internal sealed record DayStatistics(DateTime Day, int Count);

// Reine Auswertung der Historie (ohne Oberflaeche), damit sie testbar ist.
internal static class Statistics
{
    public static bool IsError(ProcessingRecord record) =>
        !string.IsNullOrEmpty(record.Result) && !record.Result.Equals("OK", StringComparison.OrdinalIgnoreCase);

    public static IReadOnlyList<ProfileStatistics> ByProfile(IEnumerable<ProcessingRecord> records, DateTime today)
    {
        var week = today.AddDays(-6);
        var month = today.AddDays(-29);
        return records
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Profile) ? "(ohne Profil)" : r.Profile)
            .Select(g => new ProfileStatistics(
                g.Key,
                g.Count(r => r.Timestamp >= today),
                g.Count(r => r.Timestamp >= week),
                g.Count(r => r.Timestamp >= month),
                g.Count(),
                g.Count(IsError)))
            .OrderByDescending(s => s.Last30Days)
            .ThenBy(s => s.Profile, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // Liefert jeden Tag des Zeitraums, auch Tage ohne PDF (Anzahl 0), neuester Tag zuerst.
    public static IReadOnlyList<DayStatistics> ByDay(IEnumerable<ProcessingRecord> records, DateTime today, int days)
    {
        var first = today.AddDays(-(days - 1));
        var counts = records
            .Where(r => r.Timestamp >= first && r.Timestamp < today.AddDays(1))
            .GroupBy(r => r.Timestamp.Date)
            .ToDictionary(g => g.Key, g => g.Count());
        return Enumerable.Range(0, days)
            .Select(i => today.AddDays(-i))
            .Select(day => new DayStatistics(day, counts.GetValueOrDefault(day)))
            .ToList();
    }
}
