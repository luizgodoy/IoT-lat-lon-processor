using IoT_lat_lon_processor.UI.ViewModels;

namespace IoT_lat_lon_processor.UI.Views;

public partial class StartView : ContentPage
{
    public StartView(StartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
