using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Live_CPR_Feedback_App.ViewModels
{
    public partial class InfoCPRVideoViewModel : ObservableObject
    {
        public ICommand OpenERCWebsiteCommand { get; }

        public InfoCPRVideoViewModel()
        {
            OpenERCWebsiteCommand = new AsyncRelayCommand(OpenERCWebsite);
        }

        private async Task OpenERCWebsite()
        {
            await Launcher.Default.OpenAsync("https://www.erc.edu");
        }
    }
}
