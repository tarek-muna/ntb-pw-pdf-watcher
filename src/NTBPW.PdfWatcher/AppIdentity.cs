using System.Reflection;

namespace NTBPW.PdfWatcher;

internal static class AppIdentity
{
    public const string ProductName = "NTB-PW PDF-Watcher Professional";
    public const string Company = "Netz, Technik, Büro GbR";
    public const string Copyright = "© Tarek Muna 2026";

    // Version kommt aus der csproj, damit sie nur an einer Stelle gepflegt wird.
    public static string DisplayVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public static string WindowTitle => $"{ProductName} {DisplayVersion}";
}
