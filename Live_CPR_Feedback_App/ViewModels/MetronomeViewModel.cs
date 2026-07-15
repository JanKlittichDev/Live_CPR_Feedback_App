using System.Windows.Input;
using Plugin.Maui.Audio;

namespace Live_CPR_Feedback_App.ViewModels;

public class MetronomeViewModel
{
    private IAudioPlayer? player;

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }

    public MetronomeViewModel()
    {
        StartCommand = new Command(Start);
        StopCommand = new Command(Stop);

        LoadAudio();
    }

    private async void LoadAudio()
    {
        var stream = await FileSystem.OpenAppPackageFileAsync("metronome100bpm.mp3"); 

        player = AudioManager.Current.CreatePlayer(stream); 

        player.Loop = true;
    }

    private void Start()
    {
        if (player != null)
        {
            player.Play();
        }
    }

    private void Stop()
    {
        if (player != null)
        {
            player.Stop();
        }
    }
}