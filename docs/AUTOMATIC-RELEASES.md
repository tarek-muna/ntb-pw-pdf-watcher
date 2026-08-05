# Automatische Releases

Ein Release wird automatisch erstellt, wenn ein Commit nach `main` gemergt wird und die Version in
`src/NTBPW.PdfWatcher/NTBPW.PdfWatcher.csproj` noch nicht als GitHub Release vorhanden ist.

## Version erhöhen

Vor dem Merge diese Werte gemeinsam ändern:

```xml
<Version>5.0.0-alpha.2</Version>
<AssemblyVersion>5.0.0.0</AssemblyVersion>
<FileVersion>5.0.0.0</FileVersion>
<InformationalVersion>5.0.0-alpha.2</InformationalVersion>
```

Der Workflow erstellt anschließend automatisch:

- Tag `v<Version>`
- GitHub Release mit automatisch generierten Hinweisen
- Windows-Installer
- SHA-256-Prüfsumme

Bleibt die Version unverändert, wird kein zweiter Release erstellt.
