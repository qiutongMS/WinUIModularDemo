# WinUI extensions with per-feature API requirements

This sample demonstrates a WinUI 3 Shell that builds optional features as independent extension
projects and discovers their `Page` or `UserControl` entry types at runtime.

The sample is self-contained under `src\WinUIExtension`. Root-level `README.md` and `build.cmd`
remain outside the solution root.

## Build

The default Windows App SDK version is configured in
`src\WinUIExtension\Directory.Packages.props`:

```xml
<WindowsAppSDKVersion>2.1.4-experimental8</WindowsAppSDKVersion>
```

Build from the repository root:

```powershell
build.cmd
```

The version can be overridden without editing the project:

```powershell
dotnet build src\WinUIExtension\WinUIModularDemo.sln `
  -c Debug `
  -p:Platform=x64 `
  -p:WindowsAppSDKVersion=2.1.3
```

The unpackaged, self-contained Shell is written to:

```text
src\WinUIExtension\Shell\bin\x64\Debug\net8.0-windows10.0.22621.0\Shell.exe
```

## Extension projects

Features live in independent class libraries under `Extensions`:

```text
Extensions/
  HelloPage/
    Ext.HelloPage.csproj
  HelloUserControl/
    Ext.HelloUserControl.csproj
```

The extension identity must match its folder and assembly suffix:

```text
HelloPage -> Extensions\HelloPage\Ext.HelloPage.csproj -> Ext.HelloPage.dll
```

The solution contains only the Shell. Extension projects enter the MSBuild graph through
conditional `ProjectReference` items.

## Extension API requirements

`Shell\Extensions.props` separates extensions that always build from extensions that currently
require experimental APIs:

```xml
<ItemGroup>
  <!-- StableExtension entries are always referenced. -->

  <ExperimentalExtension Include="HelloUserControl" />
  <ExperimentalExtension Include="HelloPage" />
</ItemGroup>
```

The Shell always references stable extensions:

```xml
<ProjectReference
  Include="@(StableExtension
    -> '..\Extensions\%(Identity)\Ext.%(Identity).csproj')" />
```

Experimental extensions are referenced only when the selected SDK provides experimental APIs:

```xml
<ItemGroup Condition="'$(ExperimentalApisAvailable)' == 'true'">
  <ProjectReference
    Include="@(ExperimentalExtension
      -> '..\Extensions\%(Identity)\Ext.%(Identity).csproj')" />
</ItemGroup>
```

With an experimental SDK, those projects build normally and their outputs are copied beside the
Shell. With a stable SDK, they do not enter the build graph and no extension DLL is produced.

CI can explicitly treat experimental APIs as available:

```powershell
dotnet build src\WinUIExtension\WinUIModularDemo.sln `
  -c Debug `
  -p:Platform=x64 `
  -p:BuildExperimentalExtensions=true
```

The selected SDK must still provide every API used by those extensions.

## Switching SDK versions

Switching from an experimental build to a stable build can leave extension files from the previous
build in the Shell output directory. Before copying the stable Shell output, `Shell.csproj` removes
the DLL, PDB, PRI, and resource directories for the registered experimental extensions. A clean is
therefore not required when switching package versions.

## Convention-based discovery

At startup, `ModuleLoader` loads `Ext.*.dll` files beside the Shell. An extension entry type must
be:

- public
- concrete
- derived from `Page` or `UserControl`
- constructible with a parameterless constructor

Helper `Page` and `UserControl` types should be `internal`.

The Shell hosts discovered types according to their WinUI base type:

| Extension type | Host |
|---|---|
| `Page` | `Frame.Navigate(Type)` |
| `UserControl` | `ContentControl.Content` |

The Shell does not contain feature names or compile-time references to extension UI types.

## Extension-owned resources

`Ext.HelloUserControl` contains its own:

- `Resources.resw`
- XAML
- SVG asset

The build produces `Ext.HelloUserControl.dll` and `Ext.HelloUserControl.pri`. Both are copied beside
the Shell, allowing `x:Uid` strings and relative asset URIs to resolve without Shell-specific
resource registration or copy logic.

## Add an extension

1. Create `Extensions\<Name>\Ext.<Name>.csproj`.
2. Add one public entry `Page` or `UserControl`.
3. Keep helper UI types internal.
4. Add it to `StableExtension` or `ExperimentalExtension` in `Shell\Extensions.props`.
5. Run `build.cmd`.

When the APIs used by an extension become stable, move its item from
`ExperimentalExtension` to `StableExtension`. The extension project and runtime discovery logic do
not change.

## Project layout

```text
WinUIModularDemo/
  .gitignore
  README.md
  build.cmd
  src/
    WinUIExtension/
      Directory.Build.props
      Directory.Packages.props
      WinUIModularDemo.sln
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
          Assets/
            ExtensionAsset.svg
          HelloView.xaml
          Ext.HelloUserControl.csproj
          Resources.resw
```
