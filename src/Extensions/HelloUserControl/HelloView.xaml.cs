using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIModularDemo;

namespace Ext.HelloUserControl;

// The [NavItem] attribute is how this feature self-registers in the Shell's menu. The Shell
// discovers it by reflection - it has no compile-time reference to this project.
[NavItem("Hello", Icon = Symbol.Emoji, Order = 10)]
public sealed partial class HelloView : UserControl
{
    private int _count;

    public HelloView()
    {
        this.InitializeComponent();
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        _count++;
        Status.Text = $"Clicked {_count} time(s) at {DateTime.Now:HH:mm:ss}";
    }
}
