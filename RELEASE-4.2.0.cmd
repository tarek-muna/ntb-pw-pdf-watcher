@echo off
setlocal
cd /d "%~dp0"
where git >nul 2>nul || (echo Git wurde nicht gefunden.& pause & exit /b 1)
git tag -a v4.2.0 -m "NTB-PW PDF-Watcher Professional 4.2.0"
git push origin v4.2.0
if errorlevel 1 (
  echo Release-Tag konnte nicht hochgeladen werden.
  pause
  exit /b 1
)
echo.
echo Tag v4.2.0 wurde hochgeladen. GitHub Actions erstellt jetzt den Release.
pause
