@echo off
setlocal
title Click Counter
set "ROOT=%~dp0click-counter"
set "EXE=%ROOT%\bin\Debug\net8.0-windows\ClickCounter.exe"

if exist "%EXE%" (
    start "" "%EXE%"
    exit /b 0
)

cd /d "%ROOT%"
echo Executable not found, building and running with dotnet...
dotnet run --project ClickCounter.csproj
if errorlevel 1 (
    echo.
    echo Build or run failed. Make sure the .NET SDK is installed and dotnet is on PATH.
    pause
    exit /b 1
)
endlocal
