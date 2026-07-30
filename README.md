# WinUI modular extensions selected by the SDK version

This minimal WinUI 3 app demonstrates how a Shell can build and discover optional `Page` and
`UserControl` extensions without a separate feature flag.

The centrally managed `Microsoft.WindowsAppSDK` version is the only input:

- a stable version such as `2.1.3` builds the core Shell only;
- a prerelease whose suffix starts with `-e`, such as `2.1.4-exp1` or
  `2.1.4-experimental8`, also builds the experimental extensions.

No `IncludeExperimentalApis` property, conditional compilation symbol, empty extension assembly,
or special build command is required.

## Build

Choose the SDK version in `Versions.props`:

```xml
<WindowsAppSDKVersion>2.1.4-experimental8</WindowsAppSDKVersion>
```

Then build normally:

```powershell
dotnet build
```

or:

```powershell
msbuild -restore -p:Configuration=Debug -p:Platform=x64
```

`build.cmd` is a convenience wrapper around the same `dotnet build` operation.

The Shell is unpackaged and self-contained. After a Debug x64 build, run:

```powershell
src\Shell\bin\x64\experimental\Debug\net8.0-windows10.0.22621.0\Shell.exe
```

## How version-driven inclusion works

`Versions.props` contains the single version value:

```xml
<PropertyGroup>
  <WindowsAppSDKVersion>2.1.4-experimental8</WindowsAppSDKVersion>
</PropertyGroup>
```

`Directory.Build.props` derives the extension mode from that value:

```xml
<PropertyGroup>
  <IsExperimentalWindowsAppSDK>
    $([System.Text.RegularExpressions.Regex]::IsMatch(
      '$(WindowsAppSDKVersion)',
      '(?i)^[0-9]+(?:\.[0-9]+)*-e'))
  </IsExperimentalWindowsAppSDK>
</PropertyGroup>
```

`Directory.Packages.props` uses the same value for central package management:

```xml
<ItemGroup>
  <PackageVersion Include="Microsoft.WindowsAppSDK"
                  Version="$(WindowsAppSDKVersion)" />
</ItemGroup>
```

The property is not a second switch. It is derived on every build and should not be set manually.
The regular expression accepts any numeric Windows App SDK version whose prerelease label starts
with `e`.

`Shell.csproj` imports `Extensions.props`, which classifies extensions by maturity, and creates
project references only when the selected SDK can compile them:

```xml
<ItemGroup>
  <StableExtension Include="SomeStableFeature" />
  <ExperimentalExtension Include="HelloUserControl" />
  <ExperimentalExtension Include="HelloPage" />
</ItemGroup>

<ItemGroup>
  <ProjectReference
    Include="@(StableExtension -> '..\Extensions\%(Identity)\Ext.%(Identity).csproj')" />
</ItemGroup>

<ItemGroup Condition="'$(IsExperimentalWindowsAppSDK)' == 'true'">
  <ProjectReference
    Include="@(ExperimentalExtension -> '..\Extensions\%(Identity)\Ext.%(Identity).csproj')" />
</ItemGroup>
```

The extension projects are intentionally not top-level entries in `WinUIModularDemo.sln`.
Otherwise a solution build would invoke them regardless of the conditional references in the
Shell. They enter the MSBuild graph only when the Shell references them:

| Selected Windows App SDK | Shell | Contracts | Experimental `Ext.*` projects |
|---|---:|---:|---:|
| `2.1.3` | built | built | not evaluated or built |
| `2.1.4-exp1` | built | built | built and copied beside the Shell |

## Runtime discovery

Each extension exposes a public entry type decorated with the shared `[NavItem]` attribute:

```csharp
[NavItem("Hello", Icon = NavIcon.Emoji, Order = 10)]
public sealed partial class HelloView : UserControl
{
}

[NavItem("Demo", Icon = NavIcon.Document, Order = 20)]
public sealed partial class DemoPage : Page
{
}
```

At startup, `ModuleLoader` loads `Ext.*.dll` files from the application directory and finds
decorated `Page` and `UserControl` types. `MainWindow` creates navigation items from the discovered
metadata, so it contains no feature-specific type references.

This is build-time modularity, not an untrusted plug-in sandbox. Only deploy extension assemblies
that are produced and trusted with the application.

## `Page` versus `UserControl`

| Entry type | Host | Use it when |
|---|---|---|
| `UserControl` | `ContentControl.Content` | The feature is a self-contained reusable view |
| `Page` | `Frame.Navigate(Type)` | The feature needs navigation parameters, lifecycle, or back stack |

The same discovery contract supports both styles. Only the hosting behavior differs.

## Add an experimental extension

1. Create `src\Extensions\<Name>\Ext.<Name>.csproj`, following either existing extension project.
2. Add a public `Page` or `UserControl` decorated with `[NavItem]`.
3. Add `<ExperimentalExtension Include="<Name>" />` to `src\Shell\Extensions.props`.
4. Run `dotnet build`.

Do not add the project as a top-level project in the solution. The conditional `ProjectReference`
is what keeps it out of stable builds.

To promote an extension to stable, move its item from `ExperimentalExtension` to
`StableExtension`. No source or project-file condition needs to change.

## Project layout

```text
WinUIModularDemo/
  Directory.Build.props
  Directory.Packages.props
  Versions.props
  WinUIModularDemo.sln
  build.cmd
  src/
    Contracts/
      NavItemAttribute.cs
      WinUIModularDemo.Contracts.csproj
    Shell/
      Extensions.props
      MainWindow.xaml
      ModuleLoader.cs
      Shell.csproj
    Extensions/
      HelloPage/
        DemoPage.xaml
        Ext.HelloPage.csproj
      HelloUserControl/
        HelloView.xaml
        Ext.HelloUserControl.csproj
```
