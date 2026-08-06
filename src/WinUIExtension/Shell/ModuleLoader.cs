using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace Shell;

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
        var extensions = AppDomain.CurrentDomain.GetAssemblies()
            .Where(IsExtensionAssembly)
            .ToList();
        var alreadyLoaded = extensions
            .Select(assembly => assembly.GetName().Name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "Ext.*.dll"))
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

    private static bool IsEntryUiType(Type? type)
    {
        return type is not null
            && type.IsPublic
            && !type.IsAbstract
            && (typeof(Page).IsAssignableFrom(type) || typeof(UserControl).IsAssignableFrom(type))
            && type.GetConstructor(Type.EmptyTypes) is not null;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderException in ex.LoaderExceptions)
            {
                Debug.WriteLine(
                    $"[ModuleLoader] Type load failure in '{assembly.FullName}': {loaderException?.Message}");
            }

            return ex.Types.Where(type => type is not null)!;
        }
    }

    private static bool IsExtensionAssembly(Assembly assembly)
    {
        return assembly.GetName().Name?.StartsWith("Ext.", StringComparison.OrdinalIgnoreCase) == true;
    }
}
