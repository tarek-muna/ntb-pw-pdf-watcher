@echo off
setlocal
cd /d "%~dp0"
call "%~dp0build.cmd"
if errorlevel 1 exit /b 1
set ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe
if not exist "%ISCC%" set ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe
if not exist "%ISCC%" (echo Inno Setup 6 wurde nicht gefunden.& pause & exit /b 1)
for /f "usebackq delims=" %%v in (`powershell.exe -NoProfile -Command "([xml](Get-Content '%~dp0src\NTBPW.PdfWatcher\NTBPW.PdfWatcher.csproj')).Project.PropertyGroup.Version"`) do set APPVERSION=%%v
if "%APPVERSION%"=="" (echo Version konnte nicht aus der csproj gelesen werden.& pause & exit /b 1)
"%ISCC%" /DMyAppVersion=%APPVERSION% "%~dp0installer\NTB-PW-PDF-Watcher.iss"
if errorlevel 1 pause & exit /b 1
set SETUP=%~dp0dist\NTB-PW-PDF-Watcher-Setup-%APPVERSION%.exe
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$f=Get-Item '%SETUP%'; $h=(Get-FileHash $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); ($h+'  '+$f.Name) | Set-Content -Encoding Ascii ($f.FullName+'.sha256')"
if errorlevel 1 pause & exit /b 1
echo.
echo Erstellt:
echo %SETUP%
echo %SETUP%.sha256
pause
