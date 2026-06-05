using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using System;
using System.Windows;
using System.Windows.Forms;

namespace StrEditor.Core.Viewport.Tools {
	public partial class LayerTransformTool {
		public void DoRotate(FrameViewer viewport, FrameViewerEventArgs args) {
			if (args.MouseArgs.Button != MouseButtons.Left)
				return;

			if (!args.HasMoved)
				return;

			_hasRotated = true;
			DoRotateRaw(viewport, args.Start, args.DeltaX, args.DeltaY);

			try {
				viewport.Controller.KeyFrameEditor.DisableEvents();
				viewport.Controller.KeyFrameEditor.SetAngle(_renderer.Inter.Angle);
			}
			finally {
				viewport.Controller.KeyFrameEditor.EnableEvents();
			}
		}

		public void DoRotateRaw(FrameViewer viewport, System.Drawing.Point start, double deltaX, double deltaY) {
			if (_keyFrameCopy == null) return;

			Point oldPositionVertex = new Point(start.X, start.Y);
			Point centerOfImage = new Point(viewport.CenterX + _renderer.Model[3, 0] * viewport.ZoomEngine.Scale, viewport.CenterY - _renderer.Model[3, 1] * viewport.ZoomEngine.Scale);
			TkVector2 pointReference = new TkVector2(1, 0);
			Point point1 = new Point(oldPositionVertex.X - centerOfImage.X, oldPositionVertex.Y - centerOfImage.Y);
			Point point2 = new Point(point1.X + deltaX, point1.Y + deltaY);

			point1.Y = -point1.Y;
			point2.Y = -point2.Y;

			double angle1 = TkVector2.CalculateAngle(new TkVector2(point1.X, point1.Y), pointReference);
			double angle2 = TkVector2.CalculateAngle(new TkVector2(point2.X, point2.Y), pointReference);
			var inter = _renderer.Inter;
			int layerIdx = _renderer.LayerIndex;

			if (point1.Y < 0) {
				angle1 = 2d * Math.PI - angle1;
			}

			if (point2.Y < 0) {
				angle2 = 2d * Math.PI - angle2;
			}

			float angle = (float)((angle2 - angle1) * 360d / (2d * Math.PI));

			_applyTransformRotate(inter, layerIdx, angle);
		}

		private void _applyTransformRotate(InterpolatedKeyFrame inter, int layerIdx, float angle) {
			inter.Angle = _keyFrameCopy.Angle;
			inter.Angle -= angle;

			if (StrEditorConfiguration.GroupEdit) {
				_applyBezierRotate(_keyFrameCopy.Bezier, inter.Bezier, angle);

				for (int index = 0; index < _str.Layers[layerIdx].KeyFrames.Count; index++) {
					var keyFrame = _layerCopy[index];
					TkVector2 v = keyFrame.Offset - inter.Offset;
					v.RotateZ(angle);

					_str.Layers[layerIdx].KeyFrames[index].Offset = inter.Offset + v;

					_applyBezierRotate(keyFrame.Bezier, _str.Layers[layerIdx].KeyFrames[index].Bezier, angle);

					_str.Layers[layerIdx].KeyFrames[index].Angle = keyFrame.Angle - angle;
				}
			}
		}

		private void _applyBezierRotate(float[] bezierSrc, float[] bezierDst, float angle) {
			for (int i = 0; i < 4; i++)
				bezierDst[i] = bezierSrc[i];

			var v = new TkVector2(bezierDst[0], bezierDst[1]);
			v.RotateZ(angle);

			bezierDst[0] = v.X;
			bezierDst[1] = v.Y;

			v = new TkVector2(bezierDst[2], bezierDst[3]);
			v.RotateZ(angle);

			bezierDst[2] = v.X;
			bezierDst[3] = v.Y;
		}

		public void DoRotateRaw(float angle) {
			if (_keyFrameCopy == null) return;
			if (angle == 0)
				return;

			var inter = _renderer.Inter;
			int layerIdx = _renderer.LayerIndex;

			_applyTransformRotate(inter, layerIdx, angle);
		}

		public void EndRotate() {
			if (_keyFrameCopy == null) return;
			var inter = _renderer.Inter;
			var layerIdx = _renderer.LayerIndex;

			float rotation = inter.Angle;
			inter.Angle = _keyFrameCopy.Angle;

			float angle = rotation - _keyFrameCopy.Angle;

			if (angle > 180) {
				rotation -= 360;
			}

			if (StrEditorConfiguration.GroupEdit) {
				// Restore layer
				_str.Layers[layerIdx] = new StrLayer(_layerCopy);

				// Apply transformation
				_str.Commands.Begin();

				for (int index = 0; index < _str.Layers[layerIdx].KeyFrames.Count; index++) {
					var keyFrame = _layerCopy[index];
					TkVector2 v = keyFrame.Offset - inter.Offset;
					v.RotateZ(-angle);

					var offset = inter.Offset + v;
					_str.Commands.SetOffset(layerIdx, index, offset.X, offset.Y);

					v = new TkVector2(keyFrame.Bezier[0], keyFrame.Bezier[1]);
					v.RotateZ(-angle);

					var v2 = new TkVector2(keyFrame.Bezier[2], keyFrame.Bezier[3]);
					v2.RotateZ(-angle);

					_str.Commands.SetBezier(layerIdx, index, new float[] {
						v.X,
						v.Y,
						v2.X,
						v2.Y
					});

					_str.Commands.SetAngle(layerIdx, index, keyFrame.Angle + angle);
				}

				if (inter.Interpolated) {
					InterpolatedKeyFrame.ConvertToFrame(inter, _str);
					_str.Commands.SetAngle(layerIdx, inter.KeyIndex, rotation);
				}

				_str.Commands.End();
			}
			else {
				InterpolatedKeyFrame.ConvertToFrame(inter, _str);
				_str.Commands.Begin();
				_str.Commands.SetAngle(layerIdx, inter.KeyIndex, rotation);
				_str.Commands.End();
			}
		}
	}
}
