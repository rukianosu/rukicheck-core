@echo off
echo ===================================
echo RukiCheck Portable EXE Build
echo ===================================
echo.

cd /d "%~dp0"

echo [1/3] Restoring packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Restore failed
    pause
    exit /b 1
)

echo.
echo [2/3] Building Release...
dotnet build -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo [3/3] Publishing Portable EXE...
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:DebugType=None /p:DebugSymbols=false
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Publish failed
    pause
    exit /b 1
)

echo.
echo ===================================
echo BUILD SUCCESS!
echo ===================================
echo.
echo Output: src\RukiCheck\bin\Release\net8.0-windows\win-x64\publish\RukiCheck.exe
echo.
pause
