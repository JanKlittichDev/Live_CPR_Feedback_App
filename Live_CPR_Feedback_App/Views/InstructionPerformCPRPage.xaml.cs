using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views;

public partial class InstructionPerformCPRPage : ContentPage
{
    public InstructionPerformCPRPage()
    {
        InitializeComponent();

        BindingContext = new InstructionPerformCPRViewModel();


        Loaded += async (s, e) =>
        {
            await Task.Delay(5);
            await InstructionScrollView.ScrollToAsync((View)FindByName("BackButton"), ScrollToPosition.Start, true);
        };
    }
}