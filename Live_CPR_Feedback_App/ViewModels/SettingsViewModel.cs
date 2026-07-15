using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Live_CPR_Feedback_App.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {

        private const string UseKIPreferenceKey = "UseKI";


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UseSV))]
        [NotifyPropertyChangedFor(nameof(ModeLabel))]
        public partial bool UseKI { get; set; }


        public bool UseSV => !UseKI;

        public string ModeLabel => UseKI ? "AI Processing Active" : "Signal Processing Active";


        public SettingsViewModel()
        {
            // Gespeicherten Wert laden (Default: true, falls noch nichts gespeichert wurde)
            UseKI = Preferences.Get(UseKIPreferenceKey, true);
        }
        partial void OnUseKIChanged(bool value)
        {
            Preferences.Set(UseKIPreferenceKey, value);
        }




        [RelayCommand]
        private async Task OpenPipelineSource()
        {
            await Launcher.OpenAsync("https://pubmed.ncbi.nlm.nih.gov/25402865/");
        }
    }
}