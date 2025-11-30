using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StudyDashboard
{
    public partial class SoundPlayerWidget : DraggableWidget
    {
        private MediaElement? _mediaPlayer;
        private bool _isPlaying = false;
        private DispatcherTimer? _progressTimer;

        public SoundPlayerWidget()
        {
            InitializeComponent();
            InitializeMediaPlayer();
        }

        private void InitializeMediaPlayer()
        {
            _mediaPlayer = new MediaElement();
            _mediaPlayer.LoadedBehavior = MediaState.Manual;
            _mediaPlayer.UnloadedBehavior = MediaState.Manual;
            _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
            _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            
            // MediaElementを非表示でコンテナに追加
            _mediaPlayer.Visibility = Visibility.Hidden;
            ((StackPanel)((Border)Content).Child).Children.Add(_mediaPlayer);
            
            // 進捗更新タイマー
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                var current = _mediaPlayer.Position;
                var total = _mediaPlayer.NaturalDuration.TimeSpan;
                
                CurrentTimeText.Text = current.ToString(@"mm\:ss");
                TotalTimeText.Text = total.ToString(@"mm\:ss");
                
                if (total.TotalSeconds > 0)
                {
                    PlaybackProgress.Value = (current.TotalSeconds / total.TotalSeconds) * 100;
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var url = UrlTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(url))
            {
                StatusText.Text = "Please enter a URL";
                return;
            }

            if (_isPlaying)
            {
                StopPlayback();
                return;
            }

            // YouTube URLの場合は外部プレイヤーで開く
            if (url.Contains("youtube.com") || url.Contains("youtu.be"))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    StatusText.Text = "Opening in browser...";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
                return;
            }

            // Spotify URLの場合
            if (url.Contains("spotify.com"))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    StatusText.Text = "Opening in Spotify...";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
                return;
            }

            // 直接再生可能なURL（MP3, WAVなど）
            try
            {
                _mediaPlayer!.Source = new Uri(url);
                _mediaPlayer.Play();
                StatusText.Text = "Loading...";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
        }

        private void StopPlayback()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _isPlaying = false;
                PlayButton.Content = "Play URL";
                StatusText.Text = "Stopped";
                _progressTimer?.Stop();
                PlaybackProgress.Value = 0;
                CurrentTimeText.Text = "00:00";
                TotalTimeText.Text = "00:00";
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = e.NewValue / 100.0;
                VolumeText.Text = $"{(int)e.NewValue}%";
            }
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string url)
            {
                UrlTextBox.Text = url;
                PlayButton_Click(sender, e);
            }
        }

        private void MediaPlayer_MediaOpened(object? sender, RoutedEventArgs e)
        {
            _isPlaying = true;
            PlayButton.Content = "Stop";
            StatusText.Text = "Playing...";
            _progressTimer?.Start();
        }

        private void MediaPlayer_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            StatusText.Text = $"Failed to load media: {e.ErrorException?.Message}";
            _isPlaying = false;
            PlayButton.Content = "Play URL";
        }

        private void MediaPlayer_MediaEnded(object? sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            PlayButton.Content = "Play URL";
            StatusText.Text = "Playback finished";
        }

        private void OpenSpotify_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "spotify:",
                    UseShellExecute = true
                });
                StatusText.Text = "Opening Spotify...";
            }
            catch
            {
                // Spotifyがインストールされていない場合はWebを開く
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://open.spotify.com",
                        UseShellExecute = true
                    });
                    StatusText.Text = "Opening Spotify Web...";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
        }
    }
}