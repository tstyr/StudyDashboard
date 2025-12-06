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
        private Point _originalPosition;
        private string _resizeCorner = "";
        private Rectangle? _snapGuide;
        
        private const double SnapThreshold = 25;
        private const double EdgeSnapThreshold = 40;

        private const double ResizeCornerSize = 20;

        public DraggableWidget()
        {
            this.MouseLeftButtonDown += DraggableWidget_MouseLeftButtonDown;
            this.MouseLeftButtonUp += DraggableWidget_MouseLeftButtonUp;
            this.MouseMove += DraggableWidget_MouseMove;
            this.MouseRightButtonDown += DraggableWidget_MouseRightButtonDown;
            this.MouseLeave += DraggableWidget_MouseLeave;
        }

        private void DraggableWidget_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDragging && !_isResizing)
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private void DraggableWidget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            
            // 四隅のリサイズ判定
            bool isBottomRight = position.X > this.ActualWidth - ResizeCornerSize && position.Y > this.ActualHeight - ResizeCornerSize;
            bool isBottomLeft = position.X < ResizeCornerSize && position.Y > this.ActualHeight - ResizeCornerSize;
            bool isTopRight = position.X > this.ActualWidth - ResizeCornerSize && position.Y < ResizeCornerSize;
            bool isTopLeft = position.X < ResizeCornerSize && position.Y < ResizeCornerSize;
            
            if (isBottomRight || isBottomLeft || isTopRight || isTopLeft)
            {
                _isResizing = true;
                _resizeStartPoint = e.GetPosition((UIElement)this.Parent);
                _originalSize = new Size(this.ActualWidth, this.ActualHeight);
                _originalPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
                
                if (isBottomRight) _resizeCorner = "BR";
                else if (isBottomLeft) _resizeCorner = "BL";
                else if (isTopRight) _resizeCorner = "TR";
                else if (isTopLeft) _resizeCorner = "TL";
            }
            else
            {
                _isDragging = true;
                _lastPosition = e.GetPosition((UIElement)this.Parent);
                _dragStartPos = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
                _dragStartSize = new Size(this.ActualWidth, this.ActualHeight);
                this.Cursor = Cursors.SizeAll;
                Canvas.SetZIndex(this, 100);
            }
            
            this.CaptureMouse();
        }

        private void DraggableWidget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && this.Parent is Canvas canvas)
            {
                Canvas.SetZIndex(this, 0);
                RemoveSnapGuide(canvas);
                
                // まず重なりをチェックして入れ替え
                bool swapped = CheckAndResolveOverlaps(canvas);
                
                // 入れ替えがなかった場合のみスナップを適用
                if (!swapped)
                {
                    ApplySnap(canvas);
                }
                
                UpdateCanvasSize(canvas);
            }
            if (_isResizing && this.Parent is Canvas canvas2)
            {
                AdjustOtherWidgets(canvas2);
            }
            _isDragging = false;
            _isResizing = false;
            this.Cursor = Cursors.Arrow;
            this.ReleaseMouseCapture();
        }

        private bool CheckAndResolveOverlaps(Canvas canvas)
        {
            double myL = Canvas.GetLeft(this);
            double myT = Canvas.GetTop(this);
            double myW = this.ActualWidth;
            double myH = this.ActualHeight;
            double myR = myL + myW;
            double myB = myT + myH;

            DraggableWidget? bestMatch = null;
            double maxOverlapArea = 0;

            foreach (UIElement child in canvas.Children)
            {
                if (child == this || !(child is DraggableWidget other) || other.Visibility != Visibility.Visible) continue;

                double oL = Canvas.GetLeft(other);
                double oT = Canvas.GetTop(other);
                double oW = other.ActualWidth;
                double oH = other.ActualHeight;
                double oR = oL + oW;
                double oB = oT + oH;

                if (myL < oR && myR > oL && myT < oB && myB > oT)
                {
                    // 重なり面積を計算
                    double overlapW = Math.Min(myR, oR) - Math.Max(myL, oL);
                    double overlapH = Math.Min(myB, oB) - Math.Max(myT, oT);
                    double overlapArea = overlapW * overlapH;
                    
                    if (overlapArea > maxOverlapArea)
                    {
                        maxOverlapArea = overlapArea;
                        bestMatch = other;
                    }
                }
            }

            // 最も重なりが大きいウィジェットと入れ替え
            if (bestMatch != null)
            {
                SwapWidgets(bestMatch);
                return true;
            }
            return false;
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
            // リサイズ時は入れ替えしない（サイズ調整のみ）
            UpdateCanvasSize(canvas);
        }

        private Point _dragStartPos;
        private Size _dragStartSize;
        
        private void SwapWidgets(DraggableWidget other)
        {
            // 相手の現在の位置とサイズを保存
            double oL = Canvas.GetLeft(other);
            double oT = Canvas.GetTop(other);
            double oW = other.ActualWidth;
            double oH = other.ActualHeight;

            // 相手を自分の元の位置・サイズに移動
            Canvas.SetLeft(other, _dragStartPos.X);
            Canvas.SetTop(other, _dragStartPos.Y);
            other.Width = _dragStartSize.Width;
            other.Height = _dragStartSize.Height;
            
            // 自分を相手の元の位置・サイズに設定
            Canvas.SetLeft(this, oL);
            Canvas.SetTop(this, oT);
            this.Width = oW;
            this.Height = oH;
        }

        private void UpdateCanvasSize(Canvas canvas)
        {
            // Canvasサイズは変更しない（ウィンドウサイズに合わせる）
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
            
            // カーソル表示（ドラッグ・リサイズ中以外）
            if (!_isDragging && !_isResizing)
            {
                bool isBottomRight = position.X > this.ActualWidth - ResizeCornerSize && position.Y > this.ActualHeight - ResizeCornerSize;
                bool isTopLeft = position.X < ResizeCornerSize && position.Y < ResizeCornerSize;
                bool isBottomLeft = position.X < ResizeCornerSize && position.Y > this.ActualHeight - ResizeCornerSize;
                bool isTopRight = position.X > this.ActualWidth - ResizeCornerSize && position.Y < ResizeCornerSize;
                
                if (isBottomRight || isTopLeft)
                    this.Cursor = Cursors.SizeNWSE;
                else if (isBottomLeft || isTopRight)
                    this.Cursor = Cursors.SizeNESW;
                else
                    this.Cursor = Cursors.Arrow;
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

                double newWidth, newHeight, newLeft, newTop;
                
                switch (_resizeCorner)
                {
                    case "BR": // 右下
                        newWidth = Math.Max(100, _originalSize.Width + deltaX);
                        newHeight = Math.Max(80, _originalSize.Height + deltaY);
                        newLeft = _originalPosition.X;
                        newTop = _originalPosition.Y;
                        break;
                    case "BL": // 左下
                        newWidth = Math.Max(100, _originalSize.Width - deltaX);
                        newHeight = Math.Max(80, _originalSize.Height + deltaY);
                        newLeft = _originalPosition.X + _originalSize.Width - newWidth;
                        newTop = _originalPosition.Y;
                        break;
                    case "TR": // 右上
                        newWidth = Math.Max(100, _originalSize.Width + deltaX);
                        newHeight = Math.Max(80, _originalSize.Height - deltaY);
                        newLeft = _originalPosition.X;
                        newTop = _originalPosition.Y + _originalSize.Height - newHeight;
                        break;
                    case "TL": // 左上
                        newWidth = Math.Max(100, _originalSize.Width - deltaX);
                        newHeight = Math.Max(80, _originalSize.Height - deltaY);
                        newLeft = _originalPosition.X + _originalSize.Width - newWidth;
                        newTop = _originalPosition.Y + _originalSize.Height - newHeight;
                        break;
                    default:
                        return;
                }

                // 境界チェック
                newLeft = Math.Max(0, newLeft);
                newTop = Math.Max(0, newTop);
                newWidth = Math.Min(newWidth, canvas2.ActualWidth - newLeft - 8);
                newHeight = Math.Min(newHeight, canvas2.ActualHeight - newTop - 8);
                
                Canvas.SetLeft(this, newLeft);
                Canvas.SetTop(this, newTop);
                this.Width = newWidth;
                this.Height = newHeight;
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
