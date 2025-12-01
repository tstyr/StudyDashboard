using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StudyDashboard
{
    public partial class TasksWidget : DraggableWidget
    {
        private bool _isDarkMode = true;
        
        public TasksWidget()
        {
            InitializeComponent();
            UpdateTaskStats();
            
            foreach (CheckBox checkBox in TasksPanel.Children.OfType<CheckBox>())
            {
                checkBox.Checked += TaskCheckBox_CheckedChanged;
                checkBox.Unchecked += TaskCheckBox_CheckedChanged;
            }
        }

        public void SetTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            var textBrush = isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x40));
            var labelBrush = isDarkMode ? new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF))
                                        : new SolidColorBrush(Color.FromArgb(0x80, 0x40, 0x50, 0x60));
            
            CompletedTasksText.Foreground = textBrush;
            RemainingTasksText.Foreground = textBrush;
            CompletedLabel.Foreground = labelBrush;
            RemainingLabel.Foreground = labelBrush;
            
            foreach (CheckBox cb in TasksPanel.Children.OfType<CheckBox>())
            {
                cb.Foreground = textBrush;
            }
            
            StatsBorder.Background = isDarkMode ? new SolidColorBrush(Color.FromArgb(0x40, 0x33, 0x33, 0x33))
                                                : new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x90, 0xA8));
            
            AddTaskButton.Background = isDarkMode ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                                                  : new SolidColorBrush(Color.FromRgb(0x40, 0x80, 0x90));
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TaskDialog(_isDarkMode);
            dialog.Owner = Window.GetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            if (dialog.ShowDialog() == true)
            {
                AddTask(dialog.TaskTitle, dialog.TaskDescription, dialog.TaskStatus, dialog.TaskPriority);
            }
        }

        private void AddTask(string title, string description, string status, string priority)
        {
            if (string.IsNullOrWhiteSpace(title)) return;

            var checkBox = new CheckBox
            {
                Content = title,
                Foreground = _isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x40)),
                Margin = new Thickness(0, 2, 0, 2),
                ToolTip = $"{description}\nPriority: {priority}"
            };
            
            checkBox.Checked += TaskCheckBox_CheckedChanged;
            checkBox.Unchecked += TaskCheckBox_CheckedChanged;
            
            TasksPanel.Children.Add(checkBox);
            UpdateTaskStats();
        }

        private void TaskCheckBox_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            UpdateTaskStats();
        }

        private void UpdateTaskStats()
        {
            var totalTasks = TasksPanel.Children.OfType<CheckBox>().Count();
            var completedTasks = TasksPanel.Children.OfType<CheckBox>().Count(cb => cb.IsChecked == true);
            var remainingTasks = totalTasks - completedTasks;

            CompletedTasksText.Text = $"{completedTasks}/{totalTasks}";
            RemainingTasksText.Text = remainingTasks.ToString();
        }
    }

    public class TaskDialog : Window
    {
        public string TaskTitle { get; private set; } = "";
        public string TaskDescription { get; private set; } = "";
        public string TaskStatus { get; private set; } = "todo";
        public string TaskPriority { get; private set; } = "normal";

        private TextBox _titleBox = null!;
        private TextBox _descBox = null!;
        private ComboBox _statusBox = null!;
        private ComboBox _priorityBox = null!;

        public TaskDialog(bool isDarkMode)
        {
            Title = "Add Task";
            Width = 350;
            Height = 400;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.NoResize;
            
            var bgColor = isDarkMode ? Color.FromArgb(0xF0, 0x20, 0x25, 0x30) : Color.FromArgb(0xF0, 0xE8, 0xEC, 0xF0);
            var textColor = isDarkMode ? Colors.White : Color.FromRgb(0x30, 0x30, 0x40);
            var labelColor = isDarkMode ? Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x80, 0x40, 0x50, 0x60);
            var inputBg = isDarkMode ? Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF);
            var borderColor = isDarkMode ? Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x60, 0x80, 0x90, 0xA0);
            
            var border = new Border
            {
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ヘッダー
            var header = new TextBlock
            {
                Text = "詳細",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // フォーム
            var form = new StackPanel();
            Grid.SetRow(form, 1);

            form.Children.Add(CreateLabel("タイトル", labelColor));
            _titleBox = CreateTextBox(inputBg, textColor, borderColor);
            form.Children.Add(_titleBox);

            form.Children.Add(CreateLabel("説明", labelColor));
            _descBox = CreateTextBox(inputBg, textColor, borderColor);
            _descBox.Height = 60;
            _descBox.TextWrapping = TextWrapping.Wrap;
            _descBox.AcceptsReturn = true;
            form.Children.Add(_descBox);

            form.Children.Add(CreateLabel("ステータス", labelColor));
            _statusBox = CreateComboBox(inputBg, borderColor);
            _statusBox.Items.Add("todo");
            _statusBox.Items.Add("in progress");
            _statusBox.Items.Add("done");
            _statusBox.SelectedIndex = 0;
            form.Children.Add(_statusBox);

            form.Children.Add(CreateLabel("優先度", labelColor));
            _priorityBox = CreateComboBox(inputBg, borderColor);
            _priorityBox.Items.Add("low");
            _priorityBox.Items.Add("normal");
            _priorityBox.Items.Add("high");
            _priorityBox.SelectedIndex = 1;
            form.Children.Add(_priorityBox);

            grid.Children.Add(form);

            // ボタン
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetRow(btnPanel, 2);

            var saveBtn = new Button
            {
                Content = "保存",
                Background = new SolidColorBrush(isDarkMode ? Color.FromRgb(0x4C, 0xAF, 0x50) : Color.FromRgb(0x40, 0x80, 0x90)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 10, 10, 0)
            };
            saveBtn.Click += (s, e) => { SaveAndClose(); };

            var cancelBtn = new Button
            {
                Content = "キャンセル",
                Background = new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80)),
                Foreground = new SolidColorBrush(textColor),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 10, 0, 0)
            };
            cancelBtn.Click += (s, e) => { DialogResult = false; Close(); };

            btnPanel.Children.Add(saveBtn);
            btnPanel.Children.Add(cancelBtn);
            grid.Children.Add(btnPanel);

            border.Child = grid;
            Content = border;

            // ドラッグ移動
            border.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        }

        private TextBlock CreateLabel(string text, Color color)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 2)
            };
        }

        private TextBox CreateTextBox(Color bg, Color fg, Color border)
        {
            return new TextBox
            {
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                BorderBrush = new SolidColorBrush(border),
                Padding = new Thickness(8, 5, 8, 5)
            };
        }

        private ComboBox CreateComboBox(Color bg, Color border)
        {
            return new ComboBox
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(border),
                Padding = new Thickness(8, 5, 8, 5)
            };
        }

        private void SaveAndClose()
        {
            TaskTitle = _titleBox.Text;
            TaskDescription = _descBox.Text;
            TaskStatus = _statusBox.SelectedItem?.ToString() ?? "todo";
            TaskPriority = _priorityBox.SelectedItem?.ToString() ?? "normal";
            DialogResult = true;
            Close();
        }
    }
}
