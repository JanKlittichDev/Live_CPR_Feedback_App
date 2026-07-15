using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Live_CPR_Feedback_App.Services;
using Live_CPR_Feedback_App.Models;

namespace Live_CPR_Feedback_App.ViewModels
{
    public partial class InfoSensorViewModel : ObservableObject
    {
        private const double GravityFactor = 9.81;

        private readonly IAccelerometerService _service;

        [ObservableProperty] public partial double X { get; set; }
        [ObservableProperty] public partial double Y { get; set; }
        [ObservableProperty] public partial double Z { get; set; }
        [ObservableProperty] public partial double Total { get; set; }
        [ObservableProperty] public partial double XCorrected { get; set; }
        [ObservableProperty] public partial double YCorrected { get; set; }
        [ObservableProperty] public partial double ZCorrected { get; set; }
        [ObservableProperty] public partial double TotalCorrected { get; set; }

        public InfoSensorViewModel(IAccelerometerService service)
        {
            _service = service;
            _service.ReadingChanged += OnReadingChanged;
        }

        private void OnReadingChanged(object? sender, AccSensorData e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                X = e.X;
                Y = e.Y;
                Z = e.Z;

                Total = Math.Sqrt(X * X + Y * Y + Z * Z);

                XCorrected = X * GravityFactor;
                YCorrected = Y * GravityFactor;
                ZCorrected = Z * GravityFactor;

                TotalCorrected = Math.Sqrt(XCorrected * XCorrected + YCorrected * YCorrected + ZCorrected * ZCorrected);
            });
        }

        [RelayCommand]
        private void Start() => _service.Start(AccSensorSpeed.UI);

        [RelayCommand]
        private void Stop() => _service.Stop();
    }
}