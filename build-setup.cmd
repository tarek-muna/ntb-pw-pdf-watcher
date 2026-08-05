@echo off
setlocal
cd /d "%~dp0"
call "%~dp0build.cmd"
if errorlevel 1 exit /b 1
set ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe
if not exist "%ISCC%" set ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe
if not exist "%ISCC%" (echo Inno Setup 6 wurde nicht gefunden.& pause & exit /b 1)
"%ISCC%" "%~dp0installer\NTB-PW-PDF-Watcher.iss"
if errorlevel 1 pause & exit /b 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$f=Get-ChildItem '%~dp0dist\NTB-PW-PDF-Watcher-Setup-4.2.0.exe'; $h=(Get-FileHash $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant(); ($h+'  '+$f.Name) | Set-Content -Encoding Ascii ($f.FullName+'.sha256')"
if errorlevel 1 pause & exit /b 1
echo.
echo Erstellt:
echo %~dp0dist\NTB-PW-PDF-Watcher-Setup-4.2.0.exe
echo %~dp0dist\NTB-PW-PDF-Watcher-Setup-4.2.0.exe.sha256
pause
