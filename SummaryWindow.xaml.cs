using Microsoft.UI.Xaml;
using System.Windows;

namespace CleanAimTracker
{
    public partial class SummaryWindow : Window
    {
        public SummaryWindow(Models.SessionSummary summary)
        {
            InitializeComponent();

            DurationText.Text = summary.Duration.ToString();
            SamplesText.Text = summary.TotalSamples.ToString();
            FlicksText.Text = summary.FlickCount.ToString();
            MicroAdjustText.Text = summary.MicroAdjustmentCount.ToString();
            OvershootText.Text = summary.OvershootCount.ToString();
            UndershootText.Text = summary.UndershootCount.ToString();
            AvgVelocityText.Text = summary.AverageVelocity.ToString("0.00");
            PeakVelocityText.Text = summary.PeakVelocity.ToString("0.00");
            JitterText.Text = summary.Jitter.ToString("0.00");
            SmoothnessText.Text = summary.Smoothness.ToString("0.00");
            ConsistencyText.Text = summary.MovementConsistency.ToString("0.00");
            SharpnessText.Text = summary.CorrectionSharpness.ToString("0.00");
            OverallQualityText.Text = summary.OverallQuality.ToString("0.00");
        }
    }
}
