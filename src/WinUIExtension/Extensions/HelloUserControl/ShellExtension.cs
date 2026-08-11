using Ext.HelloUserControl;

namespace Shell;

public sealed partial class MainWindow
{
    partial void AddHelloUserControlMenuItem()
    {
        AddFeature(
            nameof(HelloView),
            () => ShowUserControl(new HelloView()));
    }
}
