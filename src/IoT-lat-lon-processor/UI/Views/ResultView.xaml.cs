using IoT_lat_lon_processor.UI.ViewModels;

namespace IoT_lat_lon_processor.UI.Views;

public partial class ResultView : ContentPage
{
    public ResultView(ResultViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
