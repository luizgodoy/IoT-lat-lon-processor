using IoT_lat_lon_processor.UI.ViewModels;

namespace IoT_lat_lon_processor.UI.Views;

public partial class SettingsView : ContentPage
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
