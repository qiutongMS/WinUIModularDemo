@echo off
setlocal
REM  build.cmd              -> STABLE flavor  (experimental extensions compile to empty DLLs; app shows core only)
REM  build.cmd experimental -> EXPERIMENTAL flavor (extensions ship real code and appear in the menu)
REM
REM  Both build the whole SOLUTION on purpose: a solution build compiles every project it lists,
REM  which is exactly why the experimental extensions use the empty-DLL technique (see their .csproj).

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [build] .NET SDK ^(dotnet^) not found on PATH.
    exit /b 1
)

if /I "%~1"=="experimental" (
    echo === Building EXPERIMENTAL flavor ===
    dotnet build "%~dp0WinUIModularDemo.sln" -c Debug -p:Platform=x64 -p:IncludeExperimentalApis=true
) else (
    echo === Building STABLE flavor ===
    dotnet build "%~dp0WinUIModularDemo.sln" -c Debug -p:Platform=x64 -p:IncludeExperimentalApis=false
)
exit /b %errorlevel%
