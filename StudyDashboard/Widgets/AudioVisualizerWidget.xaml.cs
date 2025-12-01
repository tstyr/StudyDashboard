using System;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StudyDashboard
{
    public partial class AudioVisualizerWidget : DraggableWidget
    {
        private WasapiLoopbackCapture? _wasapiCapture;
        private DispatcherTimer _updateTimer = null!;
        private const int FFT_SIZE = 2048;
        private Complex[] _fftBuffer = new Complex[FFT_SIZE];
        private float[] _spectrumData = new float[FFT_SIZE / 2];
        private float[] _smoothedValues = new float[64];
        private int _barCount = 48;
        private Rectangle[] _spectrumBars = new Rectangle[64];
        private double _sensitivity = 300;
        private int _updateInterval = 16;
        private MMDeviceEnumerator? _deviceEnumerator;
        private MMDevice? _currentDevice;
        private int _sampleRate = 48000;
        private readonly object _lockObject = new object();
        private float _currentDb = float.NegativeInfinity;
        private float _peakFrequency = 0;
        private bool _isDarkMode = true;
        private int _infoUpdateCounter = 0;
        private const int INFO_UPDATE_INTERVAL = 5; // 5フレームに1回更新

        public AudioVisualizerWidget()
        {
            InitializeComponent();
            LoadAudioDevices();
            InitializeAudioCapture();
            InitializeVisualizer();
        }

        private void LoadAudioDevices()
        {
            try
            {
                _deviceEnumerator = new MMDeviceEnumerator();
                var devices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                DeviceComboBox.Items.Clear();
                foreach (var device in devices)
                {
                    DeviceComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = device.FriendlyName,
                        Tag = device.ID
                    });
                }

                if (DeviceComboBox.Items.Count > 0)
                {
                    DeviceComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                DeviceInfoText.Text = $"Error loading devices: {ex.Message}";
            }
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string deviceId)
            {
                try
                {
                    _wasapiCapture?.StopRecording();
                    _wasapiCapture?.Dispose();

                    _currentDevice = _deviceEnumerator?.GetDevice(deviceId);
                    if (_currentDevice != null)
                    {
                        _wasapiCapture = new WasapiLoopbackCapture(_currentDevice);
                        _sampleRate = _wasapiCapture.WaveFormat.SampleRate;
                        _wasapiCapture.DataAvailable += WasapiCapture_DataAvailable;
                        _wasapiCapture.StartRecording();
                        DeviceInfoText.Text = $"Device: {_currentDevice.FriendlyName}";
                    }
                }
                catch (Exception ex)
                {
                    DeviceInfoText.Text = $"Error: {ex.Message}";
                }
            }
        }

        private void InitializeAudioCapture()
        {
            try
            {
                _deviceEnumerator ??= new MMDeviceEnumerator();
                _currentDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                _wasapiCapture = new WasapiLoopbackCapture(_currentDevice);
                _sampleRate = _wasapiCapture.WaveFormat.SampleRate;
                _wasapiCapture.DataAvailable += WasapiCapture_DataAvailable;
                _wasapiCapture.StartRecording();

                DeviceInfoText.Text = $"Device: {_currentDevice.FriendlyName}";
            }
            catch (Exception ex)
            {
                DeviceInfoText.Text = $"Audio capture error: {ex.Message}";
            }
        }

        private void WasapiCapture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;

            var samples = new float[e.BytesRecorded / 4];
            Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);

            // ステレオをモノラルに変換（2チャンネルの場合）
            int channels = _wasapiCapture?.WaveFormat.Channels ?? 2;
            var monoSamples = new float[samples.Length / channels];
            for (int i = 0; i < monoSamples.Length; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    int idx = i * channels + ch;
                    if (idx < samples.Length) sum += samples[idx];
                }
                monoSamples[i] = sum / channels;
            }

            // FFT用バッファにコピー
            lock (_lockObject)
            {
                int copyLength = Math.Min(monoSamples.Length, FFT_SIZE);
                for (int i = 0; i < FFT_SIZE; i++)
                {
                    if (i < copyLength)
                    {
                        // ハミング窓を適用
                        float window = 0.54f - 0.46f * (float)Math.Cos(2 * Math.PI * i / (FFT_SIZE - 1));
                        _fftBuffer[i].X = monoSamples[i] * window;
                        _fftBuffer[i].Y = 0;
                    }
                    else
                    {
                        _fftBuffer[i].X = 0;
                        _fftBuffer[i].Y = 0;
                    }
                }

                // FFT実行
                FastFourierTransform.FFT(true, (int)Math.Log2(FFT_SIZE), _fftBuffer);

                // マグニチュードを計算（低周波から高周波の順）
                float maxMag = 0;
                int maxBin = 0;
                float totalPower = 0;
                for (int i = 0; i < FFT_SIZE / 2; i++)
                {
                    float magnitude = (float)Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X + _fftBuffer[i].Y * _fftBuffer[i].Y);
                    _spectrumData[i] = magnitude;
                    totalPower += magnitude * magnitude;
                    if (magnitude > maxMag)
                    {
                        maxMag = magnitude;
                        maxBin = i;
                    }
                }
                
                // dB計算
                float rms = (float)Math.Sqrt(totalPower / (FFT_SIZE / 2));
                _currentDb = rms > 0 ? 20 * (float)Math.Log10(rms) : float.NegativeInfinity;
                
                // ピーク周波数計算
                _peakFrequency = maxBin * _sampleRate / (float)FFT_SIZE;
            }
        }

        private void InitializeVisualizer()
        {
            this.Loaded += (s, e) =>
            {
                // 少し遅延させてからバーを作成（レイアウト完了後）
                Dispatcher.BeginInvoke(new Action(() => CreateSpectrumBars()), 
                    System.Windows.Threading.DispatcherPriority.Loaded);
            };
            this.SizeChanged += (s, e) => CreateSpectrumBars();
            SpectrumCanvas.SizeChanged += (s, e) => CreateSpectrumBars();

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(_updateInterval);
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void CreateSpectrumBars()
        {
            if (SpectrumCanvas.ActualWidth <= 0 || SpectrumCanvas.ActualHeight <= 0) return;

            SpectrumCanvas.Children.Clear();

            // バー数をキャンバス幅に応じて調整
            _barCount = Math.Max(16, Math.Min(64, (int)(SpectrumCanvas.ActualWidth / 10)));
            _spectrumBars = new Rectangle[_barCount];
            _smoothedValues = new float[_barCount];

            var totalWidth = SpectrumCanvas.ActualWidth - 20;
            var barWidth = Math.Max(3, (totalWidth / _barCount) - 2);
            var gap = 2;
            var canvasHeight = SpectrumCanvas.ActualHeight;

            for (int i = 0; i < _barCount; i++)
            {
                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = 3,
                    Fill = _isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x50, 0x60, 0x80)),
                    RadiusX = 1,
                    RadiusY = 1
                };

                Canvas.SetLeft(bar, 10 + i * (barWidth + gap));
                Canvas.SetTop(bar, canvasHeight - 3);
                SpectrumCanvas.Children.Add(bar);
                _spectrumBars[i] = bar;
            }
        }
        
        public void SetTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            // ホワイトモードでもバーは濃い青グレー系に
            var barBrush = isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x50, 0x60, 0x80));
            foreach (var bar in _spectrumBars)
            {
                if (bar != null) bar.Fill = barBrush;
            }
            
            // ステータステキストの色を更新（ホワイトモードでも見やすい濃い色）
            var textBrush = isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x30, 0x40, 0x50));
            var labelBrush = isDarkMode ? new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF)) 
                                        : new SolidColorBrush(Color.FromArgb(0x90, 0x50, 0x60, 0x70));
            DbText.Foreground = textBrush;
            PeakFreqText.Foreground = textBrush;
            FreqRangeText.Foreground = textBrush;
            DbLabel.Foreground = labelBrush;
            PeakFreqLabel.Foreground = labelBrush;
            FreqRangeLabel.Foreground = labelBrush;
            
            // ステータスボーダーの背景色
            StatusBorder.Background = isDarkMode ? new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF))
                                                 : new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x90, 0xA8));
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateSpectrum();
            
            // Hzステータスは数フレームに1回更新（ちらつき防止）
            _infoUpdateCounter++;
            if (_infoUpdateCounter >= INFO_UPDATE_INTERVAL)
            {
                _infoUpdateCounter = 0;
                UpdateAnalysisInfo();
            }
        }
        
        private void UpdateAnalysisInfo()
        {
            // dB表示
            if (float.IsNegativeInfinity(_currentDb) || _currentDb < -60)
                DbText.Text = "-∞ dB";
            else
                DbText.Text = $"{_currentDb:F1} dB";
            
            // ピーク周波数表示
            if (_peakFrequency < 1)
                PeakFreqText.Text = "-- Hz";
            else if (_peakFrequency >= 1000)
                PeakFreqText.Text = $"{_peakFrequency / 1000:F1} kHz";
            else
                PeakFreqText.Text = $"{_peakFrequency:F0} Hz";
            
            // 周波数範囲
            FreqRangeText.Text = $"20-{Math.Min(20000, _sampleRate / 2) / 1000}k Hz";
        }

        private void UpdateSpectrum()
        {
            if (SpectrumCanvas.ActualHeight <= 0 || _spectrumBars.Length == 0 || _spectrumBars[0] == null) return;

            var canvasHeight = SpectrumCanvas.ActualHeight;

            // 対数スケールで周波数帯域をマッピング（低周波→高周波）
            // 人間の聴覚に合わせて20Hz〜20kHzの範囲を対数スケールで分割
            float minFreq = 20f;
            float maxFreq = Math.Min(20000f, _sampleRate / 2f);
            float freqRatio = maxFreq / minFreq;

            lock (_lockObject)
            {
                for (int i = 0; i < _barCount; i++)
                {
                    if (_spectrumBars[i] == null) continue;

                    // 対数スケールで周波数範囲を計算
                    float t1 = (float)i / _barCount;
                    float t2 = (float)(i + 1) / _barCount;
                    float freq1 = minFreq * (float)Math.Pow(freqRatio, t1);
                    float freq2 = minFreq * (float)Math.Pow(freqRatio, t2);

                    // 周波数をFFTビンインデックスに変換
                    int bin1 = (int)(freq1 * FFT_SIZE / _sampleRate);
                    int bin2 = (int)(freq2 * FFT_SIZE / _sampleRate);
                    bin1 = Math.Max(0, Math.Min(bin1, FFT_SIZE / 2 - 1));
                    bin2 = Math.Max(bin1 + 1, Math.Min(bin2, FFT_SIZE / 2));

                    // 該当周波数帯域の平均マグニチュードを計算
                    float sum = 0;
                    int count = 0;
                    for (int j = bin1; j < bin2; j++)
                    {
                        sum += _spectrumData[j];
                        count++;
                    }
                    float average = count > 0 ? sum / count : 0;

                    // 感度を適用（低周波をブースト）
                    float freqBoost = 1.0f + (1.0f - t1) * 0.5f;
                    var targetHeight = average * _sensitivity * canvasHeight * freqBoost;
                    targetHeight = Math.Min(canvasHeight - 5, targetHeight);
                    targetHeight = Math.Max(3, targetHeight);

                    // スムージング
                    float smoothFactor = 0.3f;
                    float decayFactor = 0.15f;

                    if (targetHeight > _smoothedValues[i])
                        _smoothedValues[i] += (float)(targetHeight - _smoothedValues[i]) * smoothFactor;
                    else
                        _smoothedValues[i] += (float)(targetHeight - _smoothedValues[i]) * decayFactor;

                    var finalHeight = Math.Max(3, _smoothedValues[i]);
                    _spectrumBars[i].Height = finalHeight;
                    Canvas.SetTop(_spectrumBars[i], canvasHeight - finalHeight);
                }
            }
        }

        private void SensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _sensitivity = e.NewValue;
        }

        private void UpdateSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _updateInterval = (int)e.NewValue;
            if (_updateTimer != null)
            {
                _updateTimer.Interval = TimeSpan.FromMilliseconds(_updateInterval);
            }
        }

        private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MasterVolumeText != null)
            {
                MasterVolumeText.Text = $"{(int)e.NewValue}%";
                SetMasterVolume((float)(e.NewValue / 100.0));
            }
        }

        private void SetMasterVolume(float volume)
        {
            try
            {
                if (_currentDevice != null)
                {
                    _currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                }
                else if (_deviceEnumerator != null)
                {
                    var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
                }
            }
            catch { }
        }

        public void Cleanup()
        {
            _wasapiCapture?.StopRecording();
            _wasapiCapture?.Dispose();
            _updateTimer?.Stop();
        }
    }
}
