using StrEditor.Core.Viewport.Tools;
using System.Windows.Forms;

namespace StrEditor.Core.Viewport {
	public class SelectNodeTool : EditTool {
		public override bool EventController(FrameViewer viewport, FrameViewerEventArgs args) {
			switch (args.MouseEventState) {
				case MouseEventState.MouseDown:
					if (args.MouseArgs.Button != MouseButtons.Left)
						return false;

					viewport.MouseEventCapture();
					viewport.Controller.TimelineEditor.Selection.SetXY(args.PointId, viewport.Controller.TimelineEditor.SelectedLayerIndex);
					return true;
				case MouseEventState.MouseMove:
					return true;
				case MouseEventState.MouseUp:
					viewport.MouseEventRelease();
					return true;
			}

			return false;
		}
	}
}
