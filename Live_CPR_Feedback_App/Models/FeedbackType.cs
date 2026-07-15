using System;
using System.Collections.Generic;
using System.Text;

namespace Live_CPR_Feedback_App.Models
{
    public enum FeedbackType
    {
        None,          // GOOD COMPRESSIONS
        NoCPR,         
        PushDeeper,
        TooDeep,
        PushFaster,
        Slowdown,
        ReleaseFully
    }
}
