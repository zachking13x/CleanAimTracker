using System;
using System.Collections.Generic;

namespace CleanAimTracker.Models
{
    public class SessionData
    {
        public List<MouseSample> Samples { get; set; } = new();
        public List<MicroAdjustmentEvent> MicroAdjustments { get; set; } = new();
        public List<OvershootEvent> Overshoots { get; set; } = new();
        public List<UndershootEvent> Undershoots { get; set; } = new();
        public List<FlickEvent> Flicks { get; set; } = new();

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int? DpiOverride { get; set; }
        public double? SensitivityOverride { get; set; }

        public TimeSpan Duration => EndTime - StartTime;

        public SessionSummary BuildSummary()
        {
            var summary = new SessionSummary();

            summary.Duration = this.Duration;
            summary.TotalSamples = Samples.Count;
            summary.MicroAdjustmentCount = MicroAdjustments.Count;
            summary.OvershootCount = Overshoots.Count;
            summary.UndershootCount = Undershoots.Count;
            summary.FlickCount = Flicks.Count;

            if (Samples.Count > 0)
            {
                summary.AverageVelocity = 0;

                foreach (var s in Samples)
                    summary.AverageVelocity += s.Velocity;

                summary.AverageVelocity /= Samples.Count;
            }
            else
            {
                summary.AverageVelocity = 0;
            }

            if (Samples.Count > 0)
            {
                summary.PeakVelocity = 0;

                foreach (var s in Samples)
                    if (s.Velocity > summary.PeakVelocity)
                        summary.PeakVelocity = s.Velocity;
            }
            else
            {
                summary.PeakVelocity = 0;
            }


            // We will fill this in during Step 4D
            return summary;
        }

    }

    public class MouseSample
    {
        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
        public double Velocity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MicroAdjustmentEvent
    {
        public double Magnitude { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OvershootEvent
    {
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UndershootEvent
    {
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class FlickEvent
    {
        public double Angle { get; set; }
        public double Distance { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

