using Microsoft.UI.Xaml;

namespace CleanAimTracker
{
    public sealed partial class SummaryWindow : Window
    {
        // STEP 4A — MainWindow reference stored here
        public MainWindow _main;

        public SummaryWindow()
        {
            this.InitializeComponent();
            this.Closed += SummaryWindow_Closed;
        }

        // Public helper to set summary values without exposing XAML fields
        public void SetSummary(
            string time,
            string distance,
            string avgSpeed,
            string peakSpeed,
            string flicks,
            string smoothness,
            string sharpness,
            string consistency,
            string quality)
        {
            TimeText.Text = time;
            DistanceText.Text = distance;
            AvgSpeedText.Text = avgSpeed;
            PeakSpeedText.Text = peakSpeed;
            FlicksText.Text = flicks;
            SmoothnessText.Text = smoothness;
            SharpnessText.Text = sharpness;
            ConsistencyText.Text = consistency;
            QualityText.Text = quality;
        }

        // STEP 6 — Live update logic
        public void UpdateSummary()
        {
            if (_main == null)
                return;

            DispatcherQueue.TryEnqueue(() =>
            {
                TimeText.Text = $"Time: {_main._sessionSeconds:F1} s";
                DistanceText.Text = $"Distance: {_main._totalDistanceInches:F1} in";
                AvgSpeedText.Text = $"Avg Speed: {_main._averageVelocity:F1} cm/s";
                PeakSpeedText.Text = $"Peak Speed: {_main._peakVelocity:F1} cm/s";
                FlicksText.Text = $"Flicks: {_main._flickCount}";
                SmoothnessText.Text = $"Smoothness: {_main._smoothnessScore:F0}";
                SharpnessText.Text = $"Correction Sharpness: {_main._correctionSharpness:F0}";
                ConsistencyText.Text = $"Movement Consistency: {_main._movementConsistency:F0}";
                QualityText.Text = $"Overall Quality: {_main._overallQualityScore:F0}";
            });
        }
        private void SummaryWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_main != null)
                _main.StatsUpdated -= UpdateSummary;

            _main = null;
        }
    }
}