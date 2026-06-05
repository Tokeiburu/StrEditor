using System.Drawing;
using System.Windows.Forms;

namespace StrEditor.Core.Viewport.Tools {
	public class ViewPanTool : EditTool {
		private Point _startPosition;

		public override bool EventController(FrameViewer viewport, FrameViewerEventArgs args) {
			switch (args.MouseEventState) {
				case MouseEventState.MouseDown:
					if (args.MouseArgs.Button != MouseButtons.Right)
						return false;

					_startPosition = args.MouseArgs.Location;
					_viewport = viewport;
					return true;
				case MouseEventState.MouseMove:
					DoEvent(args);
					return true;
				case MouseEventState.MouseUp:
					return true;
			}

			return false;
		}

		public void DoEvent(FrameViewerEventArgs args) {
			if (args.MouseArgs.Button != MouseButtons.Right)
				return;

			var delta = new Point(args.MouseArgs.Location.X - _startPosition.X, args.MouseArgs.Location.Y - _startPosition.Y);

			if (delta.X == 0 && delta.Y == 0)
				return;

			_viewport.RelativeCenter = new System.Windows.Point(
				_viewport.RelativeCenter.X + (double)delta.X / _viewport._primary.Width,
				_viewport.RelativeCenter.Y + (double)delta.Y / _viewport._primary.Height
			);

			_startPosition = args.MouseArgs.Location;
			_viewport.QuickUpdate();
			return;
		}
	}
}
