using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CleanAimTracker
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SessionDetailWindow : Window
    {
        public SessionDetailWindow()
        {
            InitializeComponent();
        }
        public SessionDetailWindow(Models.SessionSummary session)
        {
            InitializeComponent();

            // Fill the text fields with session data
            EndTimeText.Text = $"End Time: {session.SessionEnd}";
            DurationText.Text = $"Duration: {session.Duration}";
            SamplesText.Text = $"Total Samples: {session.TotalSamples}";
            FlicksText.Text = $"Flick Count: {session.FlickCount}";
        }

    }
}
