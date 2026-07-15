using CommunityToolkit.Mvvm.ComponentModel;
using Live_CPR_Feedback_App.Models;
using Live_CPR_Feedback_App.Services;
using System.Diagnostics;


namespace Live_CPR_Feedback_App.ViewModels;

public partial class CPRViewModel : ObservableObject
{
    // Pipeline --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /*
    *  Totaltime-Start  
    *          |
    *          v
    *  IAnalysisServiceFactory.GetCurrentService()
    *          |   (wählt einmalig beim Session-Start zwischen
    *          |    AnalysisServiceKI und AnalysisServiceSP,
    *          |    anhand SettingsViewModel.UseKI)
    *          v
    *  aktueller IAnalysisService steht fest (KI oder SP)
    *          |
    *          v
    *  AccSensorDataService (liefert rohe X/Y/Z-Werte per Event)
    *          |
    *          v
    *  CPRViewModel.OnReadingChanged(AccSensorData)
    *          |   (reicht Sample nur weiter, ohne es zu interpretieren)
    *          v
    *  IAnalysisService.AddSample(AccSensorData)    UND     ISessionStorageService.SaveSessionAccData(totaltime, AccData)  
    *          |   (Puffer, Berechnung - alles Service-intern,
    *          |    verwendet die zuvor von der Factory gelieferte Instanz)
    *          |
    *          v   (irgendwann: genug Daten gesammelt)
    *  IAnalysisService liefert AnalysisResult 
    *          |
    *          v
    *  CPRViewModel nimmt AnalysisResult entgegen
    *          |   
    *          v
    *  FeedbackService.Present(AnalysisResult)    UND     ISessionStorageService.SaveSessionAnalysisResult(totaltime, AnalysisResult)     
    *          |   (Audio abspielen, ...)
    *          v
    *  UI aktualisiert sich automatisch ueber Bindings
    *
    *
    */


    private readonly IAccelerometerService _accService;
    private readonly IAnalysisServiceFactory _analysisServiceFactory;
    private readonly FeedbackService _feedbackService;
    private readonly CPRSessionStateService _sessionStateService;
    private IAnalysisService? _analysis;   // von Factory geliefert (KI oder SP)


    private readonly ICPRSessionStorageService _sessionStorage;
    private readonly Stopwatch _totalTime = new();
    private IDispatcherTimer? _uiTimer;    // fuer CPR-Time / NoFlow-Anzeige
    private bool _isRunning;


    // Bindings ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    [ObservableProperty] public partial string CPRDurationText { get; set; } = "00:00";
    [ObservableProperty] public partial string NoFlowText { get; set; } = "00:00";
    [ObservableProperty] public partial string RateText { get; set; } = "--";
    [ObservableProperty] public partial string DepthText { get; set; } = "--";
    [ObservableProperty] public partial string FeedbackText { get; set; } = "WAITING";
    [ObservableProperty] public partial Color FeedbackColor { get; set; } = Colors.Gray;


    // Konstruktor --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public CPRViewModel(
        IAccelerometerService accService,
        IAnalysisServiceFactory analysisServiceFactory,
        FeedbackService feedbackService,
        CPRSessionStateService sessionStateService,
        ICPRSessionStorageService sessionStorage)
    {
        _accService = accService;
        _analysisServiceFactory = analysisServiceFactory;
        _feedbackService = feedbackService;
        _sessionStateService = sessionStateService;
        _sessionStorage = sessionStorage;
    }

    // Lifecycle (OnAppearing/OnDisappearing der Page) ---------------------------------------------------------------------------------------------------------------------------------------------------
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        _totalTime.Restart();
        _sessionStorage.StartNewSession();

        _analysis = _analysisServiceFactory.GetCurrentService(); // einmalig KI/SP
        _analysis.AnalysisCompleted += OnAnalysisCompleted;

        _accService.ReadingChanged += OnReadingChanged;
        _accService.Start(AccSensorSpeed.Game); // Game/Fastest -> genug Samples fuer Tiefe

        _uiTimer = Application.Current!.Dispatcher.CreateTimer();
        _uiTimer.Interval = TimeSpan.FromMilliseconds(500);
        _uiTimer.Tick += OnUiTimerTick;
        _uiTimer.Start();
    }

    public async void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _totalTime.Stop();

        _accService.Stop();
        _accService.ReadingChanged -= OnReadingChanged;

        if (_analysis is not null)
            _analysis.AnalysisCompleted -= OnAnalysisCompleted;

        if (_uiTimer is not null)
        {
            _uiTimer.Stop();
            _uiTimer.Tick -= OnUiTimerTick;
            _uiTimer = null;
        }

        await _sessionStorage.StopSessionAsync();
        _feedbackService.Reset();
        _sessionStateService.Reset();
        _analysis?.Reset();
    }


    // Sensor -> Analyse + Speichern (AccSample) ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnReadingChanged(object? sender, AccSensorData sample)
    {
        _analysis?.AddSample(sample);
        _sessionStorage.SaveSessionAccData(_totalTime.Elapsed, sample);
    }

    // Timer -> SessionState-Abfrage -> CPRTime/NoFlow ---------------------------------------------------------------------------------------------------------------------------------------------------------
    private void OnUiTimerTick(object? sender, EventArgs e)
    {
        CPRDurationText = _sessionStateService.GetCPRDurationText();
        NoFlowText = _sessionStateService.GetNoFlowText();
    }

    // AnalysisResult -> SessionStateUpdate + Speichern (AnalysisResult) + Feedbackprio&Audio + UI-Rate/Depth/FeedbackText/FeedbackColor ----------------------------------------------------------------------------
    private async void OnAnalysisCompleted(object? sender, AnalysisResult result)
    {
        try
        {
            _sessionStateService.Update(result); // Zeit-State zuerst aktualisieren
            _sessionStorage.SaveSessionAnalysisResult(_totalTime.Elapsed, result);

            FeedbackResultForUI feedback = await _feedbackService.Present(result); // Feedback-Priorisierung + Audio

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (result.Frequency.HasValue) RateText = $"{result.Frequency.Value:F0}/min";
                if (result.Depth.HasValue) DepthText = $"{result.Depth.Value:F1}cm";

                FeedbackText = feedback.FeedbackText;
                FeedbackColor = feedback.IsGood switch
                {
                    true => Colors.Green,
                    false => Colors.Red,
                    null => Colors.Gray
                };
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnAnalysisCompleted: {ex}");
        }
    }
    
        

    

}