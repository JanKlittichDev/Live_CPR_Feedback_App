using Live_CPR_Feedback_App.ViewModels;
namespace Live_CPR_Feedback_App.Views;

public partial class InfoPageAbout : ContentPage 
{
	public InfoPageAbout()
	{
		InitializeComponent();
		BindingContext = new InfoAboutViewModel(); 
    }
}