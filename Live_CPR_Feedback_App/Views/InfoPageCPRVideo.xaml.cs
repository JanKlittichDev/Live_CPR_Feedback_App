using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views;

public partial class InfoPageCPRVideo : ContentPage
{
    public InfoPageCPRVideo()
    {
        InitializeComponent();

        BindingContext = new InfoCPRVideoViewModel();
    }
}