@echo off
setlocal

echo ===============================================
echo          ScanLink Project Build Script
echo ===============================================
echo.

:: Set colors for better visibility
:: Green for success, Red for errors, Yellow for warnings
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "NC=[0m"

:: Get the current directory
set "PROJECT_DIR=%~dp0"
cd /d "%PROJECT_DIR%"

echo %YELLOW%Project Directory: %PROJECT_DIR%%NC%
echo.

:: Check if dotnet is available
echo %YELLOW%Checking .NET SDK...%NC%
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo %RED%Error: .NET SDK is not installed or not in PATH%NC%
    echo Please install .NET SDK and try again.
    pause
    exit /b 1
)

echo %GREEN%.NET SDK found: %NC%
dotnet --version
echo.

:: Clean previous builds
echo %YELLOW%Cleaning previous builds...%NC%
dotnet clean ScanLinkPrinter.sln --configuration Release --verbosity minimal
if errorlevel 1 (
    echo %RED%Warning: Clean operation had issues%NC%
) else (
    echo %GREEN%Clean completed successfully%NC%
)
echo.

:: Restore packages
echo %YELLOW%Restoring NuGet packages...%NC%
dotnet restore ScanLinkPrinter.sln --force
if errorlevel 1 (
    echo %RED%Error: Package restore failed%NC%
    pause
    exit /b 1
)
echo %GREEN%Package restore completed%NC%
echo.

:: Build individual projects in dependency order
echo %YELLOW%Building projects in dependency order...%NC%
echo.

echo %YELLOW%[1/4] Building ScanLinkPrinter.Protocol (netstandard2.0)...%NC%
dotnet build "ScanLinkPrinter.Protocol/ScanLinkPrinter.Protocol.csproj" --configuration Release --verbosity minimal
if errorlevel 1 (
    echo %RED%Error: ScanLinkPrinter.Protocol build failed%NC%
    pause
    exit /b 1
)
echo %GREEN%ScanLinkPrinter.Protocol build completed%NC%
echo.

echo %YELLOW%[2/4] Building ScanLinkPrinter.Argox (net9.0)...%NC%
dotnet build "ScanLinkPrinter.Argox/ScanLinkPrinter.Argox.csproj" --configuration Release --verbosity minimal
if errorlevel 1 (
    echo %RED%Error: ScanLinkPrinter.Argox build failed%NC%
    pause
    exit /b 1
)
echo %GREEN%ScanLinkPrinter.Argox build completed%NC%
echo.

echo %YELLOW%[3/4] Building ScanLinkPrinter.ArgoxWorker (net48, x64)...%NC%
dotnet build "ScanLinkPrinter.ArgoxWorker/ScanLinkPrinter.ArgoxWorker.csproj" --configuration Release --verbosity minimal -p:Platform=x64
if errorlevel 1 (
    echo %RED%Error: ScanLinkPrinter.ArgoxWorker build failed%NC%
    pause
    exit /b 1
)
echo %GREEN%ScanLinkPrinter.ArgoxWorker build completed%NC%
echo.

echo %YELLOW%[4/4] Building ScanLink Main Application (net48, x64)...%NC%
dotnet build "ScanLink/ScanLink.csproj" --configuration Release --verbosity minimal -p:Platform=x64
if errorlevel 1 (
    echo %RED%Error: ScanLink main application build failed%NC%
    pause
    exit /b 1
)
echo %GREEN%ScanLink main application build completed%NC%
echo.

:: Display build results
echo ===============================================
echo %GREEN%           BUILD COMPLETED SUCCESSFULLY%NC%
echo ===============================================
echo.

echo %YELLOW%Build Outputs:%NC%
echo.

if exist "ScanLink\bin\x64\Release\ScanLink.exe" (
    echo %GREEN%✓ ScanLink.exe%NC% - Main Application
    for %%i in ("ScanLink\bin\x64\Release\ScanLink.exe") do echo   Size: %%~zi bytes
    for %%i in ("ScanLink\bin\x64\Release\ScanLink.exe") do echo   Modified: %%~ti
) else (
    echo %RED%✗ ScanLink.exe - NOT FOUND%NC%
)
echo.

if exist "ScanLinkPrinter.ArgoxWorker\bin\x64\Release\net48\ScanLinkPrinter.ArgoxWorker.exe" (
    echo %GREEN%✓ ScanLinkPrinter.ArgoxWorker.exe%NC% - Worker Process
    for %%i in ("ScanLinkPrinter.ArgoxWorker\bin\x64\Release\net48\ScanLinkPrinter.ArgoxWorker.exe") do echo   Size: %%~zi bytes
) else (
    echo %RED%✗ ScanLinkPrinter.ArgoxWorker.exe - NOT FOUND%NC%
)
echo.

if exist "ScanLinkPrinter.Argox\bin\Release\net9.0\ScanLinkPrinter.Argox.dll" (
    echo %GREEN%✓ ScanLinkPrinter.Argox.dll%NC% - Library
    for %%i in ("ScanLinkPrinter.Argox\bin\Release\net9.0\ScanLinkPrinter.Argox.dll") do echo   Size: %%~zi bytes
) else (
    echo %RED%✗ ScanLinkPrinter.Argox.dll - NOT FOUND%NC%
)
echo.

if exist "ScanLinkPrinter.Protocol\bin\Release\netstandard2.0\ScanLinkPrinter.Protocol.dll" (
    echo %GREEN%✓ ScanLinkPrinter.Protocol.dll%NC% - Core Library
    for %%i in ("ScanLinkPrinter.Protocol\bin\Release\netstandard2.0\ScanLinkPrinter.Protocol.dll") do echo   Size: %%~zi bytes
) else (
    echo %RED%✗ ScanLinkPrinter.Protocol.dll - NOT FOUND%NC%
)
echo.

echo %YELLOW%Main Application Location:%NC%
echo %GREEN%ScanLink\bin\x64\Release\ScanLink.exe%NC%
echo.

echo %YELLOW%Build completed at: %NC%%date% %time%
echo.

echo Press any key to exit...
pause >nul