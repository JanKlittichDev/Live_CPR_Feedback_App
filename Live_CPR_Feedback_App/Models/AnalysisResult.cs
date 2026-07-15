using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Models
{
    public class AnalysisResult
    {
        public double? Frequency { get; init; }   // null im KI-Modus 
        public double? Depth { get; init; }        // null im KI-Modus
        public StateOfRate RateState { get; init; } 
        public StateOfDepth DepthState { get; init; } 
        public StateOfRelease? ReleaseState { get; init; } // null im KI-Modus & SP-Modus
        public bool CompressionDetected { get; init; }  
    }
}
