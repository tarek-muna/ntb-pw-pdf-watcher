NTB-PW PDF-Watcher Professional 4.2.0
Netz, Technik, Büro GbR | Tarek Muna 2026

NEU IN 4.2.0 – GITHUB-UPDATES
- Update-Prüfung über GitHub Releases.
- Repository: tarek-muna/ntb-pw-pdf-watcher
- Anzeige von installierter und verfügbarer Version.
- Release Notes direkt in der Anwendung.
- Download-Fortschritt.
- Verpflichtende SHA-256-Prüfung vor der Installation.
- Manuelle Prüfung über Mehr > Nach Updates suchen.
- Optionale automatische Prüfung beim Programmstart.
- GitHub Actions erstellt Setup, Prüfsumme und Release automatisch.

ERSTER UPLOAD ZU GITHUB
1. ZIP vollständig entpacken.
2. UPLOAD-TO-GITHUB.cmd starten.
3. Bei GitHub anmelden, falls Git danach fragt.

ERSTEN RELEASE ERSTELLEN
1. Nach erfolgreichem Upload RELEASE-4.2.0.cmd starten.
2. Unter GitHub > Actions den Build abwarten.
3. Unter GitHub > Releases erscheint anschließend v4.2.0.

LOKALER BUILD
1. .NET 8 SDK installieren.
2. build.cmd starten.
3. Optional Inno Setup 6 installieren und build-setup.cmd starten.

AUSGABEN
publish\NTB-PW PDF-Watcher.exe
dist\NTB-PW-PDF-Watcher-Setup-4.2.0.exe
