using System.Threading;

namespace NTBPW.PdfWatcher;

internal static class Program
{
    private const string MutexName = "Local\\NTB-PW-PDF-Watcher-Professional-4";
    private const string ShowEventName = "Local\\NTB-PW-PDF-Watcher-Show-4";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(true, MutexName, out var firstInstance);
        if (!firstInstance)
        {
            try
            {
                using var showEvent = EventWaitHandle.OpenExisting(ShowEventName);
                showEvent.Set();
            }
            catch
            {
                MessageBox.Show(
                    "NTB-PW PDF-Watcher läuft bereits.",
                    "NTB-PW",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            return;
        }

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportFatalError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ReportFatalError(e.ExceptionObject as Exception ?? new Exception("Unbekannter Programmfehler"));

        using var context = new WatcherApplicationContext(ShowEventName);
        Application.Run(context);
    }

    private static void ReportFatalError(Exception exception)
    {
        try { AppLogger.Write("SCHWERER FEHLER: " + exception); } catch { }
        MessageBox.Show(
            exception.Message + Environment.NewLine + Environment.NewLine +
            "Details wurden in PDF-Watcher.log gespeichert.",
            "NTB-PW – Programmfehler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
