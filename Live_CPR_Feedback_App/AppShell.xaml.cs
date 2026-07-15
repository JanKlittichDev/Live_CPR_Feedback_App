namespace Live_CPR_Feedback_App
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();


            Routing.RegisterRoute(nameof(Views.PrepareSmartphonePage), typeof(Views.PrepareSmartphonePage));
            Routing.RegisterRoute(nameof(Views.InstructionPerformCPRPage), typeof(Views.InstructionPerformCPRPage));
            Routing.RegisterRoute(nameof(Views.CPRPage), typeof(Views.CPRPage));
            




        }
    }
}
