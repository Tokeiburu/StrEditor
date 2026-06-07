using System;
using System.Windows.Forms;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.Viewport.Renderers;

namespace StrEditor.Core.Viewport.Tools {
	public class OriginTool : EditTool {
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
						End(viewport, args);
					}

					viewport.Update();
					viewport.MouseEventRelease();
					return true;
			}

			return false;
		}

		public void DoEvent(FrameViewer viewport, FrameViewerEventArgs args) {
			var e = args.MouseArgs;

			if (e.Button != MouseButtons.Left)
				return;

			if (!args.HasMoved)
				return;

			_hasBeenModified = true;

			DoEventRaw(viewport, args.DeltaX, args.DeltaY);
			viewport.QuickUpdate();
			return;
		}

		public void DoEventRaw(FrameViewer viewport, double deltaX, double deltaY) {
			if (_keyFrameCopy == null) return;

			float diffX = (float)(deltaX / viewport.ZoomEngine.Scale);
			float diffY = (float)(deltaY / viewport.ZoomEngine.Scale);
			var inter = _renderer.Inter;

			if (StrEditorConfiguration.Snap > 0) {
				diffX = (float)(StrEditorConfiguration.Snap * Math.Round(diffX / StrEditorConfiguration.Snap, 0, MidpointRounding.ToEven));
				diffY = (float)(StrEditorConfiguration.Snap * Math.Round(diffY / StrEditorConfiguration.Snap, 0, MidpointRounding.ToEven));
			}

			inter.Offset.X = _keyFrameCopy.Offset.X + diffX;
			inter.Offset.Y = _keyFrameCopy.Offset.Y + diffY;

			TkVector2 vertex = new TkVector2(diffX, diffY);
			vertex.RotateZ(inter.Angle);

			for (int i = 0; i < 4; i++) {
				inter.Positions[i] = _keyFrameCopy.Positions[i] - vertex.X;
				inter.Positions[i + 4] = _keyFrameCopy.Positions[i + 4] - vertex.Y;
			}

			_kfe.OnValueChanged(KeyFrameValueType.OffsetXY, KeyFrameValueType.Points);
		}

		public void End(FrameViewer viewport, FrameViewerEventArgs args) {
			if (_keyFrameCopy == null) return;
			var inter = _renderer.Inter;

			InterpolatedKeyFrame.ConvertToFrame(inter, _str, false);
			_str.Commands.Begin();
			_str.Commands.SetPositions(_renderer.LayerIndex, inter.KeyIndex, inter.Positions);
			_str.Commands.SetOffset(_renderer.LayerIndex, inter.KeyIndex, inter.Offset.X, inter.Offset.Y);
			_str.Commands.End();
		}
	}
}
