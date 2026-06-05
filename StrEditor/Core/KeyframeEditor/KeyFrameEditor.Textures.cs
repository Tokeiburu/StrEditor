using GRF.FileFormats;
using GRF.FileFormats.StrFormat;
using GRF.Image;
using GRF.IO;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core.OpenGLComponents;
using StrEditor.WPF;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TokeiLibrary;
using Utilities.Extension;

namespace StrEditor.Core.KeyframeEditor {
	public class TextureView {
		public string ResourcePath;
		private BitmapSource _cachedImage;

		public string DisplayName { get; set; }
		public BitmapSource Image {
			get {
				if (_cachedImage == null) {
					var data = ResourceManager.GetData(ResourcePath);

					if (data != null) {
						_cachedImage = new GrfImage(data).Cast<BitmapSource>();
					}
				}

				return _cachedImage;
			}
		}
	}

	public partial class KeyFrameEditor {
		private void _buttonEditTextures_Click(object sender, RoutedEventArgs e) {
			TexturesEdit dialog = new TexturesEdit(_str, _controller.TimelineEditor.SelectedLayerIndex);
			dialog.Owner = WpfUtilities.TopWindow;

			if (dialog.ShowDialog() == true) {
				UpdateTextures(_str.Layers[_controller.TimelineEditor.SelectedLayerIndex], true);
			}
		}

		private void _gridEvents_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _gridEvents_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
				string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

				if (files != null && files.Length > 0) {
					AddTextures(files);
				}
			}
		}

		public void UpdateTextures(StrLayer layer, bool setSelection) {
			bool isLoading = _isLoading;

			try {
				_isLoading = true;
				List<string> textures = new List<string>();
				textures.AddRange(layer.TextureNames);
				textures.Add("Add new...");
				bool changed = true;

				if (textures.Count == _selectedTexture.Items.Count) {
					changed = false;

					List<TextureView> texturesSource = (List<TextureView>)_selectedTexture.ItemsSource;

					for (int i = 0; i < textures.Count; i++) {
						if (texturesSource[i].ResourcePath != textures[i]) {
							changed = true;
							break;
						}
					}
				}

				if (changed) {
					_selectedTexture.ItemsSource = textures.Select(p => new TextureView() { DisplayName = p, ResourcePath = GrfPath.Combine(Path.GetDirectoryName(_str.LoadedPath), p) }).ToList();
				}

				if (setSelection) {
					if (layer.TextureNames.Count == 0)
						_selectedTexture.SelectedIndex = -1;
					else {
						//if (!_currentFrame.Interpolated)
							_selectedTexture.SelectedIndex = _currentFrame.TextureIndex;
						//else
						//	_selectedTexture.SelectedIndex = (int)layer[_currentFrame.KeyIndex].TextureIndex;
					}
				}
			}
			finally {
				_isLoading = isLoading;
			}
		}

		private void _selectedTexture_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_isLoading || _fieldEditing)
				return;

			if (_selectedTexture.SelectedIndex == _selectedTexture.Items.Count - 1) {
				string[] paths = PathRequest.OpenFileStr("filter", FileFormat.MergeFilters(FileFormat.Image), "initialDirectory", Path.GetDirectoryName(_str.LoadedPath));

				if (paths != null && paths.Length > 0) {
					AddTextures(paths);
				}
				else {
					UpdateTextures(_str.Layers[_currentFrame.LayerIdx], true);
				}
			}
			else {
				ApplyCommand((lidx, kidx) => {
					int textureIndex = 0;

					if (_selectedTexture.SelectedIndex > 0)
						textureIndex = _selectedTexture.SelectedIndex;

					_str.Commands.ChangeTextureIndex(lidx, kidx, textureIndex);
				}, false);
			}
		}

		public void AddTextures(string[] inputPaths) {
			var paths = inputPaths.Where(p => p.IsExtension(".bmp", ".png", ".tga", ".jpg")).ToList();

			if (paths.Count == 0)
				return;

			ApplyCommand((lidx, kidx) => {
				var textures = new List<string>();
				int textureIndex = _str.Layers[lidx].TextureNames.Count;
				var hasNoTextures = textureIndex == 0;

				textures.AddRange(_str.Layers[lidx].TextureNames);
				textures.AddRange(paths.Select(Path.GetFileName));

				_fieldEditing = true;
				_str.Commands.BeginNoDelay();
				_str.Commands.ChangeTextures(lidx, textures);
				_str.Commands.ChangeTextureIndex(lidx, kidx, textureIndex);
				UpdateTextures(_str.Layers[lidx], true);

				if (hasNoTextures) {
					_setVerticesToImageSize(lidx, kidx, new GrfImage(paths[0]));
				}
			}, false);
		}

		private void _setVerticesToImageSize(int lidx, int kidx, GrfImage image) {
			try {
				float[] vertices = {
					-(image.Width / 2),
					(image.Width / 2) + (image.Width % 2),
					(image.Width / 2) + (image.Width % 2),
					-(image.Width / 2),
					-(image.Height / 2),
					-(image.Height / 2),
					(image.Height / 2) + (image.Height % 2),
					(image.Height / 2) + (image.Height % 2),
				};

				_str.Commands.SetVertices(lidx, kidx, vertices);
			}
			catch {
			}
		}
	}
}
