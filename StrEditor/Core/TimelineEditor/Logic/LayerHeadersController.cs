using ErrorManager;
using GRF.FileFormats.StrFormat;
using StrEditor.Core.TimelineEditor.Controls;
using StrEditor.Core.TimelineEditor.Rendering;
using StrEditor.Core.Viewport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TokeiLibrary;

namespace StrEditor.Core.TimelineEditor.Logic {
	public class LayerHeadersController {
		private Editor _editor;
		private StrController _controller;
		private Grid _layersGrid;
		private FrameViewer _frameViewer;
		private Line _lineMoveLayer;
		private EditorRenderer _editorRenderer;
		private readonly List<Image> _visibleImages = new List<Image>();

		private readonly SolidColorBrush _brushLayersGrid = new SolidColorBrush(Color.FromArgb(255, 155, 234, 159));
		public int LastClickedLayer;
		private int _layerMoving = -1;
		private int _destinationLayer = -1;

		public LayerHeadersController(Editor editor) {
			_editor = editor;

			_controller = editor.Controller;
			_layersGrid = _editor._layersGrid;
			_frameViewer = editor.Controller.FrameViewer;
			_lineMoveLayer = editor._lineMoveLayer;
			_editorRenderer = _editor.Renderer;
			_editor.Renderer.RendererUpdated += _editorRenderer_RendererUpdated;

			_initHeaderEvents();
		}

		private void _editorRenderer_RendererUpdated() {
			RenderLayerHeaders();
		}

		public void RenderLayerHeaders() {
			try {
				var str = _controller.Str;

				_layersGrid.RowDefinitions.Clear();

				foreach (var layerGrid in _layersGrid.Children.OfType<Grid>()) {
					((Border)layerGrid.Children[0]).Child = null;
					layerGrid.Children.Clear();
				}

				_layersGrid.Children.Clear();
				_visibleImages.Clear();

				for (int index = 0; index < str.Layers.Count; index++) {
					Grid grid = new Grid();
					Border border = new Border();
					TextBlock label = new TextBlock();
					_layersGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(_editor.KeyFrameHeight, GridUnitType.Pixel) });
					border.BorderThickness = new Thickness(0, 0, 1, 1);
					border.BorderBrush = Brushes.DimGray;
					border.Height = _editor.KeyFrameHeight;

					label.Text = "Layer " + index;
					label.Padding = new Thickness();
					label.Margin = new Thickness(3, 0, 0, 0);
					label.VerticalAlignment = VerticalAlignment.Center;
					grid.SetValue(Grid.RowProperty, index);
					grid.SetValue(Grid.ColumnProperty, 0);
					grid.Children.Add(border);
					border.Child = label;
					_layersGrid.Children.Add(grid);

					if (_editor.KeyFrameHeight >= 30) {
						label.FontSize = 12;
					}
					else if (_editor.KeyFrameHeight < 14) {
						label.FontSize = 8;
						label.Margin = new Thickness(3, -2, 0, 0);
					}
					else if (_editor.KeyFrameHeight < 20) {
						label.FontSize = 10;
					}

					if (_editor.KeyFrameHeight >= 18) {
						var indexCopy = index;
						var renderer = _frameViewer.GetLayerRenderer(index);

						Image image = new Image();
						image.Source = ApplicationManager.PreloadResourceImage(renderer == null || renderer.IsVisible ? "eye.png" : "eye_t.png");
						image.Margin = new Thickness(3, 0, 3, 0);
						image.VerticalAlignment = VerticalAlignment.Center;
						image.HorizontalAlignment = HorizontalAlignment.Right;
						image.Height = 16;
						image.Width = 16;
						grid.Children.Add(image);

						_visibleImages.Add(image);

						image.PreviewMouseLeftButtonDown += (sender, e) => {
							e.Handled = true;
							image.CaptureMouse();
						};

						image.MouseLeftButtonUp += (sender, e) => {
							ToggleVisibility(indexCopy);
							e.Handled = true;
							image.ReleaseMouseCapture();
						};
					}

					if (index == 0 && str[index].KeyFrames.Count == 0) {
						label.Visibility = Visibility.Collapsed;
						grid.Visibility = Visibility.Collapsed;

						_layersGrid.RowDefinitions[index].Height = new GridLength(0, GridUnitType.Pixel);
					}
				}

				_layersGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(_editor.KeyFrameHeight * 3, GridUnitType.Pixel) });
				_editor._gridMoveLayers.Height = _editor.KeyFrameHeight * _layersGrid.Children.Count + _editor.KeyFrameHeight * 3;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ToggleVisibility(int layerIndex) {
			try {
				var renderer = _frameViewer.GetLayerRenderer(layerIndex);

				if (renderer == null)
					return;

				renderer.IsVisible = !renderer.IsVisible;
				_visibleImages[layerIndex].Source = ApplicationManager.PreloadResourceImage(renderer.IsVisible ? "eye.png" : "eye_t.png");
				_controller.FrameViewer.Update();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void HideAllButThis(int layerIndex) {
			try {
				var renderer = _frameViewer.GetLayerRenderer(layerIndex);

				if (renderer == null)
					return;

				var layerRenderers = _controller.FrameViewer.LayerRenderers;

				layerRenderers.ForEach(p => p.IsVisible = false);
				layerRenderers[layerIndex].IsVisible = true;

				for (int i = 0; i < _visibleImages.Count && i < layerRenderers.Count; i++)
					_visibleImages[i].Source = ApplicationManager.PreloadResourceImage(layerRenderers[i].IsVisible ? "eye.png" : "eye_t.png");

				_controller.FrameViewer.Update();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ShowAll() {
			try {
				var layerRenderers = _controller.FrameViewer.LayerRenderers;

				layerRenderers.ForEach(p => p.IsVisible = true);

				for (int i = 0; i < _visibleImages.Count && i < layerRenderers.Count; i++)
					_visibleImages[i].Source = ApplicationManager.PreloadResourceImage(layerRenderers[i].IsVisible ? "eye.png" : "eye_t.png");

				_controller.FrameViewer.Update();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _initHeaderEvents() {
			_layersGrid.MouseRightButtonUp += _layersGrid_MouseRightButtonUp;
			_layersGrid.MouseLeftButtonDown += _layersGrid_MouseLeftButtonDown;
			_layersGrid.MouseMove += _layersGrid_MouseMove;
			_layersGrid.MouseLeftButtonUp += _layersGrid_MouseLeftButtonUp;
			_layersGrid.ContextMenu.Closed += _layersGrid_ContextMenu_Closed;
		}

		public void SetBackgroundBrush(int index, Brush brush) {
			if (index >= 0 && index < _layersGrid.Children.Count) {
				((Grid)_layersGrid.Children[index]).Background = Brushes.Transparent;
			}
		}

		private void _layersGrid_ContextMenu_Closed(object sender, RoutedEventArgs e) {
			try {
				SetBackgroundBrush(LastClickedLayer, Brushes.Transparent);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public int GetLayerIndexUnderMouse(MouseEventArgs e, bool isInsert = false) {
			var position = e.GetPosition(_layersGrid);

			int drawOffsetY = _editor.DrawYOffset;
			var y = position.Y < 0 ? 0 : position.Y;

			int layerIndex = (int)(y / _editor.KeyFrameHeight) + drawOffsetY;

			if (isInsert) {
				if (layerIndex < 0 || layerIndex > _controller.Str.Layers.Count)
					return -1;
			}
			else {
				if (layerIndex < 0 || layerIndex >= _controller.Str.Layers.Count)
					return -1;
			}

			return layerIndex;
		}

		private void _layersGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				var layerIndex = GetLayerIndexUnderMouse(e);

				if (layerIndex < 0)
					return;

				SetBackgroundBrush(LastClickedLayer, Brushes.Transparent);
				LastClickedLayer = layerIndex;
				SetBackgroundBrush(LastClickedLayer, _brushLayersGrid);
				_layersGrid.ContextMenu.IsOpen = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _layersGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				if (_layersGrid.IsMouseCaptured) {
					if (_destinationLayer < 0)
						return;

					SetBackgroundBrush(_layerMoving, Brushes.Transparent);

					_controller.Str.Commands.MoveLayer(_layerMoving, _destinationLayer);

					_layersGrid.ReleaseMouseCapture();
					_lineMoveLayer.Visibility = Visibility.Hidden;
					_layerMoving = -1;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _layersGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			try {
				_controller.PlayAnimation.Stop();

				if (!_layersGrid.IsMouseCaptured) {
					var layerIndex = GetLayerIndexUnderMouse(e);

					if (layerIndex < 0)
						return;

					_layerMoving = layerIndex;
					SetBackgroundBrush(layerIndex, _brushLayersGrid);
					_layersGrid.CaptureMouse();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _layersGrid_MouseMove(object sender, MouseEventArgs e) {
			try {
				if (_layersGrid.IsMouseCaptured) {
					var layerIndex = GetLayerIndexUnderMouse(e, true);

					if (layerIndex < 0)
						return;

					_destinationLayer = layerIndex;
					_lineMoveLayer.Margin = new Thickness(0, (layerIndex - _editor.DrawYOffset) * _editor.KeyFrameHeight - 1.5d, 0, 0);
					_lineMoveLayer.Visibility = Visibility.Visible;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#region Commands
		public bool IsValidLayerIndex(int layerIndex) {
			if (layerIndex == 0 && _editor.IsFirstLayerInvisible)
				return false;

			return layerIndex >= 0 && layerIndex <= _controller.Str.Layers.Count;
		}

		public void DuplicateLayer(int layerIndex) {
			try {
				if (!IsValidLayerIndex(layerIndex))
					return;

				var str = _controller.Str;
				str.Commands.InsertLayer(layerIndex + 1, new StrLayer(str.Layers[layerIndex]));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void PasteTextures(int layerIndexSrc, int layerIndexDst) {
			try {
				if (!IsValidLayerIndex(layerIndexSrc) || !IsValidLayerIndex(layerIndexDst) || layerIndexSrc == layerIndexDst)
					return;

				var str = _controller.Str;
				str.Commands.ChangeTextures(layerIndexDst, str.Layers[layerIndexSrc].TextureNames);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void DeleteLayer(int layerIndex) {
			try {
				if (!IsValidLayerIndex(layerIndex))
					return;

				var str = _controller.Str;
				str.Commands.DeleteLayer(layerIndex);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void InsertAbove(int layerIndex) {
			try {
				if (!IsValidLayerIndex(layerIndex))
					return;

				var str = _controller.Str;
				str.Commands.InsertLayer(layerIndex);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void InsertBelow(int layerIndex) {
			try {
				if (!IsValidLayerIndex(layerIndex))
					return;

				var str = _controller.Str;
				str.Commands.InsertLayer(layerIndex + 1);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		#endregion
	}
}
