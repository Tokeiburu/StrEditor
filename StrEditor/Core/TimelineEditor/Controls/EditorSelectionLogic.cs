using System;
using System.Windows;
using System.Windows.Input;
using ErrorManager;
using StrEditor.Core.TimelineEditor.State;

namespace StrEditor.Core.TimelineEditor.Controls {
	public partial class Editor {
		public bool _isWithinViewport(Point position) {
			int drawOffsetY = IsFirstLayerInvisible ? 1 : 0;
			int frameIndex = (int)(position.X / KeyFrameWidth);
			int layerIndex = (int)(position.Y / KeyFrameHeight);

			frameIndex = Math.Max(Math.Min(frameIndex, _controller.Str.KeyFrameCount), 0);

			var pointTL = new Point(frameIndex * KeyFrameWidth, layerIndex * KeyFrameHeight);
			var xAdjust = _svKeyFrames.ScrollableHeight > 0 ? SystemParameters.ScrollWidth : 0;
			var yAdjust = (_svKeyFrames.ScrollableWidth > 0 ? SystemParameters.ScrollHeight : 0) + _svKeyFrames.ActualHeight % KeyFrameHeight;

			if (_svKeyFrames.ScrollableWidth > 0) {
				if (pointTL.X + KeyFrameWidth > _svKeyFrames.HorizontalOffset + _svKeyFrames.ActualWidth - xAdjust) {
					return false;
				}
				else if (pointTL.X < _svKeyFrames.HorizontalOffset) {
					return false;
				}
			}

			if (_svKeyFrames.ScrollableHeight > 0) {
				if (pointTL.Y + KeyFrameHeight > _svKeyFrames.VerticalOffset + _svKeyFrames.ActualHeight - yAdjust) {
					var step = pointTL.Y - _svKeyFrames.ActualHeight + yAdjust - _svKeyFrames.VerticalOffset + 1 * KeyFrameHeight;
					return false;
				}
				else if (pointTL.Y < _svKeyFrames.VerticalOffset) {
					return false;
				}
			}

			return true;
		}

		private void _setFocus(int layerIndex, SelectionTarget selection) {
			try {
				int drawOffsetY = IsFirstLayerInvisible ? 1 : 0;
				int frameIndex = selection.Focus;

				frameIndex = Math.Max(Math.Min(frameIndex, _controller.Str.KeyFrameCount), 0);

				var pointTL = new Point(frameIndex * KeyFrameWidth, (layerIndex - drawOffsetY) * KeyFrameHeight);
				var xAdjust = _svKeyFrames.ScrollableHeight > 0 ? SystemParameters.ScrollWidth : 0;
				var yAdjust = _svKeyFrames.ScrollableWidth > 0 ? SystemParameters.ScrollHeight : 0;

				if (_svKeyFrames.ScrollableWidth > 0) {
					if (pointTL.X + KeyFrameWidth + selection.FocusMargin * KeyFrameWidth > _svKeyFrames.HorizontalOffset + _svKeyFrames.ActualWidth - xAdjust) {
						_svKeyFrames.ScrollToHorizontalOffset(Math.Ceiling((pointTL.X + selection.FocusMargin * KeyFrameWidth - _svKeyFrames.ActualWidth + xAdjust + 1 * KeyFrameWidth) / KeyFrameWidth) * KeyFrameWidth);
						_hasMouseMoveScrolled = true;
					}
					else if (pointTL.X - selection.FocusMargin * KeyFrameWidth < _svKeyFrames.HorizontalOffset) {
						_svKeyFrames.ScrollToHorizontalOffset(pointTL.X - selection.FocusMargin * KeyFrameWidth - 0 * KeyFrameWidth);
						_hasMouseMoveScrolled = true;
					}
				}

				if (_svKeyFrames.ScrollableHeight > 0) {
					if (pointTL.Y + KeyFrameHeight > _svKeyFrames.VerticalOffset + _svKeyFrames.ActualHeight - yAdjust) {
						var step = pointTL.Y - _svKeyFrames.ActualHeight + yAdjust - _svKeyFrames.VerticalOffset + 1 * KeyFrameHeight;
						if (step < KeyFrameHeight)
							step = KeyFrameHeight;
						_svKeyFrames.ScrollToVerticalOffset(_svKeyFrames.VerticalOffset + step);
						_hasMouseMoveScrolled = true;
					}
					else if (pointTL.Y < _svKeyFrames.VerticalOffset) {
						_svKeyFrames.ScrollToVerticalOffset(pointTL.Y - 0 * KeyFrameHeight);
						_hasMouseMoveScrolled = true;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void FocusPosition(int frameIndex, int layerIndex, int focusMargin = 0) {
			_setFocus(layerIndex, new SelectionTarget { Focus = frameIndex, FocusMargin = focusMargin });
		}

		public void FocusSelection(int focusMargin = 0) {
			_setFocus(Selection.Current.Y, new SelectionTarget { Focus = Selection.Current.X, FocusMargin = focusMargin });
		}
	}
}
