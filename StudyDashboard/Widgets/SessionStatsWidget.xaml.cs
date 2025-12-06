using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StudyDashboard
{
    public partial class SessionStatsWidget : DraggableWidget
    {
        private List<SessionRecord> _sessions = new List<SessionRecord>();
        private Dictionary<DateTime, int> _dailyMinutes = new Dictionary<DateTime, int>();

        public SessionStatsWidget()
        {
            InitializeComponent();
            this.Loaded += (s, e) => RefreshUI();
        }

        private int _currentSessionMinutes = 0;
        private DateTime? _currentSessionStart = null;

        public void AddSession(int durationMinutes, string? taskName = null)
        {
            // セッション完了時は現在のリアルタイム記録をリセット
            _currentSessionMinutes = 0;
            _currentSessionStart = null;

            var session = new SessionRecord
            {
                DateTime = DateTime.Now,
                DurationMinutes = durationMinutes,
                TaskName = taskName
            };
            _sessions.Add(session);

            var today = DateTime.Today;
            if (!_dailyMinutes.ContainsKey(today))
                _dailyMinutes[today] = 0;
            _dailyMinutes[today] += durationMinutes;

            RefreshUI();
        }

        public void UpdateElapsedTime(int elapsedMinutes)
        {
            var today = DateTime.Today;
            if (!_dailyMinutes.ContainsKey(today))
                _dailyMinutes[today] = 0;

            // 差分を追加（前回記録からの増分のみ）
            int increment = elapsedMinutes - _currentSessionMinutes;
            if (increment > 0)
            {
                _dailyMinutes[today] += increment;
                _currentSessionMinutes = elapsedMinutes;
                
                if (_currentSessionStart == null)
                    _currentSessionStart = DateTime.Now.AddMinutes(-elapsedMinutes);
                
                RefreshUI();
            }
        }

        public void ResetCurrentSession()
        {
            _currentSessionMinutes = 0;
            _currentSessionStart = null;
        }

        public void RefreshUI()
        {
            UpdateDateTimeline();
            UpdateStats();
            UpdateWeekBarChart();
            UpdateRecentSessions();
            UpdateDailyDetail();
        }

        private void UpdateDateTimeline()
        {
            DateTimeline.Children.Clear();
            for (int i = 13; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                var tb = new TextBlock
                {
                    Text = date.ToString("M/d"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    Margin = new Thickness(8, 0, 8, 0)
                };
                if (_dailyMinutes.ContainsKey(date) && _dailyMinutes[date] > 0)
                {
                    tb.Foreground = Brushes.White;
                    tb.FontWeight = FontWeights.Bold;
                }
                DateTimeline.Children.Add(tb);
            }
        }

        private void UpdateStats()
        {
            // 14日間の平均
            int totalMinutes14d = 0;
            int activeDays = 0;
            for (int i = 0; i < 14; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                if (_dailyMinutes.ContainsKey(date))
                {
                    totalMinutes14d += _dailyMinutes[date];
                    if (_dailyMinutes[date] > 0) activeDays++;
                }
            }
            int avg = activeDays > 0 ? totalMinutes14d / activeDays : 0;
            AvgMinText.Text = $"{avg} min";
            ActiveDaysText.Text = $"{activeDays} / 14";

            // ベストデイ
            DateTime bestDay = DateTime.Today;
            int bestMin = 0;
            foreach (var kv in _dailyMinutes)
            {
                if (kv.Value > bestMin)
                {
                    bestMin = kv.Value;
                    bestDay = kv.Key;
                }
            }
            if (bestMin > 0)
            {
                BestDayText.Text = $"{bestDay:M/d} ({bestMin} min)";
                BestDayDateText.Text = bestDay.ToString("yyyy/M/d");
            }
            else
            {
                BestDayText.Text = "-- (0 min)";
                BestDayDateText.Text = "";
            }

            // 7日間合計
            int weekTotal = 0;
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                if (_dailyMinutes.ContainsKey(date))
                    weekTotal += _dailyMinutes[date];
            }
            WeekTotalText.Text = $"{weekTotal} min";

            // 前週比較
            int prevWeekTotal = 0;
            for (int i = 7; i < 14; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                if (_dailyMinutes.ContainsKey(date))
                    prevWeekTotal += _dailyMinutes[date];
            }
            if (prevWeekTotal > 0)
            {
                int diff = weekTotal - prevWeekTotal;
                int percent = (int)((diff / (double)prevWeekTotal) * 100);
                WeekCompareText.Text = $"{(percent >= 0 ? "+" : "")}{percent}% vs prev 7d";
            }
            else
            {
                WeekCompareText.Text = "";
            }
        }

        private void UpdateWeekBarChart()
        {
            WeekBarChart.Children.Clear();
            WeekDayLabels.Children.Clear();

            string[] dayNames = { "月", "火", "水", "木", "金", "土", "日" };
            var weekData = new int[7];
            var today = DateTime.Today;
            int todayDow = ((int)today.DayOfWeek + 6) % 7; // 月曜=0

            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i - todayDow);
                if (_dailyMinutes.ContainsKey(date))
                    weekData[i] = _dailyMinutes[date];
            }

            int maxMin = weekData.Max();
            if (maxMin == 0) maxMin = 60;
            MaxMinText.Text = $"Max: {maxMin} min / day";

            double chartWidth = 400;
            double chartHeight = 100;
            double barWidth = 35;
            double gap = (chartWidth - barWidth * 7) / 8;

            for (int i = 0; i < 7; i++)
            {
                double height = weekData[i] > 0 ? (weekData[i] / (double)maxMin) * (chartHeight - 20) : 5;
                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = height,
                    Fill = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(bar, gap + i * (barWidth + gap));
                Canvas.SetTop(bar, chartHeight - height);
                WeekBarChart.Children.Add(bar);

                var label = new TextBlock
                {
                    Text = dayNames[i],
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    Width = barWidth,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(gap / 2, 0, gap / 2, 0)
                };
                WeekDayLabels.Children.Add(label);
            }
        }

        private void UpdateRecentSessions()
        {
            RecentSessionsList.Children.Clear();
            var recent = _sessions.OrderByDescending(s => s.DateTime).Take(5);

            foreach (var session in recent)
            {
                var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var dateText = new TextBlock
                {
                    Text = session.DateTime.ToString("yyyy/MM/dd H:mm"),
                    FontSize = 11,
                    Foreground = Brushes.White
                };
                Grid.SetColumn(dateText, 0);

                var durationText = new TextBlock
                {
                    Text = $"{session.DurationMinutes} min",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    Margin = new Thickness(15, 0, 0, 0)
                };
                Grid.SetColumn(durationText, 1);

                var taskText = new TextBlock
                {
                    Text = string.IsNullOrEmpty(session.TaskName) ? "(No Task)" : session.TaskName,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    Margin = new Thickness(15, 0, 0, 0)
                };
                Grid.SetColumn(taskText, 2);

                grid.Children.Add(dateText);
                grid.Children.Add(durationText);
                grid.Children.Add(taskText);
                RecentSessionsList.Children.Add(grid);
            }

            if (!_sessions.Any())
            {
                RecentSessionsList.Children.Add(new TextBlock
                {
                    Text = "No sessions yet",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
                });
            }
        }

        private void UpdateDailyDetail()
        {
            DailyDetailList.Children.Clear();

            int maxMin = 0;
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(-i);
                if (_dailyMinutes.ContainsKey(date) && _dailyMinutes[date] > maxMin)
                    maxMin = _dailyMinutes[date];
            }
            if (maxMin == 0) maxMin = 60;

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                int minutes = _dailyMinutes.ContainsKey(date) ? _dailyMinutes[date] : 0;

                var grid = new Grid { Margin = new Thickness(0, 2, 0, 2), Height = 20 };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var dateText = new TextBlock
                {
                    Text = date.ToString("M/d"),
                    FontSize = 11,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dateText, 0);

                double barWidth = minutes > 0 ? (minutes / (double)maxMin) * 200 : 0;
                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = 14,
                    Fill = new SolidColorBrush(Color.FromRgb(74, 144, 217)),
                    RadiusX = 2,
                    RadiusY = 2,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(bar, 1);

                grid.Children.Add(dateText);
                grid.Children.Add(bar);
                DailyDetailList.Children.Add(grid);
            }
        }

        public void LoadData(List<SessionRecord> sessions, Dictionary<DateTime, int> dailyMinutes)
        {
            _sessions = sessions ?? new List<SessionRecord>();
            _dailyMinutes = dailyMinutes ?? new Dictionary<DateTime, int>();
            RefreshUI();
        }

        public (List<SessionRecord> sessions, Dictionary<DateTime, int> dailyMinutes) GetData()
        {
            return (_sessions, _dailyMinutes);
        }
    }

    public class SessionRecord
    {
        public DateTime DateTime { get; set; }
        public int DurationMinutes { get; set; }
        public string? TaskName { get; set; }
    }
}
