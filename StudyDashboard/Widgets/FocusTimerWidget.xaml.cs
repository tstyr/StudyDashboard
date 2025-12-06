using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StudyDashboard
{
    public partial class FocusTimerWidget : DraggableWidget
    {
        private DispatcherTimer _timer = null!;
        private TimeSpan _timeRemaining;
        private TimeSpan _totalTime;
        private bool _isRunning;
        private bool _isFocusMode = true;
        private int _completedSessions = 0;
        private int _focusDuration = 25;
        private int _shortBreakDuration = 5;
        private int _longBreakDuration = 15;
        private int _interval = 4;
        private DateTime _sessionStartTime;
        private int _lastRecordedMinute = 0;
        
        public event Action<int>? SessionCompleted;
        public event Action<int>? ElapsedTimeUpdated;

        public FocusTimerWidget()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            
            SetFocusMode();
            UpdateModeText();
        }

        private void SetFocusMode()
        {
            _isFocusMode = true;
            _totalTime = TimeSpan.FromMinutes(_focusDuration);
            _timeRemaining = _totalTime;
            _lastRecordedMinute = 0;
            UpdateDisplay();
            UpdateModeText();
            PauseButton.Content = "Start";
        }

        private void SetShortBreakMode()
        {
            _isFocusMode = false;
            _totalTime = TimeSpan.FromMinutes(_shortBreakDuration);
            _timeRemaining = _totalTime;
            UpdateDisplay();
            PauseButton.Content = "Start";
        }

        private void SetLongBreakMode()
        {
            _isFocusMode = false;
            _totalTime = TimeSpan.FromMinutes(_longBreakDuration);
            _timeRemaining = _totalTime;
            UpdateDisplay();
            PauseButton.Content = "Start";
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
            
            // リアルタイムで経過時間を記録（フォーカスモードのみ、1分ごと）
            if (_isFocusMode && _isRunning)
            {
                var elapsedMinutes = (int)(_totalTime - _timeRemaining).TotalMinutes;
                if (elapsedMinutes > _lastRecordedMinute)
                {
                    _lastRecordedMinute = elapsedMinutes;
                    ElapsedTimeUpdated?.Invoke(elapsedMinutes);
                }
            }
            
            if (_timeRemaining <= TimeSpan.Zero)
            {
                _timer.Stop();
                _isRunning = false;
                
                if (_isFocusMode)
                {
                    _completedSessions++;
                    UpdateModeText();
                    SessionCompleted?.Invoke(_focusDuration);
                    
                    // 自動でブレークに切り替え
                    if (_completedSessions % _interval == 0)
                    {
                        SetLongBreakMode();
                        _timer.Start();
                        _isRunning = true;
                        PauseButton.Content = "Pause";
                    }
                    else
                    {
                        SetShortBreakMode();
                        _timer.Start();
                        _isRunning = true;
                        PauseButton.Content = "Pause";
                    }
                }
                else
                {
                    // ブレーク終了、フォーカスモードに戻る
                    SetFocusMode();
                }
            }
            
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            TimerDisplay.Text = $"{_timeRemaining.Minutes:D2}:{_timeRemaining.Seconds:D2}";
            
            if (_totalTime.TotalSeconds > 0)
            {
                var progress = 1.0 - (_timeRemaining.TotalSeconds / _totalTime.TotalSeconds);
                TimerProgress.Value = progress * 100;
            }
        }

        private void UpdateModeText()
        {
            var mode = _isFocusMode ? "Focus" : "Break";
            ModeText.Text = $"Mode: {mode} ({_completedSessions} sessions complete)";
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _timer.Stop();
                _isRunning = false;
                PauseButton.Content = "Resume";
            }
            else
            {
                if (_isFocusMode && _timeRemaining == _totalTime)
                {
                    // 新しいセッション開始
                    _sessionStartTime = DateTime.Now;
                    _lastRecordedMinute = 0;
                }
                _timer.Start();
                _isRunning = true;
                PauseButton.Content = "Pause";
            }
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            
            if (_isFocusMode)
            {
                _completedSessions++;
                UpdateModeText();
                SessionCompleted?.Invoke(_focusDuration);
                SetShortBreakMode();
            }
            else
            {
                SetFocusMode();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            
            if (_isFocusMode)
                SetFocusMode();
            else
                _timeRemaining = _totalTime;
                
            UpdateDisplay();
            PauseButton.Content = "Start";
        }

        private void ShortBreakButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            SetShortBreakMode();
        }

        private void LongBreakButton_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _isRunning = false;
            SetLongBreakMode();
        }

        private void DurationChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(FocusDurationBox?.Text, out int focus))
                _focusDuration = focus;
            if (int.TryParse(ShortBreakBox?.Text, out int shortBreak))
                _shortBreakDuration = shortBreak;
            if (int.TryParse(LongBreakBox?.Text, out int longBreak))
                _longBreakDuration = longBreak;
            if (int.TryParse(IntervalBox?.Text, out int interval))
                _interval = interval;
            
            UpdateBreakButtonText();
        }

        public void LoadSettings(AppSettings settings)
        {
            _focusDuration = settings.FocusDuration;
            _shortBreakDuration = settings.ShortBreakDuration;
            _longBreakDuration = settings.LongBreakDuration;
            _interval = settings.Interval;
            
            FocusDurationBox.Text = _focusDuration.ToString();
            ShortBreakBox.Text = _shortBreakDuration.ToString();
            LongBreakBox.Text = _longBreakDuration.ToString();
            IntervalBox.Text = _interval.ToString();
            
            SetFocusMode();
        }

        public void SaveSettings(AppSettings settings)
        {
            settings.FocusDuration = _focusDuration;
            settings.ShortBreakDuration = _shortBreakDuration;
            settings.LongBreakDuration = _longBreakDuration;
            settings.Interval = _interval;
        }

        private void UpdateBreakButtonText()
        {
            if (ShortBreakBtn != null)
                ShortBreakBtn.Content = $"Start Break ({_shortBreakDuration} min)";
            if (LongBreakBtn != null)
                LongBreakBtn.Content = $"Start Long Break ({_longBreakDuration} min)";
        }
    }
}