using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.Image;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using StrEditor.Core.TimelineEditor.Controls;
using StrEditor.Core.GifExporter;

namespace StrEditor.Core.Viewport.Renderers {
	public class GizmoRenderer : Renderer {
		private InteractionManager _im;
		private Str _str;
		private Editor _kfe;
		private GifData _gifData;

		public override void Load(FrameViewer viewport) {
			IsLoaded = true;
		}

		public override void Render(FrameViewer viewport) {
			if (!IsLoaded) {
				Load(viewport);
			}

			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			_im = viewport.InteractionManager;
			_str = viewport.Controller.Str;
			_kfe = viewport.Controller.TimelineEditor;
			_gifData = viewport.Controller.GifData;

			_im.Clear();
			_im.Register(_im.ViewportMoveTool);
			_im.Register(_im.LayerTransformTool);

			if (_gifData.IsSaving) {
				// Don't show anything!
			}
			else if (_gifData.IsGifMode || _gifData.IsPngMode) {
				_drawGifData();
			}
			else {
				var selectedLayer = viewport.GetSelectedRenderer();

				if (selectedLayer != null && selectedLayer.Inter != null) {
					selectedLayer.DrawSelection();

					if (StrEditorConfiguration.DrawTranslationPoints)
						_drawPointTranslation(selectedLayer);

					if (StrEditorConfiguration.ShowPathing && _kfe.SelectedLayerIndex > -1 && selectedLayer.Inter.KeyIndex > -1)
						_drawPathing(selectedLayer);

					if (StrEditorConfiguration.ShowBezier && _kfe.SelectedLayerIndex > -1 && _kfe.TimelineFrameIndex == _str[_kfe.SelectedLayerIndex, selectedLayer.Inter.KeyIndex].FrameIndex)
						_drawBezier(selectedLayer);
				}
			}
		}

		private void _drawPointTranslation(LayerRenderer component) {
			if (component.Inter == null)
				return;

			Vector4 color = StrEditorConfiguration.StrEditorVertexColorQuick;
			Vector4 pathColor = StrEditorConfiguration.StrEditorSpriteSelectionBorderQuick.ToVector4();
			var handle = _im.ActiveHandle;
			var tool = _im.ActiveTool;

			for (int i = 0; i < 4; i++) {
				TkVector2 p = new TkVector2(component.VertexData[5 * i + 0], component.VertexData[5 * i + 1]);
				p.RotateZ(component.Inter.Angle);
				p += new TkVector2(component.Model[3, 0], component.Model[3, 1]);

				if (handle != null && handle.Id == i && tool == _im.PointTranslateTool) {
					color = StrEditorConfiguration.StrEditorVertexSelectedColorQuick;
				}
				else {
					color = StrEditorConfiguration.StrEditorVertexColorQuick;
				}

				ShapeRenderer.DrawRectangle(p.X, p.Y, 7, pathColor);
				ShapeRenderer.DrawRectangle(p.X, p.Y, 5, color);
				_im.Register(p, 5, i, _im.PointTranslateTool);
			}
		}

		private void _drawBezier(LayerRenderer renderer) {
			var baseKeyIndex = renderer.Inter.KeyIndex;
			var keyFrame0 = _str[_kfe.SelectedLayerIndex, renderer.Inter.KeyIndex];

			if (_im.IsToolActive(_im.BezierTool, _im.OriginTool, _im.LayerTransformTool, _im.BiasTool)) {
				keyFrame0 = renderer.Inter.ToKeyFrame();
			}

			if (Math.Abs(keyFrame0.BezierPositions[0]) > 0.05 ||
				Math.Abs(keyFrame0.BezierPositions[1]) > 0.05 ||
				Math.Abs(keyFrame0.BezierPositions[2]) > 0.05 ||
				Math.Abs(keyFrame0.BezierPositions[3]) > 0.05) {
				List<Point> bezPoints = new List<Point>(3);
				bezPoints.Add(new Point());
				bezPoints.Add(new Point());
				bezPoints.Add(new Point());

				bezPoints[1] = new Point(keyFrame0.Offset.X - Str.OffsetX, -(keyFrame0.Offset.Y - Str.OffsetY));
				bezPoints[0] = new Point(bezPoints[1].X + keyFrame0.BezierPositions[0], bezPoints[1].Y - keyFrame0.BezierPositions[1]);
				bezPoints[2] = new Point(bezPoints[1].X + keyFrame0.BezierPositions[2], bezPoints[1].Y - keyFrame0.BezierPositions[3]);

				for (int i = 0; i < 2; i++) {
					ShapeRenderer.DrawLine(bezPoints[i], bezPoints[i + 1], StrEditorConfiguration.BezierLineQuick.Color);
				}

				for (int i = 0; i < 3; i++) {
					if (i == 1)
						continue;

					ShapeRenderer.DrawRectangle(bezPoints[i], 5, i == 0 ? StrEditorConfiguration.BezierNode1Quick.Color : StrEditorConfiguration.BezierNode2Quick.Color);
					_im.Register(bezPoints[i], 5, i == 0 ? 0 : 1, _im.BezierTool);
				}
			}
		}

		private void _drawPathing(LayerRenderer renderer) {
			var layer = _str[_kfe.SelectedLayerIndex];
			var keyFrame0 = layer.KeyFrames[0];
			var keyFrame1 = layer.KeyFrames.Last();
			List<Point> points = new List<Point>();

			if (keyFrame0.FrameIndex == keyFrame1.FrameIndex + 1)
				return;

			StrKeyFrame currentKeyFrame = _str[_kfe.SelectedLayerIndex, renderer.Inter.KeyIndex];

			if (_im.IsToolActive(_im.BezierTool, _im.OriginTool, _im.LayerTransformTool, _im.BiasTool)) {
				currentKeyFrame = renderer.Inter.ToKeyFrame();
			}

			for (int frameIndex = keyFrame0.FrameIndex; frameIndex <= keyFrame1.FrameIndex; frameIndex++) {
				var keyFrame = InterpolatedKeyFrame.InterpolateOffsetsOnly(_str, _kfe.SelectedLayerIndex, frameIndex, currentKeyFrame, false);

				if (keyFrame != null) {
					points.Add(new Point(keyFrame.Offset.X - Str.OffsetX, -(keyFrame.Offset.Y - Str.OffsetY)));
				}
			}

			for (int i = 0; i < points.Count - 1; i++) {
				ShapeRenderer.DrawLine(points[i], points[i + 1], StrEditorConfiguration.StrEditorPathLineColorQuick.Color);
			}

			for (int i = 0; i < points.Count; i++) {
				if (keyFrame0.FrameIndex + i == renderer.Inter.FrameIndex) {
					continue;
				}

				if (layer.FrameIndex2KeyIndex[keyFrame0.FrameIndex + i] != -1 && layer[layer.FrameIndex2KeyIndex[keyFrame0.FrameIndex + i]].FrameIndex == keyFrame0.FrameIndex + i) {
					ShapeRenderer.DrawRectangle(points[i], 5, StrEditorConfiguration.StrEditorSelectNodeColorQuick.Color);
					_im.Register(points[i], 5, keyFrame0.FrameIndex + i, _im.SelectNodeTool);
				}
				else {
					ShapeRenderer.DrawRectangle(points[i], 3, StrEditorConfiguration.StrEditorPathNodeColorQuick.Color);
				}
			}

			for (int i = 0; i < points.Count; i++) {
				if (keyFrame0.FrameIndex + i == renderer.Inter.FrameIndex) {
					ShapeRenderer.DrawRectangle(points[i], 5, StrEditorConfiguration.StrEditorPathNodeCurrentColorQuick.Color);
					_im.Register(points[i], 5, keyFrame0.FrameIndex + i, _im.OriginTool);
				}
			}
		}

		private void _drawGifData() {
			Vector4 lineColor = new Vector4(1);
			Vector4 background = StrEditorConfiguration.GifBackgroundQuick.Get();
			var backgroundGrfColor = new GrfColor(background.W, background.X, background.Y, background.Z);
			var hsl = backgroundGrfColor.Hsl;

			if (hsl.Lightness > 0.5) {
				lineColor = new Vector4(0, 0, 0, 1);
			}

			for (int i = 0; i < 8; i++) {
				TkVector2 p0 = _gifData.Points[i];
				TkVector2 p1 = _gifData.Points[(i + 1) % 8];

				ShapeRenderer.DrawLine(new Point(p0.X, p0.Y), new Point(p1.X, p1.Y), lineColor);
			}

			var midPoint = new TkVector2();
			midPoint += _gifData.Points[0];
			midPoint += _gifData.Points[2];
			midPoint += _gifData.Points[4];
			midPoint += _gifData.Points[6];
			midPoint /= 4;

			float scaleX = Math.Abs((_gifData.Points[0].X - midPoint.X) * 2) - (5 * 4) / 2;
			float scaleY = Math.Abs((_gifData.Points[1].Y - midPoint.Y) * 2) - (5 * 4) / 2;

			if (scaleX > 0 && scaleY > 0) {
				Rect rectMid = new Rect(
					new Point(midPoint.X - scaleX / 2f, midPoint.Y + scaleY / 2f),
					new Point(midPoint.X + scaleX / 2f, midPoint.Y - scaleY / 2f));

				_im.Register(rectMid, 8, _im.GifTool);
			}

			for (int i = 0; i < 8; i++) {
				TkVector2 p0 = _gifData.Points[i];
				ShapeRenderer.DrawRectangle(p0, 5, StrEditorConfiguration.StrEditorSpriteSelectionBorderQuick);
				_im.Register(p0, 5, i, _im.GifTool);
			}
		}

		public override void Unload() {
			IsUnloaded = true;
		}
	}
}
