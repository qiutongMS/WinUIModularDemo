using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using WinUIModularDemo;

namespace Shell;

/// <summary>Metadata for one discovered feature (from its [NavItem] attribute).</summary>
public sealed record FeatureModule(string Title, Symbol Icon, int Order, Type ViewType, bool IsPage);

/// <summary>
/// Attribute-based discovery with no compile-time dependency on any feature.
///
/// Rule:
/// - each extension project exposes a PUBLIC entry UI type decorated with [NavItem]
/// - that entry type is either a Page or a UserControl
///
/// We first load any Ext.*.dll sitting next to the Shell (features the Shell referenced are
/// already loaded; this also tolerates ones dropped in later), then scan every loaded assembly
/// for [NavItem] types. In the stable flavor the experimental extensions are empty DLLs that the
/// Shell never referenced, so none are copied to the output and nothing is discovered.
/// </summary>
public static class ModuleLoader
{
    public static IReadOnlyList<FeatureModule> Discover()
    {
        LoadExtensionAssemblies();

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Select(t => (Type: t, Attr: t.GetCustomAttribute<NavItemAttribute>()))
            .Where(x => x.Attr is not null && IsEntryUiType(x.Type))
            .Select(x => new FeatureModule(
                x.Attr!.Title,
                x.Attr.Icon,
                x.Attr.Order,
                x.Type,
                typeof(Page).IsAssignableFrom(x.Type)))
            .OrderBy(m => m.Order)
            .ThenBy(m => m.Title, StringComparer.Ordinal)
            .ToList();
    }

    private static void LoadExtensionAssemblies()
    {
        var baseDir = AppContext.BaseDirectory;
        var alreadyLoaded = new HashSet<string>(
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .Where(n => !string.IsNullOrEmpty(n))!,
            StringComparer.OrdinalIgnoreCase);

        foreach (var dll in Directory.GetFiles(baseDir, "Ext.*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (alreadyLoaded.Contains(name))
            {
                continue;
            }
            try { Assembly.LoadFrom(dll); }
            catch { /* unloadable / not one of ours -> skip; the app keeps running */ }
        }
    }

    private static bool IsEntryUiType(Type? t)
    {
        if (t is null || !t.IsPublic || t.IsAbstract)
        {
            return false;
        }
        return typeof(Page).IsAssignableFrom(t) || typeof(UserControl).IsAssignableFrom(t);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
