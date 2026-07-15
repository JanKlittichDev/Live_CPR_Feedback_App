namespace Live_CPR_Feedback_App.Models
{
    public class CPRSessionInfo
    {
        public string AccFilePath { get; init; } = "";
        public string AnalysisResultFilePath { get; init; } = "";
        public DateTime SessionDate { get; init; }
        public string DisplayName => SessionDate.ToString("dd.MM.yyyy HH:mm");
    }
}