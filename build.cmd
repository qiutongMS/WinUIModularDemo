@echo off
setlocal
REM Build the WinUI demo (a plain, single-project WinUI app).

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [build] .NET SDK ^(dotnet^) not found on PATH.
    exit /b 1
)

dotnet build src\WinUIExtension\WinUIModularDemo.sln -p:Platform=x64 %1 %2 %3 %4
exit /b %errorlevel%
