# NTB-PW PDF-Watcher Professional

**Netz, Technik, Büro GbR | Tarek Muna 2026**

Windows-Anwendung zum Überwachen von PDF-Eingangsordnern. Neue PDFs werden nach vollständigem Schreiben automatisch geöffnet, bei Bedarf abgelegt und protokolliert.

Aktuelle Version und Änderungen: siehe [CHANGELOG.md](CHANGELOG.md) und die [GitHub-Releases](https://github.com/tarek-muna/ntb-pw-pdf-watcher/releases).

## Funktionen

- Mehrere Scanner- und Eingangsordnerprofile, auch Netzwerk- und RDP-Pfade (`\\server\scan`, `\\tsclient\C\Scan`)
- Automatisches Öffnen vollständig geschriebener PDF-Dateien
- Ablageregeln: PDFs nach Dateiname und/oder PDF-Inhalt automatisch in Zielordner verschieben oder kopieren
- Texterkennung (OCR) mit der in Windows eingebauten Texterkennung – keine Zusatzsoftware nötig
- Dokumentenansicht mit Suche (auch im erkannten Text), Vorschau und PDF-Aktionen
- Historie, Statistik und Live-Protokoll
- Tray-Betrieb, Windows-Benachrichtigungen, optionale E-Mail-Benachrichtigung
- Optionale Kopie jeder neuen PDF in einen weiteren Ordner (z. B. OneDrive)
- Zugangsdaten verschlüsselt (Windows-DPAPI)
- Integrierte, SHA-256-geprüfte GitHub-Updates

## Systemvoraussetzungen

- Windows 10 ab Version 2004 oder Windows 11 (x64)
- Für die Vorschau: Microsoft Edge WebView2-Runtime (unter Windows 11 vorinstalliert)
- Für OCR: ein installiertes Windows-Sprachpaket mit Texterkennung (Deutsch oder Englisch sind üblicherweise vorhanden)

Die Installation erfolgt pro Benutzer und benötigt keine Administratorrechte.

## Daten

Konfiguration, Historie und Protokoll liegen unter `%LOCALAPPDATA%\NTB-PW\PDF-Watcher-3`
(`config.json`, `history.json`, `PDF-Watcher.log`). Der Ordner lässt sich über
Einstellungen → Erweitert → „Datenordner öffnen“ aufrufen.

## Interne Installation per winget

Der PDF-Watcher ist nicht im öffentlichen winget-Verzeichnis. Für die interne Verteilung erzeugt
`winget\Install-PDFWatcher.ps1` die winget-Manifeste direkt aus dem GitHub-Release
(Setup-URL und SHA-256) und installiert damit.

Einmalig pro Rechner als Administrator lokale Manifeste erlauben:

```bat
winget settings --enable LocalManifestFiles
```

Danach installieren (neueste Version oder eine bestimmte):

```bat
winget\Install-PDFWatcher.cmd
winget\Install-PDFWatcher.cmd -Version 5.2.0
```

Nur Manifeste erzeugen, z. B. für Intune oder `winget validate`:

```powershell
.\winget\Install-PDFWatcher.ps1 -OutputPath C:\Temp\ntbpw-manifest
```

Die Paketkennung lautet `NetzTechnikBuero.NTBPWPDFWatcher`. Updates kommen weiterhin über den
integrierten Updater oder durch erneutes Ausführen des Skripts.

## Entwicklung

### Lokal erstellen

1. .NET 8 SDK installieren.
2. `build.cmd` starten (erstellt `publish\NTB-PW PDF-Watcher.exe`).
3. Für das Setup Inno Setup 6 installieren und `build-setup.cmd` starten.

Tests ausführen:

```bat
dotnet test tests\NTBPW.PdfWatcher.Tests
```

### Automatische Prüfungen (GitHub Actions)

Bei jedem Push auf `main`, `feature/**` und `fix/**` sowie bei Pull Requests:

1. Anwendung kompilieren und alle Tests ausführen.
2. Setup bauen, die zuletzt veröffentlichte Version installieren, die neue darüber installieren,
   Version und Erhalt der Konfiguration prüfen, die Anwendung starten und wieder deinstallieren
   (`tools\test-installer.ps1`).

### Release erstellen

1. Version in `src\NTBPW.PdfWatcher\NTBPW.PdfWatcher.csproj` erhöhen und `CHANGELOG.md` ergänzen.
2. Änderungen per Pull Request nach `main` bringen.
3. Auf `main` `RELEASE.cmd` starten. Das Skript liest die Version aus der csproj und setzt den Tag `v<Version>`.

Der Release-Workflow prüft, dass Tag und Version übereinstimmen, führt Tests und Installer-Test aus
und veröffentlicht `NTB-PW-PDF-Watcher-Setup-<Version>.exe` samt `.sha256` im GitHub-Release.
Der integrierte Updater lädt das Setup, prüft SHA-256 und startet anschließend den Installer.
