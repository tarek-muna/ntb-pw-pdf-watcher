@echo off
setlocal
set "EXE=%~dp0publish\NTB-PW PDF-Watcher.exe"
if not exist "%EXE%" (
  echo Die EXE wurde noch nicht erstellt. Bitte zuerst build.cmd starten.
  pause
  exit /b 1
)
start "" "%EXE%" --show
