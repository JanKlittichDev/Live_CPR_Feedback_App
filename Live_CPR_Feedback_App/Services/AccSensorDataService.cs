using Live_CPR_Feedback_App.Models;

namespace Live_CPR_Feedback_App.Services
{
    

    public class AccSensorDataService : IAccelerometerService
    {
        public event EventHandler<AccSensorData>? ReadingChanged;

        public void Start(AccSensorSpeed speed = AccSensorSpeed.UI)
        {
            Accelerometer.Default.ReadingChanged += OnReadingChanged;

            var mauiSpeed = speed switch
            {
                AccSensorSpeed.Fastest => SensorSpeed.Fastest,
                AccSensorSpeed.Game => SensorSpeed.Game,
                AccSensorSpeed.UI => SensorSpeed.UI,
                AccSensorSpeed.Default => SensorSpeed.Default,
                _ => SensorSpeed.UI
            };

            Accelerometer.Default.Start(mauiSpeed);
        }

        public void Stop()
        {
            Accelerometer.Default.Stop();
            Accelerometer.Default.ReadingChanged -= OnReadingChanged;
        }

        private void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading.Acceleration;
            ReadingChanged?.Invoke(this, new AccSensorData
            {
                X = data.X,
                Y = data.Y,
                Z = data.Z
            });
        }
    }
}