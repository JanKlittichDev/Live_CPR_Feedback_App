using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views;

public partial class PrepareSmartphonePage : ContentPage
{
    public PrepareSmartphonePage()
    {
        InitializeComponent();

        BindingContext = new PrepareSmartphoneViewModel();
    }
}