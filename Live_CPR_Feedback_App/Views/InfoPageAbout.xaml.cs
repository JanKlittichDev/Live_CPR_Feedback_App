using CPR_Feedback_App.ViewModels;
namespace CPR_Feedback_App.Views;

public partial class InfoPageAbout : ContentPage 
{
	public InfoPageAbout()
	{
		InitializeComponent();
		BindingContext = new InfoAboutViewModel(); 
    }
}