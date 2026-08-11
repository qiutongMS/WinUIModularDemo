using Ext.HelloPage;

namespace Shell;

public sealed partial class MainWindow
{
    partial void AddHelloPageMenuItem()
    {
        AddFeature(
            nameof(DemoPage),
            $"Navigation_{typeof(DemoPage).FullName}",
            () => ShowPage(typeof(DemoPage)));
    }
}
