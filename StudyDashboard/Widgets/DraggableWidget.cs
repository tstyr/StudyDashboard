using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace StudyDashboard
{
    public class DraggableWidget : UserControl
    {
        private bool _isDragging;
        private Point _lastPosition;
        private bool _isResizing;
        private Point _resizeStartPoint;
        private Size _originalSize;
        private Rectangle? _snapGuide;
        
        private const double SnapThreshold = 25;
        private const double EdgeSnapThreshold = 40;

        public DraggableWidget()
        {
            this.MouseLeftButtonDown += DraggableWidget_MouseLeftButtonDown;
            this.MouseLeftButtonUp += DraggableWidget_MouseLeftButtonUp;
            this.MouseMove += DraggableWidget_MouseMove;
            this.MouseRightButtonDown += DraggableWidget_MouseRightButtonDown;
            this.Cursor = Cursors.SizeAll;
        }

        private void DraggableWidget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            
            if (position.X > this.ActualWidth - 25 && position.Y > this.ActualHeight - 25)
            {
                _isResizing = true;
                _resizeStartPoint = e.GetPosition((UIElement)this.Parent);
                _originalSize = new Size(this.ActualWidth, this.ActualHeight);
                this.Cursor = Cursors.SizeNWSE;
            }
            else
            {
                _isDragging = true;
                _lastPosition = e.GetPosition((UIElement)this.Parent);
                _dragStartPos = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
                _dragStartSize = new Size(this.ActualWidth, this.ActualHeight);
                Canvas.SetZIndex(this, 100);
            }
            
            this.CaptureMouse();
        }

        private void DraggableWidget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && this.Parent is Canvas canvas)
            {
                ApplySnap(canvas);
                Canvas.SetZIndex(this, 0);
                RemoveSnapGuide(canvas);
                CheckAndResolveOverlaps(canvas);
                UpdateCanvasSize(canvas);
            }
            if (_isResizing && this.Parent is Canvas canvas2)
            {
                AdjustOtherWidgets(canvas2);
            }
            _isDragging = false;
            _isResizing = false;
            this.Cursor = Cursors.SizeAll;
            this.ReleaseMouseCapture();
        }

        private void CheckAndResolveOverlaps(Canvas canvas)
        {
            double myL = Canvas.GetLeft(this);
            double myT = Canvas.GetTop(this);
            double myR = myL + this.ActualWidth;
            double myB = myT + this.ActualHeight;

            foreach (UIElement child in canvas.Children)
            {
                if (child == this || !(child is DraggableWidget other) || other.Visibility != Visibility.Visible) continue;

                double oL = Canvas.GetLeft(other);
                double oT = Canvas.GetTop(other);
                double oR = oL + other.ActualWidth;
                double oB = oT + other.ActualHeight;

                if (myL < oR && myR > oL && myT < oB && myB > oT)
                {
                    // 重なっている - 位置を入れ替え
                    SwapOrAdjust(canvas, other);
                }
            }
        }

        private void ApplySnap(Canvas canvas)
        {
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);
            double width = this.ActualWidth;
            double height = this.ActualHeight;
            double canvasW = canvas.ActualWidth;
            double canvasH = canvas.ActualHeight;

            // 画面端へのスナップ
            if (left < EdgeSnapThreshold) left = 8;
            else if (left + width > canvasW - EdgeSnapThreshold) left = canvasW - width - 8;

            if (top < EdgeSnapThreshold) top = 8;
            else if (top + height > canvasH - EdgeSnapThreshold) top = canvasH - height - 8;

            // 他のウィジェットへのスナップ
            foreach (UIElement child in canvas.Children)
            {
                if (child == this || !(child is DraggableWidget other) || other.Visibility != Visibility.Visible) continue;

                double oL = Canvas.GetLeft(other);
                double oT = Canvas.GetTop(other);
                double oR = oL + other.ActualWidth;
                double oB = oT + other.ActualHeight;

                // 左端を他の右端にスナップ
                if (Math.Abs(left - oR) < SnapThreshold) left = oR + 8;
                // 右端を他の左端にスナップ
                if (Math.Abs(left + width - oL) < SnapThreshold) left = oL - width - 8;
                // 上端を他の下端にスナップ
                if (Math.Abs(top - oB) < SnapThreshold) top = oB + 8;
                // 下端を他の上端にスナップ
                if (Math.Abs(top + height - oT) < SnapThreshold) top = oT - height - 8;
                // 左端同士
                if (Math.Abs(left - oL) < SnapThreshold) left = oL;
                // 上端同士
                if (Math.Abs(top - oT) < SnapThreshold) top = oT;
                // 右端同士
                if (Math.Abs(left + width - oR) < SnapThreshold) left = oR - width;
                // 下端同士
                if (Math.Abs(top + height - oB) < SnapThreshold) top = oB - height;
            }

            Canvas.SetLeft(this, Math.Max(0, Math.Min(left, canvasW - width)));
            Canvas.SetTop(this, Math.Max(0, Math.Min(top, canvasH - height)));
        }

        private void AdjustOtherWidgets(Canvas canvas)
        {
            double myL = Canvas.GetLeft(this);
            double myT = Canvas.GetTop(this);
            double myR = myL + this.ActualWidth;
            double myB = myT + this.ActualHeight;

            var overlapping = new List<DraggableWidget>();
            
            foreach (UIElement child in canvas.Children)
            {
                if (child == this || !(child is DraggableWidget other) || other.Visibility != Visibility.Visible) continue;

                double oL = Canvas.GetLeft(other);
                double oT = Canvas.GetTop(other);
                double oR = oL + other.ActualWidth;
                double oB = oT + other.ActualHeight;

                // 重なりチェック
                if (myL < oR && myR > oL && myT < oB && myB > oT)
                    overlapping.Add(other);
            }

            foreach (var other in overlapping)
            {
                SwapOrAdjust(canvas, other);
            }
            
            // Canvasサイズを更新
            UpdateCanvasSize(canvas);
        }

        private Point _dragStartPos;
        private Size _dragStartSize;
        
        private void SwapOrAdjust(Canvas canvas, DraggableWidget other)
        {
            double myL = Canvas.GetLeft(this);
            double myT = Canvas.GetTop(this);
            double myW = this.ActualWidth;
            double myH = this.ActualHeight;
            
            double oL = Canvas.GetLeft(other);
            double oT = Canvas.GetTop(other);
            double oW = other.ActualWidth;
            double oH = other.ActualHeight;

            // 重なり量を計算
            double overlapL = Math.Max(myL, oL);
            double overlapR = Math.Min(myL + myW, oL + oW);
            double overlapT = Math.Max(myT, oT);
            double overlapB = Math.Min(myT + myH, oT + oH);
            double overlapW = overlapR - overlapL;
            double overlapH = overlapB - overlapT;
            double overlapArea = overlapW * overlapH;
            double otherArea = oW * oH;

            // 50%以上重なっている場合は位置とサイズを入れ替え
            if (overlapArea > otherArea * 0.5)
            {
                // 位置を入れ替え
                Canvas.SetLeft(other, _dragStartPos.X);
                Canvas.SetTop(other, _dragStartPos.Y);
                
                // サイズも入れ替え
                other.Width = _dragStartSize.Width;
                other.Height = _dragStartSize.Height;
            }
            else
            {
                // 押し出す
                double pushRight = myL + myW - oL;
                double pushLeft = oL + oW - myL;
                double pushDown = myT + myH - oT;
                double pushUp = oT + oH - myT;

                double minPush = Math.Min(Math.Min(pushRight, pushLeft), Math.Min(pushDown, pushUp));

                if (minPush == pushRight)
                    Canvas.SetLeft(other, myL + myW + 8);
                else if (minPush == pushLeft)
                    Canvas.SetLeft(other, myL - oW - 8);
                else if (minPush == pushDown)
                    Canvas.SetTop(other, myT + myH + 8);
                else
                    Canvas.SetTop(other, myT - oH - 8);
            }
        }

        private void UpdateCanvasSize(Canvas canvas)
        {
            double maxRight = 0;
            double maxBottom = 0;
            
            foreach (UIElement child in canvas.Children)
            {
                if (child is DraggableWidget widget && widget.Visibility == Visibility.Visible)
                {
                    double r = Canvas.GetLeft(widget) + widget.ActualWidth + 20;
                    double b = Canvas.GetTop(widget) + widget.ActualHeight + 20;
                    maxRight = Math.Max(maxRight, r);
                    maxBottom = Math.Max(maxBottom, b);
                }
            }
            
            canvas.Width = Math.Max(canvas.MinWidth, maxRight);
            canvas.Height = Math.Max(canvas.MinHeight, maxBottom);
        }

        private void ShowSnapGuide(Canvas canvas, double x, double y, double w, double h)
        {
            if (_snapGuide == null)
            {
                _snapGuide = new Rectangle
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0xA0, 0xFF)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0xA0, 0xFF))
                };
                canvas.Children.Add(_snapGuide);
            }
            Canvas.SetLeft(_snapGuide, x);
            Canvas.SetTop(_snapGuide, y);
            _snapGuide.Width = w;
            _snapGuide.Height = h;
            Canvas.SetZIndex(_snapGuide, 99);
        }

        private void RemoveSnapGuide(Canvas canvas)
        {
            if (_snapGuide != null)
            {
                canvas.Children.Remove(_snapGuide);
                _snapGuide = null;
            }
        }

        private void DraggableWidget_MouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            
            // リサイズカーソル表示
            if (!_isDragging && !_isResizing)
            {
                this.Cursor = (position.X > this.ActualWidth - 25 && position.Y > this.ActualHeight - 25) 
                    ? Cursors.SizeNWSE : Cursors.SizeAll;
            }

            if (_isDragging && e.LeftButton == MouseButtonState.Pressed && this.Parent is Canvas canvas)
            {
                var currentPosition = e.GetPosition(canvas);
                var deltaX = currentPosition.X - _lastPosition.X;
                var deltaY = currentPosition.Y - _lastPosition.Y;

                var newLeft = Canvas.GetLeft(this) + deltaX;
                var newTop = Canvas.GetTop(this) + deltaY;

                newLeft = Math.Max(0, Math.Min(newLeft, canvas.ActualWidth - this.ActualWidth));
                newTop = Math.Max(0, Math.Min(newTop, canvas.ActualHeight - this.ActualHeight));

                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);
                _lastPosition = currentPosition;

                // スナップガイド表示
                ShowSnapGuideIfNear(canvas, newLeft, newTop);
            }
            else if (_isResizing && e.LeftButton == MouseButtonState.Pressed && this.Parent is Canvas canvas2)
            {
                var currentPosition = e.GetPosition(canvas2);
                var deltaX = currentPosition.X - _resizeStartPoint.X;
                var deltaY = currentPosition.Y - _resizeStartPoint.Y;

                var newWidth = Math.Max(150, _originalSize.Width + deltaX);
                var newHeight = Math.Max(100, _originalSize.Height + deltaY);

                var maxWidth = canvas2.ActualWidth - Canvas.GetLeft(this) - 8;
                var maxHeight = canvas2.ActualHeight - Canvas.GetTop(this) - 8;
                
                this.Width = Math.Min(newWidth, maxWidth);
                this.Height = Math.Min(newHeight, maxHeight);
            }
        }

        private void ShowSnapGuideIfNear(Canvas canvas, double left, double top)
        {
            foreach (UIElement child in canvas.Children)
            {
                if (child == this || !(child is DraggableWidget other) || other.Visibility != Visibility.Visible) continue;

                double oL = Canvas.GetLeft(other);
                double oR = oL + other.ActualWidth;

                if (Math.Abs(left - oR) < SnapThreshold)
                {
                    ShowSnapGuide(canvas, oR + 4, top, 4, this.ActualHeight);
                    return;
                }
                if (Math.Abs(left + this.ActualWidth - oL) < SnapThreshold)
                {
                    ShowSnapGuide(canvas, oL - 8, top, 4, this.ActualHeight);
                    return;
                }
            }
            RemoveSnapGuide(canvas);
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
                this.Width = 300;
                this.Height = 250;
            };
            contextMenu.Items.Add(resetSizeItem);
            
            contextMenu.Items.Add(new Separator());
            
            var bringToFrontItem = new MenuItem { Header = "前面に移動" };
            bringToFrontItem.Click += (s, args) => Canvas.SetZIndex(this, 999);
            contextMenu.Items.Add(bringToFrontItem);
            
            var sendToBackItem = new MenuItem { Header = "背面に移動" };
            sendToBackItem.Click += (s, args) => Canvas.SetZIndex(this, 0);
            contextMenu.Items.Add(sendToBackItem);
            
            this.ContextMenu = contextMenu;
        }
    }
}
