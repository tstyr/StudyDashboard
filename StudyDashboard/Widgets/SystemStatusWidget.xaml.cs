using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Threading;

namespace StudyDashboard
{
    public partial class SystemStatusWidget : DraggableWidget
    {
        private DispatcherTimer _updateTimer = null!;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _memoryCounter;

        public SystemStatusWidget()
        {
            InitializeComponent();
            InitializePerformanceCounters();
            InitializeTimer();
        }

        private void InitializePerformanceCounters()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                
                // 初回読み取り（正確な値を得るため）
                _cpuCounter.NextValue();
            }
            catch (Exception)
            {
                CpuUsageText.Text = "N/A";
                MemoryUsageText.Text = "N/A";
            }
        }

        private void InitializeTimer()
        {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(100); // 100msごとに更新
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
            
            // 時刻を即座に更新
            UpdateTime();
        }

        private int _updateCounter = 0;

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            // 時刻は毎回更新
            UpdateTime();
            
            // システム統計は2秒ごとに更新（パフォーマンス考慮）
            _updateCounter++;
            if (_updateCounter >= 20) // 100ms * 20 = 2秒
            {
                UpdateSystemStats();
                _updateCounter = 0;
            }
        }

        private void UpdateSystemStats()
        {
            try
            {
                // CPU使用率
                if (_cpuCounter != null)
                {
                    var cpuUsage = _cpuCounter.NextValue();
                    CpuUsageText.Text = $"{cpuUsage:F0}%";
                }

                // メモリ使用量
                if (_memoryCounter != null)
                {
                    var availableMemory = _memoryCounter.NextValue();
                    var totalMemory = GetTotalPhysicalMemory();
                    var usedMemory = (totalMemory - availableMemory) / 1024; // GB
                    MemoryUsageText.Text = $"{usedMemory:F1}GB";
                }

                // CPU温度（WMIを使用）
                UpdateCpuTemperature();
                
                // GPU使用率
                UpdateGpuUsage();
                
                // ディスク使用率
                UpdateDiskUsage();
                
                // ネットワーク使用量
                UpdateNetworkUsage();
            }
            catch (Exception)
            {
                // エラーが発生した場合は前の値を保持
            }
        }

        private void UpdateGpuUsage()
        {
            try
            {
                // GPU使用率の取得（簡易版）
                var random = new Random();
                var gpuUsage = random.Next(10, 60); // デモ用ランダム値
                GpuUsageText.Text = $"{gpuUsage}%";
            }
            catch
            {
                GpuUsageText.Text = "N/A";
            }
        }

        private void UpdateDiskUsage()
        {
            try
            {
                var drives = System.IO.DriveInfo.GetDrives();
                var cDrive = drives.FirstOrDefault(d => d.Name == "C:\\");
                if (cDrive != null)
                {
                    var usedSpace = cDrive.TotalSize - cDrive.AvailableFreeSpace;
                    var usagePercent = (double)usedSpace / cDrive.TotalSize * 100;
                    DiskUsageText.Text = $"{usagePercent:F0}%";
                }
            }
            catch
            {
                DiskUsageText.Text = "N/A";
            }
        }

        private void UpdateNetworkUsage()
        {
            try
            {
                // ネットワーク使用量（デモ用）
                var random = new Random();
                var downloadSpeed = random.NextDouble() * 5; // 0-5 MB/s
                var uploadSpeed = random.NextDouble() * 2; // 0-2 MB/s
                NetworkText.Text = $"↓{downloadSpeed:F1} ↑{uploadSpeed:F1}MB/s";
            }
            catch
            {
                NetworkText.Text = "N/A";
            }
        }

        private void UpdateCpuTemperature()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var temp = Convert.ToDouble(obj["CurrentTemperature"]);
                        var celsius = (temp / 10.0) - 273.15; // ケルビンから摂氏に変換
                        CpuTempText.Text = $"{celsius:F0}°C";
                        return;
                    }
                }
                
                // 温度が取得できない場合のフォールバック
                CpuTempText.Text = "N/A";
            }
            catch
            {
                CpuTempText.Text = "N/A";
            }
        }

        private double GetTotalPhysicalMemory()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024); // MB
                    }
                }
            }
            catch
            {
                return 16384; // デフォルト値 16GB
            }
            return 16384;
        }

        private void UpdateTime()
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss.fff");
        }

        public void Cleanup()
        {
            _updateTimer?.Stop();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
        }
    }
}