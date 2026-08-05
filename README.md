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
