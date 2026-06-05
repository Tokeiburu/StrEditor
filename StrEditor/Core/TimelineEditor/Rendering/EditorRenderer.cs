using ErrorManager;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.TimelineEditor.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.Shortcuts;
using Utilities;

namespace StrEditor.Core.TimelineEditor.Rendering {
	public class EditorRenderer {
		private Editor _editor;
		private StrController _controller;
		private Grid _primaryGrid;
		private readonly List<EditorLayerRenderer> _layerRenderers = new List<EditorLayerRenderer>();
		public List<EditorLayerRenderer> LayerRenderers => _layerRenderers;

		public delegate void RendererUpdatedEventHandler();

		public event RendererUpdatedEventHandler RendererUpdated;

		public void OnRendererUpdated() => RendererUpdated?.Invoke();

		public EditorRenderer(Editor editor) {
			_editor = editor;
			_controller = _editor.Controller;

			_primaryGrid = _editor._primaryGrid;
			_initializeZoomingFeature();
		}

		private void _initializeZoomingFeature() {
			_editor._bZoomIn.Click += (s, e) => AddZoomHeight(2);
			_editor._bZoomOut.Click += (s, e) => AddZoomHeight(-2);

			ApplicationShortcut.Link(StrEditorCommands.LayerEditorZoomIn, () => AddZoomHeight(2), _editor);
			ApplicationShortcut.Link(StrEditorCommands.LayerEditorZoomOut, () => AddZoomHeight(-2), _editor);
		}

		public void AddZoomHeight(int increment) {
			if (increment < 0 && _editor.KeyFrameHeight <= Editor.MinKeyFrameHeight)
				return;
			if (increment > 0 && _editor.KeyFrameHeight >= Editor.MaxKeyFrameHeight)
				return;

			_editor.KeyFrameHeight = Methods.Clamp(_editor.KeyFrameHeight + increment, Editor.MinKeyFrameHeight, Editor.MaxKeyFrameHeight);
			StrEditorConfiguration.KeyFrameHeight = _editor.KeyFrameHeight;
			Reload();
		}

		public void Reload() {
			try {
				var str = _controller.Str;
				_primaryGrid.Children.Clear();
				_layerRenderers.Clear();
				_primaryGrid.RowDefinitions.Clear();
				_primaryGrid.Width = _editor.KeyFrameWidth * str.KeyFrameCount + 72;

				EditorLayerRenderer.PreRender(_editor.KeyFrameHeight - Editor.MaxKeyFrameHeight);

				for (int index = 0; index < str.Layers.Count; index++) {
					var layer = str.Layers[index];

					_addNewTimeline(index);
					_layerRenderers[index].Set(str, layer);
				}

				OnRendererUpdated();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _addNewTimeline(int layerIndex, int fixedOffset = -1) {
			try {
				EditorLayerRenderer layerTimeline = new EditorLayerRenderer(_editor);
				_primaryGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(_editor.KeyFrameHeight, GridUnitType.Pixel) });
				layerTimeline.SetValue(Grid.RowProperty, _primaryGrid.RowDefinitions.Count - 1);
				layerTimeline.SetValue(Grid.ColumnProperty, 1);

				if (fixedOffset > -1) {
					_primaryGrid.Children.Insert(fixedOffset, layerTimeline);
					_layerRenderers.Insert(fixedOffset, layerTimeline);
				}
				else {
					_primaryGrid.Children.Add(layerTimeline);
					_layerRenderers.Add(layerTimeline);
				}

				if (layerIndex == 0 && _controller.Str[layerIndex].KeyFrames.Count == 0) {
					layerTimeline.Visibility = Visibility.Collapsed;
					_primaryGrid.RowDefinitions[layerIndex].Height = new GridLength(0, GridUnitType.Pixel);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
