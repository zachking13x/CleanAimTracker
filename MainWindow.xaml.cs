using Microsoft.UI.Xaml;
using CleanAimTracker.Services;
using System;

namespace CleanAimTracker
{
    public sealed partial class MainWindow : Window
    {
        private RawInputService _rawInput;
        private double _totalDistance = 0;
        private bool _isTracking = false;
        private DateTime _sessionStart;
        private DispatcherTimer _timer = new DispatcherTimer();


        public MainWindow()
        {
            this.InitializeComponent();

            // Get HWND for this WinUI window
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // Set window size (WinUI 3 requires code)
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(600, 400));

            // Initialize raw input
            _rawInput = new RawInputService();
            _rawInput.MouseMoved += OnMouseMoved;

            // Register raw input using HWND
            _rawInput.Register(hwnd);
        }

        private void OnMouseMoved(int dx, int dy)
        {
            MovementText.Text = $"ΔX: {dx}   ΔY: {dy}";

            double distance = Math.Sqrt(dx * dx + dy * dy);
            _totalDistance += distance;

            DistanceText.Text = $"Total Distance: {_totalDistance:F2}";
        }
        private void StartButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // we'll wire this up next step
        }

        private void StopButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // we'll wire this up next step
        }

        private void ResetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // we'll wire this up next step
        }

    }
}

