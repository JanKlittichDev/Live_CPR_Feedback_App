using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace Live_CPR_Feedback_App.ViewModels
{
    public class InfoAboutViewModel 
    {
        
        public string HS_Link => "https://www.hs-pforzheim.de/";
        public string Seifert_Link1 => "https://www.hs-pforzheim.de/profile/saschaseifert/";
        public string Seifert_Link2 => "https://www.linkedin.com/in/sascha-seifert-5ba282a";
        public string Barchet_Link => "https://www.linkedin.com/in/max-barchet-1181b6233";
        public string Basnet_Link => "https://www.linkedin.com/in/sanjita-basnet-22301a391/";
        public string Reim_Link => "https://www.linkedin.com/in/nico-reim-7b549929b/";
        public string Kiryokoz_Link => "https://www.linkedin.com/in/michael-kiryokoz-753877344/";
        public string Klittich_Link => "https://www.linkedin.com/in/jan-klittich-09a23638a/";


        public ICommand OpenHSLinkCommand { get; } 
        public ICommand OpenSeifertLink1Command { get; }
        public ICommand OpenSeifertLink2Command { get; }
        public ICommand OpenBarchetLinkCommand { get; }
        public ICommand OpenBasnetLinkCommand { get; }
        public ICommand OpenReimLinkCommand { get; }
        public ICommand OpenKiryokozLinkCommand { get; }
        public ICommand OpenKlittichLinkCommand { get; }



        public InfoAboutViewModel()
        {
            OpenHSLinkCommand = new AsyncRelayCommand(OpenHS);
            OpenSeifertLink1Command = new AsyncRelayCommand(OpenSeifert1);
            OpenSeifertLink2Command = new AsyncRelayCommand(OpenSeifert2);
            OpenBarchetLinkCommand = new AsyncRelayCommand(OpenBarchet);
            OpenKlittichLinkCommand = new AsyncRelayCommand(OpenKlittich);
            OpenBasnetLinkCommand = new AsyncRelayCommand(OpenBasnet);
            OpenKiryokozLinkCommand = new AsyncRelayCommand(OpenKiryokoz);
            OpenReimLinkCommand = new AsyncRelayCommand(OpenReim);
        }



        async Task OpenHS() => 
            await Launcher.Default.OpenAsync(HS_Link);
        async Task OpenSeifert1() =>
            await Launcher.Default.OpenAsync(Seifert_Link1);
        async Task OpenSeifert2() =>
            await Launcher.Default.OpenAsync(Seifert_Link2);
        async Task OpenBarchet() =>
            await Launcher.Default.OpenAsync(Barchet_Link);
        async Task OpenReim() =>
            await Launcher.Default.OpenAsync(Reim_Link);
        async Task OpenBasnet() =>
            await Launcher.Default.OpenAsync(Basnet_Link);
        async Task OpenKiryokoz() =>
            await Launcher.Default.OpenAsync(Kiryokoz_Link);
        async Task OpenKlittich() =>
            await Launcher.Default.OpenAsync(Klittich_Link);



    }
}

