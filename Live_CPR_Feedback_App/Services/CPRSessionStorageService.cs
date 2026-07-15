using System.Globalization;
using System.Text;
using Live_CPR_Feedback_App.Models;

namespace Live_CPR_Feedback_App.Services
{
    public class CPRSessionStorageService : ICPRSessionStorageService
    {
        private readonly StringBuilder _accBuffer = new();
        private readonly StringBuilder _analysisResultBuffer = new();

        private string _accFilePath = "";
        private string _analysisResultFilePath = "";
        private volatile bool _sessionActive;

        // Neue Session -> neue Dateien, Header schreiben --------------------------
        public void StartNewSession()
        {
            _accBuffer.Clear();
            _analysisResultBuffer.Clear();

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var folder = FileSystem.AppDataDirectory;

            _accFilePath = Path.Combine(folder, $"acc_{stamp}.csv");
            _analysisResultFilePath = Path.Combine(folder, $"analysisResult_{stamp}.csv");

            _accBuffer.AppendLine("TotalTimeSeconds,X,Y,Z");
            _analysisResultBuffer.AppendLine("TotalTimeSeconds,CompressionDetected,Frequency,Depth,RateState,DepthState,ReleaseState");

            _sessionActive = true;
        }

        // Sample-Zeile puffern -----------------------------------------------------
        public void SaveSessionAccData(TimeSpan totalTime, AccSensorData sample)
        {
            if (!_sessionActive) return;

            _accBuffer.AppendLine(string.Join(",",
                totalTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture),
                sample.X.ToString("F5", CultureInfo.InvariantCulture),
                sample.Y.ToString("F5", CultureInfo.InvariantCulture),
                sample.Z.ToString("F5", CultureInfo.InvariantCulture)));
        }

        // AnalysisResult-Zeile puffern ----------------------------------------------
        public void SaveSessionAnalysisResult(TimeSpan totalTime, AnalysisResult result)
        {
            if (!_sessionActive) return;

            _analysisResultBuffer.AppendLine(string.Join(",",
                totalTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture),
                result.CompressionDetected,
                result.Frequency?.ToString("F1", CultureInfo.InvariantCulture) ?? "",
                result.Depth?.ToString("F1", CultureInfo.InvariantCulture) ?? "",
                result.RateState,
                result.DepthState,
                result.ReleaseState?.ToString() ?? ""));
        }

        // Session beenden -> auf Disk schreiben --------------------------------------
        public async Task StopSessionAsync()
        {
            if (!_sessionActive) return;
            _sessionActive = false;

            await Task.Delay(50);

            try
            {
                await File.WriteAllTextAsync(_accFilePath, _accBuffer.ToString());
                await File.WriteAllTextAsync(_analysisResultFilePath, _analysisResultBuffer.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CPRSessionStorageService: {ex}");
            }
        }


        public List<CPRSessionInfo> GetAllSessions()
        {
            var folder = FileSystem.AppDataDirectory;
            var accFiles = Directory.GetFiles(folder, "acc_*.csv");

            var sessions = new List<CPRSessionInfo>();
            foreach (var accPath in accFiles)
            {
                var stamp = Path.GetFileNameWithoutExtension(accPath).Replace("acc_", "");
                var analysisResultPath = Path.Combine(folder, $"analysisResult_{stamp}.csv");

                if (!File.Exists(analysisResultPath)) continue; // unvollstaendiges Paar ueberspringen

                if (DateTime.TryParseExact(stamp, "yyyyMMdd_HHmmss", null,
                    System.Globalization.DateTimeStyles.None, out var date))
                {
                    sessions.Add(new CPRSessionInfo
                    {
                        AccFilePath = accPath,
                        AnalysisResultFilePath = analysisResultPath,
                        SessionDate = date
                    });
                }
            }

            return sessions.OrderByDescending(s => s.SessionDate).ToList();
        }


        public Task DeleteSessionAsync(CPRSessionInfo session)
        {
            try
            {
                if (File.Exists(session.AccFilePath)) File.Delete(session.AccFilePath);
                if (File.Exists(session.AnalysisResultFilePath)) File.Delete(session.AnalysisResultFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteSessionAsync: {ex}");
            }
            return Task.CompletedTask;
        }



    }
}