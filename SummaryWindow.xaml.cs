using Microsoft.UI.Xaml;
using CleanAimTracker.Models;

namespace CleanAimTracker
{
    public sealed partial class SummaryWindow : Window
    {
        private SessionSummary _summary;
        public MainWindow _main;

        public SummaryWindow(SessionSummary summary)
        {
            this.InitializeComponent();
            this.Closed += SummaryWindow_Closed;

            _summary = summary;

            // ⭐ Populate UI once XAML is loaded
            // Window does not have a Loaded event; attach to the root content's Loaded instead.
            var root = this.Content as FrameworkElement;
            if (root != null)
            {
                root.Loaded += SummaryWindow_Loaded;
            }
            else
            {
                // Fallback: use Activated to initialize once when the window is activated
                this.Activated += SummaryWindow_Activated;
            }

        }

        private void SummaryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // These names must match your XAML TextBlocks
            DurationText.Text = _summary.Duration.ToString();
            TotalSamplesText.Text = _summary.TotalSamples.ToString();
            MicroCountText.Text = _summary.MicroAdjustmentCount.ToString();
            OvershootText.Text = _summary.OvershootCount.ToString();
            UndershootText.Text = _summary.UndershootCount.ToString();
            FlickText.Text = _summary.FlickCount.ToString();
            AvgVelocityText.Text = _summary.AverageVelocity.ToString("F2");
            PeakVelocityText.Text = _summary.PeakVelocity.ToString("F2");
        }

        private void SummaryWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            this.Activated -= SummaryWindow_Activated;
            SummaryWindow_Loaded(this, null);
        }

        private void SummaryWindow_Closed(object sender, WindowEventArgs args)
        {
            // Optional: reopen main window or cleanup
        }
    }
}
