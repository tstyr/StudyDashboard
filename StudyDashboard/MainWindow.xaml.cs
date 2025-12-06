using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StudyDashboard
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings = null!;
        private bool _isDarkMode = true;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyDown += MainWindow_KeyDown;
            this.Closing += MainWindow_Closing;
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            SetDefaultLayout();

            FocusTimer.SessionCompleted += (duration) =>
            {
                SessionStats.ResetCurrentSession();
                SessionStats.AddSession(duration);
            };

            FocusTimer.ElapsedTimeUpdated += (elapsedMinutes) =>
            {
                SessionStats.UpdateElapsedTime(elapsedMinutes);
            };
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // ウィンドウサイズ変更時にレイアウトを調整
            if (WidgetCanvas.ActualWidth > 0 && WidgetCanvas.ActualHeight > 0)
            {
                AdjustLayoutToFit();
            }
        }

        private void SetDefaultLayout()
        {
            double w = WidgetCanvas.ActualWidth > 0 ? WidgetCanvas.ActualWidth : 1380;
            double h = WidgetCanvas.ActualHeight > 0 ? WidgetCanvas.ActualHeight : 830;
            double gap = 8;

            // 左列
            Canvas.SetLeft(FocusTimer, gap);
            Canvas.SetTop(FocusTimer, gap);
            FocusTimer.Width = 380;
            FocusTimer.Height = 440;

            Canvas.SetLeft(AudioVisualizer, gap);
            Canvas.SetTop(AudioVisualizer, FocusTimer.Height + gap * 2);
            AudioVisualizer.Width = 380;
            AudioVisualizer.Height = h - FocusTimer.Height - 70 - gap * 4;

            // 中央列
            Canvas.SetLeft(SessionStats, 380 + gap * 2);
            Canvas.SetTop(SessionStats, gap);
            SessionStats.Width = 520;
            SessionStats.Height = h - 70 - gap * 3;

            // 右列
            double rightX = 380 + 520 + gap * 3;
            Canvas.SetLeft(SoundPlayer, rightX);
            Canvas.SetTop(SoundPlayer, gap);
            SoundPlayer.Width = w - rightX - gap;
            SoundPlayer.Height = h - 220 - 70 - gap * 4;

            Canvas.SetLeft(TodaysTasks, rightX);
            Canvas.SetTop(TodaysTasks, SoundPlayer.Height + gap * 2);
            TodaysTasks.Width = w - rightX - gap;
            TodaysTasks.Height = 220;

            // 下部
            Canvas.SetLeft(SystemStatus, gap);
            Canvas.SetTop(SystemStatus, h - 70 - gap);
            SystemStatus.Width = w - gap * 2;
            SystemStatus.Height = 60;
        }

        private void AdjustLayoutToFit()
        {
            double w = WidgetCanvas.ActualWidth;
            double h = WidgetCanvas.ActualHeight;
            if (w < 100 || h < 100) return;

            double gap = 8;
            double col1W = Math.Max(300, w * 0.28);
            double col2W = Math.Max(400, w * 0.38);
            double col3W = w - col1W - col2W - gap * 4;
            double statusH = 60;

            // 左列
            FocusTimer.Width = col1W;
            FocusTimer.Height = Math.Min(440, (h - statusH - gap * 4) * 0.55);
            Canvas.SetLeft(FocusTimer, gap);
            Canvas.SetTop(FocusTimer, gap);

            AudioVisualizer.Width = col1W;
            AudioVisualizer.Height = h - FocusTimer.Height - statusH - gap * 4;
            Canvas.SetLeft(AudioVisualizer, gap);
            Canvas.SetTop(AudioVisualizer, FocusTimer.Height + gap * 2);

            // 中央列
            SessionStats.Width = col2W;
            SessionStats.Height = h - statusH - gap * 3;
            Canvas.SetLeft(SessionStats, col1W + gap * 2);
            Canvas.SetTop(SessionStats, gap);

            // 右列
            double rightX = col1W + col2W + gap * 3;
            SoundPlayer.Width = col3W;
            SoundPlayer.Height = (h - statusH - gap * 4) * 0.7;
            Canvas.SetLeft(SoundPlayer, rightX);
            Canvas.SetTop(SoundPlayer, gap);

            TodaysTasks.Width = col3W;
            TodaysTasks.Height = h - SoundPlayer.Height - statusH - gap * 4;
            Canvas.SetLeft(TodaysTasks, rightX);
            Canvas.SetTop(TodaysTasks, SoundPlayer.Height + gap * 2);

            // 下部
            SystemStatus.Width = w - gap * 2;
            SystemStatus.Height = statusH;
            Canvas.SetLeft(SystemStatus, gap);
            Canvas.SetTop(SystemStatus, h - statusH - gap);
        }

        private void LoadSettings()
        {
            _settings = AppSettings.Load();
            _isDarkMode = _settings.IsDarkMode;
            FocusTimer.LoadSettings(_settings);
            ApplyTheme();
        }

        private void SaveSettings()
        {
            FocusTimer.SaveSettings(_settings);
            _settings.IsDarkMode = _isDarkMode;
            _settings.Save();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var darkBg = Color.FromArgb(0xC0, 0x00, 0x00, 0x00);
            var lightBg = Color.FromArgb(0xE8, 0xD8, 0xE0, 0xE8);
            var darkBorder = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF);
            var lightBorder = Color.FromArgb(0x60, 0x90, 0xA0, 0xB8);
            var darkTitleBar = Color.FromArgb(0x60, 0x00, 0x00, 0x00);
            var lightTitleBar = Color.FromArgb(0xD0, 0xC8, 0xD4, 0xE0);

            if (_isDarkMode)
            {
                this.Background = new SolidColorBrush(darkBg);
                MainBorder.BorderBrush = new SolidColorBrush(darkBorder);
                TitleBarGrid.Background = new SolidColorBrush(darkTitleBar);
                MenuDashboard.Foreground = Brushes.White;
                MenuEditLayout.Foreground = Brushes.White;
                MoonIcon.Foreground = Brushes.White;
                SunIcon.Foreground = Brushes.White;
                MinBtn.Foreground = Brushes.White;
                MaxBtn.Foreground = Brushes.White;
            }
            else
            {
                this.Background = new SolidColorBrush(lightBg);
                MainBorder.BorderBrush = new SolidColorBrush(lightBorder);
                TitleBarGrid.Background = new SolidColorBrush(lightTitleBar);
                var darkText = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x40));
                MenuDashboard.Foreground = darkText;
                MenuEditLayout.Foreground = darkText;
                MoonIcon.Foreground = darkText;
                SunIcon.Foreground = darkText;
                MinBtn.Foreground = darkText;
                MaxBtn.Foreground = darkText;
            }

            ThemeToggle.IsChecked = !_isDarkMode;
            AudioVisualizer.SetTheme(_isDarkMode);
            TodaysTasks.SetTheme(_isDarkMode);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                MaximizeButton_Click(sender, e);
            else
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void DefaultLayout_Click(object sender, RoutedEventArgs e) => SetDefaultLayout();

        private void ResetLayout_Click(object sender, RoutedEventArgs e) => AdjustLayoutToFit();

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            FocusTimer.Visibility = Visibility.Visible;
            SessionStats.Visibility = Visibility.Visible;
            SoundPlayer.Visibility = Visibility.Visible;
            AudioVisualizer.Visibility = Visibility.Visible;
            TodaysTasks.Visibility = Visibility.Visible;
            SystemStatus.Visibility = Visibility.Visible;
            AdjustLayoutToFit();
        }

        private void ShowTimerOnly_Click(object sender, RoutedEventArgs e)
        {
            FocusTimer.Visibility = Visibility.Visible;
            SessionStats.Visibility = Visibility.Collapsed;
            SoundPlayer.Visibility = Visibility.Collapsed;
            AudioVisualizer.Visibility = Visibility.Collapsed;
            TodaysTasks.Visibility = Visibility.Collapsed;
            SystemStatus.Visibility = Visibility.Collapsed;
        }

        private void ShowStatsOnly_Click(object sender, RoutedEventArgs e)
        {
            FocusTimer.Visibility = Visibility.Collapsed;
            SessionStats.Visibility = Visibility.Visible;
            SoundPlayer.Visibility = Visibility.Collapsed;
            AudioVisualizer.Visibility = Visibility.Collapsed;
            TodaysTasks.Visibility = Visibility.Collapsed;
            SystemStatus.Visibility = Visibility.Collapsed;
        }
    }
}
