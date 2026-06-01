@echo off
setlocal EnableDelayedExpansion
title Ontological Studio - Build installer

REM ============================================================
REM  build_installer.bat
REM  1. Publishes the desktop app self-contained (win-x64, Release)
REM  2. Builds the NSIS installer using OntologicalStudio.nsi
REM
REM  Requirements:
REM    - .NET 8 SDK installed (https://dotnet.microsoft.com/download)
REM    - NSIS 3.x installed (https://nsis.sourceforge.io/Download)
REM      (the script also searches the default install path)
REM
REM  Optional argument: version number (defaults to 0.1.0)
REM     build_installer.bat 0.4.0
REM ============================================================

cd /d "%~dp0"

set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=0.1.0"

echo.
echo === Ontological Studio - Installer build ===
echo Version: %VERSION%
echo.

REM ---------------------------------------------------------
REM 1. Check dotnet
REM ---------------------------------------------------------
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet SDK is not installed or not in PATH.
    echo Get it from https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do echo dotnet SDK: %%v

REM ---------------------------------------------------------
REM 2. Locate makensis (NSIS)
REM ---------------------------------------------------------
set "MAKENSIS="
where makensis >nul 2>&1 && set "MAKENSIS=makensis"

if "%MAKENSIS%"=="" (
    if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" set "MAKENSIS=%ProgramFiles(x86)%\NSIS\makensis.exe"
)
if "%MAKENSIS%"=="" (
    if exist "%ProgramFiles%\NSIS\makensis.exe" set "MAKENSIS=%ProgramFiles%\NSIS\makensis.exe"
)
if "%MAKENSIS%"=="" (
    echo [ERROR] makensis.exe not found.
    echo Install NSIS from https://nsis.sourceforge.io/Download or add it to PATH.
    pause
    exit /b 1
)
echo NSIS:       %MAKENSIS%

REM ---------------------------------------------------------
REM 3. Clean and publish
REM ---------------------------------------------------------
echo.
echo === [1/2] Publishing self-contained build (win-x64, Release) ===
if exist publish rmdir /s /q publish
if exist OntologicalStudio-Setup-*.exe del /q OntologicalStudio-Setup-*.exe

dotnet publish "..\OntologicalStudio.Desktop\OntologicalStudio.Desktop.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=false ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    -p:Version=%VERSION% ^
    -p:FileVersion=%VERSION% ^
    -p:AssemblyVersion=%VERSION% ^
    -o publish

if errorlevel 1 (
    echo [ERROR] dotnet publish failed.
    pause
    exit /b 1
)

REM Sanity check
if not exist "publish\OntologicalStudio.Desktop.exe" (
    echo [ERROR] Expected publish\OntologicalStudio.Desktop.exe not found.
    pause
    exit /b 1
)

REM Show approximate published size
for /f "tokens=3" %%s in ('dir /s /-c publish ^| findstr /R "File(s)"') do set "PUBSIZE=%%s"
echo Published bytes: %PUBSIZE%

REM ---------------------------------------------------------
REM 4. Run NSIS
REM ---------------------------------------------------------
echo.
echo === [2/2] Building NSIS installer ===
"%MAKENSIS%" /DOVERRIDE_VERSION=%VERSION% OntologicalStudio.nsi
if errorlevel 1 (
    echo [ERROR] makensis returned an error.
    pause
    exit /b 1
)

echo.
echo ========================================================
echo  Installer ready:
for %%f in (OntologicalStudio-Setup-*.exe) do echo    %%~ff   (%%~zf bytes^)
echo ========================================================
echo.
echo You can now run the .exe to install the app per-user
echo (no admin rights required).
echo.
pause
endlocal
