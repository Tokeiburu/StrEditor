using GRF.FileFormats.StrFormat;
using StrEditor.Core.TimelineEditor.Controls;
using StrEditor.Core.TimelineEditor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Utilities;

namespace StrEditor.Core.TimelineEditor.State {
	public class SelectionChangedArgs {
		public int OldFrame;
		public int NewFrame;
		public int OldLayer;
		public int NewLayer;
		public bool FrameChanged;
		public bool LayerChanged;
	}

	public struct SelectionPosition {
		public int X;
		public int Y;

		public override string ToString() {
			return $"{X};{Y}";
		}
	}

	public class Selection {
		private readonly KeySelector _keySelector;
		private readonly Editor _editor;

		// Starting position of the selection
		public SelectionPosition Anchor;

		public int CurrentX {
			get {
				return (_moveOverride ?? Current).X;
			}
			set {
				if (_moveOverride != null) {
					Current.X = _moveOverride.Value.X;
					_moveOverride = null;
				}

				Current.X = value;
			}
		}

		// End target position of the selection
		public SelectionPosition Current;

		// Currently selected position in the selection
		public SelectionPosition? _moveOverride;

		// The current selection
		public int SelectedFrame => Anchor.X;
		public int SelectedLayer => Anchor.Y;

		// Computed properties for easier enumeration
		public int StartFrame => Math.Min(Anchor.X, Current.X);
		public int StartLayer => Math.Min(Anchor.Y, Current.Y);

		public int EndFrame => Math.Max(Anchor.X, Current.X) + 1;
		public int EndLayer => Math.Max(Anchor.Y, Current.Y) + 1;

		public int FrameCount => EndFrame - StartFrame;
		public int LayerCount => EndLayer - StartLayer;

		public int Length => LayerCount * FrameCount;

		public bool IsActive = false;

		public IEnumerable<int> Layers => Enumerable.Range(StartLayer, LayerCount);
		public IEnumerable<int> Frames => Enumerable.Range(StartFrame, FrameCount);
		public IEnumerable<(int LIndex, int LayerIndex)> IndexedLayers {
			get {
				int startLayer = StartLayer;

				for (int lidx = 0; lidx < LayerCount; lidx++) {
					yield return (lidx, lidx + startLayer);
				}
			}
		}

		public int StartKeyIndex => _controller.Str[StartLayer].FrameIndex2KeyIndex[StartFrame];

		public int MoveStartFrame;
		public int MoveStartLayer;

		private StrController _controller;

		public delegate void SelectionChangedEventHandler(SelectionChangedArgs args);
		public event SelectionChangedEventHandler SelectionChanged;
		public void OnSelectionChanged(SelectionChangedArgs args) => SelectionChanged?.Invoke(args);

		public Selection(KeySelector keySelector, StrController controller) {
			_keySelector = keySelector;
			_controller = controller;
			_editor = _controller.TimelineEditor;

			controller.TimelineEditor.Renderer.RendererUpdated += _renderer_RendererUpdated;
		}

		private void _renderer_RendererUpdated() {
			UpdateVisual();
		}

		public void SetXY(int startFrame, int startLayer, bool sanitize = true) {
			Set(startFrame, 1, startLayer, 1, sanitize);
		}

		public void Set(int startFrame, int frameCount, int startLayer, int layerCount, bool sanitize = true, bool enableEvents = true) {
			if (enableEvents)
				_saveState();

			Anchor.X = startFrame;
			Anchor.Y = startLayer;

			Current.X = Anchor.X + frameCount - 1;
			Current.Y = Anchor.Y + layerCount - 1;
			_moveOverride = null;

			if (sanitize)
				Sanitize();

			IsActive = true;
			_updateVisualSelection();

			if (enableEvents)
				_compareState();
		}

		public void SetLayerTarget(int startLayer, int targetLayer, bool sanitize = true) {
			_saveState();
			Anchor.Y = startLayer;
			Current.Y = targetLayer;

			if (sanitize)
				Sanitize();

			IsActive = true;
			_updateVisualSelection();
			_compareState();
		}

		private int _oldStartFrame;
		private int _oldStartLayer;

		private void _saveState() {
			if (_moveOverride != null) {
				Anchor = _moveOverride.Value;
				_moveOverride = null;
			}

			_oldStartFrame = SelectedFrame;
			_oldStartLayer = SelectedLayer;
		}

		private void _compareState() {
			if (_oldStartFrame != SelectedFrame || _oldStartLayer != SelectedLayer) {
				OnSelectionChanged(new SelectionChangedArgs {
					OldLayer = _oldStartLayer, 
					OldFrame = _oldStartFrame, 
					NewLayer = SelectedLayer, 
					NewFrame = SelectedFrame,
					FrameChanged = _oldStartFrame != SelectedFrame, 
					LayerChanged = _oldStartLayer != SelectedLayer
				});
			}
		}

		public void SetTarget(int targetFrame, int targetLayer) {
			SetTarget(new SelectionPosition { X = targetFrame, Y = targetLayer });
		}

		public void SetTarget(SelectionPosition current) {
			if (_moveOverride != null) {
				Anchor = _moveOverride.Value;
				_moveOverride = null;
			}

			Current = current;

			Sanitize();

			_updateVisualSelection();
			//_compareState();
		}

		public void Set(Selection selection, bool sanitize = true) {
			if (selection.Length == 0)
				return;

			_saveState();
			Anchor = selection.Anchor;
			Current = selection.Current;

			if (sanitize)
				Sanitize();

			IsActive = true;
			_updateVisualSelection();
			_compareState();
		}

		public void UpdateVisual() {
			_updateVisualSelection();
		}

		private void _updateVisualSelection() {
			if (!IsActive) {
				if (_keySelector.Visibility != Visibility.Collapsed)
					_keySelector.Visibility = Visibility.Collapsed;

				return;
			}

			if (StartFrame >= _controller.Str.KeyFrameCount || StartLayer >= _controller.Str.Layers.Count) {
				_keySelector.Visibility = Visibility.Collapsed;
				return;
			}

			if (FrameCount <= 0 || LayerCount <= 0) {
				_keySelector.Visibility = Visibility.Collapsed;
				return;
			}

			int startLayer = StartLayer;
			int startFrame = StartFrame;
			int frameCount = FrameCount;
			int layerCount = LayerCount;

			if (startLayer < _editor.DrawYOffset) {
				layerCount = layerCount - (_editor.DrawYOffset - startLayer);
				startLayer = _editor.DrawYOffset;
			}

			if (layerCount <= 0) {
				_keySelector.Visibility = Visibility.Collapsed;
				return;
			}

			if (startFrame < 0) {
				frameCount += startFrame;
				startFrame = 0;
			}

			if (frameCount <= 0) {
				_keySelector.Visibility = Visibility.Collapsed;
				return;
			}

			if (startLayer >= _controller.Str.Layers.Count) {
				_keySelector.Visibility = Visibility.Collapsed;
				return;
			}

			if (frameCount > _controller.Str.KeyFrameCount) {
				_keySelector.Visibility = Visibility.Collapsed;
				return;
			}

			if (startLayer + layerCount > _controller.Str.Layers.Count)
				layerCount = _controller.Str.Layers.Count - startLayer;

			if (startFrame + frameCount > _controller.Str.KeyFrameCount)
				frameCount = _controller.Str.KeyFrameCount - startFrame;

			_keySelector.Margin = new Thickness(startFrame * _editor.KeyFrameWidth, (startLayer - _editor.DrawYOffset) * _editor.KeyFrameHeight, 0, 0);
			_keySelector.Width = _editor.KeyFrameWidth * frameCount;
			_keySelector._gridInternal.Height = _editor.KeyFrameHeight * layerCount - 1;
			_keySelector.Visibility = Visibility.Visible;
		}

		public bool IsWithinSelection(Point position) {
			if (FrameCount <= 0)
				return false;

			var xMin = _keySelector.Margin.Left;
			var xMax = xMin + _editor.KeyFrameWidth * FrameCount;

			var yMin = _keySelector.Margin.Top;
			var yMax = yMin + LayerCount * _editor.KeyFrameHeight;

			if (position.X >= xMin && position.X <= xMax && position.Y <= yMax && position.Y >= yMin) {
				return true;
			}

			return false;
		}

		public void Deselect() {
			if (!IsActive)
				return;

			_saveState();
			Current = Anchor;
			IsActive = false;
			_keySelector.Visibility = Visibility.Collapsed;
			_keySelector.Margin = new Thickness(0, 0, 0, 0);
			//_compareState();
		}

		public void SetOffset(int startFrame, int startLayer) {
			MoveStartFrame = startFrame;
			MoveStartLayer = startLayer;
		}

		public Selection Sanitize() {
			int maxLayers = _controller.Str.Layers.Count;
			int maxFrames = _controller.Str.KeyFrameCount;

			// Left/top overflow
			Anchor.X = Methods.Clamp(Anchor.X, 0, maxFrames - 1);
			Anchor.Y = Methods.Clamp(Anchor.Y, _editor.DrawYOffset, maxLayers - 1);

			Current.X = Methods.Clamp(Current.X, 0, maxFrames - 1);
			Current.Y = Methods.Clamp(Current.Y, _editor.DrawYOffset, maxLayers - 1);
			_moveOverride = null;

			//if (MoveOverride != null) {
			//	Selected.X = Methods.Clamp(Selected.X, Math.Min(Anchor.X, Current.X), Math.Max(Anchor.X, Current.X));
			//	Selected.Y = Methods.Clamp(Selected.Y, Math.Min(Anchor.Y, Current.Y), Math.Max(Anchor.Y, Current.Y));
			//}

			MoveStartFrame = 0;
			MoveStartLayer = 0;

			return this;
		}

		public List<int> GetActiveKeys(int layerIndex) {
			return _controller.Str[layerIndex].GetKeyIndexesInRange(StartFrame, FrameCount);
		}

		public List<int> GetActiveKeysDescending(int layerIndex) {
			var r = _controller.Str[layerIndex].GetKeyIndexesInRange(StartFrame, FrameCount);
			r.Reverse();
			return r;
		}

		public List<int> GetActiveFrames(int layerIndex) {
			List<int> frameIndexes = new List<int>();
			var cachedFrameIndex2KeyIndex = _controller.Str[layerIndex].FrameIndex2KeyIndex;

			for (int fidx = 0; fidx < FrameCount; fidx++) {
				int frameIndex = fidx + StartFrame;

				if (cachedFrameIndex2KeyIndex[frameIndex] != -1) {
					frameIndexes.Add(frameIndex);
				}
			}

			return frameIndexes;
		}

		public void PreviewTo(int frameIndex, int layerIndex) {
			Set(StartFrame + (frameIndex - MoveStartFrame), FrameCount, StartLayer + (layerIndex - MoveStartLayer), LayerCount, false);
			SetOffset(frameIndex, layerIndex);
		}

		public void SetMoveOverride(SelectionPosition currentPosition) {
			_moveOverride = currentPosition;
		}

		public bool LayerIntersect(Selection selection) {
			return	selection.StartLayer < this.EndLayer &&
					selection.EndLayer > this.StartLayer;
		}

		public bool IsEquals(Selection selection) {
			return
				selection.StartFrame == StartFrame &&
				selection.StartLayer == StartLayer &&
				selection.LayerCount == LayerCount &&
				selection.FrameCount == FrameCount;
		}

		public void AddPositionCommand(Str str, bool sanitize = false) {
			if (sanitize)
				Sanitize();

			str.Commands.SetEditorPosition(StartFrame, FrameCount, StartLayer, LayerCount);
		}
	}
}
