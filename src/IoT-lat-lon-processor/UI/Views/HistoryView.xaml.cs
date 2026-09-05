using IoT_lat_lon_processor.UI.ViewModels;

namespace IoT_lat_lon_processor.UI.Views;

public partial class HistoryView : ContentPage
{
    private readonly HistoryViewModel _viewModel;

    public HistoryView(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadHistoryAsync();
    }
}
