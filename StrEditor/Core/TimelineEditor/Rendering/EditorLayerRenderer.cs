using GRF.FileFormats.StrFormat;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.TimelineEditor.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities;

namespace StrEditor.Core.TimelineEditor.Rendering {
	public class EditorLayerRenderer : FrameworkElement {
		private List<StrKeyFrame> _keyFrames;
		private Str _str;
		private StrLayer _layer;
		private Editor _editor;

		public EditorLayerRenderer(Editor editor) {
			_editor = editor;
		}

		public class DrawSegment {
			public int Length;
			public double Left;
			public StrKeyFrame Begin;
			public StrKeyFrame End;
			public bool Repeat;
		}

		protected override void OnRender(DrawingContext dc) {
			try {
				int previous = -2;
				RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

				StrKeyFrame previousFrame = null;
				List<DrawSegment> segments = new List<DrawSegment>();

				for (int keyIndex = 0; keyIndex < _keyFrames.Count; keyIndex++) {
					var frame = _keyFrames[keyIndex];

					if (frame.Type != 0)
						continue;

					_makeSegment(segments, frame, _layer[keyIndex + 1], previous, previousFrame);
					previous = frame.FrameIndex;
					previousFrame = frame;
				}

				dc.DrawRectangle(_timelineBackgroundStartBrush, null, new Rect(0, 0, _editor.KeyFrameWidth, _editor.KeyFrameHeight));
				dc.DrawRectangle(_timelineBackgroundBrush, null, new Rect(_editor.KeyFrameWidth, 0, _str.MaxKeyFrame * _editor.KeyFrameWidth, _editor.KeyFrameHeight));

				foreach (var segment in segments) {
					_drawKeyFrame(dc, segment);
				}
			}
			catch (Exception err) {
				Z.F(err);
			}
		}

		private void _makeSegment(List<DrawSegment> segments, StrKeyFrame begin, StrKeyFrame end, int previousFrameIndex, StrKeyFrame previous) {
			DrawSegment segment;

			if (end != null && begin.FrameIndex == end.FrameIndex - 1 &&
				!(Math.Abs(begin.BezierPositions[0]) > 0.01 || Math.Abs(begin.BezierPositions[1]) > 0.01 || Math.Abs(begin.BezierPositions[2]) > 0.01 || Math.Abs(begin.BezierPositions[3]) > 0.01)) {
				if (previousFrameIndex == begin.FrameIndex - 1 &&
					!(Math.Abs(previous.BezierPositions[0]) > 0.01 || Math.Abs(previous.BezierPositions[1]) > 0.01 || Math.Abs(previous.BezierPositions[2]) > 0.01 || Math.Abs(previous.BezierPositions[3]) > 0.01)) {
					segments[segments.Count - 1].Length++;
					return;
				}

				segment = new DrawSegment();
				segment.Repeat = true;
				segment.Length = 1;
				segment.Begin = begin;
				segment.End = end;
				segment.Left = _editor.KeyFrameWidth * begin.FrameIndex;
				segments.Add(segment);
				return;
			}

			var _length = (end == null ? _str.KeyFrameCount : end.FrameIndex) - begin.FrameIndex;

			if (end != null) {
				if (begin.FrameIndex + 1 == end.FrameIndex) {
					_length = 1;
				}
				else {
					if (StrEditorConfiguration.ShowNonInterpolated) {
						_length = end.FrameIndex - begin.FrameIndex;
					}
					else {
						if (begin.IsInterpolated) {
							_length = end.FrameIndex - begin.FrameIndex;
						}
						else {
							_length = 1;
						}
					}
				}
			}
			else {
				if (begin.IsInterpolated) {
					_length = _str.KeyFrameCount - begin.FrameIndex;
				}
				else {
					_length = 1;
				}
			}

			segment = new DrawSegment();
			segment.Length = _length;
			segment.Begin = begin;
			segment.End = end;
			segment.Left = _editor.KeyFrameWidth * begin.FrameIndex;

			segments.Add(segment);
		}

		private void _drawKeyFrame(DrawingContext dc, DrawSegment segment) {
			double left = segment.Left;
			double top = 0;
			double width = _editor.KeyFrameWidth * segment.Length;
			double height = _editor.KeyFrameHeight;
			var length = segment.Length;
			var begin = segment.Begin;
			var end = segment.End;

			if (width <= 0)
				return;

			if (segment.Repeat) {
				dc.DrawRectangle(_keyframeDotRepeatBrush, null, new Rect(left - 1, -1, _editor.KeyFrameWidth * segment.Length + 1, _editor.KeyFrameHeight + 1));
				dc.DrawLine(_keyframeBorderPen, new Point(left - 1, 0), new Point(left + _editor.KeyFrameWidth * segment.Length, 0));
				return;
			}

			List<Brush> backgroundColors = new List<Brush>();

			if (length > 1 && begin.IsInterpolated) {
				switch (begin.AnimationType) {
					case AnimationType.Stop:
						break;
					case AnimationType.Interpolation:
					case AnimationType.Once:
					case AnimationType.Loop:
					case AnimationType.ReverseLoop:
					case AnimationType.BiLoop:
						backgroundColors.Insert(0, StrEditorConfiguration.LayerEditorAnimationColorQuick.Get());
						break;
				}
			}
			
			if (Math.Abs(begin.BezierPositions[0]) > 0.01 || Math.Abs(begin.BezierPositions[1]) > 0.01 || Math.Abs(begin.BezierPositions[2]) > 0.01 || Math.Abs(begin.BezierPositions[3]) > 0.01) {
				backgroundColors.Insert(0, StrEditorConfiguration.LayerEditorBezierColorQuick.Get());
			}
			else if (begin.IsInterpolated && (begin.AngleBias != 0 || begin.ScaleBias != 0 || begin.OffsetBias != 0)) {
				backgroundColors.Insert(0, StrEditorConfiguration.LayerEditorEaseColorQuick.Get());
			}
			else if (end == null && length > 1) {
				backgroundColors.Clear();
				backgroundColors.Insert(0, StrEditorConfiguration.LayerEditorErrorColorQuick.Get());
			}
			
			var topAdjust = _editor.KeyFrameHeight - Editor.MaxKeyFrameHeight;

			dc.DrawRectangle(_keyframeBackgroundBrush, _keyframeBorderPen, new Rect(left, top, width, height));

			if (backgroundColors.Count > 0) {
				double bTotalHeight = _editor.KeyFrameHeight - 1;
				double bHeight = Math.Ceiling(bTotalHeight / backgroundColors.Count);
				double y = 0;

				for (int i = 0; i < backgroundColors.Count; i++) {
					double rHeight = Math.Min(bTotalHeight - y, y + bHeight);
					dc.DrawRectangle(backgroundColors[i], null, new Rect(left, y, _editor.KeyFrameWidth * length - 1, rHeight));
					y += bHeight;
				}
			}

			dc.DrawImage(_keyframeDotImage, new Rect(left, topAdjust, _keyframeDotImage.PixelWidth, _keyframeDotImage.PixelHeight));

			if (length > 1 && begin.IsInterpolated) {
				// Generate arrow
				if (length >= 2) // end of arrow
					dc.DrawRectangle(_brushArrowPart3, null, new Rect(left + (length - 1) * _editor.KeyFrameWidth, 0, _editor.KeyFrameWidth, _editor.KeyFrameHeight));

				if (length >= 3) // start of arrow
					dc.DrawRectangle(_brushArrowPart1, null, new Rect(left + _editor.KeyFrameWidth, 0, _editor.KeyFrameWidth, _editor.KeyFrameHeight));
				
				if (length >= 4)
					dc.DrawRectangle(_brushArrowPart2, null, new Rect(left + 2 * _editor.KeyFrameWidth, 0, (length - 3) * _editor.KeyFrameWidth, _editor.KeyFrameHeight));
			}

			if (_editor.KeyFrameHeight > 20 && length > 3) {
				if (begin.TextureIndex >= 0 && begin.TextureIndex < _layer.TextureNames.Count) {
					string text = Path.GetFileNameWithoutExtension(_layer.TextureNames[(int)begin.TextureIndex]);
					dc.PushClip(new RectangleGeometry(new Rect(left, 0, _editor.KeyFrameWidth * length - 2, _editor.KeyFrameHeight)));
					dc.DrawText(GetFormattedText(text), new Point(left + 1, 1));
					dc.Pop();
				}
			}
		}

		public FormattedText GetFormattedText(string text) {
			if (_cachedStrings.TryGetValue(text, out FormattedText formattedText))
				return formattedText;

			int fontSize = 12;

			if (_editor.KeyFrameHeight < 28) {
				fontSize = 10;
			}

			formattedText = new FormattedText(text,
				CultureInfo.CurrentUICulture,
				FlowDirection.LeftToRight,
				new Typeface(SystemFonts.MessageFontFamily,
							 FontStyles.Normal,
							 FontWeights.Normal,
							 FontStretches.Normal),
				fontSize,
				Brushes.Black,
				VisualTreeHelper.GetDpi(this).PixelsPerDip);

			_cachedStrings[text] = formattedText;
			return formattedText;
		}

		private static Dictionary<string, FormattedText> _cachedStrings = new Dictionary<string, FormattedText>();

		public static void PreRender(double height) {
			_cachedStrings.Clear();
			_keyframeBackgroundBrush = CreateKeyframeBrush("keyframe_c.png", height);
			_brushArrowPart1 = CreateKeyframeBrush("keyframe_arrow_p1.png", height);
			_brushArrowPart2 = CreateKeyframeBrush("keyframe_arrow_p2.png", height);
			_brushArrowPart3 = CreateKeyframeBrush("keyframe_arrow_p3.png", height);
			_timelineBackgroundStartBrush = CreateKeyframeBrush("keyframe_b.png", height);
			_timelineBackgroundBrush = _timelineBackgroundStartBrush.Clone();
			_timelineBackgroundBrush.Viewport = new Rect(10, height, 50, 30);
			_keyframeDotRepeatBrush = CreateKeyframeBrushRepeat("keyframe_s.png", height);
		}

		public void Set(Str str, StrLayer layer) {
			_str = str;
			_layer = layer;
			_keyFrames = layer.KeyFrames;
			InvalidateVisual();
		}

		private static Pen _keyframeBorderPen = CreateKeyframeBorderPen();
		private static BitmapImage _keyframeDotImage = CreateKeyframeDot();
		private static ImageBrush _keyframeBackgroundBrush;
		private static ImageBrush _brushArrowPart1;
		private static ImageBrush _brushArrowPart2;
		private static ImageBrush _brushArrowPart3;
		private static ImageBrush _keyframeDotRepeatBrush;
		private static ImageBrush _timelineBackgroundBrush;
		private static ImageBrush _timelineBackgroundStartBrush;

		private static Pen CreateKeyframeBorderPen() {
			Pen pen = new Pen(Brushes.Black, 1);
			pen.Freeze();
			return pen;
		}

		private static ImageBrush CreateKeyframeBrush(string name, double height) {
			var image = new BitmapImage(new Uri($"pack://application:,,,/Resources/{name}", UriKind.Absolute));
			image.Freeze();

			var brush = new ImageBrush(image) {
				TileMode = TileMode.Tile,
				Viewport = new Rect(0, height, image.PixelWidth, image.PixelHeight),
				ViewportUnits = BrushMappingMode.Absolute,
				Stretch = Stretch.Fill
			};

			brush.Freeze();
			return brush;
		}

		private static ImageBrush CreateKeyframeBrushRepeat(string name, double height) {
			var image = new BitmapImage(new Uri($"pack://application:,,,/Resources/{name}", UriKind.Absolute));
			image.Freeze();

			var brush = new ImageBrush(image) {
				TileMode = TileMode.Tile,
				Viewport = new Rect(-1, height - 1, image.PixelWidth, image.PixelHeight),
				ViewportUnits = BrushMappingMode.Absolute,
				Stretch = Stretch.Fill
			};

			brush.Freeze();
			return brush;
		}

		private static BitmapImage CreateKeyframeDot() {
			var r = new BitmapImage(new Uri("pack://application:,,,/Resources/keyframe_df.png", UriKind.Absolute));
			r.Freeze();
			return r;
		}
	}
}
