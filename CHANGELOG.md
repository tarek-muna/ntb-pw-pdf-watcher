# Changelog

Alle wesentlichen Änderungen am NTB-PW PDF-Watcher werden hier dokumentiert.

## [5.0.1] - unveröffentlicht

### Behoben
- Überwachung startet jetzt auch beim Windows-Autostart (`--minimized`), ohne dass das Dashboard geöffnet werden muss.
- Beim Programmstart werden bereits vorhandene PDFs nicht mehr alle erneut geöffnet.
- PDFs, die beim ersten Prüfen noch nicht fertig geschrieben waren, werden erneut versucht statt dauerhaft übersprungen.
- Ein neuer Scan mit gleichem Dateinamen wird wieder erkannt.
- Langsame oder getrennte Netzwerk-/RDP-Ordner frieren die Oberfläche nicht mehr ein.
- Änderungen an einem Scanner-Profil gehen beim Wechsel zu einem anderen Profil nicht mehr verloren; „Abbrechen“ verwirft Profiländerungen jetzt wirklich.
- Die Einstellung „Beim Autostart minimiert starten“ wird berücksichtigt.
- „Windows-Benachrichtigung anzeigen“ zeigt jetzt eine echte Benachrichtigung im Infobereich.
- Eine beschädigte `config.json` wird vor dem Zurücksetzen gesichert.
- Protokoll-Export stürzt nicht mehr ab, wenn noch keine Logdatei existiert; parallele Logeinträge gehen nicht mehr verloren.
- Tesseract kann nicht mehr durch ungelesene Fehlerausgaben hängen bleiben (Zeitlimit 2 Minuten).
- Setup-Dateiname und Installer-Version folgen jetzt der Projektversion (v5.0.0 wurde fälschlich als `Setup-4.2.0.exe` ausgeliefert).

### Entfernt
- Veralteter Workflow `apply-v5-documents.yml` samt Patch-Skript sowie ungenutzter Code im Hauptfenster.

## [5.0.0] - 2026-08-05

### Hinzugefügt
- Modernes Dashboard mit Seitenleiste, Statuskarten und Live-Protokoll.
- Verwaltung mehrerer Scanner- und Eingangsordnerprofile.
- Dokumentenansicht mit Suche und PDF-Aktionen.
- Historie und Statistik für verarbeitete PDF-Dateien.
- Integrierte Update-Seite mit SHA-256-Prüfung.

### Geändert
- Produkt- und Versionsinformationen zentral über `AppIdentity` verwaltet.
- Scanner-Profile über einen kompakten Einstellungsdialog konfigurierbar.
- PDF-Watcher als eigenständiges Produkt von allgemeinen Toolbox-Funktionen getrennt.

### Entfernt
- Tools Center sowie allgemeine Software-, Netzwerk-, Windows- und Systemwerkzeuge.
- Toolbox-spezifische Workflows und Integrationslogik.

## [4.2.0]
- GitHub-Updateprüfung und automatischer Release-Workflow.
