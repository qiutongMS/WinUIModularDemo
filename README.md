# WinUI extensions selected by the SDK version

This sample demonstrates a WinUI 3 Shell that conditionally builds and composes extension
projects containing either a `Page` or a `UserControl`.

The sample is self-contained under `src\WinUIExtension`. Root-level `README.md` and `build.cmd`
remain outside the sample project.

## Build

Choose the Windows App SDK version in
`src\WinUIExtension\Directory.Packages.props`:

```xml
<WindowsAppSDKVersion>2.1.4-experimental8</WindowsAppSDKVersion>
```

Then build from the repository root:

```powershell
dotnet build src\WinUIExtension\WinUIExtensionDemo.slnf
```

or:

```powershell
build.cmd
```

The unpackaged, self-contained Shell is written to:

```text
src\WinUIExtension\Shell\bin\x64\Debug\net8.0-windows10.0.22621.0\Shell.exe
```

## Select experimental extensions

`Shell.csproj` includes the experimental extension projects and their direct composition source
when the selected SDK version contains `-exp`:

```xml
<ItemGroup
  Condition="$([System.String]::Copy('$(WindowsAppSDKVersion)').Contains('-exp')) Or
             '$(BuildExperimentalExtensions)' == 'true'">
  <Compile Include="..\Extensions\HelloPage\ShellExtension.cs"
           Link="Extensions\HelloPage\ShellExtension.cs" />
  <Compile Include="..\Extensions\HelloUserControl\ShellExtension.cs"
           Link="Extensions\HelloUserControl\ShellExtension.cs" />
  <ProjectReference Include="..\Extensions\HelloPage\Ext.HelloPage.csproj" />
  <ProjectReference Include="..\Extensions\HelloUserControl\Ext.HelloUserControl.csproj" />
</ItemGroup>
```

CI can explicitly force that build graph:

```powershell
dotnet build src\WinUIExtension\WinUIExtensionDemo.slnf `
  -p:BuildExperimentalExtensions=true
```

When forcing it, CI must also select a `WindowsAppSDKVersion` that provides every API used by the
experimental extensions.

The single conditional block includes each extension's Shell composition source and project
reference. On a stable SDK those source files and projects are both absent.

### Solution visibility follows project references

Visual Studio `.sln` membership is static; it does not evaluate MSBuild `Condition` attributes to
decide which projects appear. `WinUIModularDemo.sln` is therefore the complete project catalog: it
lists Shell and both extension projects.

`WinUIExtensionDemo.slnf` is the normal entry point. It initially selects only Shell, and Visual
Studio/MSBuild loads project dependencies from Shell's evaluated `ProjectReference` items:

- Stable SDK: only Shell is loaded and built.
- Experimental SDK or `BuildExperimentalExtensions=true`: both extension projects are loaded and
  built with Shell.

Open or build the `.slnf` to preserve that on-demand behavior. Opening or building the complete
`.sln` intentionally loads/builds all cataloged projects.

After changing between stable and experimental SDK versions in an existing checkout, run
`dotnet clean src\WinUIExtension\WinUIExtensionDemo.slnf` once before rebuilding. This removes
generated XAML metadata and copied extension files from the previous graph.

## Native cross-project composition

WinUI controls do not require a loader. Each extension folder owns a small Shell composition file:

```csharp
partial void AddHelloPageMenuItem()
{
    AddFeature(nameof(DemoPage), ..., () => ShowPage(typeof(DemoPage)));
}
```

The extension `.csproj` excludes that file from its own assembly. Shell conditionally links and
compiles it alongside the matching `ProjectReference`. `Frame.Navigate(pageType)` is the native
Page path, and assigning `new HelloView()` to `ContentControl.Content` is the native UserControl
path. There is no DLL scanning, reflection, `Assembly.LoadFrom`, or `Activator.CreateInstance`.

On a stable SDK, both partial methods have no implementation, so the C# compiler removes their
calls. Shell remains independent of extension types and assemblies.

## Extension resources

`Ext.HelloUserControl` follows the WinUI class-library pattern used by WindowsAppSDK-Samples: an
extension-owned `Resources.resw` file consumed by a specifically named `TextBlock` through
`x:Uid`:

```xml
<TextBlock
  x:Uid="ExtensionResourceText"
  AutomationProperties.AutomationId="HelloView_ResourceText" />
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

## Add an experimental extension

1. Create `src\WinUIExtension\Extensions\<Name>\Ext.<Name>.csproj`.
2. Add a `ShellExtension.cs` partial implementation in that extension folder.
3. Add the project reference to the conditional `ItemGroup` in `Shell.csproj`.
4. Include `ShellExtension.cs` in the same conditional `ItemGroup`.
5. Exclude `ShellExtension.cs` from the extension project's own compilation.
6. Run `build.cmd`.

To promote an extension to stable, make its project reference and composition source unconditional.

## Project layout

```text
WinUIModularDemo/
  README.md
  build.cmd
  src/
    WinUIExtension/
      Directory.Build.props
      Directory.Packages.props
      WinUIExtensionDemo.slnf
      WinUIModularDemo.sln
      Shell/
        MainWindow.xaml
        Shell.csproj
      Extensions/
        HelloPage/
          DemoPage.xaml
          Ext.HelloPage.csproj
          ShellExtension.cs
        HelloUserControl/
          Assets/
            ExtensionAsset.svg
          HelloView.xaml
          Ext.HelloUserControl.csproj
          Resources.resw
          ShellExtension.cs
```
