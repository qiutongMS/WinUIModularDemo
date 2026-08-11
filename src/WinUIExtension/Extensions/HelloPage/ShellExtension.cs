using Ext.HelloPage;

namespace Shell;

public sealed partial class MainWindow
{
    partial void AddHelloPageMenuItem()
    {
        AddFeature(
            nameof(DemoPage),
            () => ShowPage(typeof(DemoPage)));
    }
}
