@echo off
REM  build.cmd              -> core only (no experimental features)
REM  build.cmd experimental -> core + every project under src\Experimental
REM
REM  Uses MSBuild (located via vswhere) because WinUI class libraries with XAML need the
REM  MSIX/PRI build tasks that ship with VS / VS Build Tools. `dotnet build` also works on
REM  a .NET SDK that includes those tasks.
setlocal enabledelayedexpansion
set "SHELL_PROJ=%~dp0src\Shell\Shell.csproj"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

set "MSBUILD="
if exist "%VSWHERE%" (
  for /f "usebackq delims=" %%m in (`"%VSWHERE%" -prerelease -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    if not defined MSBUILD set "MSBUILD=%%m"
  )
)
if not defined MSBUILD (
  echo [build] Could not locate MSBuild via vswhere.
  echo         Install Visual Studio or "Build Tools for Visual Studio" with the MSBuild component.
  exit /b 1
)
echo [build] Using: %MSBUILD%

if /I "%~1"=="experimental" (
  echo === Building CORE + EXPERIMENTAL ===
  "%MSBUILD%" "%SHELL_PROJ%" /restore /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /p:IncludeExperimental=true /v:minimal /nologo
) else (
  echo === Building CORE only ===
  "%MSBUILD%" "%SHELL_PROJ%" /restore /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /v:minimal /nologo
)
