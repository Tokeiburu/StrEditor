using System;
using System.Windows.Forms;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport.Tools;

namespace StrEditor.Core.Viewport {
	public class PointTranslateTool : EditTool {
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

			int point = args.PointId;

			if (point == 0)
				point = 2;
			else if (point == 2)
				point = 0;

			DoEventRaw(point, viewport, args.DeltaX, args.DeltaY);

			//try {
			//	viewport.Controller.KeyFrameEditor.DisableEvents();
			//	viewport.Controller.KeyFrameEditor.SetPositions(_renderer.Inter.Positions);
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

			float diffX = (float)(deltaX / viewport.ZoomEngine.Scale);
			float diffY = (float)(deltaY / viewport.ZoomEngine.Scale);
			TkVector2 vertex = new TkVector2(diffX, diffY);
			vertex.RotateZ(_renderer.Inter.Angle);

			_renderer.Inter.Positions[point] = _keyFrameCopy.Positions[point] + vertex.X;
			_renderer.Inter.Positions[point + 4] = _keyFrameCopy.Positions[point + 4] + vertex.Y;

			if (StrEditorConfiguration.Snap > 0) {
				_renderer.Inter.Positions[point] = (float)(StrEditorConfiguration.Snap * Math.Round(_renderer.Inter.Positions[point] / StrEditorConfiguration.Snap, 0, MidpointRounding.ToEven));
				_renderer.Inter.Positions[point + 4] = (float)(StrEditorConfiguration.Snap * Math.Round(_renderer.Inter.Positions[point + 4] / StrEditorConfiguration.Snap, 0, MidpointRounding.ToEven));
			}

			_kfe.OnValueChanged(KeyFrameValueType.P1 + point);
		}

		public void DoEventRaw(FrameViewer viewport, double deltaX, double deltaY, bool applyScale = true) {
			if (_keyFrameCopy == null) return;

			if (deltaX == 0 && deltaY == 0)
				return;

			float diffX = (float)(applyScale ? deltaX / viewport.ZoomEngine.Scale : deltaX);
			float diffY = (float)(applyScale ? deltaY / viewport.ZoomEngine.Scale : deltaY);
			TkVector2 vertex = new TkVector2(diffX, diffY);
			vertex.RotateZ(_renderer.Inter.Angle);

			for (int i = 0; i < 4; i++) {
				_renderer.Inter.Positions[i + 0] = _keyFrameCopy.Positions[i + 0] + vertex.X;
				_renderer.Inter.Positions[i + 4] = _keyFrameCopy.Positions[i + 4] + vertex.Y;
			}

			_kfe.OnValueChanged(KeyFrameValueType.Points);
		}

		public void End() {
			if (_keyFrameCopy == null) return;

			InterpolatedKeyFrame.ConvertToFrame(_renderer.Inter, _str);
			_str.Commands.Begin();
			_str.Commands.SetPositions(_renderer.LayerIndex, _renderer.Inter.KeyIndex, _renderer.Inter.Positions);
			_str.Commands.End();
		}
	}
}
