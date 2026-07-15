using System.Diagnostics;
namespace Live_CPR_Feedback_App_DataCollection
{
    public partial class MainPage : ContentPage
    {
        private Stopwatch stopwatch;
        private StreamWriter? logWriter;
        private readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "acceleration_log.txt");

        public MainPage()
        {
            InitializeComponent();
            stopwatch = Stopwatch.StartNew();

            logWriter = new StreamWriter(filePath, append: false)
            {
                AutoFlush = true
            };

            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.Fastest);
            }
        }

        private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            var x = data.Acceleration.X;
            var y = data.Acceleration.Y;
            var z = data.Acceleration.Z;

            var totalAcceleration = Math.Sqrt(x * x + y * y + z * z);
            var elapsedTime = stopwatch.Elapsed;

            var accLogLine = $"{x:F4};{y:F4};{z:F4};{totalAcceleration:F4};{elapsedTime.TotalSeconds:F4}";

            logWriter?.WriteLine(accLogLine);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AccelXLabel.Text = $"Acceleration_X: {x:F4}";
                AccelYLabel.Text = $"Acceleration_Y: {y:F4}";
                AccelZLabel.Text = $"Acceleration_Z: {z:F4}";
                AccelTotalLabel.Text = $"Total Acceleration: {totalAcceleration:F4}";
                ElapsedTimeLabel.Text = $"Time Since Start: {elapsedTime:hh\\:mm\\:ss}";
            });
        }
    }
}