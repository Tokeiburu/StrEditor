using ErrorManager;
using StrEditor.Core.TimelineEditor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StrEditor.Core.TimelineEditor.Controls {
	/// <summary>
	/// Interaction logic for Timeline.xaml
	/// </summary>
	public partial class Timeline : UserControl {
		private Editor _editor;
		private StrController _controller;
		private ScrollViewer _svKeyFrames;

		public TimelineIndicatorRenderer TimelineIndicatorRenderer => _renderer;

		public Timeline() {
			InitializeComponent();
		}

		public void Init(Editor editor) {
			_editor = editor;
			_controller = _editor.Controller;
			_svKeyFrames = _editor._svKeyFrames;
			_renderer.Init(editor);

			_editor.Renderer.RendererUpdated += _renderer_RendererUpdated;
			_editor.TimelineFrameIndexChanged += _editor_TimelineFrameIndexChanged;
			_editor.ViewChanged += _editor_ViewChanged;
			_svKeyFrames.ScrollChanged += _svKeyFrames_ScrollChanged;

			_gridTopPart.MouseLeftButtonDown += _gridTopPart_MouseLeftButtonDown;
			_timelineSelector._borderSelector.MouseLeftButtonDown += _timeline_MouseLeftButtonDown;
			_timelineSelector._borderSelector.MouseMove += _timeline_MouseMove;
			_timelineSelector._borderSelector.MouseLeftButtonUp += _timeline_MouseLeftButtonUp;
		}

		private void _svKeyFrames_ScrollChanged(object sender, ScrollChangedEventArgs e) {
			_checkLineVisibility(_svKeyFrames);
		}

		private void _editor_ViewChanged(ScrollViewer scrollViewer, double horizontalOffset) {
			_svTimeline.ScrollToHorizontalOffset(horizontalOffset);
			_svTopPartFrameSelector.ScrollToHorizontalOffset(horizontalOffset);
			_timelineSelectorLine.Height = scrollViewer.ViewportHeight;
		}

		private void _editor_TimelineFrameIndexChanged() {
			_timelineSelector.Margin = new Thickness(-1 + _editor.TimelineFrameIndex * _editor.KeyFrameWidth, 0, 0, 0);
			_timelineSelectorLine.Margin = _timelineSelector.Margin;
			_checkLineVisibility(_svKeyFrames);
		}

		private void _checkLineVisibility(ScrollViewer view) {
			Visibility targetVisibility = Visibility.Visible;

			if (view.ComputedVerticalScrollBarVisibility != Visibility.Visible) {
				targetVisibility = Visibility.Visible;
			}
			else {
				var wp = _timelineSelectorLine.Margin.Left - _svTopPartFrameSelector.HorizontalOffset + 5;
				
				if (wp < view.ViewportWidth) {
					targetVisibility = Visibility.Visible;
				}
				else {
					targetVisibility = Visibility.Collapsed;
				}
			}

			if (targetVisibility != _timelineSelectorLine.Visibility)
				_timelineSelectorLine.Visibility = targetVisibility;
		}

		private void _renderer_RendererUpdated() {
			_updateTimelineLabels();
		}

		private void _timeline_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (_controller.Str == null)
				return;

			_timelineSelector._borderSelector.CaptureMouse();
		}

		private void _timeline_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			_timelineSelector._borderSelector.ReleaseMouseCapture();
			e.Handled = true;
		}

		private void _timeline_MouseMove(object sender, MouseEventArgs e) {
			try {
				if (_controller.Str == null)
					return;

				if (_timelineSelector._borderSelector.IsMouseCaptured) {
					var position = e.GetPosition(_gridTopPart);

					var positionViewport = e.GetPosition(_svTopPartFrameSelector);
					var xAdjust = _svKeyFrames.ScrollableHeight > 0 ? SystemParameters.ScrollWidth : 0;
					
					if (positionViewport.X > _svTopPartFrameSelector.ActualWidth - xAdjust) {
						_svKeyFrames.ScrollToHorizontalOffset(_svTopPartFrameSelector.HorizontalOffset + _editor.KeyFrameWidth);
					}
					else if (positionViewport.X < 0) {
						_svKeyFrames.ScrollToHorizontalOffset(_svTopPartFrameSelector.HorizontalOffset - _editor.KeyFrameWidth);
					}
					_checkLineVisibility(_svKeyFrames);

					var x = position.X;

					if (x >= _controller.Str.KeyFrameCount * _editor.KeyFrameWidth) {
						x = (_controller.Str.KeyFrameCount - 1) * _editor.KeyFrameWidth;
					}

					if (x < 0) {
						x = 0;
					}

					var frameIndex = (int)(x / _editor.KeyFrameWidth);
					_editor.TimelineFrameIndex = frameIndex;
					e.Handled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _gridTopPart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				if (_controller.Str == null)
					return;

				_controller.PlayAnimation.Stop();

				_timelineSelector._borderSelector.CaptureMouse();

				var position = e.GetPosition(_gridTopPart);
				var x = position.X;

				if (x >= _controller.Str.KeyFrameCount * _editor.KeyFrameWidth) {
					x = (_controller.Str.KeyFrameCount - 1) * _editor.KeyFrameWidth;
				}

				if (x < 0) {
					x = 0;
				}

				var frameIndex = (int)(x / _editor.KeyFrameWidth);
				_editor.TimelineFrameIndex = frameIndex;
				e.Handled = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void _updateTimelineLabels() {
			try {
				var str = _controller.Str;

				_timelineIndicatorColumn.Width = new GridLength(_editor.KeyFrameWidth * str.KeyFrameCount + Math.Max(120, SystemParameters.PrimaryScreenWidth), GridUnitType.Pixel);
				_gridFrameIndexSelector.Width = _editor.KeyFrameWidth * str.KeyFrameCount + 120;

				List<int> indicators = new List<int>();
				indicators.Add(0);

				int last = 0;

				for (int i = 1; i < str.KeyFrameCount; i++) {
					if (i % 5 == 0) {
						indicators.Add(i);
						last = i;
					}
				}

				int min = (int)((SystemParameters.PrimaryScreenWidth + 30) / _editor.KeyFrameWidth);

				for (int i = 1; i <= min; i++) {
					if (i % 5 == 0)
						indicators.Add(last + i);
				}

				_renderer.Set(indicators);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public double GetSelectorPosition() {
			return _timelineSelector.Margin.Left + 1;
		}
	}
}
