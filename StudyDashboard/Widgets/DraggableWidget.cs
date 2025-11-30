using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StudyDashboard
{
    public class DraggableWidget : UserControl
    {
        private bool _isDragging;
        private Point _lastPosition;
        private bool _isResizing;
        private Point _resizeStartPoint;
        private Size _originalSize;

        public DraggableWidget()
        {
            this.MouseLeftButtonDown += DraggableWidget_MouseLeftButtonDown;
            this.MouseLeftButtonUp += DraggableWidget_MouseLeftButtonUp;
            this.MouseMove += DraggableWidget_MouseMove;
            this.MouseRightButtonDown += DraggableWidget_MouseRightButtonDown;
        }

        private void DraggableWidget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            
            // 右下角での右クリックでリサイズモード
            if (position.X > this.ActualWidth - 20 && position.Y > this.ActualHeight - 20)
            {
                _isResizing = true;
                _resizeStartPoint = e.GetPosition((UIElement)this.Parent);
                _originalSize = new Size(this.ActualWidth, this.ActualHeight);
            }
            else
            {
                _isDragging = true;
                _lastPosition = e.GetPosition((UIElement)this.Parent);
            }
            
            this.CaptureMouse();
        }

        private void DraggableWidget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _isResizing = false;
            this.ReleaseMouseCapture();
        }

        private void DraggableWidget_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition((UIElement)this.Parent);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                var newLeft = Canvas.GetLeft(this) + deltaX;
                var newTop = Canvas.GetTop(this) + deltaY;

                // 親コンテナの境界内に制限
                if (this.Parent is Canvas canvas)
                {
                    newLeft = Math.Max(0, Math.Min(newLeft, canvas.ActualWidth - this.ActualWidth));
                    newTop = Math.Max(0, Math.Min(newTop, canvas.ActualHeight - this.ActualHeight));
                }

                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);

                _lastPosition = currentPosition;
            }
            else if (_isResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition((UIElement)this.Parent);
                var deltaX = currentPosition.X - _resizeStartPoint.X;
                var deltaY = currentPosition.Y - _resizeStartPoint.Y;

                var newWidth = Math.Max(100, _originalSize.Width + deltaX);
                var newHeight = Math.Max(80, _originalSize.Height + deltaY);

                // 親コンテナの境界内に制限
                if (this.Parent is Canvas canvas)
                {
                    var maxWidth = canvas.ActualWidth - Canvas.GetLeft(this);
                    var maxHeight = canvas.ActualHeight - Canvas.GetTop(this);
                    newWidth = Math.Min(newWidth, maxWidth);
                    newHeight = Math.Min(newHeight, maxHeight);
                }

                this.Width = newWidth;
                this.Height = newHeight;
            }
        }

        private void DraggableWidget_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var contextMenu = new ContextMenu();
            
            var resetPosItem = new MenuItem { Header = "位置をリセット" };
            resetPosItem.Click += (s, args) => {
                Canvas.SetLeft(this, 20);
                Canvas.SetTop(this, 20);
            };
            contextMenu.Items.Add(resetPosItem);
            
            var resetSizeItem = new MenuItem { Header = "サイズをリセット" };
            resetSizeItem.Click += (s, args) => {
                this.Width = 280;
                this.Height = 200;
            };
            contextMenu.Items.Add(resetSizeItem);
            
            contextMenu.Items.Add(new Separator());
            
            var bringToFrontItem = new MenuItem { Header = "前面に移動" };
            bringToFrontItem.Click += (s, args) => {
                if (this.Parent is Canvas canvas)
                {
                    Canvas.SetZIndex(this, 999);
                }
            };
            contextMenu.Items.Add(bringToFrontItem);
            
            var sendToBackItem = new MenuItem { Header = "背面に移動" };
            sendToBackItem.Click += (s, args) => {
                if (this.Parent is Canvas canvas)
                {
                    Canvas.SetZIndex(this, 0);
                }
            };
            contextMenu.Items.Add(sendToBackItem);
            
            this.ContextMenu = contextMenu;
        }
    }
}