using Android.Media;

using Plugin.Maui.Audio;

using System.ComponentModel;

using System.Windows.Input;



namespace CPR_Feedback.ViewModels;



public class CPRViewModel : INotifyPropertyChanged

{

    #region UI Aktualisieren 

    public event PropertyChangedEventHandler? PropertyChanged; // informiert XAML über Wertänderungen 

    #endregion



    #region Zentrale Variablen 

    IDispatcherTimer timer;      // Timer für regelmäßige CPR-Prüfung 

    IAudioPlayer? player;        // Audio Player 

    bool isSpeaking = false;     // verhindert Audio-Überlappung 

    const int NoFlowWarningSeconds = 5; // Warnung nach 5 Sekunden ohne CPR 

    double frequency = 0;        // aktuelle Kompressionsfrequenz [1/min] 

    double depth = 0;            // aktuelle Kompressionstiefe [cm] 

    bool fullRelease = true;     // vollständige Brustkorbentlastung 

    List<int> feedbackClasses = new(); // erkannte Feedback-Klassen 

    #endregion



    #region Zeitwerte 

    DateTime cprStartTime;       // Startzeit der CPR 

    DateTime noFlowStartTime;    // Startzeit der CPR-Unterbrechung 

    bool cprStarted = false;     // CPR wurde mindestens einmal erkannt 

    bool noFlowActive = false;   // No-Flow-Zähler läuft 

    #endregion



    #region Texte für die Anzeige 

    string cprDurationText = "00:00"; // Anzeige CPR-Dauer 

    string noFlowText = "0 s";        // Anzeige No-Flow-Zeit 

    string feedbackText = "WAITING";  // Feedback-Anzeige 

    Color feedbackColor = Colors.Gray;// Farbe des Feedback-Feldes 

    string rateText = "0 /min";       // Frequenz-Anzeige 

    string depthText = "0.0 cm";      // Tiefe-Anzeige 

    #endregion



    #region Commands 

    public ICommand BackCommand { get; } // Back Button 

    #endregion



    #region Werte für Anzeige 

    public string FeedbackText  // gibt den aktuellen Feedback-Text zurück 

    {

        get => feedbackText;

        set

        {

            feedbackText = value;

            UpdateUI(nameof(FeedbackText));

        }

    }

    public Color FeedbackColor // gibt die aktuelle Feedback-Farbe zurück 

    {

        get => feedbackColor;

        set

        {

            feedbackColor = value;

            UpdateUI(nameof(FeedbackColor));

        }

    }

    public string RateText // gibt die aktuelle Frequenzanzeige zurück 

    {

        get => rateText;

        set

        {

            rateText = value;

            UpdateUI(nameof(RateText));

        }

    }

    public string DepthText // gibt die aktuelle Tiefenanzeige zurück 

    {

        get => depthText;

        set

        {

            depthText = value;

            UpdateUI(nameof(DepthText));

        }

    }

    public string CPRDurationText // gibt die aktuelle CPR-Zeit zurück 

    {

        get => cprDurationText;

        set

        {

            cprDurationText = value;

            UpdateUI(nameof(CPRDurationText));

        }

    }

    public string NoFlowText // gibt die aktuelle No-Flow-Zeit zurück 

    {

        get => noFlowText;

        set

        {

            noFlowText = value;

            UpdateUI(nameof(NoFlowText));

        }

    }

    #endregion



    #region Konstruktor 

    public CPRViewModel()

    {

        timer = Application.Current!.Dispatcher.CreateTimer(); // Timer erstellen 

        timer.Interval = TimeSpan.FromSeconds(2); // alle 2 Sekunden CPR prüfen 

        timer.Tick += async (s, e) => await CheckCPR(); // Timer-Event 

        BackCommand = new Command(async () => await Shell.Current.GoToAsync("/InstructionPerformCPRPage")); // zurück navigieren 

    }

    #endregion



    #region CPR starten und stoppen 

    public void Start() // CPR-Seite startet 

    {

        cprStarted = false;   // CPR-Status zurücksetzen 

        noFlowActive = false; // No-Flow-Status zurücksetzen 

        CPRDurationText = "00:00";

        NoFlowText = "0 s";

        FeedbackText = "WAITING";

        FeedbackColor = Colors.Gray;

        timer.Start();

    }

    public void Stop() // CPR-Seite wird verlassen 

    {

        timer.Stop();

        StopAudio();

        isSpeaking = false;

        cprStarted = false;

        noFlowActive = false;

        CPRDurationText = "00:00";

        NoFlowText = "0 s";

    }

    #endregion



    #region CPR prüfen 

    async Task CheckCPR() // Hauptprüfung alle 2 Sekunden 

    {

        GetValues(); // Werte holen 

        ShowValues(); // Werte anzeigen 

        if (NoCPRDetected()) // keine CPR erkannt 

        {

            HandleNoCPRDetected(); // No-Flow starten 

            UpdateTimeCounters(); // No-Flow-Anzeige aktualisieren 

            await CheckNoFlowWarning(); // Warnung prüfen 

            return;

        }



        HandleCPRDetected(); // CPR erkannt 

        UpdateTimeCounters(); // CPR-Dauer aktualisieren 

        List<string> errors = new(); // sichtbare Fehlermeldungen 

        List<string> sounds = new(); // passende Audiodateien 

        AddErrorsFromClasses(errors, sounds); // Klassen in Text/Sound umwandeln 

        if (errors.Count == 0) // keine Fehler 

        {

            ShowFeedback("GOOD COMPRESSIONS", Colors.Green);

            return;

        }



        ShowFeedback(string.Join("\n", errors), Colors.Red); // Fehler anzeigen 

        if (!isSpeaking) // Audio nur starten, wenn aktuell keine Ansage läuft 

        {

            await PlayErrorsOneByOne(sounds);

        }

    }

    #endregion



    #region MATLAB Werte 

    void GetValues() // später MATLAB-Werte hier einfügen 

    {

        // TESTWERTE 

        //frequency = 90; 

        //depth = 4; 

        //fullRelease = true; 



        frequency = 0;

        depth = 0;

        fullRelease = true;

        feedbackClasses = GetFeedbackClasses(frequency, depth, fullRelease);



        // SPÄTER: 

        // frequency = matlabFrequency; 

        // depth = matlabDepth; 

        // fullRelease = matlabFullRelease; 

        // 

        // feedbackClasses = matlabFeedbackClasses; 

    }

    #endregion



    #region CPR Analyse 

    List<int> GetFeedbackClasses(double frequency, double depth, bool fullRelease)

    {

        List<int> classes = new();



        if (frequency < 100)

            classes.Add(1); // PUSH FASTER 



        if (depth < 5)

            classes.Add(2); // PUSH DEEPER 



        if (frequency > 120)

            classes.Add(3); // SLOW DOWN 



        if (depth > 6)

            classes.Add(4); // DO NOT PUSH TOO DEEP 



        if (!fullRelease)

            classes.Add(5); // RELEASE FULLY 



        if (classes.Count == 0)

            classes.Add(0); // GOOD COMPRESSIONS 



        return classes;

    }

    #endregion



    #region Prüfen ob CPR läuft 

    bool NoCPRDetected() // prüft, ob keine CPR stattfindet 

    {

        return frequency <= 0 || depth <= 0;

    }

    #endregion



    #region Zeitüberwachung der CPR 

    void UpdateTimeCounters() // aktualisiert CPR-Dauer und No-Flow-Zeit 

    {

        if (cprStarted)

        {

            TimeSpan duration = DateTime.Now - cprStartTime;

            CPRDurationText = $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}";

        }

        if (noFlowActive)

        {

            TimeSpan noFlow = DateTime.Now - noFlowStartTime;

            NoFlowText = $"{noFlow.TotalSeconds:F0} s";

        }

        else

        {

            NoFlowText = "0 s";

        }

    }

    void HandleCPRDetected() // CPR erkannt 

    {

        if (!cprStarted)

        {

            cprStarted = true;

            cprStartTime = DateTime.Now;

            CPRDurationText = "00:00";

        }

        noFlowActive = false; // No-Flow stoppen 

        NoFlowText = "0 s";

    }

    void HandleNoCPRDetected() // keine CPR erkannt 

    {

        if (!noFlowActive)

        {

            noFlowActive = true;

            noFlowStartTime = DateTime.Now;

        }

    }

    double GetNoFlowSeconds() // aktuelle No-Flow-Zeit berechnen 

    {

        if (!noFlowActive) return 0;

        return (DateTime.Now - noFlowStartTime).TotalSeconds;

    }

    async Task CheckNoFlowWarning() // Warnung bei langer CPR-Unterbrechung 

    {

        if (GetNoFlowSeconds() >= NoFlowWarningSeconds)

        {

            ShowFeedback("CONTINUE COMPRESSIONS", Colors.Red);

            if (!isSpeaking)

            {

                await PlaySound("continue.mp3");

            }

        }

    }

    #endregion



    #region CPR Fälle 

    void AddErrorsFromClasses(List<string> errors, List<string> sounds)

    {

        foreach (int feedbackClass in feedbackClasses)

        {

            if (feedbackClass == 1)

            {

                errors.Add("PUSH FASTER");

                sounds.Add("push_faster.mp3");

            }

            if (feedbackClass == 2)

            {

                errors.Add("PUSH DEEPER");

                sounds.Add("push_deeper.mp3");

            }

            if (feedbackClass == 3)

            {

                errors.Add("SLOW DOWN");

                sounds.Add("slowdown.mp3");

            }

            if (feedbackClass == 4)

            {

                errors.Add("DO NOT PUSH TOO DEEP");

                sounds.Add("too_deep.mp3");

            }

            if (feedbackClass == 5)

            {

                errors.Add("RELEASE FULLY");

                sounds.Add("release_fully.mp3");

            }

        }

    }

    void ShowFeedback(string text, Color color) // Feedback-Text und Farbe setzen 

    {

        FeedbackText = text; FeedbackColor = color;

    }

    #endregion



    #region Messwerte anzeigen 

    void ShowValues() // Messwerte in UI anzeigen 

    {

        RateText = $"{frequency:F0} /min";

        DepthText = $"{depth:F1} cm";

    }



    #endregion



    #region Audio abspielen 

    async Task PlayErrorsOneByOne(List<string> sounds) // mehrere Ansagen nacheinander 

    {

        isSpeaking = true;



        foreach (string sound in sounds)

        {

            await PlaySound(sound); // Sound starten 

            while (player != null && player.IsPlaying)

            {

                await Task.Delay(50); // warten bis Audio fertig ist 

            }

            await Task.Delay(1700); // Pause zwischen Ansagen 

        }

        isSpeaking = false;

    }

    async Task PlaySound(string fileName) // eine Audiodatei abspielen 

    {

        StopAudio(); // alten Sound stoppen 

        var stream = await FileSystem.OpenAppPackageFileAsync(fileName); // Datei öffnen 

        player = AudioManager.Current.CreatePlayer(stream); // Player erstellen 

        player.Play(); // Sound starten 

    }

    void StopAudio() // Audio stoppen und Player freigeben 

    {

        if (player != null)

        {

            player.Stop();

            player.Dispose();

            player = null;

        }

    }

    #endregion



    #region UI neu laden 

    void UpdateUI(string propertyName) // UI über Änderung informieren 

    {

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }

    #endregion

}