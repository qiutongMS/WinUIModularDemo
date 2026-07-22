using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinUIModularDemo;

namespace Ext.HelloPage;

// Same self-registration as the UserControl feature - but this entry type is a Page, so the
// Shell hosts it in a Frame and it receives navigation lifecycle callbacks (OnNavigatedTo).
[NavItem("Demo", Icon = Symbol.Document, Order = 20)]
public sealed partial class DemoPage : Page
{
    public DemoPage()
    {
        this.InitializeComponent();
    }

    // The extra thing a Page gives you that a plain UserControl does not:
    // the Frame calls this when it navigates to the page.
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Status.Text = $"OnNavigatedTo fired at {DateTime.Now:HH:mm:ss} (a Page lifecycle event)";
    }
}
