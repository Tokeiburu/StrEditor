using ErrorManager;
using GRF.FileFormats.StrFormat;
using StrEditor.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace StrEditor.Core.Viewport {
	public class FrameViewerController {
		private StrController _controller;
		private ComboBox _cbZoom;

		public FrameViewerController(StrController controller) {
			_controller = controller;

			_cbZoom = _controller.StrEditorWindow._cbZoom;

			_cbZoom.SelectionChanged += _cbZoom_SelectionChanged;
			_cbZoom.PreviewKeyDown += _cbZoom_PreviewKeyDown;
		}

		public void MergeStr(string path) {
			try {
				if (path == null) {
					return;
				}

				Str str = new Str(path);
				str.ConvertInterpolatedFrames();

				if (StrEditorConfiguration.AttemptReconstrustBias) {
					str.DetectInterpolatedFrames();
				}

				var layers = str.Layers.Where(p => p.KeyFrames.Count > 0).ToList();

				_controller.Str.Commands.BeginNoDelay();

				foreach (var layer in layers) {
					_controller.Str.Commands.InsertLayer(_controller.Str.Layers.Count, layer);
				}

				_controller.Str.Commands.End();
				_controller.Str.InvalidateVisualRedraw();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _cbZoom_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_controller == null || _cbZoom.SelectedIndex < 0) return;

			_controller.FrameViewer.ZoomEngine.SetZoom(double.Parse(((string)((ComboBoxItem)_cbZoom.SelectedItem).Content).Replace(" %", "")) / 100f);
			_cbZoom.Text = _controller.FrameViewer.ZoomEngine.ScaleText;
			_controller.FrameViewer.Update();
		}

		private void _cbZoom_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				try {
					string text = _cbZoom.Text;

					text = text.Replace(" ", "").Replace("%", "");
					_cbZoom.SelectedIndex = -1;

					double value = double.Parse(text);

					_controller.FrameViewer.ZoomEngine.SetZoom(value / 100f);
					_cbZoom.Text = _controller.FrameViewer.ZoomEngine.ScaleText;
					_controller.FrameViewer.Update();
					e.Handled = true;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}
	}
}
