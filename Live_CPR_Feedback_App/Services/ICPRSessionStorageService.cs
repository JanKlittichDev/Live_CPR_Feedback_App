using Live_CPR_Feedback_App.Models;

namespace Live_CPR_Feedback_App.Services
{
    public interface ICPRSessionStorageService
    {
        void StartNewSession();
        void SaveSessionAccData(TimeSpan totalTime, AccSensorData sample);
        void SaveSessionAnalysisResult(TimeSpan totalTime, AnalysisResult result);
        Task StopSessionAsync();

        List<CPRSessionInfo> GetAllSessions();      
        Task DeleteSessionAsync(CPRSessionInfo session); 
    }
}