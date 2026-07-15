using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views;

public partial class StoragePage : ContentPage
{
    private readonly StorageViewModel _viewModel;

    public StoragePage(StorageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadSessions(); // Liste bei jedem Betreten neu laden
    }
}