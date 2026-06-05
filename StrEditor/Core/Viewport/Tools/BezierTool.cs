using System.Windows.Forms;
using System.Windows.Input;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.Core.Viewport.Renderers;

namespace StrEditor.Core.Viewport.Tools {
	public class BezierTool : EditTool {
		private bool _hasBeenModified;

		public override void BeginEvent(FrameViewer viewport, LayerRenderer renderer) {
			base.BeginEvent(viewport, renderer);

			_hasBeenModified = false;
		}

		public override bool EventController(FrameViewer viewport, FrameViewerEventArgs args) {
			switch (args.MouseEventState) {
				case MouseEventState.MouseDown:
					if (args.MouseArgs.Button != MouseButtons.Left)
						return false;

					_hasBeenModified = false;
					var renderer = viewport.GetSelectedRenderer();

					if (renderer == null)
						return false;

					viewport.MouseEventCapture();
					BeginEvent(viewport, renderer);
					return true;
				case MouseEventState.MouseMove:
					DoEvent(args);
					return true;
				case MouseEventState.MouseUp:
					if (_hasBeenModified) {
						End();
					}

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

			_hasBeenModified = true;
			
			DoEventRaw(args.DeltaX, args.DeltaY, args.PointId, (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control);
			_viewport.Controller.KeyFrameEditor.Execute(kfs => {
				kfs.SetBezier(_renderer.Inter.Bezier);
			});
			_viewport.QuickUpdate();
			return;
		}

		public void DoEventRaw(double deltaX, double deltaY, int point, bool both) {
			if (_keyFrameCopy == null) return;

			float diffX = (float)(deltaX / _viewport.ZoomEngine.Scale);
			float diffY = (float)(deltaY / _viewport.ZoomEngine.Scale);
			TkVector2 vertex = new TkVector2(diffX, diffY);
			var inter = _renderer.Inter;

			if (both) {
				var v0 = new TkVector2(_keyFrameCopy.Bezier[2 * point], _keyFrameCopy.Bezier[2 * point + 1]);
				var v1 = v0 + vertex;

				var angle = TkVector2.CalculateAngle(v0, v1);
				var sign = TkVector2.CalculateSignedAngle(v0, v1);

				inter.Bezier[2 * point] = _keyFrameCopy.Bezier[2 * point] + vertex.X;
				inter.Bezier[2 * point + 1] = _keyFrameCopy.Bezier[2 * point + 1] + vertex.Y;

				if (double.IsNaN(angle))
					return;

				point = (point + 1) % 2;

				v0 = new TkVector2(_keyFrameCopy.Bezier[2 * point], _keyFrameCopy.Bezier[2 * point + 1]);
				vertex = v0;

				vertex.RotateZ((float)MathHelper.RadiansToDegrees(angle * (sign < 0 ? 1 : -1)));
				inter.Bezier[2 * point] = vertex.X;
				inter.Bezier[2 * point + 1] = vertex.Y;
			}
			else {
				inter.Bezier[2 * point] = _keyFrameCopy.Bezier[2 * point] + vertex.X;
				inter.Bezier[2 * point + 1] = _keyFrameCopy.Bezier[2 * point + 1] + vertex.Y;
			}
		}

		public void End() {
			if (_keyFrameCopy == null) return;
			var inter = _renderer.Inter;

			InterpolatedKeyFrame.ConvertToFrame(inter, _str);
			_str.Commands.Begin();
			_str.Commands.SetBezier(_renderer.LayerIndex, inter.KeyIndex, inter.Bezier);
			_str.Commands.End();
		}
	}
}
