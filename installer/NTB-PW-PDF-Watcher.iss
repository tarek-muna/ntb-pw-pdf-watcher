#define MyAppName "NTB-PW PDF-Watcher Professional"
; Kann per ISCC /DMyAppVersion=x.y.z ueberschrieben werden (Release-Workflow nutzt den Git-Tag).
#ifndef MyAppVersion
  #define MyAppVersion "5.0.1"
#endif
#define MyAppPublisher "Netz, Technik, Büro GbR"
#define MyAppExeName "NTB-PW PDF-Watcher.exe"
[Setup]
AppId={{C8C5093C-0A92-47BD-A845-NTBPW3000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\NTB-PW\PDF-Watcher
DefaultGroupName=NTB-PW
OutputDir=..\dist
OutputBaseFilename=NTB-PW-PDF-Watcher-Setup-{#MyAppVersion}
SetupIconFile=..\src\NTBPW.PdfWatcher\Assets\NTB-PW.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
; Signatur von Setup und Deinstallationsprogramm (fuer die intelligente App-Steuerung muessen alle
; ausfuehrbaren Dateien signiert sein). Der Release-Workflow uebergibt /DUseSignTool und das
; Signaturkommando per ISCC /Sntbpw=... (tools\sign-file.ps1).
#ifdef UseSignTool
SignTool=ntbpw
SignedUninstaller=yes
#endif
[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\NTB-PW PDF-Watcher"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\NTB-PW PDF-Watcher"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; GroupDescription: "Zusätzliche Symbole:"
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "NTB-PW PDF-Watcher starten"; Flags: nowait postinstall skipifsilent
