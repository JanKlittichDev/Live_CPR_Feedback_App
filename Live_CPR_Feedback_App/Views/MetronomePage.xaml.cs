using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views;

public partial class MetronomePage : ContentPage
{
	public MetronomePage()
	{
		InitializeComponent();
        BindingContext = new MetronomeViewModel();
    }
}