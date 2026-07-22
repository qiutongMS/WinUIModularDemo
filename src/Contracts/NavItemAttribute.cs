using System;
using Microsoft.UI.Xaml.Controls;

namespace WinUIModularDemo;

/// <summary>
/// Marks a feature's entry UI type (a <see cref="Page"/> or a <see cref="UserControl"/>) for
/// inclusion in the Shell's navigation menu.
///
/// The Shell scans every loaded assembly at startup for types carrying this attribute and builds
/// the menu dynamically. Extensions therefore self-register: there is no central list to edit in
/// the Shell when a feature is added, and a feature that was not built simply never appears.
///
/// Discovery is by attribute (not by name), so the entry type can be either a <see cref="Page"/>
/// (hosted in a <c>Frame</c>, gets navigation lifecycle) or a <see cref="UserControl"/> (hosted
/// directly in a <c>ContentControl</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NavItemAttribute : Attribute
{
    public NavItemAttribute(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public Symbol Icon { get; init; } = Symbol.Document;

    /// <summary>
    /// Lower values appear first. Use multiples of 10 to leave room for inserting new features
    /// without renumbering siblings.
    /// </summary>
    public int Order { get; init; } = 1000;
}
