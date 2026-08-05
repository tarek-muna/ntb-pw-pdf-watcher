@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul || (echo .NET 8 SDK wurde nicht gefunden.& pause & exit /b 1)
dotnet publish ".\src\NTBPW.PdfWatcher\NTBPW.PdfWatcher.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ".\publish"
if errorlevel 1 pause & exit /b 1
echo.
echo Erstellt: %CD%\publish\NTB-PW PDF-Watcher.exe
pause
