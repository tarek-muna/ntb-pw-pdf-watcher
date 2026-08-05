# NTB-PW PDF-Watcher Professional 4.2.0

**Netz, Technik, Büro GbR | Tarek Muna 2026**

Windows-Anwendung zum Überwachen von PDF-Eingangsordnern. Neue PDFs werden nach vollständigem Schreiben automatisch geöffnet und protokolliert.

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

Nach dem ersten Push genügt ein Tag:

```bat
RELEASE-4.2.0.cmd
```

Die GitHub Action erstellt Setup und SHA-256-Datei und veröffentlicht beides im Release.
