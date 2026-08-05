@echo off
setlocal
cd /d "%~dp0"
where git >nul 2>nul || (echo Git wurde nicht gefunden.& pause & exit /b 1)
git tag -a v5.0.0 -m "NTB-PW PDF-Watcher Professional 5.0.0"
git push origin v5.0.0
if errorlevel 1 (
  echo Release-Tag konnte nicht hochgeladen werden.
  pause
  exit /b 1
)
echo.
echo Tag v5.0.0 wurde hochgeladen. GitHub Actions erstellt jetzt den Release.
pause
