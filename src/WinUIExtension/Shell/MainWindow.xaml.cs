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

        var modules = ModuleLoader.Discover();
        if (modules.Count > 0)
        {
            Nav.MenuItems.Add(new NavigationViewItemSeparator());
            Nav.MenuItems.Add(new NavigationViewItemHeader { Content = "Features" });

            foreach (var viewType in modules)
            {
                var item = new NavigationViewItem
                {
                    Content = viewType.Name,
                    Tag = viewType,
                };
                AutomationProperties.SetAutomationId(item, $"Navigation_{viewType.FullName}");
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

            case Type viewType:
                Show(viewType);
                break;
        }
    }

    private void Show(Type viewType)
    {
        if (typeof(Page).IsAssignableFrom(viewType))
        {
            UserControlHost.Visibility = Visibility.Collapsed;
            PageHost.Visibility = Visibility.Visible;
            PageHost.Navigate(viewType);
        }
        else
        {
            PageHost.Visibility = Visibility.Collapsed;
            UserControlHost.Visibility = Visibility.Visible;
            UserControlHost.Content = Activator.CreateInstance(viewType);
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
                new TextBlock
                {
                    Text = "Core app",
                    Style = (Style)Application.Current.Resources["TitleTextBlockStyle"],
                },
                new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = "The Shell discovers extension Page and UserControl types at runtime " +
                           "without knowing individual feature names."
                }
            }
        };
    }
}
