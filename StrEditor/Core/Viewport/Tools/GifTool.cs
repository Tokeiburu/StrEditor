using System.Windows.Forms;
using GRF.Graphics;
using StrEditor.Core.GifExporter;

namespace StrEditor.Core.Viewport.Tools {
	public class GifTool : EditTool {
		private GifData _gifData;

		public override bool EventController(FrameViewer viewport, FrameViewerEventArgs args) {
			switch (args.MouseEventState) {
				case MouseEventState.MouseDown:
					if (args.MouseArgs.Button != MouseButtons.Left)
						return false;

					_gifData = viewport.Controller.GifData;
					_gifData.PointsCopy.Clear();
					_gifData.PointsCopy.AddRange(_gifData.Points);
					viewport.MouseEventCapture();
					_viewport = viewport;
					return true;
				case MouseEventState.MouseMove:
					DoEvent(args);
					return true;
				case MouseEventState.MouseUp:
					viewport.Update();
					viewport.MouseEventRelease();
					return true;
			}

			return false;
		}

		public void DoEvent(FrameViewerEventArgs args) {
			if (args.MouseArgs.Button != MouseButtons.Left)
				return;

			if (!args.HasMoved)
				return;

			float diffX = (float)(args.DeltaX / _viewport.ZoomEngine.Scale);
			float diffY = -(float)(args.DeltaY / _viewport.ZoomEngine.Scale);

			switch (args.PointId) {
				case 0: // top left
					_gifData.Points[0] = new TkVector2(_gifData.PointsCopy[0].X + diffX, _gifData.PointsCopy[0].Y + diffY);
					_gifData.Points[2] = new TkVector2(_gifData.PointsCopy[2].X, _gifData.PointsCopy[2].Y + diffY);
					_gifData.Points[6] = new TkVector2(_gifData.PointsCopy[6].X + diffX, _gifData.PointsCopy[6].Y);
					break;
				case 1: // mid top
					_gifData.Points[0] = new TkVector2(_gifData.PointsCopy[0].X, _gifData.PointsCopy[0].Y + diffY);
					_gifData.Points[2] = new TkVector2(_gifData.PointsCopy[2].X, _gifData.PointsCopy[2].Y + diffY);
					break;
				case 2: // top right
					_gifData.Points[2] = new TkVector2(_gifData.PointsCopy[2].X + diffX, _gifData.PointsCopy[2].Y + diffY);
					_gifData.Points[4] = new TkVector2(_gifData.PointsCopy[4].X + diffX, _gifData.PointsCopy[4].Y);
					_gifData.Points[0] = new TkVector2(_gifData.PointsCopy[0].X, _gifData.PointsCopy[0].Y + diffY);
					break;
				case 3: // mid right
					_gifData.Points[2] = new TkVector2(_gifData.PointsCopy[2].X + diffX, _gifData.PointsCopy[2].Y);
					_gifData.Points[4] = new TkVector2(_gifData.PointsCopy[4].X + diffX, _gifData.PointsCopy[4].Y);
					break;
				case 4: // bottom right
					_gifData.Points[4] = new TkVector2(_gifData.PointsCopy[4].X + diffX, _gifData.PointsCopy[4].Y + diffY);
					_gifData.Points[2] = new TkVector2(_gifData.PointsCopy[2].X + diffX, _gifData.PointsCopy[2].Y);
					_gifData.Points[6] = new TkVector2(_gifData.PointsCopy[6].X, _gifData.PointsCopy[6].Y + diffY);
					break;
				case 5: // mid bottom
					_gifData.Points[4] = new TkVector2(_gifData.PointsCopy[4].X, _gifData.PointsCopy[4].Y + diffY);
					_gifData.Points[6] = new TkVector2(_gifData.PointsCopy[6].X, _gifData.PointsCopy[6].Y + diffY);
					break;
				case 6: // bottom left
					_gifData.Points[6] = new TkVector2(_gifData.PointsCopy[6].X + diffX, _gifData.PointsCopy[6].Y + diffY);
					_gifData.Points[4] = new TkVector2(_gifData.PointsCopy[4].X, _gifData.PointsCopy[4].Y + diffY);
					_gifData.Points[0] = new TkVector2(_gifData.PointsCopy[0].X + diffX, _gifData.PointsCopy[0].Y);
					break;
				case 7: // mid left
					_gifData.Points[6] = new TkVector2(_gifData.PointsCopy[6].X + diffX, _gifData.PointsCopy[6].Y);
					_gifData.Points[0] = new TkVector2(_gifData.PointsCopy[0].X + diffX, _gifData.PointsCopy[0].Y);
					break;
				case 8:
					_gifData.Points[0] = new TkVector2(_gifData.PointsCopy[0].X + diffX, _gifData.PointsCopy[0].Y + diffY);
					_gifData.Points[2] = new TkVector2(_gifData.PointsCopy[2].X + diffX, _gifData.PointsCopy[2].Y + diffY);
					_gifData.Points[4] = new TkVector2(_gifData.PointsCopy[4].X + diffX, _gifData.PointsCopy[4].Y + diffY);
					_gifData.Points[6] = new TkVector2(_gifData.PointsCopy[6].X + diffX, _gifData.PointsCopy[6].Y + diffY);
					break;
			}

			_gifData.Points[1] = new TkVector2((_gifData.Points[0].X + _gifData.Points[2].X) / 2, _gifData.Points[0].Y);
			_gifData.Points[3] = new TkVector2(_gifData.Points[2].X, (_gifData.Points[2].Y + _gifData.Points[4].Y) / 2);
			_gifData.Points[5] = new TkVector2((_gifData.Points[0].X + _gifData.Points[2].X) / 2, _gifData.Points[4].Y);
			_gifData.Points[7] = new TkVector2(_gifData.Points[0].X, (_gifData.Points[0].Y + _gifData.Points[6].Y) / 2);

			_gifData.OnPointsChanged();
			_viewport.QuickUpdate();
		}
	}
}
