using IoT_lat_lon_processor.UI.ViewModels;

namespace IoT_lat_lon_processor.UI.Views;

public partial class ProcessingView : ContentPage
{
    public ProcessingView(ProcessingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
