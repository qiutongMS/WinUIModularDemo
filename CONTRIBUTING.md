# Contributing an experimental feature

This is the whole rule. Read time: 60 seconds.

## Add a feature

```powershell
.\new-feature.ps1 -Name Reports            # a UserControl feature
.\new-feature.ps1 -Name Report -Kind Page  # a Page feature
```

That creates `src/Experimental/Experimental.<Name>/` with one class. Then:

```cmd
build.cmd experimental
```

Your feature shows up in the app's menu. **You did not touch the Shell.**

## The three rules

1. **One feature = one project** under `src/Experimental/Experimental.*`.
2. **Each project exposes exactly one `public` entry UI type.**
   That type is either a `UserControl` or a `Page`. Any helper controls in the same project
   should be `internal`.
3. **It's optional.** `build.cmd` ships core only. `build.cmd experimental` includes every
   `Experimental.*` project. If yours isn't built, it just isn't in the menu — nothing breaks.

## UserControl vs Page — which do I pick?

- **`UserControl` (default).** A self-contained view. The Shell drops it into a `ContentControl`.
  No navigation stack, no lifecycle callbacks. Pick this unless you need otherwise.
- **`Page`.** The Shell hosts it in a `Frame`, so you get `OnNavigatedTo` / back-stack /
  navigation parameters. Pick this only if you actually need that machinery.

## What "View" means

- **`UserControl`** is the real WinUI type.
- **`Page`** is another real WinUI type, used for navigation.
- **`View`** is only a naming convention for the top-level UI of a feature.

So `ReportsView : UserControl` is common naming, but the loader does **not** care about the
word `View`. It cares whether the type is a `UserControl` or a `Page`.

## Why an AI can do this in one shot

Adding a feature is: create a folder, drop one `.csproj`, add one public entry UI type, and keep
helpers internal. No wiring, no registration, no Shell edits. That's exactly the kind of
mechanical, well-bounded change an agent gets right every time — which is the point: the pattern
stays cheap to extend.

## Note on the build tool

Features use normal WinUI **XAML**. Building a WinUI class library with XAML needs the MSIX/PRI
MSBuild tasks from Visual Studio or "Build Tools for Visual Studio"; `build.cmd` locates MSBuild
for you via `vswhere`. `dotnet build` works too when your .NET SDK ships those tasks (some
previews don't — then use `build.cmd`). This only affects which build tool you run, not the
pattern itself.
