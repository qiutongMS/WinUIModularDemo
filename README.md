# WinUI extensions selected by the SDK version

This sample demonstrates a WinUI 3 Shell that conditionally builds and discovers extension
projects containing either a `Page` or a `UserControl`.

The sample project is self-contained under `src\WinUIExtensionDemo`, matching the folder shape
needed to move it into
[WindowsAppSDK-Samples](https://github.com/microsoft/WindowsAppSDK-Samples) later. Root-level
`README.md` and `build.cmd` remain outside the sample project.

## Build

Choose the Windows App SDK version in
`src\WinUIExtensionDemo\Directory.Packages.props`:

```xml
<WindowsAppSDKVersion>2.1.4-experimental8</WindowsAppSDKVersion>
```

Then build from the repository root:

```powershell
dotnet build src\WinUIExtensionDemo\WinUIModularDemo.sln
```

or:

```powershell
build.cmd
```

The unpackaged, self-contained Shell is written to:

```text
src\WinUIExtensionDemo\Shell\bin\x64\Debug\net8.0-windows10.0.22621.0\Shell.exe
```

## Select experimental extensions

`Shell.csproj` includes experimental extension projects when the selected SDK version contains
`-exp`:

```xml
<ItemGroup
  Condition="$([System.String]::Copy('$(WindowsAppSDKVersion)').Contains('-exp')) Or
             '$(BuildExperimentalExtensions)' == 'true'">
  <ProjectReference
    Include="@(ExperimentalExtension -> '..\Extensions\%(Identity)\Ext.%(Identity).csproj')" />
</ItemGroup>
```

CI can explicitly force that build graph:

```powershell
dotnet build src\WinUIExtensionDemo\WinUIModularDemo.sln `
  -p:BuildExperimentalExtensions=true
```

When forcing it, CI must also select a `WindowsAppSDKVersion` that provides every API used by the
experimental extensions.

The extension list lives in `Shell\Extensions.props`:

```xml
<ItemGroup>
  <!-- StableExtension entries are always referenced. -->
  <ExperimentalExtension Include="HelloUserControl" />
  <ExperimentalExtension Include="HelloPage" />
</ItemGroup>
```

The extension projects are intentionally not top-level solution entries. They enter the MSBuild
graph only through the conditional `ProjectReference` items in the Shell.

After changing between stable and experimental SDK versions in an existing checkout, run
`dotnet clean src\WinUIExtensionDemo\WinUIModularDemo.sln` once before rebuilding. This removes
generated XAML metadata and copied extension files from the previous graph.

## Convention-based discovery

No shared contract or registration attribute is required. At startup, `ModuleLoader` loads
`Ext.*.dll` files beside the Shell and treats every public, concrete `Page` or `UserControl` with
a parameterless constructor as an entry type.

Entry titles are derived by removing the `Page` or `View` suffix:

| Extension type | Navigation title | Host |
|---|---|---|
| `Ext.HelloPage.DemoPage` | Demo | `Frame.Navigate(Type)` |
| `Ext.HelloUserControl.HelloView` | Hello | `ContentControl.Content` |

Keep helper `Page` and `UserControl` types `internal` so they are not discovered as entries.

## Extension resources

`Ext.HelloUserControl` demonstrates an extension-owned resource dictionary:

```xml
<ResourceDictionary Source="ExtensionResources.xaml" />
```

`ExtensionResources.xaml` defines `ExtensionStatusTextStyle`, and `HelloView.xaml` consumes it with
`{StaticResource ExtensionStatusTextStyle}`. Building the extension produces its own `.pri`
containing the compiled XAML resources. The project reference copies the extension DLL and PRI
beside the Shell, and the relative dictionary URI resolves within the extension when `HelloView`
is created.

The experimental Shell output therefore contains:

```text
Ext.HelloUserControl.dll
Ext.HelloUserControl.pri
```

The extension PRI indexes `ExtensionResources.xbf` and `HelloView.xbf` under the
`Ext.HelloUserControl` resource map.

## Add an experimental extension

1. Create `src\WinUIExtensionDemo\Extensions\<Name>\Ext.<Name>.csproj`.
2. Add one public entry `Page` or `UserControl`; keep helper UI types internal.
3. Add `<ExperimentalExtension Include="<Name>" />` to
   `src\WinUIExtensionDemo\Shell\Extensions.props`.
4. Run `build.cmd`.

To promote an extension to stable, move its item from `ExperimentalExtension` to
`StableExtension`.

## Project layout

```text
WinUIModularDemo/
  README.md
  build.cmd
  src/
    WinUIExtensionDemo/
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
          ExtensionResources.xaml
          HelloView.xaml
          Ext.HelloUserControl.csproj
```
