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
            ConvertExistingTasks();
            UpdateTaskStats();
        }

        private void ConvertExistingTasks()
        {
            // 既存のCheckBoxをGrid形式（削除ボタン付き）に変換
            var existingCheckBoxes = TasksPanel.Children.OfType<CheckBox>().ToList();
            TasksPanel.Children.Clear();

            foreach (var cb in existingCheckBoxes)
            {
                var title = cb.Content?.ToString() ?? "";
                var isChecked = cb.IsChecked == true;
                var taskItem = CreateTaskItem(title, "", "normal", null);
                
                // チェック状態を復元
                var newCheckBox = taskItem.Children.OfType<CheckBox>().FirstOrDefault();
                if (newCheckBox != null)
                    newCheckBox.IsChecked = isChecked;
                
                TasksPanel.Children.Add(taskItem);
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
                AddTask(dialog.TaskTitle, dialog.TaskDescription, dialog.TaskStatus, dialog.TaskPriority, dialog.TaskDueDate);
            }
        }

        private void AddTask(string title, string description, string status, string priority, DateTime? dueDate = null)
        {
            if (string.IsNullOrWhiteSpace(title)) return;

            var taskItem = CreateTaskItem(title, description, priority, dueDate);
            TasksPanel.Children.Add(taskItem);
            UpdateTaskStats();
        }

        private Grid CreateTaskItem(string title, string description, string priority, DateTime? dueDate)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBrush = _isDarkMode ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x40));

            // チェックボックスとタイトル
            var checkBox = new CheckBox
            {
                Foreground = textBrush,
                Margin = new Thickness(0, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
            };

            // タイトルと期限を表示
            var contentPanel = new StackPanel { Orientation = Orientation.Horizontal };
            contentPanel.Children.Add(new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center });
            
            if (dueDate.HasValue)
            {
                var dueBrush = dueDate.Value.Date < DateTime.Today 
                    ? Brushes.Red 
                    : (dueDate.Value.Date == DateTime.Today ? Brushes.Orange : new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)));
                contentPanel.Children.Add(new TextBlock 
                { 
                    Text = $" ({dueDate.Value:M/d})", 
                    Foreground = dueBrush,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            
            checkBox.Content = contentPanel;
            checkBox.ToolTip = $"{description}\nPriority: {priority}" + (dueDate.HasValue ? $"\nDue: {dueDate.Value:yyyy/MM/dd}" : "");
            checkBox.Checked += TaskCheckBox_CheckedChanged;
            checkBox.Unchecked += TaskCheckBox_CheckedChanged;
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            // 削除ボタン
            var deleteBtn = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0x50, 0x50)),
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) => DeleteTask(grid);
            Grid.SetColumn(deleteBtn, 1);
            grid.Children.Add(deleteBtn);

            return grid;
        }

        private void DeleteTask(Grid taskItem)
        {
            TasksPanel.Children.Remove(taskItem);
            UpdateTaskStats();
        }

        private void TaskCheckBox_CheckedChanged(object? sender, RoutedEventArgs e)
        {
            UpdateTaskStats();
        }

        private void UpdateTaskStats()
        {
            // CheckBoxを直接含む場合とGrid内のCheckBoxの両方をカウント
            var directCheckBoxes = TasksPanel.Children.OfType<CheckBox>();
            var gridCheckBoxes = TasksPanel.Children.OfType<Grid>()
                .SelectMany(g => g.Children.OfType<CheckBox>());
            var allCheckBoxes = directCheckBoxes.Concat(gridCheckBoxes).ToList();

            var totalTasks = allCheckBoxes.Count;
            var completedTasks = allCheckBoxes.Count(cb => cb.IsChecked == true);
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
        public DateTime? TaskDueDate { get; private set; } = null;

        private TextBox _titleBox = null!;
        private TextBox _descBox = null!;
        private ComboBox _statusBox = null!;
        private ComboBox _priorityBox = null!;
        private DatePicker _dueDatePicker = null!;

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

            // ヘッダー（タイトルと閉じるボタン）
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var header = new TextBlock
            {
                Text = "タスク追加",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(header, 0);
            headerGrid.Children.Add(header);

            var closeBtn = new Button
            {
                Content = "×",
                Width = 28,
                Height = 28,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(textColor),
                BorderThickness = new Thickness(0),
                FontSize = 18,
                Cursor = Cursors.Hand
            };
            closeBtn.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(closeBtn, 1);
            headerGrid.Children.Add(closeBtn);

            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            // スクロール可能なフォーム
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 10, 0, 0)
            };
            
            // スクロールバーのスタイル設定
            var scrollBarColor = isDarkMode ? Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x40, 0x60, 0x70, 0x80);
            scrollViewer.Resources.Add(SystemColors.ControlBrushKey, new SolidColorBrush(Colors.Transparent));
            
            var form = new StackPanel { Margin = new Thickness(0, 0, 5, 0) };

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

            form.Children.Add(CreateLabel("期限", labelColor));
            _dueDatePicker = new DatePicker
            {
                Background = new SolidColorBrush(inputBg),
                BorderBrush = new SolidColorBrush(borderColor),
                Padding = new Thickness(8, 5, 8, 5)
            };
            form.Children.Add(_dueDatePicker);

            scrollViewer.Content = form;
            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

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
            TaskDueDate = _dueDatePicker.SelectedDate;
            DialogResult = true;
            Close();
        }
    }
}
