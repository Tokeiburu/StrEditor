using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport.Tools;
using System;
using System.Windows;
using System.Windows.Input;
using Utilities;

namespace StrEditor.Core.KeyframeEditor {
	public partial class KeyFrameEditor {
		private Point _clickedPoint;

		private void _initLabelEvents() {
			var im = _interactionManager;
			BindUIElementEdit(_labelAngle, im.LayerTransformTool, 0, (dx, dy) => im.LayerTransformTool.DoRotateRaw(-dy /*+ dx*/), im.LayerTransformTool.EndRotate);
			BindUIElementEdit(_labelP1, im.PointTranslateTool, 2, (dx, dy) => im.PointTranslateTool.DoEventRaw(0, _controller.FrameViewer, dx, dy), im.PointTranslateTool.End);
			BindUIElementEdit(_labelP2, im.PointTranslateTool, 1, (dx, dy) => im.PointTranslateTool.DoEventRaw(1, _controller.FrameViewer, dx, dy), im.PointTranslateTool.End);
			BindUIElementEdit(_labelP3, im.PointTranslateTool, 0, (dx, dy) => im.PointTranslateTool.DoEventRaw(2, _controller.FrameViewer, dx, dy), im.PointTranslateTool.End);
			BindUIElementEdit(_labelP4, im.PointTranslateTool, 3, (dx, dy) => im.PointTranslateTool.DoEventRaw(3, _controller.FrameViewer, dx, dy), im.PointTranslateTool.End);
			BindUIElementEdit(_labelOffset, im.LayerTransformTool, 0, (dx, dy) => im.LayerTransformTool.DoTranslateRaw(_controller.FrameViewer, dx, dy), im.LayerTransformTool.EndTranslate);
			BindUIElementEdit(_labelBezierP1, im.BezierTool, 0, (dx, dy) => im.BezierTool.DoEventRaw(dx, dy, 0, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control), im.BezierTool.End);
			BindUIElementEdit(_labelBezierP2, im.BezierTool, 1, (dx, dy) => im.BezierTool.DoEventRaw(dx, dy, 1, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control), im.BezierTool.End);
			BindUIElementEdit(_labelUvP1, im.UvTranslateTool, 0, (dx, dy) => im.UvTranslateTool.DoEventRaw(0, _controller.FrameViewer, dx, dy), im.UvTranslateTool.End);
			BindUIElementEdit(_labelUvP2, im.UvTranslateTool, 1, (dx, dy) => im.UvTranslateTool.DoEventRaw(1, _controller.FrameViewer, dx, dy), im.UvTranslateTool.End);
		}

		public void BindUIElementEdit(UIElement element, EditTool tool, int id, Action<float, float> update, Action end) {
			element.MouseLeftButtonDown += (s, e) => _label_MouseDown(s, e, tool, id);
			element.MouseMove += (s, e) => _label_MouseMove(s, e, update);
			element.MouseUp += (s, e) => _label_MouseUp(s, end);
		}

		private void _label_MouseMove(object sender, MouseEventArgs e, Action<float, float> layer) {
			if (_isLoading)
				return;

			if (_currentFrame == null)
				return;

			const int boundary = 50;
			UIElement element = sender as UIElement;

			if (element != null) {
				if (!element.IsMouseCaptured)
					return;

				DisableEvents();

				try {
					var current = e.GetPosition(element);
					var deltaY = current.Y - _clickedPoint.Y;
					var deltaX = current.X - _clickedPoint.X;

					if (current.X < -boundary) {
						var realPosition = Application.Current.MainWindow.PointToScreen(Mouse.GetPosition(Application.Current.MainWindow));
						_clickedPoint.X += boundary;
						NativeMethods.SetCursorPos((int)(realPosition.X + boundary), (int)realPosition.Y);
					}

					if (current.X > boundary) {
						var realPosition = Application.Current.MainWindow.PointToScreen(Mouse.GetPosition(Application.Current.MainWindow));
						_clickedPoint.X -= boundary;
						NativeMethods.SetCursorPos((int)(realPosition.X - boundary), (int)realPosition.Y);
					}

					if (current.Y < -boundary) {
						var realPosition = Application.Current.MainWindow.PointToScreen(Mouse.GetPosition(Application.Current.MainWindow));
						_clickedPoint.Y += boundary;
						NativeMethods.SetCursorPos((int)(realPosition.X + 0), (int)realPosition.Y + boundary);
					}

					if (current.Y > boundary) {
						var realPosition = Application.Current.MainWindow.PointToScreen(Mouse.GetPosition(Application.Current.MainWindow));
						_clickedPoint.Y -= boundary;
						NativeMethods.SetCursorPos((int)(realPosition.X + 0), (int)realPosition.Y - boundary);
					}

					layer((float)deltaX, (float)deltaY);
					_controller.FrameViewer.QuickUpdate();
				}
				finally {
					EnableEvents();
				}
			}
		}

		private void _label_MouseDown(object sender, MouseButtonEventArgs e, EditTool tool, int pointId) {
			_currentEditLayer = _controller.FrameViewer.GetSelectedRenderer();

			if (_currentFrame == null || _currentEditLayer == null)
				return;

			UIElement element = sender as UIElement;

			if (element != null) {
				_clickedPoint = e.GetPosition(element);
				_interactionManager.SetActiveTool(tool);
				_interactionManager.ActiveHandle = new ToolHandle(pointId);
				tool.BeginEvent(_controller.FrameViewer, _currentEditLayer);

				if (!element.IsMouseCaptured) {
					element.CaptureMouse();
					Mouse.OverrideCursor = Cursors.None;
				}
			}
		}

		private void _label_MouseUp(object sender, Action action) {
			if (_currentFrame == null)
				return;

			UIElement element = sender as UIElement;

			if (element != null && _currentEditLayer != null) {
				if (element.IsMouseCaptured) {
					action();
					element.ReleaseMouseCapture();
					_str.InvalidateVisualRedraw();
					Mouse.OverrideCursor = null;
				}

				_interactionManager.SetActiveTool(null);
			}
		}
	}
}
