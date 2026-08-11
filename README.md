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
dotnet build src\WinUIExtension\WinUIModularDemo.sln
```

or:

```powershell
build.cmd
```

The unpackaged, self-contained Shell is written to:

```text
src\WinUIExtension\Shell\bin\x64\Debug\net8.0-windows10.0.22621.0\Shell.exe
```

## Select extensions by API requirements

The Shell includes an extension's project and direct composition source when the selected SDK
provides the APIs it requires:

```xml
<ItemGroup Condition="'$(ExperimentalApisAvailable)' == 'true'">
  <Compile Include="..\Extensions\HelloPage\ShellExtension.cs"
           Link="Extensions\HelloPage\ShellExtension.cs" />
  <ProjectReference Include="..\Extensions\HelloPage\Ext.HelloPage.csproj" />
</ItemGroup>
```

The matching `Compile` and `ProjectReference` entries keep composition compiler-checked without a
runtime loader. `ExtensionRequiringExperimentalApis` lists the same extension identities for
stale-output cleanup when switching back to a stable SDK.

CI can explicitly force that build graph:

```powershell
dotnet build src\WinUIExtension\WinUIModularDemo.sln `
  -p:BuildExperimentalExtensions=true
```

When forcing it, CI must also select a `WindowsAppSDKVersion` that provides every API used by the
experimental extensions.

On a stable SDK, sources and projects that require experimental APIs are absent. When an
extension's APIs stabilize, move its `Compile` and `ProjectReference` entries outside the
conditional ItemGroup and remove its identity from `ExtensionRequiringExperimentalApis`.

### Solution build entry point

`WinUIModularDemo.sln` contains only Shell as the solution build entry point. Extension projects
are implementation details selected by Shell through conditional `ProjectReference` items:

- Stable SDK: Shell builds without experimental extension source files or projects.
- Experimental SDK or `BuildExperimentalExtensions=true`: Shell references and builds the
  extension projects as dependencies.

This keeps the build command the same for stable and experimental SDKs while avoiding placeholder
extension assemblies. To inspect or edit extension project files in Visual Studio, open the files
from the `Extensions` folder or temporarily add the project to the solution.

When switching from an experimental build to a stable build without cleaning, Shell removes stale
extension DLL, PDB, PRI, and resource-directory outputs before copying its stable output.

## Native cross-project composition

WinUI controls do not require a loader. Each extension folder owns a small Shell composition file:

```csharp
partial void AddHelloPageMenuItem()
{
    AddFeature(nameof(DemoPage), () => ShowPage(typeof(DemoPage)));
}
```

The extension `.csproj` excludes that file from its own assembly. Shell conditionally links and
compiles it alongside the matching `ProjectReference`. `Frame.Navigate(pageType)` is the native
Page path, and assigning `new HelloView()` to `ContentControl.Content` is the native UserControl
path. There is no DLL scanning, reflection, `Assembly.LoadFrom`, or `Activator.CreateInstance`.

On a stable SDK, both partial methods have no implementation, so the C# compiler removes their
calls. Shell remains independent of extension types and assemblies.

## Add an extension

1. Create `src\WinUIExtension\Extensions\<Name>\Ext.<Name>.csproj`.
2. Add a `ShellExtension.cs` partial implementation in that extension folder.
3. Add its project reference and composition source to the matching ItemGroup in `Shell.csproj`.
4. Exclude `ShellExtension.cs` from the extension project's own compilation.
5. Run `build.cmd`.

To promote an extension to stable, make its project reference and composition source unconditional
and remove it from the stale experimental output list.

## Project layout

```text
WinUIModularDemo/
  README.md
  build.cmd
  src/
    WinUIExtension/
      Directory.Build.props
      Directory.Packages.props
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
          HelloView.xaml
          Ext.HelloUserControl.csproj
          ShellExtension.cs
```
