using System;
using System.Windows;
using System.Windows.Input;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GrfToWpfBridge;

namespace StrEditor.Core.Viewport.Tools {
	public partial class LayerTransformTool {
		public void UpdateKeyFrameScalingData(FrameViewer viewport) {
			if (_keyFrameCopy == null) return;

			var oriKeyFrame = _keyFrameCopy;
			var newKeyFrame = _renderer.Inter;

			float[] vertices = new float[8];
			float x = 0;
			float y = 0;
			TkVector2[] points = new TkVector2[4];

			for (int i = 0; i < 4; i++) {
				x += oriKeyFrame.Vertices[i];
				y += oriKeyFrame.Vertices[i + 4];

				points[i] = new TkVector2(oriKeyFrame.Vertices[i], oriKeyFrame.Vertices[i + 4]);
			}

			x /= 4;
			y /= 4;

			TkVector2 m = new TkVector2(x, y);

			for (int i = 0; i < 4; i++) {
				TkVector2 p = (points[i] - m);
				p.X *= newKeyFrame.Scale.X;
				p.Y *= newKeyFrame.Scale.Y;

				p += m;

				vertices[i] = p.X;
				vertices[i + 4] = p.Y;
			}

			viewport.Controller.KeyFrameEditor.Execute(kfs => {
				kfs.SetVertices(vertices);
				//kfs.UpdateScale(vertices);
			});
		}

		public void DoScale(FrameViewer viewport, FrameViewerEventArgs args) {
			Point current = new Point(args.MouseArgs.Location.X, args.MouseArgs.Y);
			Point oldPositionVertex = new Point(args.Start.X, args.Start.Y);

			double deltaX = args.DeltaX;
			double deltaY = args.DeltaY;

			if (deltaX == 0 && deltaY == 0)
				return;

			if (_favoriteOrientation == null)
				_favoriteOrientation = deltaX * deltaX > deltaY * deltaY ? ScaleDirection.Horizontal : ScaleDirection.Vertical;

			var inter = _renderer.Inter;
			_hasScaled = true;

			// Find center of layer
			TkVector2 center = new TkVector2();

			for (int i = 0; i < 4; i++) {
				center.X += inter.Vertices[i];
				center.Y += inter.Vertices[i + 4];
			}

			center /= 4;
			center.RotateZ(-inter.Angle);

			center.X += inter.Offset.X - Str.OffsetX;
			center.Y += (inter.Offset.Y - (Str.OffsetY - 1));

			center.X = (float)(center.X * viewport.ZoomEngine.Scale + viewport.CenterX);
			center.Y = (float)(center.Y * viewport.ZoomEngine.Scale + viewport.CenterY);

			TkVector2 diffVector = new TkVector2(oldPositionVertex.X, oldPositionVertex.Y) - center;

			if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) {
				if (_favoriteOrientation != null) {
					if (_favoriteOrientation.Value == ScaleDirection.Horizontal)
						deltaY = 0;
					else if (_favoriteOrientation.Value == ScaleDirection.Vertical)
						deltaX = 0;
				}

				DoScaleRaw(viewport, diffVector, deltaX, deltaY);
			}
			else if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) {
				double scale = (current.ToTkVector2() - new TkVector2(center.X, center.Y)).Length / (oldPositionVertex.ToTkVector2() - new TkVector2(center.X, center.Y)).Length;
				DoScaleRaw(Math.Pow(scale, 1.2d));
			}
			else {
				_favoriteOrientation = null;
				DoScaleRaw(viewport, diffVector, deltaX, deltaY);
			}
		}

		public void DoScaleRaw(double scale) {
			if (_keyFrameCopy == null) return;

			var inter = _renderer.Inter;
			double scaleX;
			double scaleY;

			TkVector2 a = new TkVector2(inter.Vertices[2], -inter.Vertices[6]);
			TkVector2 b = new TkVector2(inter.Vertices[1], -inter.Vertices[5]);
			TkVector2 c = new TkVector2(inter.Vertices[0], -inter.Vertices[4]);
			TkVector2 d = new TkVector2(inter.Vertices[3], -inter.Vertices[7]);

			double width = ((a + b) / 2 - (c + d) / 2).Length;
			double height = ((c + b) / 2 - (a + d) / 2).Length;

			if (width == 0 || height == 0) {
				scaleX = 0;
				scaleY = 0;
			}
			else {
				scaleX = _keyFrameCopy.Scale.X * scale;
				scaleY = _keyFrameCopy.Scale.Y * scale;
			}

			inter.Scale.X = (float)scaleX;
			inter.Scale.Y = (float)scaleY;
		}

		public void DoScaleRaw(FrameViewer viewport, TkVector2 diffVector, double deltaX, double deltaY) {
			if (_keyFrameCopy == null) return;

			var inter = _renderer.Inter;

			if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift) {
				TkVector2 click = diffVector;
				TkVector2 dest = new TkVector2((float)(click.X + deltaX), (float)(click.Y + deltaY));

				click.RotateZ(inter.Angle);
				dest.RotateZ(inter.Angle);

				inter.Scale.X = _keyFrameCopy.Scale.X * (dest.X / click.X);
				inter.Scale.Y = _keyFrameCopy.Scale.Y * (dest.Y / click.Y);

				return;
			}

			double diffX = deltaX * 2d / viewport.ZoomEngine.Scale;
			double diffY = deltaY * 2d / viewport.ZoomEngine.Scale;

			double scaleX;
			double scaleY;

			TkVector2 a = new TkVector2(inter.Vertices[2], -inter.Vertices[6]);
			TkVector2 b = new TkVector2(inter.Vertices[1], -inter.Vertices[5]);
			TkVector2 c = new TkVector2(inter.Vertices[0], -inter.Vertices[4]);
			TkVector2 d = new TkVector2(inter.Vertices[3], -inter.Vertices[7]);

			double width = ((a + b) / 2 - (c + d) / 2).Length;
			double height = ((c + b) / 2 - (a + d) / 2).Length;

			if (width == 0 || height == 0) {
				scaleX = 0;
				scaleY = 0;
			}
			else {
				// We have to add diffX pixels to the image, which is... a simple ratio
				if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) {
					double scale = Math.Max(width, height);
					scale = (scale + diffX) / scale;

					scaleX = _keyFrameCopy.Scale.X * scale;
					scaleY = _keyFrameCopy.Scale.Y * scale;
				}
				else {
					scaleX = _keyFrameCopy.Scale.X * (width + diffX) / width;
					scaleY = _keyFrameCopy.Scale.Y * (height + diffY) / height;
				}
			}

			inter.Scale.X = (float)scaleX;
			inter.Scale.Y = (float)scaleY;
		}

		public void EndScale() {
			if (_keyFrameCopy == null) return;

			var inter = _renderer.Inter;
			float scaleX = inter.Scale.X;
			float scaleY = inter.Scale.Y;

			// Restore original settings
			inter.Scale.X = _keyFrameCopy.Scale.X;
			inter.Scale.Y = _keyFrameCopy.Scale.Y;

			InterpolatedKeyFrame.ConvertToFrame(inter, _str);
			_str.Commands.Begin();
			_str.Commands.ScaleCenter(_renderer.LayerIndex, inter.KeyIndex, scaleX, scaleY);
			_str.Commands.End();

			_viewport.Controller.KeyFrameEditor.InvalidateKeyFrame();
		}
	}
}