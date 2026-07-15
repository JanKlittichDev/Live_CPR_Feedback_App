using Live_CPR_Feedback_App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Services
{
    public interface IAccelerometerService
    {
        event EventHandler<AccSensorData> ReadingChanged;
        void Start(AccSensorSpeed speed = AccSensorSpeed.UI);
        void Stop();
    }
}
