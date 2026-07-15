using Live_CPR_Feedback_App.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Services
{
    public interface IAnalysisService
    {
        event EventHandler<AnalysisResult>? AnalysisCompleted;
        void AddSample(AccSensorData sample);
        void Reset();
    }

}
