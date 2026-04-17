using System;

namespace CleanAimTracker.Models
{
    public class SessionSummary
    {
        public DateTime SessionStart { get; set; }
        public DateTime SessionEnd { get; set; }

        public TimeSpan Duration { get; set; }
        public int TotalSamples { get; set; }

        // Existing fields
        public int MicroAdjustmentCount { get; set; }
        public int OvershootCount { get; set; }
        public int UndershootCount { get; set; }
        public int FlickCount { get; set; }
        public double AverageVelocity { get; set; }
        public double PeakVelocity { get; set; }

        // Added fields for Summary Window
        public double Jitter { get; set; }
        public double Smoothness { get; set; }
        public double MovementConsistency { get; set; }
        public double CorrectionSharpness { get; set; }
        public double OverallQuality { get; set; }

        // Optional unique ID
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
