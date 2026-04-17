using System;
using System.Windows;

namespace CleanAimTracker
{
    public partial class SessionDetailWindow : Window
    {
        public SessionDetailWindow()
        {
            InitializeComponent();
        }

        public SessionDetailWindow(Models.SessionSummary session)
        {
            InitializeComponent();

            EndTimeText.Text = $"End Time: {session.SessionEnd}";
            DurationText.Text = $"Duration: {session.Duration}";
            SamplesText.Text = $"Total Samples: {session.TotalSamples}";
            FlicksText.Text = $"Flick Count: {session.FlickCount}";
        }
    }
}
