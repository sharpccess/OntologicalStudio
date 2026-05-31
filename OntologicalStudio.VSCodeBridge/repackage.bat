@echo off
setlocal EnableDelayedExpansion
title Ontological Studio Bridge - Build ^& Package

REM ============================================================
REM  repackage.bat
REM  Compiles and packages the VSCode/TRAE extension into a .vsix
REM  Optional: pass an argument to auto-install in your editor.
REM     repackage.bat            -> only build + package
REM     repackage.bat trae       -> also install in TRAE
REM     repackage.bat code       -> also install in VSCode
REM     repackage.bat both       -> install in both
REM ============================================================

cd /d "%~dp0"

echo.
echo === [1/4] Checking Node.js ===
where node >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Node.js is not installed or not in PATH.
    echo Install it from https://nodejs.org/  (LTS) and re-open this window.
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('node --version') do echo Node: %%v
for /f "tokens=*" %%v in ('npm --version') do echo npm:  %%v

echo.
echo === [2/4] Installing dependencies ===
call npm install
if errorlevel 1 (
    echo [ERROR] npm install failed.
    pause
    exit /b 1
)

echo.
echo === [3/4] Compiling TypeScript ===
call npm run compile
if errorlevel 1 (
    echo [ERROR] TypeScript compilation failed.
    pause
    exit /b 1
)

echo.
echo === [4/4] Packaging .vsix ===
REM Clean previous .vsix to avoid confusion
del /q *.vsix 2>nul
call npx --yes @vscode/vsce package --no-yarn
if errorlevel 1 (
    echo [ERROR] vsce package failed.
    pause
    exit /b 1
)

REM Find the produced .vsix
set "VSIX="
for %%f in (*.vsix) do set "VSIX=%%f"
if not defined VSIX (
    echo [ERROR] No .vsix file produced.
    pause
    exit /b 1
)

echo.
echo ========================================================
echo  Package generated: %VSIX%
echo ========================================================

set "TARGET=%~1"
if /i "%TARGET%"=="" goto :done
if /i "%TARGET%"=="trae"  goto :install_trae
if /i "%TARGET%"=="code"  goto :install_code
if /i "%TARGET%"=="both"  goto :install_both
echo [WARN] Unknown target "%TARGET%". Valid: trae ^| code ^| both
goto :done

:install_trae
echo.
echo --- Installing in TRAE ---
where trae >nul 2>&1
if errorlevel 1 (
    echo [WARN] 'trae' command not in PATH. Open TRAE manually and use 'Install from VSIX'.
    goto :done
)
call trae --install-extension "%VSIX%" --force
goto :done

:install_code
echo.
echo --- Installing in VSCode ---
where code >nul 2>&1
if errorlevel 1 (
    echo [WARN] 'code' command not in PATH. Open VSCode manually and use 'Install from VSIX'.
    goto :done
)
call code --install-extension "%VSIX%" --force
goto :done

:install_both
call :install_trae
call :install_code
goto :done

:done
echo.
echo Done. Press any key to close.
pause >nul
endlocal
