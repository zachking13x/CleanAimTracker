using CleanAimTracker.Helpers;
using CleanAimTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinRT.Interop;

namespace CleanAimTracker
{
    public sealed partial class MainWindow : Window
    {
        // -----------------------------
        // Services
        // -----------------------------
        private readonly RawInputService _rawInput;

        // -----------------------------
        // Events
        // -----------------------------
        public event Action? StatsUpdated;

        // -----------------------------
        // Aim analytics fields
        // -----------------------------
        private double _totalDistance = 0;
        private double _currentVelocity = 0;          // cm/s
        public double _peakVelocity = 0;              // fastest cm/s recorded
        private int _lastDx;
        private int _lastDy;
        public double _averageVelocity = 0;           // cm/s over entire session
        private int _movementEvents = 0;              // number of raw mouse events this session
        public double _sessionSeconds = 0;            // total session time in seconds
        public double _totalDistanceInches = 0;       // total distance in inches
        public int _flickCount = 0;                   // number of high-speed flicks
        private DateTime _lastFlickTime = DateTime.MinValue;
        private int _smallFlicks = 0;                 // 50–100 cm/s
        private int _largeFlicks = 0;                 // 100+ cm/s
        private double _jitterAmount = 0;             // total tiny movements
        private double _movementDensity = 0;          // events per second
        private double _idleTime = 0;                 // seconds without movement
        private int _lastMovementEvents = 0;          // used to detect idle
        private double _lastAngle = 0;                // last movement direction in degrees
        private double _angleStability = 0;           // accumulated angle changes
        private double _angleChangeTotal = 0;         // total angle change between events
        private double _rawAngleDelta = 0;            // per-event angle difference
        private double _trueAngleDelta = 0;           // difference between current and previous angle
        private double _previousAngle = 0;            // angle from previous event
        private double _distancePerEventTotal = 0;    // total raw movement magnitude
        private double _averageDistancePerEvent = 0;  // average movement magnitude
        private double _peakDistancePerEvent = 0;     // largest single movement magnitude
        private double _peakVelocityChange = 0;       // largest speed spike between events
        private double _previousVelocity = 0;         // used to compute velocity change
        private double _currentAcceleration = 0;      // acceleration per event
        private double _peakAcceleration = 0;         // highest acceleration spike
        private double _totalAcceleration = 0;        // sum of all acceleration values
        private double _averageAcceleration = 0;      // mean acceleration
        public double _smoothnessScore = 100;         // 0–100 tracking smoothness
        public double _correctionSharpness = 0;       // 0–100 correction intensity
        public double _movementConsistency = 100;     // 0–100 movement consistency score
        public double _overallQualityScore = 100;     // combined aim quality score

        private int _movementCountThisSecond = 0;     // events in the current 1-second window
        private int _lastMovementCount = 0;           // used to compute per-second bursts
        private double _rollingDensity = 0;           // events per second (rolling)

        private int _idleBurstCount = 0;              // number of bursts after idle
        private bool _wasIdle = false;                // tracks if user was idle last tick
        private double _idleTimeSeconds = 0;          // time since last movement
        private double _idlePercentage = 0;           // percent of session spent idle
        private DateTime _lastMoveTime = DateTime.Now;

        // -----------------------------
        // Session + timer
        // -----------------------------
        private bool _isTracking = false;
        private DateTime _sessionStart;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        // -----------------------------
        // DPI + sensitivity
        // -----------------------------
        private double _dpi = 800;
        private double _sensitivity = 1.0;

        // -----------------------------
        // Constructor
        // -----------------------------
        public MainWindow()
        {
            this.InitializeComponent();

            // Resize window
            nint hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 900));

            // Raw input service
            _rawInput = new RawInputService();
            _rawInput.MouseMoved += OnMouseMoved;
            _rawInput.Register(hwnd);

            // Windows message hook for WM_INPUT
            WindowMessageHook.Initialize(this, WndProc);

            // Timer for per-second updates
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        // -----------------------------
        // Window procedure for WM_INPUT
        // -----------------------------
        private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_INPUT = 0x00FF;

            if (msg == WM_INPUT)
                _rawInput.ProcessRawInput(lParam);

            return IntPtr.Zero;
        }

        // -----------------------------
        // Raw mouse movement handler
        // -----------------------------
        private void OnMouseMoved(int dx, int dy)
        {
            _lastDx = dx;
            _lastDy = dy;

            LastDeltaText.Text = $"Last Δ: {dx}, {dy}";

            if (!_isTracking)
                return;

            _movementEvents++;

            MovementText.Text = $"ΔX: {dx}   ΔY: {dy}";

            // Distance per event
            double eventDistance = Math.Sqrt(dx * dx + dy * dy);
            _distancePerEventTotal += eventDistance;
            DistancePerEventText.Text = $"Dist/Event: {_distancePerEventTotal:F0}";

            // Movement consistency (0–100)
            double distanceDeviation = Math.Abs(eventDistance - _averageDistancePerEvent);
            _movementConsistency = 100 - (distanceDeviation * 10);
            if (_movementConsistency < 0) _movementConsistency = 0;
            else if (_movementConsistency > 100) _movementConsistency = 100;
            MovementConsistencyText.Text = $"Movement Consistency: {_movementConsistency:F0}";

            // Overall aim quality (0–100)
            _overallQualityScore = (_smoothnessScore + _correctionSharpness + _movementConsistency) / 3;
            if (_overallQualityScore < 0) _overallQualityScore = 0;
            else if (_overallQualityScore > 100) _overallQualityScore = 100;
            OverallQualityText.Text = $"Overall Quality: {_overallQualityScore:F0}";

            // Peak distance per event
            if (eventDistance > _peakDistancePerEvent)
            {
                _peakDistancePerEvent = eventDistance;
                PeakDistancePerEventText.Text = $"Peak Dist/Event: {_peakDistancePerEvent:F0}";
            }

            // Movement angle
            _lastAngle = Math.Atan2(dy, dx) * (180 / Math.PI);
            AngleText.Text = $"Angle: {_lastAngle:F0}°";

            // True angle delta
            double angleDifference = Math.Abs(_lastAngle - _previousAngle);
            _trueAngleDelta = angleDifference;
            TrueAngleDeltaText.Text = $"True Delta: {_trueAngleDelta:F2}°";

            // Smoothness (0–100)
            _smoothnessScore = 100 - (Math.Abs(_trueAngleDelta) * 2);
            if (_smoothnessScore < 0) _smoothnessScore = 0;
            else if (_smoothnessScore > 100) _smoothnessScore = 100;
            SmoothnessText.Text = $"Smoothness: {_smoothnessScore:F0}";

            // Raw angle delta
            _rawAngleDelta = angleDifference;
            RawAngleDeltaText.Text = $"Raw Delta: {_rawAngleDelta:F2}°";

            // Angle change total
            _angleChangeTotal += angleDifference;
            AngleChangeText.Text = $"Angle Change: {_angleChangeTotal:F2}°";

            // Angle stability
            _angleStability = angleDifference;
            AngleStabilityText.Text = $"Stability: {_angleStability:F2}°";

            // Jitter detection
            if (Math.Abs(dx) + Math.Abs(dy) < 3)
            {
                _jitterAmount++;
                JitterText.Text = $"Jitter: {_jitterAmount:F0}";
            }

            _previousAngle = _lastAngle;
            PreviousAngleText.Text = $"Prev Angle: {_previousAngle:F0}°";

            // Convert counts → cm
            double counts = Math.Abs(dx) + Math.Abs(dy);
            double cmMoved = counts / _dpi * 2.54;
            _totalDistance += cmMoved;
            _totalDistanceInches += cmMoved / 2.54;
            DistanceText.Text = $"Total Distance: {_totalDistance:F2} cm";

            // Velocity (cm/s)
            DateTime now = DateTime.Now;
            double deltaTime = (now - _lastMoveTime).TotalSeconds;
            _lastMoveTime = now;

            if (deltaTime > 0)
            {
                _currentVelocity = cmMoved / deltaTime;
                VelocityText.Text = $"Speed: {_currentVelocity:F2} cm/s";
                CurrentVelocityText.Text = $"Current: {_currentVelocity:F1} cm/s";

                // Acceleration
                _currentAcceleration = (_currentVelocity - _previousVelocity) / deltaTime;
                AccelerationText.Text = $"Accel: {_currentAcceleration:F2} cm/s²";

                _totalAcceleration += _currentAcceleration;
                if (_movementEvents > 0)
                {
                    _averageAcceleration = _totalAcceleration / _movementEvents;
                    AverageAccelerationText.Text = $"Avg Accel: {_averageAcceleration:F2} cm/s²";
                }

                if (_currentAcceleration > _peakAcceleration)
                {
                    _peakAcceleration = _currentAcceleration;
                    PeakAccelerationText.Text = $"Peak Accel: {_peakAcceleration:F2} cm/s²";
                }

                // Idle burst detection
                if (_wasIdle && _currentVelocity > 20)
                {
                    _idleBurstCount++;
                    IdleBurstText.Text = $"Idle Bursts: {_idleBurstCount}";
                }

                _wasIdle = (_currentVelocity < 1);

                // Peak velocity + flicks
                if (_currentVelocity > _peakVelocity)
                {
                    _peakVelocity = _currentVelocity;
                    PeakVelocityText.Text = $"Peak: {_peakVelocity:F2} cm/s";

                    if (_currentVelocity > 50)
                    {
                        DateTime nowInner = DateTime.Now;
                        if ((nowInner - _lastFlickTime).TotalMilliseconds > 150)
                        {
                            _flickCount++;
                            FlickCountText.Text = $"Flicks: {_flickCount}";

                            if (_currentVelocity < 100)
                            {
                                _smallFlicks++;
                                SmallFlicksText.Text = $"Small Flicks: {_smallFlicks}";
                            }
                            else
                            {
                                _largeFlicks++;
                                LargeFlicksText.Text = $"Large Flicks: {_largeFlicks}";
                            }

                            _lastFlickTime = nowInner;
                        }
                    }
                }

                // Velocity change
                double velocityChange = Math.Abs(_currentVelocity - _previousVelocity);

                _correctionSharpness = velocityChange * 2;
                if (_correctionSharpness > 100)
                    _correctionSharpness = 100;
                CorrectionSharpnessText.Text = $"Correction Sharpness: {_correctionSharpness:F0}";

                if (velocityChange > _peakVelocityChange)
                {
                    _peakVelocityChange = velocityChange;
                    PeakVelocityChangeText.Text = $"Peak Vel Change: {_peakVelocityChange:F2}";
                }

                _previousVelocity = _currentVelocity;
            }

            // Notify listeners
            StatsUpdated?.Invoke();
        }

        // -----------------------------
        // Start button
        // -----------------------------
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Read DPI + sensitivity from UI
            if (double.TryParse(DpiInput.Text, out double dpi))
                _dpi = dpi;

            if (double.TryParse(SensitivityInput.Text, out double sens))
                _sensitivity = sens;

            _isTracking = true;
            _sessionStart = DateTime.Now;
            _totalDistance = 0;

            DistanceText.Text = "Total Distance: 0";
            MovementText.Text = "ΔX: 0   ΔY: 0";

            double cm360 = CalculateCmPer360();
            Cm360Text.Text = $"cm/360: {cm360:F2}";

            _timer.Start();
        }

        // -----------------------------
        // Stop button
        // -----------------------------
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isTracking = false;
            _timer.Stop();
        }

        // -----------------------------
        // Reset button
        // -----------------------------
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _isTracking = false;
            _timer.Stop();

            _totalDistance = 0;
            DistanceText.Text = "Total Distance: 0";

            MovementText.Text = "ΔX: 0   ΔY: 0";

            _lastDx = 0;
            _lastDy = 0;
            LastDeltaText.Text = "Last Δ: 0, 0";

            TimeText.Text = "Session Time: 00:00";

            Cm360Text.Text = "cm/360: --";

            _peakVelocity = 0;
            PeakVelocityText.Text = "Peak: 0 cm/s";

            _averageVelocity = 0;
            AverageVelocityText.Text = "Average: 0 cm/s";

            _currentVelocity = 0;
            CurrentVelocityText.Text = "Current: 0 cm/s";

            _movementEvents = 0;
            _sessionSeconds = 0;
            _totalDistanceInches = 0;

            _flickCount = 0;
            FlickCountText.Text = "Flicks: 0";

            _lastFlickTime = DateTime.MinValue;

            _smallFlicks = 0;
            SmallFlicksText.Text = "Small Flicks: 0";

            _largeFlicks = 0;
            LargeFlicksText.Text = "Large Flicks: 0";

            _jitterAmount = 0;
            JitterText.Text = "Jitter: 0";

            _movementDensity = 0;
            MovementDensityText.Text = "Density: 0 eps";

            _idleTime = 0;
            IdleTimeText.Text = "Idle: 0 s";

            _lastMovementEvents = 0;

            _lastAngle = 0;
            AngleText.Text = "Angle: 0°";

            _angleStability = 0;
            AngleStabilityText.Text = "Stability: 0";

            _angleChangeTotal = 0;
            AngleChangeText.Text = "Angle Change: 0";

            _rawAngleDelta = 0;
            RawAngleDeltaText.Text = "Raw Delta: 0";

            _trueAngleDelta = 0;
            TrueAngleDeltaText.Text = "True Delta: 0";

            _previousAngle = 0;
            PreviousAngleText.Text = "Prev Angle: 0°";

            _distancePerEventTotal = 0;
            DistancePerEventText.Text = "Dist/Event: 0";

            _averageDistancePerEvent = 0;
            AverageDistancePerEventText.Text = "Avg Dist/Event: 0";

            _peakDistancePerEvent = 0;
            PeakDistancePerEventText.Text = "Peak Dist/Event: 0";

            _peakVelocityChange = 0;
            PeakVelocityChangeText.Text = "Peak Vel Change: 0";

            _previousVelocity = 0;

            _movementCountThisSecond = 0;
            MovementCountPerSecondText.Text = "MPS: 0";

            _lastMovementCount = 0;

            _rollingDensity = 0;
            RollingDensityText.Text = "Rolling Density: 0 eps";

            _idleBurstCount = 0;
            IdleBurstText.Text = "Idle Bursts: 0";

            _wasIdle = false;

            _idleTimeSeconds = 0;
            IdleTimeSinceMoveText.Text = "Idle Time: 0.0 s";

            _idlePercentage = 0;
            IdlePercentageText.Text = "Idle %: 0%";

            _currentAcceleration = 0;
            AccelerationText.Text = "Accel: 0 cm/s²";

            _peakAcceleration = 0;
            PeakAccelerationText.Text = "Peak Accel: 0 cm/s²";

            _totalAcceleration = 0;
            _averageAcceleration = 0;
            AverageAccelerationText.Text = "Avg Accel: 0 cm/s²";

            _smoothnessScore = 100;
            SmoothnessText.Text = "Smoothness: 100";

            _correctionSharpness = 0;
            CorrectionSharpnessText.Text = "Correction Sharpness: 0";

            _movementConsistency = 100;
            MovementConsistencyText.Text = "Movement Consistency: 100";

            _overallQualityScore = 100;
            OverallQualityText.Text = "Overall Quality: 100";
        }

        // -----------------------------
        // Open summary button
        // -----------------------------
        private void OpenSummary_Click(object sender, RoutedEventArgs e)
        {
            // Manually trigger a summary window using the current session
            // NOTE: this assumes you want to end the current session when opening summary
            _isTracking = false;
            _timer.Stop();

            // Build a summary from current stats (UI-driven session)
            var summary = new Models.SessionSummary
            {
                Duration = TimeSpan.FromSeconds(_sessionSeconds),
                TotalSamples = _movementEvents,
                MicroAdjustmentCount = 0, // not tracked here
                OvershootCount = 0,       // not tracked here
                UndershootCount = 0,      // not tracked here
                FlickCount = _flickCount,
                AverageVelocity = _averageVelocity,
                PeakVelocity = _peakVelocity
            };
            SessionStorage.SaveSession(summary);

            var win = new SummaryWindow(summary);
            win.Activate();
        }

        // -----------------------------
        // Timer tick (per second)
        // -----------------------------
        private void Timer_Tick(object sender, object e)
        {
            if (!_isTracking)
                return;

            TimeSpan elapsed = DateTime.Now - _sessionStart;
            TimeText.Text = $"Session Time: {elapsed:mm\\:ss}";

            _sessionSeconds = elapsed.TotalSeconds;

            // Idle percentage
            if (_sessionSeconds > 0)
            {
                _idlePercentage = (_idleTimeSeconds / _sessionSeconds) * 100.0;
                IdlePercentageText.Text = $"Idle %: {_idlePercentage:F1}%";
            }

            // Movement count per second
            int eventsThisTick = _movementEvents - _lastMovementCount;
            _movementCountThisSecond = eventsThisTick;
            MovementCountPerSecondText.Text = $"MPS: {_movementCountThisSecond}";
            _lastMovementCount = _movementEvents;

            // Idle time since last movement
            if (_wasIdle)
                _idleTimeSeconds += 1.0;
            else
                _idleTimeSeconds = 0;

            IdleTimeSinceMoveText.Text = $"Idle Time: {_idleTimeSeconds:F1} s";

            // Rolling density
            _rollingDensity = _movementCountThisSecond;
            RollingDensityText.Text = $"Rolling Density: {_rollingDensity:F2} eps";

            // Average distance per event
            if (_movementEvents > 0)
            {
                _averageDistancePerEvent = _distancePerEventTotal / _movementEvents;
                AverageDistancePerEventText.Text = $"Avg Dist/Event: {_averageDistancePerEvent:F2}";
            }

            // Idle time
            if (_movementEvents == _lastMovementEvents)
            {
                _idleTime++;
                IdleTimeText.Text = $"Idle: {_idleTime:F0} s";
            }
            else
            {
                _idleTime = 0;
                IdleTimeText.Text = "Idle: 0 s";
            }

            _lastMovementEvents = _movementEvents;

            // Movement density
            if (_sessionSeconds > 0)
            {
                _movementDensity = _movementEvents / _sessionSeconds;
                MovementDensityText.Text = $"Density: {_movementDensity:F2} eps";
            }

            // Average velocity
            double seconds = elapsed.TotalSeconds;
            if (seconds > 0)
            {
                _averageVelocity = _totalDistance / seconds;
                AverageVelocityText.Text = $"Average: {_averageVelocity:F2} cm/s";
            }
        }

        // -----------------------------
        // Sensitivity helper
        // -----------------------------
        private double CalculateCmPer360()
        {
            // Formula: cm/360 = (360 / (sensitivity * 0.022)) * (2.54 / dpi)
            double inchesPer360 = 360.0 / (_sensitivity * 0.022);
            double cmPer360 = inchesPer360 * 2.54 / _dpi;
            return cmPer360;
        }
    }
}
