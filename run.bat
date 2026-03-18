@echo off
setlocal

echo ===============================================
echo          ScanLink Application Launcher
echo ===============================================
echo.

:: Set colors
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "NC=[0m"

:: Get the current directory
set "PROJECT_DIR=%~dp0"
cd /d "%PROJECT_DIR%"

set "APP_PATH=ScanLink\bin\x64\Release\ScanLink.exe"

echo %YELLOW%Checking for application...%NC%

if exist "%APP_PATH%" (
    echo %GREEN%✓ ScanLink.exe found%NC%
    for %%i in ("%APP_PATH%") do echo %YELLOW%  Size: %%~zi bytes%NC%
    for %%i in ("%APP_PATH%") do echo %YELLOW%  Modified: %%~ti%NC%
    echo.
    echo %GREEN%Starting ScanLink Application...%NC%
    echo.
    start "" "%APP_PATH%"
    echo %GREEN%Application launched successfully!%NC%
    echo.
    echo Press any key to exit...
    pause >nul
) else (
    echo %RED%✗ ScanLink.exe not found at: %APP_PATH%%NC%
    echo.
    echo %YELLOW%Please run build.bat first to create the application.%NC%
    echo.
    pause
    exit /b 1
)