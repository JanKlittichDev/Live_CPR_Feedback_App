using System.Windows.Input;

namespace Live_CPR_Feedback_App.ViewModels
{
    public class InstructionPerformCPRViewModel
    {
        public ICommand StartCPRCommand { get; }
        public ICommand BackCommand { get; }

        public InstructionPerformCPRViewModel()
        {
            StartCPRCommand = new Command(StartCPR);
            BackCommand = new Command(Back);
        }

        private async void StartCPR()
        {
            await Shell.Current.GoToAsync("CPRPage");
        }

        private async void Back()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}