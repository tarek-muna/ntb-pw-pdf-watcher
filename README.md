# NTB-PW PDF-Watcher Professional 5.0.0

**Netz, Technik, Büro GbR | Tarek Muna 2026**

Windows-Anwendung zum Überwachen von PDF-Eingangsordnern. Neue PDFs werden nach vollständigem Schreiben automatisch geöffnet und protokolliert.

## Funktionen

- Mehrere überwachbare Scanner- und Eingangsordnerprofile
- Automatisches Öffnen vollständig geschriebener PDF-Dateien
- Dokumentenansicht mit Suche und PDF-Aktionen
- Historie, Statistik und Live-Protokoll
- Tray-Betrieb und konfigurierbare Benachrichtigungen
- Integrierte, SHA-256-geprüfte GitHub-Updates

Die Anwendung enthält ausschließlich Funktionen des PDF-Watchers. Allgemeine System-, Software- und Netzwerkwerkzeuge werden getrennt im Projekt `ntb-toolbox` gepflegt.

## GitHub-Updates

Die Anwendung prüft dieses Repository:

`tarek-muna/ntb-pw-pdf-watcher`

Ein Release muss diese beiden Assets enthalten:

- `NTB-PW-PDF-Watcher-Setup-<Version>.exe`
- `NTB-PW-PDF-Watcher-Setup-<Version>.exe.sha256`

Der integrierte Updater lädt das Setup, prüft SHA-256 und startet anschließend den Installer.

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
winget\Install-PDFWatcher.cmd -Version 5.0.1
```

Nur Manifeste erzeugen, z. B. für Intune oder `winget validate`:

```powershell
.\winget\Install-PDFWatcher.ps1 -OutputPath C:\Temp\ntbpw-manifest
```

Die Paketkennung lautet `NetzTechnikBuero.NTBPWPDFWatcher`. Updates kommen weiterhin über den
integrierten Updater oder durch erneutes Ausführen des Skripts.

## Lokal erstellen

1. .NET 8 SDK installieren.
2. `build.cmd` starten.
3. Für das Setup Inno Setup 6 installieren.
4. `build-setup.cmd` starten.

## GitHub Release erstellen

Nach dem Merge in `main` genügt:

```bat
RELEASE-5.0.0.cmd
```

Die GitHub Action erstellt Setup und SHA-256-Datei und veröffentlicht beides im Release.
