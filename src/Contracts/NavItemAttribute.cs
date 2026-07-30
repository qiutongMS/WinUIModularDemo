using System;

namespace WinUIModularDemo;

/// <summary>
/// Marks a feature's entry UI type for inclusion in the Shell's navigation menu.
///
/// The Shell scans loaded extension assemblies at startup for types carrying this attribute and
/// builds the menu dynamically. Extensions therefore self-register: there is no central list to
/// edit in the Shell when a feature is added, and a feature that was not built simply never appears.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NavItemAttribute : Attribute
{
    public NavItemAttribute(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public NavIcon Icon { get; init; } = NavIcon.Document;

    /// <summary>
    /// Lower values appear first. Use multiples of 10 to leave room for inserting new features
    /// without renumbering siblings.
    /// </summary>
    public int Order { get; init; } = 1000;
}

public enum NavIcon
{
    Document,
    Emoji,
}
