using System.Windows.Input;

namespace Live_CPR_Feedback_App.ViewModels;
public class MainViewModel
{
    public ICommand ContinueCommand { get; }

    public MainViewModel()
    {
        ContinueCommand = new Command(OpenPrepareSmartphonePage); 
    }

    private async void OpenPrepareSmartphonePage()
    {
        await Shell.Current.GoToAsync("PrepareSmartphonePage"); 
    }
}