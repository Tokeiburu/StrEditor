using ColorPicker;
using ColorPicker.Sliders;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using GRF.Image;
using GrfToWpfBridge;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport.Tools;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Utilities;

namespace StrEditor.Core.KeyframeEditor {
	public partial class KeyFrameEditor {
		public void ApplyCommand(Action<int, int> action, bool interpolateCheck = true, bool quickUpdate = false) {
			if (_isLoading || _fieldEditing)
				return;

			try {
				_str.Commands.Begin();

				if (interpolateCheck) {
					InterpolatedKeyFrame.ConvertToFrame(_currentFrame, _str);

					if (_currentFrame.Interpolated)
						return;
				}

				_fieldEditing = true;
				action(_currentFrame.LayerIdx, _currentFrame.KeyIndex);

				if (quickUpdate)
					_str.InvalidateVisual();
				else
					_str.InvalidateVisualRedraw();
			}
			catch {
			}
			finally {
				_str.Commands.End();
				_fieldEditing = false;
			}
		}

		private void _cbAnimations_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				int type = _cbAnimations.SelectedIndex;

				if (type < 0)
					type = 0;

				_str.Commands.ChangeAnimationType(lidx, kidx, type);
			});
		}

		private void _tbPosX_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => _str.Commands.SetOffset(_currentFrame.LayerIdx, _currentFrame.KeyIndex, FormatConverters.SingleConverter(_tbPosX.Text) + Str.OffsetX, FormatConverters.SingleConverter(_tbPosY.Text) + Str.OffsetY));
		}

		private void _tbPosY_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => _str.Commands.SetOffset(_currentFrame.LayerIdx, _currentFrame.KeyIndex, FormatConverters.SingleConverter(_tbPosX.Text) + Str.OffsetX, FormatConverters.SingleConverter(_tbPosY.Text) + Str.OffsetY));
		}

		private void _tbAngle_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => _str.Commands.SetAngle(_currentFrame.LayerIdx, _currentFrame.KeyIndex, FormatConverters.SingleConverter(_tbAngle.Text)));
		}

		private void _qcsKeyFrameColor_ColorChanged(object sender, Color color) {
			ApplyCommand((lidx, kidx) => {
				var layer = _str[lidx, kidx];
				layer.Color[0] = _qcsKeyFrameColor.InitialColor.R;
				layer.Color[1] = _qcsKeyFrameColor.InitialColor.G;
				layer.Color[2] = _qcsKeyFrameColor.InitialColor.B;
				layer.Color[3] = _qcsKeyFrameColor.InitialColor.A;
				_str.Commands.ChangeColor(_currentFrame.LayerIdx, _currentFrame.KeyIndex, _qcsKeyFrameColor.Color.ToGrfColor());
				_tbColorR.Text = _qcsKeyFrameColor.Color.R.ToString("0.##");
				_tbColorG.Text = _qcsKeyFrameColor.Color.G.ToString("0.##");
				_tbColorB.Text = _qcsKeyFrameColor.Color.B.ToString("0.##");
				_tbColorA.Text = _qcsKeyFrameColor.Color.A.ToString("0.##");
			});
		}

		private void _tbColor_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				float a = FormatConverters.SingleConverter(_tbColorA.Text);
				float r = FormatConverters.SingleConverter(_tbColorR.Text);
				float g = FormatConverters.SingleConverter(_tbColorG.Text);
				float b = FormatConverters.SingleConverter(_tbColorB.Text);
				_str.Commands.ChangeColor(_currentFrame.LayerIdx, _currentFrame.KeyIndex, a, r, g, b);
				_qcsKeyFrameColor.Color = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
			});
		}

		private void _cbSrc_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => _str.Commands.SetSrcBlend(_currentFrame.LayerIdx, _currentFrame.KeyIndex, _cbSrc.SelectedIndex + 1));
		}

		private void _cbDst_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => _str.Commands.SetDstBlend(_currentFrame.LayerIdx, _currentFrame.KeyIndex, _cbSrc.SelectedIndex + 1));
		}

		private void _boxPos_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				for (int i = 0; i < 8; i++) {
					if (sender == _tbVertices[i]) {
						_fieldEditing = true;

						_str.Commands.SetVertex(lidx, kidx, i, (i >= 4 ? -1 : 1) * FormatConverters.SingleConverter(_tbVertices[i].Text));
						break;
					}
				}
			});
		}

		private void _boxUVs_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				float[] vertices = new float[8];

				for (int i = 0; i < 4; i++)
					vertices[i] = FormatConverters.SingleConverter(_tbTextCoords[i].Text);

				vertices[4] = 0;
				vertices[5] = 0;
				vertices[6] = 1;
				vertices[7] = 1;

				_str.Commands.SetTextCoords(lidx, kidx, vertices);
			});
		}

		private void _boxBezier_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				float[] vertices = new float[4];

				for (int i = 0; i < 4; i++)
					vertices[i] = FormatConverters.SingleConverter(_tbBezier[i].Text);

				_str.Commands.SetBezier(lidx, kidx, vertices);
			});
		}

		private void _tbDelay_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				int v = FormatConverters.IntOrHexConverter(((TextBox)sender).Text);

				if (v < 0)
					return;

				_str.Commands.ChangeDelay(lidx, kidx, 1f / v);
			});
		}

		private void _sliderScale_ValueChanged(object sender, ValueEventArgs args) {
			ApplyCommand((lidx, kidx) => {
				int result = (int)(_sliderRange * args.Value);

				((SliderColor)sender).SetPosition((double)result / _sliderRange, true);
				_tbScaleBias.Text = (result - _sliderMid).ToString(CultureInfo.InvariantCulture);
				_str.Commands.SetScaleBias(lidx, kidx, result - _sliderMid);
			});
		}

		private void _sliderOffset_ValueChanged(object sender, ValueEventArgs args) {
			ApplyCommand((lidx, kidx) => {
				int result = (int)(_sliderRange * args.Value);

				((SliderColor)sender).SetPosition((double)result / _sliderRange, true);
				_tbOffsetBias.Text = (result - _sliderMid).ToString(CultureInfo.InvariantCulture);
				_str.Commands.SetOffsetBias(lidx, kidx, result - _sliderMid);
			});
		}

		private void _sliderAngle_ValueChanged(object sender, ValueEventArgs args) {
			ApplyCommand((lidx, kidx) => {
				int result = (int)(_sliderRange * args.Value);

				((SliderColor)sender).SetPosition((double)result / _sliderRange, true);
				_tbAngleBias.Text = (result - _sliderMid).ToString(CultureInfo.InvariantCulture);
				_str.Commands.SetAngleBias(lidx, kidx, result - _sliderMid);
			});
		}

		private void _tbScaleBias_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				float v = FormatConverters.SingleConverter(((TextBox)sender).Text);
				_sliderScale.SetPosition((double)(v + _sliderMid) / _sliderRange, true);
				_str.Commands.SetScaleBias(lidx, kidx, v);
			});
		}

		private void _tbOffsetBias_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				float v = FormatConverters.SingleConverter(((TextBox)sender).Text);
				_sliderOffset.SetPosition((double)(v + _sliderMid) / _sliderRange, true);
				_str.Commands.SetOffsetBias(lidx, kidx, v);
			});
		}

		private void _tbAngleBias_TextChanged(object sender, TextChangedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				float v = FormatConverters.SingleConverter(((TextBox)sender).Text);
				_sliderAngle.SetPosition((double)(v + _sliderMid) / _sliderRange, true);
				_str.Commands.SetAngleBias(lidx, kidx, v);
			});
		}

		private void _isInterpolated_Click(object sender, RoutedEventArgs e) {
			ApplyCommand((lidx, kidx) => {
				_str.Commands.SetInterpolated(_currentFrame.LayerIdx, _currentFrame.KeyIndex, _isInterpolated.IsChecked.Value);
			}, false);
		}

		private void _scaleChanged() {
			if (_isLoading || _fieldEditing)
				return;

			var renderer = _controller.FrameViewer.GetSelectedRenderer();

			if (renderer == null)
				return;

			ApplyCommand((lidx, kidx) => {
				float newScaleX = FormatConverters.SingleConverter(_tbScaleX.Text);
				float newScaleY = FormatConverters.SingleConverter(_tbScaleY.Text);
				var scale = _getFrameScale(renderer, false);

				float[] vertices = new float[8];

				for (int i = 0; i < 8; i++)
					vertices[i] = _currentFrame.Vertices[i];

				if (scale.X == 0) {
					vertices[0] = -scale.TextureWidth / 2;
					vertices[1] = -vertices[0];
					vertices[2] = -vertices[0];
					vertices[3] = vertices[0];
				}
				else {
					for (int i = 0; i < 4; i++)
						vertices[i] = vertices[i] / scale.X * newScaleX;
				}

				if (scale.Y == 0) {
					vertices[4] = -scale.TextureHeight / 2;
					vertices[5] = vertices[4];
					vertices[6] = -vertices[4];
					vertices[7] = -vertices[4];
				}
				else {
					for (int i = 4; i < 8; i++)
						vertices[i] = vertices[i] / scale.Y * newScaleY;
				}

				_controller.Str.Commands.SetVertices(lidx, kidx, vertices);

				try {
					DisableEvents();
					SetVertices(vertices);
				}
				finally {
					EnableEvents();
				}
			});
		}

		private void _tbScaleX_TextChanged(object sender, TextChangedEventArgs e) {
			_scaleChanged();
		}

		private void _tbScaleY_TextChanged(object sender, TextChangedEventArgs e) {
			_scaleChanged();
		}

		private float[] _originalVertices = new float[8];

		private (float X, float Y, float TextureWidth, float TextureHeight) _getFrameScale(LayerRenderer renderer, bool useRenderer) {
			var textures = renderer.Textures;
			float[] vertices = _originalVertices;

			float frameWidth = vertices[1] - vertices[0];
			float frameHeight = vertices[6] - vertices[4];
			float textureWidth = 64f;
			float textureHeight = 64f;

			if (_currentFrame.TextureIndex >= 0 && _currentFrame.TextureIndex < textures.Count) {
				var texture = textures[_currentFrame.TextureIndex];

				if (texture != null) {
					textureWidth = Math.Max(1, texture.Width);
					textureHeight = Math.Max(1, texture.Height);
				}
			}

			float sx = frameWidth / textureWidth;
			float sy = frameHeight / textureHeight;
			return (sx, sy, textureWidth, textureHeight);
		}

		private bool _isProcessingKey = false;

		private void _frameViewer_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
			if (!_isProcessingKey)
				return;

			var left = Keyboard.IsKeyUp(Key.Left);
			var right = Keyboard.IsKeyUp(Key.Right);
			var up = Keyboard.IsKeyUp(Key.Up);
			var down = Keyboard.IsKeyUp(Key.Down);

			if (left && right && up && down) {
				var im = _interactionManager;

				im.LayerTransformTool.EndTranslate();
				im.PointTranslateTool.End();
				_str.InvalidateVisualRedraw();
				im.SetActiveTool(null);
				_isProcessingKey = false;
			}
		}

		private void _frameViewer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			TkVector2 delta;

			var left = Keyboard.IsKeyDown(Key.Left);
			var right = Keyboard.IsKeyDown(Key.Right);
			var up = Keyboard.IsKeyDown(Key.Up);
			var down = Keyboard.IsKeyDown(Key.Down);

			if (left || right || up || down) {
				delta = new TkVector2((left ? -1 : 0) + (right ? 1 : 0), (up ? -1 : 0) + (down ? 1 : 0));
			}
			else {
				return;
			}

			var im = _interactionManager;

			try {
				DisableEvents();

				EditTool tool = null;

				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
					tool = im.PointTranslateTool;
				}
				else {
					tool = im.LayerTransformTool;
				}

				if (!_isProcessingKey) {
					_currentEditLayer = _controller.FrameViewer.GetSelectedRenderer();

					if (_currentFrame == null || _currentEditLayer == null)
						return;

					im.SetActiveTool(tool);
					im.ActiveHandle = new ToolHandle(0);
					tool.BeginEvent(_controller.FrameViewer, _currentEditLayer);
					_clickedPoint = new Point();
					_isProcessingKey = true;
				}

				_clickedPoint.X += delta.X;
				_clickedPoint.Y += delta.Y;

				if (tool == im.PointTranslateTool) {
					im.PointTranslateTool.DoEventRaw(_controller.FrameViewer, _clickedPoint.X, _clickedPoint.Y, false);
					SetVertices(_currentEditLayer.Inter.Vertices);
				}
				else {
					im.LayerTransformTool.DoTranslateRaw(_controller.FrameViewer, _clickedPoint.X, _clickedPoint.Y, false);
					SetOffsets(_currentEditLayer.Inter.Offset);
				}

				_controller.FrameViewer.QuickUpdate();
			}
			finally {
				EnableEvents();
			}

			e.Handled = true;
		}

		public void TranslateOffset(float x, float y) {
			TkVector2 p = new TkVector2(x, y);

			ApplyCommand((lidx, kidx) => {
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
					p.RotateZ(_currentFrame.Angle);

					float[] vertices = new float[8];

					for (int i = 0; i < 8; i++)
						vertices[i] = _currentFrame.Vertices[i];

					for (int i = 0; i < 4; i++)
						vertices[i] += p.X;

					for (int i = 4; i < 8; i++)
						vertices[i] += p.Y;

					_controller.Str.Commands.SetVertices(lidx, kidx, vertices);

					try {
						DisableEvents();
						_currentFrame.Vertices = vertices;
						SetVertices(vertices);
					}
					finally {
						EnableEvents();
					}
				}
				else {
					var offset = _currentFrame.Offset;
					offset += p;
					_controller.Str.Commands.SetOffset(lidx, kidx, offset.X, offset.Y);

					try {
						DisableEvents();
						_currentFrame.Offset = offset;
						SetOffsets(offset);
					}
					finally {
						EnableEvents();
					}
				}
			});
		}
	}
}
