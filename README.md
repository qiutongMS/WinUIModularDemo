# WinUI Modular Demo — optional experimental features via a build flag

A minimal WinUI 3 app showing how to split features into **separate, optional projects**.
No custom interface. No runtime plugin contract. Two build commands.

## The idea

- The **Shell** (`src/Shell`) is the core app. It always builds and runs on its own.
- Each **experimental feature** is its own project under `src/Experimental/Experimental.*`.
- The Shell references those projects with a **conditional glob** — they are pulled in
  **only** when you pass `-p:IncludeExperimental=true`.
- At startup the Shell **reflects** over `Experimental.*.dll` in its output folder and adds
  one menu item per feature it finds. If a feature wasn't built, its DLL is absent and it
  simply doesn't appear. **The Shell has zero per-feature code.**

## Build

```cmd
build.cmd                :: core only  -> menu shows just "Home (core)"
build.cmd experimental   :: core + features -> "Hello" and "Demo" also appear
```

Equivalent raw command (uses MSBuild — `build.cmd` locates it via vswhere):

```cmd
msbuild src\Shell\Shell.csproj /restore /t:Rebuild /p:Configuration=Debug /p:Platform=x64
msbuild src\Shell\Shell.csproj /restore /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /p:IncludeExperimental=true
```

> WinUI class libraries with XAML need the MSIX/PRI MSBuild tasks that ship with Visual Studio
> or "Build Tools for Visual Studio". `dotnet build` also works **if** your .NET SDK includes
> those tasks; some SDKs (e.g. certain .NET 10 previews) don't, in which case use MSBuild.

## Run

```cmd
dotnet run --project src\Shell\Shell.csproj -p:Configuration=Debug -p:Platform=x64
```

Notes:
- Core-only `dotnet run` works after the Shell now separates core vs experimental `obj`/`bin` folders.
- Experimental `dotnet run --project src\Shell\Shell.csproj -p:Configuration=Debug -p:Platform=x64 -p:IncludeExperimental=true`
  still depends on your installed .NET SDK including the WinUI MSIX/PRI build tasks.
- On SDKs that do not include them (seen on this machine with `10.0.300-preview.0.26177.108`),
  use `.\build.cmd experimental` and then run:

  ```cmd
  src\Shell\bin\experimental\x64\Debug\net10.0-windows10.0.19041.0\Shell.exe
  ```

## The two feature styles (this is the part you asked about)

| Project | Entry type | Base class | How the Shell hosts it |
|---|---|---|---|
| `Experimental.HelloUserControl` | `HelloView` | `UserControl` | `ContentControl.Content = new HelloView()` — no Page, no Frame |
| `Experimental.HelloPage` | `DemoPage` | `Page` | `Frame.Navigate(typeof(DemoPage))` — gets `OnNavigatedTo` lifecycle |

Because `Page` derives from `UserControl`, a single generic `ContentControl` could host either —
but the `Page` only receives navigation lifecycle events when a `Frame` drives it. The demo
shows both paths side by side so you can see the difference.

**Convention (no interface):**
- each `Experimental.*` project exposes exactly **one public entry UI type**
- that entry type is either a `UserControl` or a `Page`
- helper controls in the same project should be `internal`

`View` is just a human-friendly naming convention. `UserControl` and `Page` are the real WinUI types.

## Add your own feature

Use the scaffold — one command, no Shell edits:

```powershell
.\new-feature.ps1 -Name Reports            # a UserControl feature
.\new-feature.ps1 -Name Report -Kind Page  # a Page feature
build.cmd experimental                      # it appears in the menu
```

Or by hand:

1. Create `src/Experimental/Experimental.MyThing/Experimental.MyThing.csproj`
   (copy one of the existing `.csproj` files).
2. Add exactly one **public** entry UI type: `MyThingView : UserControl` or `MyThingPage : Page`.
3. Make any helper controls in the same project `internal`.
4. `build.cmd experimental`. It appears in the menu automatically. No Shell edits.

See `CONTRIBUTING.md` for the full 60-second guide.

## Note on the build toolchain

The features use normal WinUI **XAML** (`HelloView.xaml`, `DemoPage.xaml`). Building a WinUI
class library that has XAML needs the MSIX/PRI MSBuild tasks that come with Visual Studio or
"Build Tools for Visual Studio". `build.cmd` finds MSBuild automatically via `vswhere`.

`dotnet build` works too **when** the installed .NET SDK carries those MSIX tasks. On an SDK
that doesn't (seen with some .NET 10 previews), `dotnet build` fails on the library PRI step —
use `build.cmd` (MSBuild) instead. Nothing about the *pattern* depends on this; it's purely
which build tool you invoke.
