using System;

namespace CleanAimTracker.Models
{
    public class SessionSummary
    {
        public DateTime SessionStart { get; set; }
        public DateTime SessionEnd { get; set; }

        public TimeSpan Duration { get; set; }
        public int TotalSamples { get; set; }
        public int MicroAdjustmentCount { get; set; }
        public int OvershootCount { get; set; }
        public int UndershootCount { get; set; }
        public int FlickCount { get; set; }
        public double AverageVelocity { get; set; }
        public double PeakVelocity { get; set; }

        // Optional: a unique ID for the session
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
