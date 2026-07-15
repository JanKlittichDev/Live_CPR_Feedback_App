using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Models
{
    public class FeedbackResultForUI
    {
        public FeedbackType Type { get; init; }
        public string FeedbackText { get; init; } = "WAITING";
        public bool? IsGood { get; init; }   
    }
}