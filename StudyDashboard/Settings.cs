using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace StudyDashboard
{
    public class AppSettings
    {
        public bool IsDarkMode { get; set; } = true;
        public double Sensitivity { get; set; } = 200;
        public double MasterVolume { get; set; } = 50;
        
        // ウィジェット位置
        public double FocusTimerLeft { get; set; } = 10;
        public double FocusTimerTop { get; set; } = 10;
        public double SessionStatsLeft { get; set; } = 380;
        public double SessionStatsTop { get; set; } = 10;
        public double SoundPlayerLeft { get; set; } = 920;
        public double SoundPlayerTop { get; set; } = 10;
        public double AudioVisualizerLeft { get; set; } = 10;
        public double AudioVisualizerTop { get; set; } = 310;
        public double TasksLeft { get; set; } = 920;
        public double TasksTop { get; set; } = 410;
        public double SystemStatusLeft { get; set; } = 10;
        public double SystemStatusTop { get; set; } = 650;
        
        // タイマー設定
        public int FocusDuration { get; set; } = 25;
        public int ShortBreakDuration { get; set; } = 5;
        public int LongBreakDuration { get; set; } = 15;
        public int Interval { get; set; } = 4;

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StudyDashboard", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}