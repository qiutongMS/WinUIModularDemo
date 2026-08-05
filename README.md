# WinUI extensions with per-feature API requirements

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

## Extension API requirements

Extensions are optional modules; they are not inherently experimental. The extension list lives
in `Shell\Extensions.props`, with a separate property for extensions that currently require
experimental APIs:

```xml
<PropertyGroup>
  <ExtensionsRequiringExperimentalApis>HelloUserControl;HelloPage</ExtensionsRequiringExperimentalApis>
</PropertyGroup>

<ItemGroup>
  <Extension Include="HelloUserControl" />
  <Extension Include="HelloPage" />
</ItemGroup>
```

When the selected `WindowsAppSDKVersion` contains `-exp`, those extensions compile normally and
the Shell discovers their public `Page` or `UserControl` types at startup. With a stable SDK,
extensions that require experimental APIs compile as empty placeholder assemblies, so they remain
visible in the solution but contribute no navigation entries.

CI can explicitly force experimental APIs to be treated as available:

```powershell
dotnet build src\WinUIExtensionDemo\WinUIModularDemo.sln `
  -p:BuildExperimentalExtensions=true
```

When forcing it, CI must also select a `WindowsAppSDKVersion` that provides every API used by
extensions listed in `ExtensionsRequiringExperimentalApis`.

## Convention-based discovery

No shared contract or registration attribute is required. At startup, `ModuleLoader` loads
`Ext.*.dll` files beside the Shell and treats every public, concrete `Page` or `UserControl` with
a parameterless constructor as an entry type.

The Shell uses the type name as the navigation title and hosts it according to its WinUI base
type:

| Extension type | Host |
|---|---|
| `Ext.HelloPage.DemoPage` | `Frame.Navigate(Type)` |
| `Ext.HelloUserControl.HelloView` | `ContentControl.Content` |

Keep helper `Page` and `UserControl` types `internal` so they are not discovered as entries.

## Extension resources

`Ext.HelloUserControl` follows the WinUI class-library pattern used by WindowsAppSDK-Samples: an
extension-owned `Resources.resw` file consumed by a specifically named `TextBlock` through
`x:Uid`:

```xml
<!-- ExtensionResourceText.Text is supplied by this project's Resources.resw. -->
<TextBlock
  x:Uid="ExtensionResourceText"
  AutomationProperties.AutomationId="HelloView_ResourceText" />
```

```xml
<data name="ExtensionResourceText.Text" xml:space="preserve">
  <value>This text is loaded from the extension's Resources.resw.</value>
</data>
```

Building the extension produces its own `.pri` containing the compiled resource. The project
reference copies the extension DLL and PRI beside the Shell, so the text resolves from the
extension resource map when `HelloView` is created.

The same extension also owns `Assets\ExtensionAsset.svg` and displays it with a relative URI:

```xml
<Image Source="Assets/ExtensionAsset.svg" />
```

The SVG is indexed in the extension PRI and resolves relative to `HelloView.xaml`; the Shell does
not need to know about or copy individual extension resources.

The experimental Shell output therefore contains:

```text
Ext.HelloUserControl.dll
Ext.HelloUserControl.pri
```

## Add an experimental extension

1. Create `src\WinUIExtensionDemo\Extensions\<Name>\Ext.<Name>.csproj`.
2. Add one public entry `Page` or `UserControl`; keep helper UI types internal.
3. Import `..\Extension.Build.props` from the extension project.
4. Add `<Extension Include="<Name>" />` and, if needed, add `<Name>` to
   `ExtensionsRequiringExperimentalApis` in
   `src\WinUIExtensionDemo\Shell\Extensions.props`.
5. Add the extension project to `src\WinUIExtensionDemo\WinUIModularDemo.sln`.
6. Run `build.cmd`.

To promote an extension to stable, remove its name from `ExtensionsRequiringExperimentalApis`. No
file moves or Shell changes are required.

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
        Extension.Build.props
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
