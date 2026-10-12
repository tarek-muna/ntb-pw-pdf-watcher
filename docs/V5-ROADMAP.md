# NTB-PW PDF-Watcher Professional – Roadmap 5.x

## Erledigt

### 5.0 – Grundlage
- Kompaktes Dashboard mit Seitenleiste, Statuskarten und Live-Protokoll
- Mehrere Scanner-Profile, Dokumentenansicht, Historie und Statistik
- GitHub-Updates mit SHA-256-Prüfung

### 5.0.1 – Stabilisierung
- Überwachung startet auch beim Autostart (minimiert)
- Keine erneute Verarbeitung vorhandener PDFs beim Start, Wiederholung unfertiger Dateien
- Überwachung im Hintergrund, keine UI-Hänger bei Netzwerk-/RDP-Pfaden
- Setup-Version folgt der Projektversion

### 5.1 – Vertrauen und Ablage
- Zugangsdaten mit DPAPI verschlüsselt
- Aufgeräumte Einstellungen, Profile nur an einer Stelle
- Ablageregeln mit eigener Oberfläche
- PDF-Vorschau über WebView2, Protokoll-Rotation
- Automatische Tests in Build und Release

### 5.2 – Inhalt erkennen
- Windows-OCR statt Tesseract, Ablageregeln nach PDF-Inhalt
- Suche im erkannten Text
- Installations- und Upgrade-Test in GitHub Actions

## Offen

### Code-Signatur
- Setup und EXE signieren (z. B. Azure Trusted Signing), damit SmartScreen nicht warnt
- Updater prüft zusätzlich die Signatur des heruntergeladenen Setups
- Benötigt: Azure-Konto bzw. Zertifikat

### Komfort
- Dashboard: Status je Profil statt eines Gesamtstatus
- Statistik: aussagekräftige Spalten statt Rohdaten
- Import/Export der Konfiguration (Profile und Regeln)

### Optional
- Dunkles Design und englische Oberfläche (derzeit ausgeblendet)
- KI-Klassifizierung (derzeit ausgeblendet) auf Basis des OCR-Textes
