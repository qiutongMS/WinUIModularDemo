# WinUI Modular Demo — optional experimental features via one build flag

A minimal, framework-agnostic WinUI 3 app that demonstrates the extension model used by the
[Windows App SDK **WindowsAIFoundry** sample](https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/WindowsAIFoundry/cs-winui).
It shows the three moving parts of that pattern in isolation, with nothing else to distract you:

1. **The empty-DLL technique** — one build flag flips the app between a *stable* flavor and an
   *experimental* flavor without adding or removing projects from the solution.
2. **Reflection-based navigation discovery** — the Shell has **zero** per-feature code; it finds
   features at runtime by scanning for a `[NavItem]` attribute.
3. **Per-flavor build isolation** — stable and experimental artifacts live in separate `bin`/`obj`
   folders, so you can switch flavors without ever running `dotnet clean`.

**The problem it solves.** Code that uses experimental APIs can't build against the stable SDK —
the types don't exist. Rather than stripping that code or maintaining a separate branch, this
pattern keeps **one** source tree where a single MSBuild property switches the whole app between
stable and experimental — no `#if`, no file moves, no broken builds.

## The one switch: `IncludeExperimentalApis`

Everything is driven by a single MSBuild property, `IncludeExperimentalApis` (default **true**).

| | `IncludeExperimentalApis=true` (experimental) | `IncludeExperimentalApis=false` (stable) |
|---|---|---|
| Windows App SDK | `2.1.4-experimental8` | `2.1.3` (latest stable) |
| Extension projects | compile **real code** | compile to **empty DLLs** |
| Shell references extensions? | yes | no |
| Menu shows | Home · **Hello** · **Demo** | Home only |

## Build

```cmd
build.cmd                :: STABLE flavor       -> menu shows just "Home (core)"
build.cmd experimental   :: EXPERIMENTAL flavor -> "Hello" and "Demo" also appear
```

`build.cmd` builds the **whole solution** with MSBuild (located via `vswhere`). Equivalent raw
commands:

```cmd
msbuild src\WinUIExtension\WinUIModularDemo.sln /restore /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /p:IncludeExperimentalApis=false
msbuild src\WinUIExtension\WinUIModularDemo.sln /restore /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /p:IncludeExperimentalApis=true
```

`dotnet build src\WinUIExtension\WinUIModularDemo.sln -c Debug -p:Platform=x64 -p:IncludeExperimentalApis=<true|false>`
also works on a .NET SDK that carries the WinUI MSIX/PRI build tasks.

## Run

The Shell is unpackaged and self-contained, so you can launch the built `.exe` directly:

```cmd
:: experimental
src\WinUIExtension\Shell\bin\experimental\x64\Debug\net8.0-windows10.0.22621.0\Shell.exe
:: stable
src\WinUIExtension\Shell\bin\stable\x64\Debug\net8.0-windows10.0.22621.0\Shell.exe
```

## How it works

### 1. The empty-DLL technique

This repo is built as a **solution**, and a solution build compiles **every** project it lists —
a build-time property cannot drop a project from that set. So an experimental extension can't
simply "not build" in the stable flavor. Instead it builds with **no content**:

```xml
<!-- src/WinUIExtension/Extensions/HelloUserControl/Ext.HelloUserControl.csproj -->
<EnableDefaultItems Condition="'$(IncludeExperimentalApis)' != 'true'">false</EnableDefaultItems>
<UseWinUI            Condition="'$(IncludeExperimentalApis)' == 'true'">true</UseWinUI>

<ItemGroup Condition="'$(IncludeExperimentalApis)' == 'true'">
  <ProjectReference Include="..\..\Contracts\WinUIModularDemo.Contracts.csproj" />
  <PackageReference Include="Microsoft.WindowsAppSDK" />
</ItemGroup>
```

In the stable flavor, `EnableDefaultItems=false` compiles no source, so the project produces an
**empty assembly**. The solution still builds green. Because the Shell references the experimental
extensions **only** when the flag is on (see below), the empty DLLs are never copied to the Shell's
output and are never discovered at runtime.

> **Why not just build `Shell.csproj` directly, or drop the project from the `.sln`?** Building the
> Shell alone keeps unreferenced experimental projects out of the build graph (no empty DLL needed)
> — but this demo standardizes on **solution** builds so everything builds uniformly. Removing the
> project from the solution would also work, but it breaks IDE discoverability: you could no longer
> browse or edit the extension code in Visual Studio.

### 2. The Shell references extensions per flavor — `Extensions.props`

`src/WinUIExtension/Shell/Extensions.props` is the single source of truth for which extensions exist and whether
they are stable or experimental:

```xml
<ItemGroup>
  <!-- <StableExtension Include="..." />  (none yet) -->
  <ExperimentalExtension Include="HelloUserControl" />
  <ExperimentalExtension Include="HelloPage" />
</ItemGroup>
```

`Shell.csproj` imports it and turns those identities into `ProjectReference`s — stable ones always,
experimental ones only when `IncludeExperimentalApis=true`. Each `Identity` maps to
`src\WinUIExtension\Extensions\<Identity>\Ext.<Identity>.csproj`.

### 3. Reflection-based navigation discovery

Each extension's entry UI type is decorated with `[NavItem]` (defined in the SDK-neutral
`WinUIModularDemo.Contracts` project):

```csharp
[NavItem("Hello", Icon = Symbol.Emoji, Order = 10)]
public sealed partial class HelloView : UserControl { ... }

[NavItem("Demo", Icon = Symbol.Document, Order = 20)]
public sealed partial class DemoPage : Page { ... }
```

At startup `ModuleLoader.Discover()` loads any `Ext.*.dll` next to the Shell, scans every loaded
assembly for `[NavItem]` types, and returns them ordered by `Order`. `MainWindow` adds one menu
item per result. **The Shell contains no feature names** — add or remove a feature and the menu
follows automatically.

Two deliberate choices here: discovery is by **reflection** (not hardcoded XAML nav items), so the
menu only ever shows features that were actually compiled — no dead entries that crash on click;
and it keys off a **`[NavItem]` attribute** (not "scan every `Page`"), so a feature can carry a
title, icon, and sort order, and only opted-in types appear (dialogs and sub-views stay out).

### 4. Per-flavor build isolation

The root `Directory.Build.props` sets `BaseOutputPath`/`BaseIntermediateOutputPath` to
`bin\<flavor>\` / `obj\<flavor>\`. Every project therefore keeps its stable and experimental
outputs apart, so switching flavors never needs a manual `dotnet clean` (this matters for WinUI's
XAML-generated files, which would otherwise collide between flavors).

## The two feature styles

`[NavItem]` works on either WinUI hosting model, and the demo ships one of each so you can compare:

| Project | Entry type | Base class | How the Shell hosts it |
|---|---|---|---|
| `Ext.HelloUserControl` | `HelloView` | `UserControl` | `ContentControl.Content = new HelloView()` — no Frame |
| `Ext.HelloPage` | `DemoPage` | `Page` | `Frame.Navigate(typeof(DemoPage))` — gets `OnNavigatedTo` lifecycle |

`Page` derives from `UserControl`, so a single `ContentControl` *could* host either — but only a
`Page` driven by a `Frame` receives navigation lifecycle events. Pick `UserControl` for a
self-contained view; pick `Page` when you need navigation parameters or a back stack.

### Hosting in other UI containers

Discovery is independent of how you render each item — only the nav container changes:

| UI pattern | Render a discovered item as |
|---|---|
| **NavigationView** (this demo) | `NavigationViewItem { Content = Title, Icon = new SymbolIcon(Icon) }` |
| **TabView** | `TabViewItem { Header = Title, IconSource = new SymbolIconSource { Symbol = Icon } }` |
| **ListView + Frame** | `ListViewItem { Content = Title }` → on selection `Frame.Navigate(type)` |

## Project layout

```
WinUIModularDemo/
  .gitignore
  README.md
  build.cmd                        builds the solution per flavor
  src/
    WinUIExtension/                solution root
      WinUIModularDemo.sln
      Directory.Packages.props
      Shell/                       Shell.csproj and application source
```

## Verify both flavors

With per-flavor isolation you can run these back to back **without** a `dotnet clean`:

```powershell
dotnet build src\WinUIExtension\WinUIModularDemo.sln -c Debug -p:Platform=x64                                   # experimental (default)
dotnet build src\WinUIExtension\WinUIModularDemo.sln -c Debug -p:Platform=x64 -p:IncludeExperimentalApis=false  # stable
```

| Check | Experimental | Stable |
|---|---|---|
| Build succeeds | ✅ | ✅ |
| Extension DLLs contain code | ✅ | ❌ (empty) |
| `Ext.*.dll` copied next to the Shell | ✅ | ❌ |
| Feature nav items appear | ✅ (Home · Hello · Demo) | ❌ (Home only) |

## Common pitfalls

| Pitfall | Symptom | Fix |
|---|---|---|
| Shell references an experimental API directly | CS0234 in the stable build | Move that code into an extension project |
| Contracts references SDK-specific types | Extension can't compile standalone | Keep Contracts SDK-neutral |
| Forgot `EnableDefaultItems=false` in an experimental extension | Stable build fails on missing APIs | Add the conditional property |
| Forgot to register in `Extensions.props` | Extension DLL isn't referenced/copied | Add it to `StableExtension` or `ExperimentalExtension` |
| Assembly not named `Ext.*` | Feature never discovered at runtime | Match the `Ext.<Feature>` naming so `ModuleLoader` finds it |

## Add or promote a feature

### Add an experimental feature

No scaffolding script, and no central registry beyond `Extensions.props` — a feature self-registers
via one attribute.

1. **Create** `src/WinUIExtension/Extensions/<Name>/Ext.<Name>.csproj` by copying an existing one (e.g.
   `Ext.HelloUserControl.csproj`); rename `RootNamespace`/`AssemblyName` to `Ext.<Name>` and keep
   the empty-DLL block:

   ```xml
   <EnableDefaultItems Condition="'$(IncludeExperimentalApis)' != 'true'">false</EnableDefaultItems>
   <UseWinUI            Condition="'$(IncludeExperimentalApis)' == 'true'">true</UseWinUI>

   <ItemGroup Condition="'$(IncludeExperimentalApis)' == 'true'">
     <ProjectReference Include="..\..\Contracts\WinUIModularDemo.Contracts.csproj" />
     <PackageReference Include="Microsoft.WindowsAppSDK" />
   </ItemGroup>
   ```

2. **Add one `public` entry UI type** (a `UserControl` or a `Page`) decorated with `[NavItem]`; keep
   any helpers `internal`:

   ```csharp
   using WinUIModularDemo;   // NavItemAttribute (Contracts project)
   using Microsoft.UI.Xaml.Controls;

   namespace Ext.MyThing;

   [NavItem("My Thing", Icon = Symbol.Home, Order = 30)]
   public sealed partial class MyThingView : UserControl { public MyThingView() => InitializeComponent(); }
   ```

3. **Register** it in `src/WinUIExtension/Shell/Extensions.props`: `<ExperimentalExtension Include="MyThing" />`.

4. **Add to the solution and build:**

   ```cmd
   dotnet sln src\WinUIExtension\WinUIModularDemo.sln add src\WinUIExtension\Extensions\MyThing\Ext.MyThing.csproj
   build.cmd experimental
   ```

It appears in the menu automatically — **you never touch the Shell's code.**

### Promote a feature to stable

No file moves, no namespace changes. In the feature's `.csproj`:

1. Delete the `EnableDefaultItems` line.
2. Set `<UseWinUI>true</UseWinUI>` unconditionally.
3. Remove the `Condition` from the `<ItemGroup>` that pulls in Contracts + `Microsoft.WindowsAppSDK`.

Then in `src/WinUIExtension/Shell/Extensions.props`, move its entry from `<ExperimentalExtension>` to
`<StableExtension>`. It now builds and loads in **both** flavors.
