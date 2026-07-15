using Live_CPR_Feedback_App.Services;
using Live_CPR_Feedback_App.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Services
{
    public class AnalysisServiceFactory : IAnalysisServiceFactory
    {
        private readonly AnalysisServiceKI _ki;
        private readonly AnalysisServiceSP _sp;
        private readonly SettingsViewModel _settings;

        public AnalysisServiceFactory(AnalysisServiceKI ki, AnalysisServiceSP sp, SettingsViewModel settings)
        {
            _ki = ki;
            _sp = sp;
            _settings = settings;
        }

        public IAnalysisService GetCurrentService()
        {
            return _settings.UseKI ? _ki : _sp;
        }
    }
}
