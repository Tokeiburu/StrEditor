using System;
using System.Windows.Forms;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport.Tools;

namespace StrEditor.Core.Viewport {
	public class UvTranslateTool : EditTool {
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
					DoEvent(viewport, args);
					return true;
				case MouseEventState.MouseUp:
					if (_hasBeenModified) {
						End();
					}

					viewport.MouseEventRelease();
					viewport.Update();
					return true;
			}

			return false;
		}

		private void DoEvent(FrameViewer viewport, FrameViewerEventArgs args) {
			if (args.MouseArgs.Button != MouseButtons.Left)
				return;

			if (!args.HasMoved)
				return;

			_hasBeenModified = true;

			DoEventRaw(args.PointId, viewport, args.DeltaX, args.DeltaY);

			//try {
			//	viewport.Controller.KeyFrameEditor.DisableEvents();
			//	viewport.Controller.KeyFrameEditor.SetUVs(_renderer.Inter.UVs);
			//}
			//finally {
			//	viewport.Controller.KeyFrameEditor.EnableEvents();
			//}

			viewport.QuickUpdate();
		}

		public void DoEventRaw(int point, FrameViewer viewport, double deltaX, double deltaY) {
			if (_keyFrameCopy == null) return;

			if (deltaX == 0 && deltaY == 0)
				return;

			// Estimate layer dimensions
			var topLeft = _keyFrameCopy.GetXYVector(PointId.TopLeft);
			var bottomRight = _keyFrameCopy.GetXYVector(PointId.BottomRight);

			double width = bottomRight.X - topLeft.X;
			double height = bottomRight.Y - topLeft.Y;

			if (width == 0)
				width = 64f;
			if (height == 0)
				height = 64f;

			deltaX /= width;
			deltaY /= height;

			float diffX = -(float)(deltaX / viewport.ZoomEngine.Scale);
			float diffY = -(float)(deltaY / viewport.ZoomEngine.Scale);
			TkVector2 vertex = new TkVector2(diffX, diffY);
			vertex.RotateZ(_renderer.Inter.Angle);

			_renderer.Inter.UVs[2 * point + 0] = _keyFrameCopy.UVs[2 * point + 0] + vertex.X;
			_renderer.Inter.UVs[2 * point + 1] = _keyFrameCopy.UVs[2 * point + 1] + vertex.Y;

			if (StrEditorConfiguration.Snap > 0) {
				var snap = 1d / StrEditorConfiguration.Snap * 0.1d;
				_renderer.Inter.UVs[2 * point + 0] = (float)(snap * Math.Round(_renderer.Inter.UVs[2 * point + 0] / snap, 0, MidpointRounding.ToEven));
				_renderer.Inter.UVs[2 * point + 1] = (float)(snap * Math.Round(_renderer.Inter.UVs[2 * point + 1] / snap, 0, MidpointRounding.ToEven));
			}

			_kfe.OnValueChanged(KeyFrameValueType.UVs);
		}

		public void End() {
			if (_keyFrameCopy == null) return;

			InterpolatedKeyFrame.ConvertToFrame(_renderer.Inter, _str);
			_str.Commands.Begin();
			_str.Commands.SetUVs(_renderer.LayerIndex, _renderer.Inter.KeyIndex, _renderer.Inter.UVs);
			_str.Commands.End();
		}
	}
}
