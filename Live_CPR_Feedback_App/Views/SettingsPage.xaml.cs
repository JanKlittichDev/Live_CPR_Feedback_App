using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel settings)
    {
        InitializeComponent();
        BindingContext = settings;
    }
}
