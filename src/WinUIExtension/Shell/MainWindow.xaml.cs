using System;
using Ext.HelloPage;
using Ext.HelloUserControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Shell;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        BuildMenu();
    }

    // A plain, single-project app: the menu is hard-coded and every page lives in this same
    // Shell project. Adding a feature means editing this method and adding a page here.
    private void BuildMenu()
    {
        Nav.MenuItems.Add(new NavigationViewItem { Content = "Home", Tag = "home" });
        Nav.MenuItems.Add(new NavigationViewItem { Content = "Hello", Tag = "hello" });
        Nav.MenuItems.Add(new NavigationViewItem { Content = "Demo", Tag = "demo" });

        Nav.SelectedItem = Nav.MenuItems[0];
        ShowHome();
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        switch (args.InvokedItemContainer?.Tag as string)
        {
            case "home":
                ShowHome();
                break;

            case "hello":
                // A UserControl is just a reusable piece of UI. It does not need a Frame and
                // does not get Page navigation lifecycle callbacks.
                PageHost.Visibility = Visibility.Collapsed;
                UserControlHost.Visibility = Visibility.Visible;
                UserControlHost.Content = new HelloView();
                break;

            case "demo":
                // A Page is navigation-aware. Hosting it in a Frame gives it OnNavigatedTo,
                // navigation parameters, and back-stack behavior.
                UserControlHost.Visibility = Visibility.Collapsed;
                PageHost.Visibility = Visibility.Visible;
                PageHost.Navigate(typeof(DemoPage));
                break;
        }
    }

    private void ShowHome()
    {
        PageHost.Visibility = Visibility.Collapsed;
        UserControlHost.Visibility = Visibility.Visible;
        UserControlHost.Content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Home", Style = (Style)Application.Current.Resources["TitleTextBlockStyle"] },
                new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "A plain WinUI app. Home, Hello and Demo are all defined in this one Shell " +
                           "project and wired into the menu by hand."
                }
            }
        };
    }
}
