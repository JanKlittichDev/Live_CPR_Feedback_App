using Live_CPR_Feedback_App.Models;

namespace Live_CPR_Feedback_App.Services
{
    // Zeitverwaltung (Allgemein UND NoFlow/CPR-Time)
    public class CPRSessionStateService 
    {
        private DateTime? _cprSegmentStartTime = null;      // Start des aktuellen CPR-Segments
        private DateTime? _noFlowSegmentStartTime = null;   // Start des aktuellen NoFlow-Segments
        private TimeSpan _cprDuration = TimeSpan.Zero;    // Summe aller abgeschlossenen CPR-Segmente
        private TimeSpan _noFlowDuration = TimeSpan.Zero; // Summe aller abgeschlossenen No-Flow-Segmente

        // State-Uebergang: durch AnalysisResult getriggert
        public void Update(AnalysisResult result)
        {
            if (result.CompressionDetected)
            {
                // Falls gerade eine No-Flow-Phase lief: abschließen und aufaddieren
                if (_noFlowSegmentStartTime.HasValue)
                {
                    _noFlowDuration += DateTime.Now - _noFlowSegmentStartTime.Value;
                    _noFlowSegmentStartTime = null;
                }
                // Neues CPR-Segment starten, falls noch keins läuft
                _cprSegmentStartTime ??= DateTime.Now;
            }
            else
            {
                // Falls gerade eine CPR-Phase lief: abschließen und aufaddieren
                if (_cprSegmentStartTime.HasValue)
                {
                    _cprDuration += DateTime.Now - _cprSegmentStartTime.Value;
                    _cprSegmentStartTime = null;
                }
                // Neues No-Flow-Segment starten, falls noch keins läuft
                _noFlowSegmentStartTime ??= DateTime.Now;
            }
        }

        // Anzeige-Strings: durch Timer-Tick abgefragt
        public string GetCPRDurationText()
        {
            var total = _cprDuration;
            if (_cprSegmentStartTime.HasValue)
                total += DateTime.Now - _cprSegmentStartTime.Value; // laufendes Segment dazu

            return $"{(int)total.TotalMinutes:D2}:{total.Seconds:D2}";
        }

        public string GetNoFlowText()
        {
            var total = _noFlowDuration;
            if (_noFlowSegmentStartTime.HasValue)
                total += DateTime.Now - _noFlowSegmentStartTime.Value;

            return $"{(int)total.TotalMinutes:D2}:{total.Seconds:D2}";
        }



        // Helper
        public bool IsNoFlowActive => _noFlowSegmentStartTime.HasValue;

        // Session-Ende
        public void Reset()
        {
            _cprSegmentStartTime = null;
            _noFlowSegmentStartTime = null;
            _cprDuration = TimeSpan.Zero;
            _noFlowDuration = TimeSpan.Zero;
        }
    }
}