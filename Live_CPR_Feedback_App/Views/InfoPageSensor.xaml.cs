namespace Live_CPR_Feedback_App.Views;
using Live_CPR_Feedback_App.ViewModels;

public partial class InfoPageSensor : ContentPage
{
	public InfoPageSensor(InfoSensorViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is InfoSensorViewModel vm)
        {
            vm.StopCommand.Execute(null);
        }
    }
}