using System;

namespace CleanAimTracker.Models
{
    public class SessionSummary
    {
        public TimeSpan Duration { get; set; }

        public int TotalSamples { get; set; }
        public int MicroAdjustmentCount { get; set; }
        public int OvershootCount { get; set; }
        public int UndershootCount { get; set; }
        public int FlickCount { get; set; }

        public double AverageVelocity { get; set; }
        public double PeakVelocity { get; set; }
    }
}
