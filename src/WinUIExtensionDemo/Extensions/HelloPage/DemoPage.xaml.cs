using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Ext.HelloPage;

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
