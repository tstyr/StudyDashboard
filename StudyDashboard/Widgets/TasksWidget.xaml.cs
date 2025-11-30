using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StudyDashboard
{
    public partial class TasksWidget : DraggableWidget
    {
        public TasksWidget()
        {
            InitializeComponent();
            UpdateTaskStats();
            
            // 既存のタスクにイベントハンドラーを追加
            foreach (CheckBox checkBox in TasksPanel.Children.OfType<CheckBox>())
            {
                checkBox.Checked += TaskCheckBox_CheckedChanged;
                checkBox.Unchecked += TaskCheckBox_CheckedChanged;
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewTask();
        }

        private void NewTaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewTask();
            }
        }

        private void AddNewTask()
        {
            var taskText = NewTaskTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(taskText) || taskText == "Add new task...")
            {
                return;
            }

            var checkBox = new CheckBox
            {
                Content = taskText,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 2, 0, 2)
            };
            
            checkBox.Checked += TaskCheckBox_CheckedChanged;
            checkBox.Unchecked += TaskCheckBox_CheckedChanged;
            
            TasksPanel.Children.Add(checkBox);
            NewTaskTextBox.Text = "Add new task...";
            
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

        private void NewTaskTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NewTaskTextBox.Text == "Add new task...")
            {
                NewTaskTextBox.Text = "";
            }
        }

        private void NewTaskTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTaskTextBox.Text))
            {
                NewTaskTextBox.Text = "Add new task...";
            }
        }
    }
}