using Live_CPR_Feedback_App.ViewModels;

namespace Live_CPR_Feedback_App.Views
{
    public partial class MainPage : ContentPage
    {
 
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainViewModel();
        }
    }
}