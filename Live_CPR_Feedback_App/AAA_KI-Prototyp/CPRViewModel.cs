using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Live_CPR_Feedback_App.Services;
using Live_CPR_Feedback_App.Models;
using System.Windows.Input;

namespace Live_CPR_Feedback_App.ViewModels
{
    public partial class CPRViewModel : ObservableObject
    {
        bool sensorSpeed = false;

        public string CurrentCCD { get; private set; }
        public string CurrentCPM { get; private set; }

        private readonly Queue<double> compressionTimes = new();
        KIService kIService;
        bool KIstart = false;
        int kiCounter = 0;

        private readonly List<float> accDataBuffer = new List<float>(200);

        private readonly IAccelerometerService _service;

        public ICommand Go_Back_Command { get; }


        public CPRViewModel(IAccelerometerService service)
        {
            kIService = new KIService();

            _service = service;
            _service.ReadingChanged += OnReadingChanged;

            _ = InitializeKiAsync();
        }
        private async Task InitializeKiAsync()
        {
            await kIService.InitializeAsync();
            KIstart = true;
            System.Diagnostics.Debug.WriteLine("KI erfolgreich geladen");
            _service.Start(sensorSpeed);
        }
        private async void OnReadingChanged(object sender, AccSensorData e)
        {
            if (!KIstart)
            {
                System.Diagnostics.Debug.WriteLine("KI noch nicht bereit");
                return;
            }

            if (kIService == null) { return; }

            DateTime now = DateTime.UtcNow;

            accDataBuffer.Add(MathF.Sqrt(e.X * e.X + e.Y * e.Y + e.Z * e.Z));


            kiCounter++;
            if (accDataBuffer.Count > 200)
            {
                accDataBuffer.RemoveAt(0);
            }


            if (accDataBuffer.Count == 200 && kiCounter >= 100)
            {
                kiCounter = 0;
                float[] pDepth = kIService.PredictDepth(accDataBuffer.ToArray());
                float[] pRate = kIService.PredictFrequency(accDataBuffer.ToArray());
                float max_p_d = pDepth[0];
                float max_p_r = pRate[0];
                int i_p_d = 0;
                int i_p_r = 0;
                for (int i = 0; i < pDepth.Length; i++)
                {
                    if (pDepth[i] > max_p_d)
                    {
                        max_p_d = pDepth[i];
                        i_p_d = i;
                    }

                    if (pRate[i] > max_p_r)
                    {
                        max_p_r = pRate[i];
                        i_p_r = i;
                    }

                }

                string[] depthLabels =
                    {
                    "zu Flach",
                    "Optimal",
                    "zu Tief"};


                string[] rateLabels =
                {"zu Langsam",
                    "Optimal",
                    "zu Schnell"};

                CurrentCCD = depthLabels[i_p_d];
                CurrentCPM = rateLabels[i_p_r];

                OnPropertyChanged(nameof(CurrentCCD));
                OnPropertyChanged(nameof(CurrentCPM));
            }
        }
    }
}