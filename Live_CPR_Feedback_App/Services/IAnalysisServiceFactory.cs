using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Services
{
    public interface IAnalysisServiceFactory
    {
        IAnalysisService GetCurrentService();
    }
}
