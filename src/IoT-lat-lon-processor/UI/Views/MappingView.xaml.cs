using IoT_lat_lon_processor.UI.ViewModels;

namespace IoT_lat_lon_processor.UI.Views;

public partial class MappingView : ContentPage
{
    public MappingView(MappingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
