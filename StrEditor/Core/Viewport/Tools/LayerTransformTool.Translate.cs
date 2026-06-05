using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using System;
using System.Windows.Forms;
using System.Windows.Input;

namespace StrEditor.Core.Viewport.Tools {
	public partial class LayerTransformTool {
		public void DoTranslate(FrameViewer viewport, FrameViewerEventArgs args) {
			if (args.MouseArgs.Button != MouseButtons.Left)
				return;

			if (!args.HasMoved)
				return;

			if (DoTranslateRaw(viewport, args.DeltaX, args.DeltaY)) {
				_hasTranslated = true;
				var inter = _renderer.Inter;

				viewport.Controller.KeyFrameEditor.Execute(kfs => {
					kfs.SetOffsetX(inter.Offset.X);
					kfs.SetOffsetY(inter.Offset.Y);
				});
			}
		}

		public bool DoTranslateRaw(FrameViewer viewport, double deltaX, double deltaY, bool applyScale = true) {
			if (_keyFrameCopy == null) return false;

			var inter = _renderer.Inter;
			float diffX = (float)(applyScale ? deltaX / viewport.ZoomEngine.Scale : deltaX);
			float diffY = (float)(applyScale ? deltaY / viewport.ZoomEngine.Scale : deltaY);
			float oldX = inter.Offset.X;
			float oldY = inter.Offset.Y;

			inter.Offset.X = _keyFrameCopy.Offset.X + diffX;
			inter.Offset.Y = _keyFrameCopy.Offset.Y + diffY;

			if (StrEditorConfiguration.Snap > 0) {
				inter.Offset.X = (float)(StrEditorConfiguration.Snap * Math.Round(inter.Offset.X / StrEditorConfiguration.Snap, 0, MidpointRounding.ToEven));
				inter.Offset.Y = (float)(StrEditorConfiguration.Snap * Math.Round(inter.Offset.Y / StrEditorConfiguration.Snap, 0, MidpointRounding.ToEven));
			}

			if (oldX == inter.Offset.X && oldY == inter.Offset.Y)
				return false;

			if (StrEditorConfiguration.GroupEdit) {
				// Adjust for snap
				diffX = inter.Offset.X - _keyFrameCopy.Offset.X;
				diffY = inter.Offset.Y - _keyFrameCopy.Offset.Y;

				var layer = _str.Layers[_renderer.LayerIndex];

				for (int index = 0; index < layer.KeyFrames.Count; index++) {
					layer.KeyFrames[index].Offset = new TkVector2(_layerCopy.KeyFrames[index].Offset.X + diffX, _layerCopy.KeyFrames[index].Offset.Y + diffY);
				}
			}

			return true;
		}

		public void EndTranslate() {
			if (_keyFrameCopy == null) return;
			var inter = _renderer.Inter;
			var layerIdx = _renderer.LayerIndex;

			float x = inter.Offset.X;
			float y = inter.Offset.Y;

			// Restore original settings
			inter.Offset.X = _keyFrameCopy.Offset.X;
			inter.Offset.Y = _keyFrameCopy.Offset.Y;

			if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) {
				//_act.Commands.Translate(_preview.SelectedAction, diffX, diffY);
			}
			else {
				if (StrEditorConfiguration.GroupEdit) {
					// Restore layer
					_str.Layers[layerIdx] = new StrLayer(_layerCopy);

					// Apply transformation
					float diffX = x - inter.Offset.X;
					float diffY = y - inter.Offset.Y;

					_str.Commands.Begin();

					for (int index = 0; index < _str.Layers[layerIdx].KeyFrames.Count; index++) {
						var keyFrame = _str.Layers[layerIdx].KeyFrames[index];
						_str.Commands.SetOffset(layerIdx, index, keyFrame.Offset.X + diffX, keyFrame.Offset.Y + diffY);
					}

					if (inter.Interpolated) {
						InterpolatedKeyFrame.ConvertToFrame(inter, _str, false);
						_str.Commands.SetOffset(layerIdx, inter.KeyIndex, x, y);
					}

					_str.Commands.End();
				}
				else {
					InterpolatedKeyFrame.ConvertToFrame(inter, _str, false);
					_str.Commands.Begin();
					_str.Commands.SetOffset(layerIdx, inter.KeyIndex, x, y);
					_str.Commands.End();
				}
			}
		}
	}
}
