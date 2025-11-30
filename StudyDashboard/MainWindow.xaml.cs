using System;
using System.Windows;
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
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();

            // タイマーセッション完了時にセッション統計を更新
            FocusTimer.SessionCompleted += (duration) =>
            {
                SessionStats.AddSession(duration);
            };
        }

        private void LoadSettings()
        {
            _settings = AppSettings.Load();
            _isDarkMode = _settings.IsDarkMode;

            // タイマー設定を復元
            FocusTimer.LoadSettings(_settings);

            // テーマを適用
            ApplyTheme();
        }

        private void SaveSettings()
        {
            // タイマー設定を保存
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
            var lightBg = Color.FromArgb(0xF0, 0xE8, 0xEC, 0xF0);
            var darkBorder = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF);
            var lightBorder = Color.FromArgb(0x80, 0x80, 0x90, 0xA0);
            var darkTitleBar = Color.FromArgb(0x60, 0x00, 0x00, 0x00);
            var lightTitleBar = Color.FromArgb(0xE0, 0xD8, 0xDC, 0xE4);

            if (_isDarkMode)
            {
                this.Background = new SolidColorBrush(darkBg);
                MainBorder.BorderBrush = new SolidColorBrush(darkBorder);
                TitleBarGrid.Background = new SolidColorBrush(darkTitleBar);
                
                // メニューとボタンの色
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
                
                // メニューとボタンの色
                var darkText = new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x40));
                MenuDashboard.Foreground = darkText;
                MenuEditLayout.Foreground = darkText;
                MoonIcon.Foreground = darkText;
                SunIcon.Foreground = darkText;
                MinBtn.Foreground = darkText;
                MaxBtn.Foreground = darkText;
            }

            ThemeToggle.IsChecked = !_isDarkMode;

            // オーディオビジュアライザーのテーマを更新
            AudioVisualizer.SetTheme(_isDarkMode);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void DefaultLayout_Click(object sender, RoutedEventArgs e)
        {
            // Grid レイアウトなので特に何もしない
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e)
        {
            // Grid レイアウトなので特に何もしない
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            FocusTimer.Visibility = Visibility.Visible;
            SessionStats.Visibility = Visibility.Visible;
            SoundPlayer.Visibility = Visibility.Visible;
            AudioVisualizer.Visibility = Visibility.Visible;
            TodaysTasks.Visibility = Visibility.Visible;
            SystemStatus.Visibility = Visibility.Visible;
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
