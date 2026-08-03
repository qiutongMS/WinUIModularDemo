using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace Shell;

/// <summary>
/// Convention-based discovery with no compile-time dependency on any extension type.
///
/// Rule:
/// - every public, concrete Page or UserControl in an Ext.* assembly is an entry type
/// - helper Page/UserControl types should be internal
///
/// With a stable Windows App SDK the Shell does not reference experimental extension projects,
/// so they never enter the build graph or appear in the output.
/// </summary>
public static class ModuleLoader
{
    public static IReadOnlyList<Type> Discover()
    {
        return LoadExtensionAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(IsEntryUiType)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<Assembly> LoadExtensionAssemblies()
    {
        var baseDir = AppContext.BaseDirectory;
        var extensions = AppDomain.CurrentDomain.GetAssemblies()
            .Where(IsExtensionAssembly)
            .ToList();
        var alreadyLoaded = extensions
            .Select(a => a.GetName().Name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(baseDir, "Ext.*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (alreadyLoaded.Contains(name))
            {
                continue;
            }

            try
            {
                extensions.Add(Assembly.LoadFrom(dll));
            }
            catch (BadImageFormatException ex)
            {
                Debug.WriteLine($"[ModuleLoader] Ignoring '{dll}': {ex.Message}");
            }
            catch (FileLoadException ex)
            {
                Debug.WriteLine($"[ModuleLoader] Could not load '{dll}': {ex.Message}");
            }
        }

        return extensions;
    }

    private static bool IsEntryUiType(Type? t)
    {
        if (t is null || !t.IsPublic || t.IsAbstract)
        {
            return false;
        }
        return (typeof(Page).IsAssignableFrom(t) || typeof(UserControl).IsAssignableFrom(t))
            && t.GetConstructor(Type.EmptyTypes) is not null;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderException in ex.LoaderExceptions)
            {
                Debug.WriteLine($"[ModuleLoader] Type load failure in '{assembly.FullName}': {loaderException?.Message}");
            }
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static bool IsExtensionAssembly(Assembly assembly)
    {
        return assembly.GetName().Name?.StartsWith("Ext.", StringComparison.OrdinalIgnoreCase) == true;
    }

}
