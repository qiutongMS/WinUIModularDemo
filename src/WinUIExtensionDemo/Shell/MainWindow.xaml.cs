using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Shell;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        BuildMenu();
    }

    // The Shell knows nothing about specific feature names.
    // It always shows "Home", then asks ModuleLoader for extension entry UI types.
    private void BuildMenu()
    {
        var homeItem = new NavigationViewItem
        {
            Content = "Home (core)",
            Tag = "home",
        };
        AutomationProperties.SetAutomationId(homeItem, "Navigation_Home");
        Nav.MenuItems.Add(homeItem);

        var modules = ModuleLoader.Discover();
        if (modules.Count > 0)
        {
            Nav.MenuItems.Add(new NavigationViewItemSeparator());
            Nav.MenuItems.Add(new NavigationViewItemHeader { Content = "Features" });
            foreach (var m in modules)
            {
                var item = new NavigationViewItem
                {
                    Content = m.Title,
                    Icon = new SymbolIcon(m.Icon),
                    Tag = m,
                };
                AutomationProperties.SetAutomationId(item, $"Navigation_{m.ViewType.FullName}");
                Nav.MenuItems.Add(item);
            }
        }

        Nav.SelectedItem = Nav.MenuItems[0];
        ShowHome();
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        switch (args.InvokedItemContainer?.Tag)
        {
            case "home":
                ShowHome();
                break;

            case FeatureModule m:
                Show(m);
                break;
        }
    }

    private void Show(FeatureModule m)
    {
        if (m.IsPage)
        {
            // A Page is navigation-aware. Hosting it in a Frame gives it OnNavigatedTo,
            // navigation parameters, and back-stack behavior.
            UserControlHost.Visibility = Visibility.Collapsed;
            PageHost.Visibility = Visibility.Visible;
            PageHost.Navigate(m.ViewType);
        }
        else
        {
            // A UserControl is just a reusable piece of UI. It does not need a Frame and
            // does not get Page navigation lifecycle callbacks.
            PageHost.Visibility = Visibility.Collapsed;
            UserControlHost.Visibility = Visibility.Visible;
            UserControlHost.Content = Activator.CreateInstance(m.ViewType);
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
                new TextBlock { Text = "Core app", Style = (Style)Application.Current.Resources["TitleTextBlockStyle"] },
                new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "This is the always-present core. Experimental features appear automatically " +
                           "when Directory.Packages.props selects a Windows App SDK -exp prerelease."
                }
            }
        };
    }
}
