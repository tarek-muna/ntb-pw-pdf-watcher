using System.Security.Cryptography;
using System.Text;

namespace NTBPW.PdfWatcher;

// Verschluesselt Zugangsdaten mit Windows-DPAPI. Nur der angemeldete Windows-Benutzer
// kann sie wieder entschluesseln; auf einem anderen Konto ergibt sich ein leerer Wert.
internal static class SecretProtector
{
    private const string Prefix = "dpapi:";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("NTB-PW PDF-Watcher");

    public static string Protect(string? plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), Entropy, DataProtectionScope.CurrentUser);
        return Prefix + Convert.ToBase64String(data);
    }

    public static string Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal)) return "";
        try
        {
            var data = ProtectedData.Unprotect(Convert.FromBase64String(stored[Prefix.Length..]), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return "";
        }
    }
}
