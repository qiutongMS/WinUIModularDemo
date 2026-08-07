@echo off
setlocal

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [build] .NET SDK ^(dotnet^) not found on PATH.
    exit /b 1
)

dotnet build "%~dp0src\WinUIExtensionDemo\WinUIExtensionDemo.slnf" -c Debug -p:Platform=x64
exit /b %errorlevel%
