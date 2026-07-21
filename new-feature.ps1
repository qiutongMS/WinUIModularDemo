<#
.SYNOPSIS
  Scaffold a new experimental feature project (with XAML). One command; no edits to the Shell.

.EXAMPLE
  .\new-feature.ps1 -Name Reports            # UserControl feature (default)
  .\new-feature.ps1 -Name Report -Kind Page  # Page feature (Frame-hosted lifecycle)
#>
param(
    [Parameter(Mandatory = $true)] [string] $Name,
    [ValidateSet('View', 'Page')] [string] $Kind = 'View'
)

$ErrorActionPreference = 'Stop'
$root     = $PSScriptRoot
$projName = "Experimental.$Name"
$projDir  = Join-Path $root "src\Experimental\$projName"
$typeName = "$Name$Kind"                         # e.g. ReportsView / ReportPage
$rootElem = if ($Kind -eq 'Page') { 'Page' } else { 'UserControl' }

if (Test-Path $projDir) { throw "Project already exists: $projDir" }
New-Item -ItemType Directory -Force -Path $projDir | Out-Null

# ---- .csproj (WinUI class library) ----
@"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>$projName</RootNamespace>
    <UseWinUI>true</UseWinUI>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <Platforms>x64;ARM64</Platforms>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.250108002" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.1742" />
  </ItemGroup>

</Project>
"@ | Set-Content -Encoding UTF8 (Join-Path $projDir "$projName.csproj")

# ---- XAML ----
# "View" is only a naming convention. The actual host behavior comes from the REAL type:
# UserControl => drop into a ContentControl
# Page        => navigate in a Frame
$hostLine = if ($Kind -eq 'Page') {
    'The Shell hosts me via Frame.Navigate(typeof(' + $typeName + ')), so I get OnNavigatedTo.'
} else {
    'The Shell hosts me by setting ContentControl.Content = new ' + $typeName + '(). No Page, no Frame.'
}
@"
<?xml version="1.0" encoding="utf-8"?>
<$rootElem
    x:Class="$projName.$typeName"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <StackPanel Spacing="12">
        <TextBlock Text="$Name" Style="{ThemeResource TitleTextBlockStyle}" />
        <TextBlock TextWrapping="Wrap" Text="$hostLine" />
        <TextBlock x:Name="Status" Foreground="{ThemeResource SystemAccentColor}" />
    </StackPanel>
</$rootElem>
"@ | Set-Content -Encoding UTF8 (Join-Path $projDir "$typeName.xaml")

# ---- code-behind ----
$navUsing  = if ($Kind -eq 'Page') { "`nusing Microsoft.UI.Xaml.Navigation;" } else { '' }
$lifecycle = if ($Kind -eq 'Page') {
@"

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Status.Text = `$"OnNavigatedTo fired at {DateTime.Now:HH:mm:ss}";
    }
"@
} else { '' }
@"
using System;
using Microsoft.UI.Xaml.Controls;$navUsing

namespace $projName;

// $hostLine
public sealed partial class $typeName : $rootElem
{
    public $typeName()
    {
        this.InitializeComponent();
    }$lifecycle
}
"@ | Set-Content -Encoding UTF8 (Join-Path $projDir "$typeName.xaml.cs")

# Best-effort: register in the solution so VS shows it (build still driven by the flag).
$solution = Get-ChildItem $root -Filter 'WinUIModularDemo.sln*' | Select-Object -First 1
if ($solution) { dotnet sln $solution.FullName add (Join-Path $projDir "$projName.csproj") | Out-Null }

Write-Host "Created $projName  ($typeName : $rootElem)" -ForegroundColor Green
Write-Host "Build it in:  build.cmd experimental"
