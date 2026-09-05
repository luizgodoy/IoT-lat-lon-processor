using IoT_lat_lon_processor.UI.Views;

namespace IoT_lat_lon_processor;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("MappingView", typeof(MappingView));
        Routing.RegisterRoute("ProcessingView", typeof(ProcessingView));
        Routing.RegisterRoute("ResultView", typeof(ResultView));
    }
}
