@echo off
setlocal
cd /d "%~dp0"
where git >nul 2>nul || (echo Git wurde nicht gefunden.& pause & exit /b 1)

rem Version aus der Projektdatei lesen, damit Tag und Setup immer zusammenpassen.
for /f "usebackq delims=" %%v in (`powershell.exe -NoProfile -Command "([xml](Get-Content '%~dp0src\NTBPW.PdfWatcher\NTBPW.PdfWatcher.csproj')).Project.PropertyGroup.Version"`) do set VERSION=%%v
if "%VERSION%"=="" (echo Version konnte nicht aus der csproj gelesen werden.& pause & exit /b 1)

git fetch origin --tags
for /f %%b in ('git rev-parse --abbrev-ref HEAD') do set BRANCH=%%b
if not "%BRANCH%"=="main" (echo Bitte auf dem Branch main ausfuehren ^(aktuell: %BRANCH%^).& pause & exit /b 1)
git status --porcelain | findstr . >nul && (echo Es gibt nicht committete Aenderungen.& pause & exit /b 1)
git rev-parse -q --verify "refs/tags/v%VERSION%" >nul && (echo Tag v%VERSION% existiert bereits. Bitte zuerst die Version in der csproj erhoehen.& pause & exit /b 1)

echo Release v%VERSION% wird erstellt ...
git tag -a v%VERSION% -m "NTB-PW PDF-Watcher Professional %VERSION%"
git push origin v%VERSION%
if errorlevel 1 (
  echo Release-Tag konnte nicht hochgeladen werden.
  pause
  exit /b 1
)
echo.
echo Tag v%VERSION% wurde hochgeladen. GitHub Actions testet, baut und veroeffentlicht jetzt den Release.
pause
