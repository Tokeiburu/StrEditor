using System.Drawing;
using System.Windows.Forms;

namespace StrEditor.Core.Viewport.Tools {
	public delegate bool FrameViewerEventHandler(FrameViewer viewport, FrameViewerEventArgs args);

	public enum MouseEventState {
		MouseMove,
		MouseDown,
		MouseUp
	}

	public class FrameViewerEventArgs {
		public MouseEventArgs MouseArgs;
		public int PointId;
		public MouseEventState MouseEventState;
		public int DeltaX;
		public int DeltaY;
		public Point Start;
		public object Data;
		public bool HasMoved => DeltaX != 0 || DeltaY != 0;

		public static FrameViewerEventArgs Create(MouseEventArgs e, MouseEventState state) {
			var args = new FrameViewerEventArgs();
			args.MouseArgs = e;
			args.MouseEventState = state;
			return args;
		}
	}
}
