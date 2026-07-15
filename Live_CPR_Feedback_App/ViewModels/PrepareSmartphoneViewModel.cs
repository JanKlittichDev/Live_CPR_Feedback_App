using System.Windows.Input;

namespace Live_CPR_Feedback_App.ViewModels
{
    public class PrepareSmartphoneViewModel
    {
        public ICommand ContinueCommand { get; }
        

        public PrepareSmartphoneViewModel()
        {
            ContinueCommand = new Command(Continue);
        }

        private async void Continue()
        {
            await Shell.Current.GoToAsync("InstructionPerformCPRPage");
        }


    }
}