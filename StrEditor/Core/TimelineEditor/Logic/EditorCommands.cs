using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.TimelineEditor.Controls;
using StrEditor.Core.TimelineEditor.State;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using static GRF.FileFormats.StrFormat.Commands.ScaleFromPivotCommand;

namespace StrEditor.Core.TimelineEditor.Logic {
	public class EditorCommands {
		private readonly List<List<StrKeyFrame>> _copyframes = new List<List<StrKeyFrame>>();
		private Editor _editor;
		private StrController _controller;

		public EditorCommands(Editor editor) {
			_editor = editor;
			_controller = editor.Controller;
		}

		public void CreateBezier() => _setBezierSelection(new float[4] { -10, 0, 10, 0 });
		public void DeleteBezier() => _setBezierSelection(new float[4]);

		private bool _getScaleValue(out float newScale) {
			newScale = 0;

			var dialog = new InputDialog("Enter the magnifier scale.", "Magnify", Configuration.ConfigAsker["[ActEditor - Magnify value]", "2"]);
			dialog.Owner = WpfUtilities.TopWindow;
			dialog.TextBoxInput.VerticalContentAlignment = VerticalAlignment.Center;

			if (dialog.ShowDialog() != true) {
				return false;
			}

			if (!float.TryParse(dialog.Input.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out newScale)) {
				ErrorHandler.HandleException("The magnifier value is not valid. Only float values are allowed.", ErrorLevel.Warning);
				return false;
			}

			return true;
		}

		public void Scale(ScaleMode scaleMode, PivotMode pivotMode) {
			try {
				if (scaleMode == ScaleMode.KeyFrame && !_controller.KeyFrameEditor.CanEdit)
					return;

				if (!_getScaleValue(out float newScale))
					return;

				_controller.Str.Commands.Begin();
				TkVector2 pivot = default;

				if (pivotMode == PivotMode.Defined)
					pivot = new TkVector2(Str.OffsetX, Str.OffsetY);

				switch(scaleMode) {
					case ScaleMode.KeyFrame:
						_controller.KeyFrameEditor.ApplyCommand((layerIndex, keyIndex) => {
							_controller.Str.Commands.Scale(layerIndex, keyIndex, newScale, newScale, pivot, pivotMode);
						});
						break;
					case ScaleMode.Layer:
						foreach (var lidx in _editor.Selection.IndexedLayers) {
							_controller.Str.Commands.Scale(lidx.LayerIndex, newScale, newScale, pivot, pivotMode);
						}
						break;
					case ScaleMode.Str:
						_controller.Str.Commands.Scale(newScale, newScale, pivot, pivotMode);
						break;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				_controller.Str.Commands.CancelEdit();
			}
			finally {
				_controller.Str.Commands.End();
				_controller.KeyFrameEditor.InvalidateKeyFrame();
			}
		}

		public void CenterOrigin() {
			if (_controller.Str == null)
				return;

			try {
				if (StrEditorConfiguration.GroupEdit) {
					_controller.Str.Commands.CenterOrigin(_editor.Selection.StartLayer);
				}
				else {
					_controller.Str.Commands.CenterOrigin(_editor.Selection.StartLayer, _editor.Selection.StartKeyIndex, StrEditorConfiguration.GroupEdit);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_controller.KeyFrameEditor.InvalidateKeyFrame();
			}
		}

		public void SetInterpolate(Selection selection, bool value) {
			if (_controller.Str == null)
				return;

			try {
				_controller.Str.Commands.Begin();

				foreach (var layerIndex in selection.Layers) {
					foreach (var keyIndex in selection.GetActiveKeysDescending(layerIndex)) {
						_controller.Str.Commands.SetInterpolated(layerIndex, keyIndex, value);
					}
				}
				_controller.KeyFrameEditor.InvalidateKeyFrame();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_controller.Str.Commands.End();
			}
		}

		private void _setBezierSelection(float[] bezier) {
			if (_controller.Str == null)
				return;

			try {
				_controller.Str.Commands.Begin();

				foreach (var layerIndex in _editor.Selection.Layers) {
					foreach (var keyIndex in _editor.Selection.GetActiveKeysDescending(layerIndex)) {
						_controller.Str.Commands.SetBezierPositions(layerIndex, keyIndex, bezier);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_controller.Str.Commands.End();
				_controller.KeyFrameEditor.InvalidateKeyFrame();
			}
		}

		public int Copy(Selection selection = null) {
			int total = 0;

			try {
				if (_controller.Str == null)
					return 0;

				selection = selection ?? _editor.Selection;

				try {
					_controller.KeyFrameEditor.Copy();
				}
				catch {
					return 0;
				}

				_copyframes.Clear();

				foreach (var lidx in selection.IndexedLayers) {
					var layer = _controller.Str[lidx.LayerIndex];
					int lastKeyIndex = -2;

					_copyframes.Add(new List<StrKeyFrame>());

					foreach (var frameIndex in selection.Frames) {
						int keyIndex = layer.FrameIndex2KeyIndex[frameIndex];

						if (keyIndex == -1) {
							var keyFrame = new StrKeyFrame();
							keyFrame.FrameIndex = frameIndex;
							keyFrame.Type = 2;
							_copyframes[lidx.LIndex].Add(keyFrame);
							total++;
							lastKeyIndex = keyIndex;
							continue;
						}

						if (lastKeyIndex == keyIndex)
							continue;

						if (layer[keyIndex].FrameIndex < selection.StartFrame)
							continue;

						_copyframes[lidx.LIndex].Add(new StrKeyFrame(layer[keyIndex]));
						total++;
						lastKeyIndex = keyIndex;
					}
				}

				foreach (var copyFrames in _copyframes) {
					foreach (var frame in copyFrames) {
						frame.FrameIndex -= selection.StartFrame;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return total;
		}

		private void _paste(List<StrKeyFrame> copyFrames, int layerIndexDextination, int frameIndexDestination) {
			if (layerIndexDextination >= _controller.Str.Layers.Count || copyFrames.Count == 0 || layerIndexDextination < _editor.DrawYOffset)
				return;

			CopyState state = new CopyState(_controller.Str, layerIndexDextination, copyFrames);
			state.Init(frameIndexDestination);
			state.Paste(state.Delete());
		}

		public void Paste(Selection selection = null) {
			if (_controller.Str == null)
				return;

			selection = selection ?? _editor.Selection;
			
			try {
				if (_copyframes.Count == 0)
					return;

				_controller.Str.Commands.Begin();

				for (int lidx = 0; lidx < _copyframes.Count; lidx++) {
					_paste(_copyframes[lidx], selection.StartLayer + lidx, selection.StartFrame);
				}
			}
			catch {
				_controller.Str.Commands.CancelEdit();
			}
			finally {
				_controller.Str.Commands.End();
			}
		}

		public void DeleteKeys(Selection selection) {
			try {
				var copyFrames = new List<List<StrKeyFrame>>();

				foreach (var lidx in selection.IndexedLayers) {
					copyFrames.Add(new List<StrKeyFrame>());

					for (int fidx = 0; fidx < selection.FrameCount; fidx++) {
						StrKeyFrame frame = new StrKeyFrame();
						frame.FrameIndex = fidx;
						frame.Type = 2;
						copyFrames[lidx.LIndex].Add(frame);
					}
				}

				try {
					_controller.Str.Commands.Begin();

					foreach (var lidx in selection.IndexedLayers) {
						CopyState state = new CopyState(_controller.Str, lidx.LayerIndex, copyFrames[lidx.LIndex]);
						state.Init(selection.StartFrame);
						state.Paste(state.Delete());
					}
				}
				finally {
					_controller.Str.Commands.End();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void DeleteKeysAll(Selection selection) {
			try {
				try {
					_controller.Str.Commands.Begin();

					foreach (var layerIndex in selection.Layers) {
						_controller.Str.Commands.DeleteKeys(layerIndex, selection.StartFrame, selection.FrameCount);
					}
				}
				finally {
					_controller.Str.Commands.End();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void SelectAllKeysInLayer(Selection selection) {
			try {
				if (_controller.Str == null)
					return;
				
				var currentPosition = selection.Anchor;
				selection.Set(0, _controller.Str.KeyFrameCount, selection.StartLayer, selection.LayerCount, sanitize: true, enableEvents: false);
				selection.SetMoveOverride(currentPosition);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void SetNewKey(Selection selection) {
			try {
				_controller.Str.Commands.CreateNew(selection.StartLayer, selection.StartFrame, StrEditorConfiguration.InterpolateNewKey);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void SetEndKey(Selection selection) {
			try {
				_controller.Str.Commands.CreateEndKey(selection.StartLayer, selection.StartFrame);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void CopyPreviousKey(Selection selection) {
			var str = _controller.Str;

			int frameIndex = selection.SelectedFrame;

			if (frameIndex <= 0)
				return;

			try {
				var layer = str[selection.SelectedLayer];

				str.Commands.Begin();

				var previousKeyFrame = layer[layer.FrameIndex2KeyIndex[frameIndex - 1]];

				if (previousKeyFrame == null)
					return;

				var newKeyFrame = new StrKeyFrame(previousKeyFrame);
				newKeyFrame.FrameIndex = frameIndex;

				str.Commands.SetKey(selection.StartLayer, selection.StartFrame, newKeyFrame);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				str.Commands.End();
			}
		}

		private void _delete(List<int> frames, int layerIndex) {
			var layer = _controller.Str.Layers[layerIndex];
			int lastKeyIndex = -1;

			for (int i = frames.Count - 1; i >= 0; i--) {
				int keyIndex = layer.FrameIndex2KeyIndex[frames[i]];

				if (keyIndex == lastKeyIndex || keyIndex == -1)
					continue;

				if (layer[keyIndex].FrameIndex < frames[0])
					break;

				_controller.Str.Commands.DeleteKey(layerIndex, keyIndex);
				lastKeyIndex = keyIndex;
			}
		}

		private void _pasteMoveSelection(int frameIndexStart, int layerIndex, int copyFrameLayerIndex, int moveFrameIndexEnd) {
			List<StrKeyFrame> copyFrames = new List<StrKeyFrame>();
			var layer = _controller.Str[layerIndex];

			// Adjust frame indexes
			for (int i = 0; i < _copyframes[copyFrameLayerIndex].Count; i++) {
				var keyFrame = new StrKeyFrame(_copyframes[copyFrameLayerIndex][i]);

				keyFrame.FrameIndex += frameIndexStart;

				if (keyFrame.FrameIndex < 0)
					continue;

				if (keyFrame.Type == 0 && keyFrame.FrameIndex >= _controller.Str.KeyFrameCount)
					break;

				if (keyFrame.Type == 0 && keyFrame.IsInterpolated && keyFrame.FrameIndex == _controller.Str.KeyFrameCount - 1) {
					keyFrame.IsInterpolated = false;
				}

				// Last frame, check for interpolation
				if (keyFrame.Type == 0 && i == _copyframes[copyFrameLayerIndex].Count - 1) {
					bool isInterpolated = keyFrame.IsInterpolated;

					if (_editor.Selection.FrameCount == 1) {
						if (!isInterpolated && frameIndexStart > _editor.Selection.StartFrame) {
							var lastKeyIndex = layer.FrameIndex2KeyIndex[frameIndexStart];

							if (lastKeyIndex == -1)
								isInterpolated = false;
						}
					}
					else if (!keyFrame.IsInterpolated) {
						var lastFrameIndex = moveFrameIndexEnd < _editor.Selection.StartFrame ? moveFrameIndexEnd - 1 : Math.Max(moveFrameIndexEnd - 1, _editor.Selection.EndFrame);
						var lastKeyIndex = layer.FrameIndex2KeyIndex[lastFrameIndex];

						if (layer.IsInter(lastKeyIndex)) {
							bool nextKeyImmediatelyFollows = layer[lastKeyIndex + 1] != null && layer[lastKeyIndex + 1].FrameIndex == lastFrameIndex + 1;

							// If there's a key on the next frame, skip, the segment is complete
							if (!nextKeyImmediatelyFollows) {
								isInterpolated = true;
							}
						}
					}

					keyFrame.IsInterpolated = isInterpolated;
				}

				copyFrames.Add(keyFrame);
			}

			// Attempt to paste!
			for (int i = 0; i < copyFrames.Count; i++) {
				if (copyFrames[i].Type != 2)
					_controller.Str.Commands.SetKey(layerIndex, copyFrames[i]);
			}
		}

		public void MoveSelection() {
			try {
				var str = _controller.Str;

				if (str == null)
					return;

				var selection = _editor.Selection;
				var selectionPreview = _editor.SelectionPreview;

				// The target move area is the same as the selection, there is nothing to do.
				if (selection.IsEquals(selectionPreview))
					return;

				//.StartFrame == selectionPreview.StartFrame && selection.StartLayer == selectionPreview.StartLayer

				int moveFrameIndexStart = Methods.Clamp(selectionPreview.StartFrame, 0, selectionPreview.StartFrame);
				int moveFrameIndexEnd = Methods.Clamp(selectionPreview.EndFrame, selectionPreview.EndFrame, str.KeyFrameCount);

				// Copy the frames to the _copyFrames list, this is... weird and needs some fixing.
				int framesCopied = Copy(selection);

				// No frames were copied, there is nothing to do
				if (framesCopied == 0)
					return;

				try {
					str.Commands.Begin();
					selection.AddPositionCommand(str);

					// If the moving target area is on a different layer and
					// there is no intersection, use the MoveSelectionLayers method instead.
					if (!selectionPreview.LayerIntersect(selection)) {
						MoveSelectionLayers(str);
						return;
					}

					// There are 2 or more layers and they intersect with the each other, this is not supported.
					if (selection.StartLayer != selectionPreview.StartLayer && selection.LayerCount > 1)
						return;

					var layer = str.Layers[selection.StartLayer];

					// If there is only one key frame in the selection, these are handly differently
					if (selection.GetActiveFrames(selection.StartLayer).Count == 1) {
						foreach (var lidx in selection.IndexedLayers) {
							if (_copyframes[lidx.LIndex].Count == 0)
								continue;

							int layerIndex = lidx.LayerIndex;
							var activeFrameIndexes = selection.GetActiveFrames(layerIndex);

							if (activeFrameIndexes.Count == 0)
								continue;

							layer = str.Layers[layerIndex];
							var baseFrameIndex = activeFrameIndexes[0];
							var baseKeyIndex = layer.FrameIndex2KeyIndex[baseFrameIndex];

							if (!layer.IsInter(baseKeyIndex - 1) && !layer.IsInter(baseKeyIndex)) {
								// Single move copy
								_deleteKeys(str, layerIndex, baseFrameIndex, baseFrameIndex + 1);
								_deleteKeys(str, layerIndex, moveFrameIndexStart, moveFrameIndexStart + 1);
								_copyframes[lidx.LIndex][0].FrameIndex = moveFrameIndexStart;
								str.Commands.SetKey(lidx.LayerIndex, _copyframes[lidx.LIndex][0]);
							}
							else {
								_deleteKeys(str, layerIndex, Math.Min(baseFrameIndex, moveFrameIndexStart), Math.Max(baseFrameIndex, moveFrameIndexStart) + 1);
								_pasteMoveSelection(moveFrameIndexStart, layerIndex, lidx.LIndex, moveFrameIndexEnd);
							}
						}
					}
					else {
						foreach (var lidx in selection.IndexedLayers) {
							if (_copyframes[lidx.LIndex].Count == 0)
								continue;

							int layerIndex = lidx.LayerIndex;
							layer = str.Layers[layerIndex];

							// Moving more than one
							var baseFrameIndex = selection.StartFrame;
							var targetKeyIndex = layer.FrameIndex2KeyIndex[moveFrameIndexEnd - 1];

							_deleteKeys(str, layerIndex, moveFrameIndexStart, moveFrameIndexEnd);
							_deleteKeys(str, layerIndex, selection.StartFrame, selection.EndFrame);

							_pasteMoveSelection(selectionPreview.StartFrame, layerIndex, lidx.LIndex, moveFrameIndexEnd);
						}
					}
				}
				catch {
					str.Commands.CancelEdit();
				}
				finally {
					selectionPreview.AddPositionCommand(str, true);
					str.Commands.End();
					selection.Set(selectionPreview);
					str.InvalidateVisualRedraw();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void MoveSelectionLayers(Str str) {
			foreach (var lidx in _editor.Selection.IndexedLayers) {
				_delete(_editor.Selection.Frames.ToList(), lidx.LayerIndex);
				_paste(_copyframes[lidx.LIndex], _editor.SelectionPreview.StartLayer + lidx.LIndex, _editor.SelectionPreview.StartFrame);
			}
		}

		private void _deleteKeys(Str str, int layerIndex, int startFrameIndex, int endFrameIndex) {
			str.Commands.DeleteKeys(layerIndex, startFrameIndex, endFrameIndex - startFrameIndex);
		}

		public void SetMaxKeyFrameCount(int maxFrames) {
			var str = _controller.Str;

			try {
				str.Commands.Begin();

				maxFrames++;

				// Gotta delete a whole bunch of stuff, boo.
				for (int layerIndex = 0; layerIndex < str.Layers.Count; layerIndex++) {
					var layer = str[layerIndex];
					StrKeyFrame keyFrame = null;

					for (int keyIndex = layer.KeyFrames.Count - 1; keyIndex >= 0; keyIndex--) {
						if (layer[keyIndex].FrameIndex >= maxFrames) {
							str.Commands.DeleteKey(layerIndex, keyIndex);
						}
						else if (keyFrame != null && layer[keyIndex].IsInterpolated && layer[keyIndex].FrameIndex < maxFrames) {
							str.Commands.SetKey(layerIndex, InterpolatedKeyFrame.InterpolateSub(str, layerIndex, maxFrames - 1, layer[keyIndex], keyFrame).ToKeyFrame());
							break;
						}
						else {
							break;
						}
					}
				}

				str.Commands.SetMaxFrame(maxFrames - 1);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				str.Commands.End();
			}
		}

		public enum PasteDataType {
			Color,
			Blend,
			Offset,
			Angle,
			Positions,
			Texture,
			Animation,
			Bias,
			BezierPositions,
		}

		public void PasteData(PasteDataType type) {
			if (_controller.Str == null)
				return;

			try {
				if (!_controller.KeyFrameEditor.CanEdit || !_controller.KeyFrameEditor.HasCopyFrame)
					return;

				var str = _controller.Str;

				_controller.KeyFrameEditor.ApplyCommand((layerIndex, keyIndex) => {
					switch(type) {
						case PasteDataType.BezierPositions:
							_controller.Str.Commands.SetBezierPositions(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.BezierPositions);
							break;
						case PasteDataType.Bias:
							_controller.Str.Commands.SetOffsetBias(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.OffsetBias);
							_controller.Str.Commands.SetScaleBias(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.ScaleBias);
							_controller.Str.Commands.SetAngleBias(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.AngleBias);
							break;
						case PasteDataType.Animation:
							_controller.Str.Commands.SetAnimationType(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.AnimationType);
							break;
						case PasteDataType.Texture:
							_controller.Str.Commands.SetTextureIndex(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.TextureIndex);
							break;
						case PasteDataType.Positions:
							_controller.Str.Commands.SetPositions(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.Positions);
							break;
						case PasteDataType.Angle:
							_controller.Str.Commands.SetAngle(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.Angle);
							break;
						case PasteDataType.Offset:
							_controller.Str.Commands.SetOffset(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.Offset.X, _controller.KeyFrameEditor.CopyFrame.Offset.Y);
							break;
						case PasteDataType.Blend:
							_controller.Str.Commands.SetBlendSrc(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.BlendSrc);
							_controller.Str.Commands.SetBlendDst(layerIndex, keyIndex, _controller.KeyFrameEditor.CopyFrame.BlendDst);
							break;
						case PasteDataType.Color:
							_controller.Str.Commands.SetColor(layerIndex, keyIndex,
								_controller.KeyFrameEditor.CopyFrame.Color[3],
								_controller.KeyFrameEditor.CopyFrame.Color[0],
								_controller.KeyFrameEditor.CopyFrame.Color[1],
								_controller.KeyFrameEditor.CopyFrame.Color[2]);
							break;

					}
				});
				_editor.OnPositionChanged();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
