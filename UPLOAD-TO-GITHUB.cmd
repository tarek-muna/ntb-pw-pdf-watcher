@echo off
setlocal
cd /d "%~dp0"
where git >nul 2>nul || (echo Git wurde nicht gefunden.& pause & exit /b 1)
if not exist ".git" git init

git add .
git commit -m "NTB-PW PDF-Watcher Professional 4.2.0 mit GitHub-Updater"
git branch -M main

git remote get-url origin >nul 2>nul
if errorlevel 1 (
  git remote add origin https://github.com/tarek-muna/ntb-pw-pdf-watcher.git
) else (
  git remote set-url origin https://github.com/tarek-muna/ntb-pw-pdf-watcher.git
)

git push -u origin main
if errorlevel 1 (
  echo.
  echo Der Push ist fehlgeschlagen. Bitte GitHub-Anmeldung oder Berechtigung pruefen.
  pause
  exit /b 1
)

echo.
echo Projekt wurde nach GitHub hochgeladen.
pause
