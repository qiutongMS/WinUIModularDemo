using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ext.HelloUserControl;

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
