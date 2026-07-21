using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace Shell;

/// <summary>Metadata for one discovered experimental feature.</summary>
public sealed record FeatureModule(string Title, Type ViewType, bool IsPage);

/// <summary>
/// Convention-based discovery with no custom interface.
///
/// Rule:
/// - each Experimental.* project exposes exactly one PUBLIC entry UI type
/// - that entry type is either a Page or a UserControl
/// - helper controls inside the same project should be INTERNAL, not public
///
/// We discover by TYPE, not by name. "View" is only a human naming convention; the actual
/// distinction is Page vs UserControl, because that determines how the Shell hosts it.
/// </summary>
public static class ModuleLoader
{
   public static IReadOnlyList<FeatureModule> Discover()
   {
       var results = new List<FeatureModule>();
        var baseDir = AppContext.BaseDirectory;

        foreach (var dll in Directory.GetFiles(baseDir, "Experimental.*.dll"))
        {
            Assembly asm;
            try { asm = Assembly.LoadFrom(dll); }
            catch { continue; } // unloadable / not one of ours -> skip, app keeps running

            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).ToArray()!; }

            var entryTypes = types
                .Where(IsEntryUiType)
                .ToList();

            if (entryTypes.Count != 1)
            {
                Debug.WriteLine(
                    $"[ModuleLoader] Skipped {Path.GetFileName(dll)}: found {entryTypes.Count} public entry UI types, expected 1.");
                continue;
            }

            var entryType = entryTypes[0];
            bool isPage = typeof(Page).IsAssignableFrom(entryType);
            results.Add(new FeatureModule(FormatTitle(entryType.Name), entryType, isPage));
        }

        return results.OrderBy(m => m.Title).ToList();
    }

    private static bool IsEntryUiType(Type? t)
    {
        if (t is null || !t.IsPublic || t.IsAbstract)
            return false;

        return typeof(Page).IsAssignableFrom(t) || typeof(UserControl).IsAssignableFrom(t);
    }

    // A nice menu title is still helpful, but it is cosmetic only.
    // "HelloView" -> "Hello",  "DemoPage" -> "Demo",  "ReportsPanel" -> "ReportsPanel"
    private static string FormatTitle(string typeName)
    {
        foreach (var suffix in new[] { "View", "Page" })
            if (typeName.EndsWith(suffix) && typeName.Length > suffix.Length)
                return typeName[..^suffix.Length];
        return typeName;
    }
}
