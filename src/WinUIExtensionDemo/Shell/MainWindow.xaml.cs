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

    private void BuildMenu()
    {
        var homeItem = new NavigationViewItem
        {
            Content = "Home (core)",
            Tag = "home",
        };
        AutomationProperties.SetAutomationId(homeItem, "Navigation_Home");
        Nav.MenuItems.Add(homeItem);

        var coreMenuItemCount = Nav.MenuItems.Count;
        AddHelloPageMenuItem();
        AddHelloUserControlMenuItem();
        if (Nav.MenuItems.Count > coreMenuItemCount)
        {
            Nav.MenuItems.Insert(coreMenuItemCount, new NavigationViewItemSeparator());
            Nav.MenuItems.Insert(coreMenuItemCount + 1, new NavigationViewItemHeader { Content = "Experimental features" });
        }

        Nav.SelectedItem = Nav.MenuItems[0];
        ShowHome();
    }

    partial void AddHelloPageMenuItem();

    partial void AddHelloUserControlMenuItem();

    private void AddFeature(string title, string automationId, Action show)
    {
        var item = new NavigationViewItem
        {
            Content = title,
            Tag = show,
        };
        AutomationProperties.SetAutomationId(item, automationId);
        Nav.MenuItems.Add(item);
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        switch (args.InvokedItemContainer?.Tag)
        {
            case "home":
                ShowHome();
                break;

            case Action show:
                show();
                break;
        }
    }

    private void ShowPage(Type pageType)
    {
        UserControlHost.Visibility = Visibility.Collapsed;
        PageHost.Visibility = Visibility.Visible;
        PageHost.Navigate(pageType);
    }

    private void ShowUserControl(UserControl control)
    {
        PageHost.Visibility = Visibility.Collapsed;
        UserControlHost.Visibility = Visibility.Visible;
        UserControlHost.Content = control;
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
                           "for a Windows App SDK -exp prerelease or when BuildExperimentalExtensions=true."
                }
            }
        };
    }
}
