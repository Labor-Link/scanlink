@echo off
echo ============================================
echo   ScanLink - Windows Build Script
echo ============================================
echo.

:: Restore NuGet packages
echo [1/3] Restoring packages...
dotnet restore ScanLinkPrinter.sln
if %ERRORLEVEL% NEQ 0 (
    echo FAILED: Package restore failed
    pause
    exit /b 1
)
echo.

:: Build Release
echo [2/3] Building Release...
dotnet build ScanLinkPrinter.sln --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo FAILED: Build failed
    pause
    exit /b 1
)
echo.

:: Show output location
echo [3/3] Build complete!
echo.
echo ============================================
echo   OUTPUT FILES:
echo   ScanLink.exe        -> ScanLink\bin\Release\ScanLink.exe
echo   ArgoxWorker.exe     -> ScanLinkPrinter.ArgoxWorker\bin\Release\net48\ScanLinkPrinter.ArgoxWorker.exe
echo   Protocol.dll        -> ScanLinkPrinter.Protocol\bin\Release\netstandard2.0\ScanLinkPrinter.Protocol.dll
echo   Argox.dll           -> ScanLinkPrinter.Argox\bin\Release\net9.0\ScanLinkPrinter.Argox.dll
echo ============================================
echo.
pause
