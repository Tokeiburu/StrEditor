using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using Gif.Components;
using GRF.FileFormats;
using GRF.Graphics;
using GRF.Image;
using GRF.IO;
using GRF.GrfSystem;
using GRF.Threading;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using OpenTK.Graphics.OpenGL;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core;
using TokeiLibrary;
using TokeiLibrary.Paths;
using Utilities;
using Color = System.Windows.Media.Color;
using Image = System.Drawing.Image;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using Point = System.Windows.Point;
using GRF.Image.Decoders;
using System.Drawing.Imaging;
using StrEditor.Core.StrConverters;
using StrEditor.Core.GifExporter;

namespace StrEditor.Core.GifExporter {
	/// <summary>
	/// Interaction logic for GifSettings.xaml
	/// </summary>
	public partial class GifEditControl : UserControl, IProgress {
		private StrController _controller;
		private GifData _gifData;
		private readonly AsyncOperation _asyncOperation;
		private GifEditViewModel _gifEditViewModel;

		public GifEditControl() {
			InitializeComponent();

			_asyncOperation = new AsyncOperation(_progressBar);

			_colorBackground.Color = StrEditorConfiguration.GifBackground;
			_colorBackground.Init(StrEditorConfiguration.ConfigAsker.RetrieveSetting(() => StrEditorConfiguration.GifBackground));
			
			_colorBackground.ColorChanged += delegate(object sender, Color value) {
				StrEditorConfiguration.GifBackgroundQuick.Set(new OpenTK.Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f));
				_controller.FrameViewer.QuickUpdate();
			};
			
			_colorBackground.PreviewColorChanged += delegate(object sender, Color value) {
				StrEditorConfiguration.GifBackgroundQuick.Set(new OpenTK.Vector4(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f));
				_controller.FrameViewer.QuickUpdate();
			};
		}

		public void Hide() {
			_gifEditViewModel.IsCancelEnabled = true;
			_gifEditViewModel.IsSaving = false;
			_gifData.IsGifMode = false;
			this.Dispatch(p => p.Visibility = Visibility.Collapsed);
			this.Dispatch(p => _controller.FrameViewer.Update());
		}

		public void Show() {
			this.Visibility = Visibility.Visible;

			_gifEditViewModel.Setup();
		}

		public virtual void InitComponent(StrController controller) {
			if (controller.GifData == null)
				controller.GifData = new GifData(controller);

			_controller = controller;
			_gifData = controller.GifData;
			_gifEditViewModel = new GifEditViewModel(controller.GifData, controller);
			DataContext = _gifEditViewModel;

			_gifEditViewModel.RequestClosing += _gifEditViewModel_RequestClosing;
			_gifEditViewModel.RequestSave += _gifEditViewModel_RequestSave;
		}

		private void _gifEditViewModel_RequestSave(object sender, int width, int height) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _saveGif(width, height), this));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _saveGif(int width, int height) {
			var primary = _controller.FrameViewer._primary;
			List<string> paths = new List<string>();
			Progress = -1;

			var old = StrEditorConfiguration.GifBackgroundQuick.Get();

			try {
				if (_gifData.IsPngMode)
					this.Dispatch(p => StrEditorConfiguration.GifBackgroundQuick.Set(new OpenTK.Vector4(0, 0, 0, 1)));

				_gifEditViewModel.IsSaving = true;

				_controller.FrameViewer.Dispatch(p => {
					p._primary.Width = width;
					p._primary.Height = height;
				});

				int startIndex = _gifData.FrameIndexStart;
				int endIndex = _gifData.FrameIndexEnd;
				int keyFramesCount = _controller.Str.KeyFrameCount;
				int totalFrames = endIndex - startIndex;

				if (_gifData.IsPngMode) {
					for (int k = 0; k < 2; k++) {
						int k2 = k;

						this.Dispatch(p => StrEditorConfiguration.GifBackgroundQuick.Set(k2 == 0 ? new OpenTK.Vector4(0, 0, 0, 1) : new OpenTK.Vector4(1, 1, 1, 1)));

						for (int i = startIndex; i <= endIndex; i++) {
							int frameIndex = i;

							_controller.TimelineEditor.Dispatch(delegate {
								_controller.TimelineEditor.TimelineFrameIndexNoEvent = frameIndex;
								_controller.FrameViewer.ForceUpdate();
							});

							string path = TemporaryFilesManager.GetTemporaryFilePath("gif_{0:0000}.bmp");
							var image = _gifData.Bitmap;
							image.Save(path, ImageFormat.Bmp);

							Progress = ((float)((i - startIndex + 1) + k * totalFrames) / (totalFrames * 2)) * 50f;
							paths.Add(path);

							AProgress.IsCancelling(this);
						}
					}
				}
				else {
					for (int i = startIndex; i <= endIndex; i++) {
						int frameIndex = i;

						_controller.TimelineEditor.Dispatch(delegate {
							_controller.TimelineEditor.TimelineFrameIndexNoEvent = frameIndex;
							_controller.FrameViewer.ForceUpdate();
						});

						string path = TemporaryFilesManager.GetTemporaryFilePath("gif_{0:0000}.bmp");
						var image = _gifData.Bitmap;
						image.Save(path);

						Progress = ((float)(i - startIndex + 1) / totalFrames) * 50f;
						paths.Add(path);

						AProgress.IsCancelling(this);
					}
				}

				_controller.FrameViewer.RelativeCenter = _gifEditViewModel.ViewportCenter;

				_controller.FrameViewer.Dispatch(p => {
					primary.Width = (int)_controller.FrameViewer.ActualWidth;
					primary.Height = (int)_controller.FrameViewer.ActualHeight;
					_controller.FrameViewer.QuickUpdate();
				});

				if (_gifData.IsPngMode) {
					int count = paths.Count / 2;

					for (int index = 0; index < count; index++) {
						GrfImage black = paths[index];
						GrfImage white = paths[index + count];

						Progress = ((float)(index + 1) / count) * 50f + 50f;
						GrfImage result = GrfImageAnalysis.CreateTransparencyImage(black, white);
						result.Save(GrfPath.Combine(_gifData.PngPath, String.Format("img_{0:0000}.png", index + startIndex)));

						AProgress.IsCancelling(this);
					}
				}
				else {
					int fps = _gifData.Fps;

					if (fps < 10 || fps > 500)
						fps = 60;

					int delay = (int)Math.Ceiling((1000 / fps) / 10d);

					if (delay <= 1)
						delay = 2;

					GrfPath.Delete(_gifEditViewModel.OutputPath);

					var outputGifPath = _gifEditViewModel.OutputPath;

					double realInterval = 1000d / fps;
					double outputInterval = delay * 10d;

					double timeOriginal = 0;
					double timeOutput = 0;
					bool canSkipFrames = StrEditorConfiguration.AllowSkipGifFrames;

					using (var fs = new FileStream(outputGifPath, FileMode.Create))
					using (var muxer = new CustomPaletteGifMuxer(fs, delay * 10, 0)) {
						for (int index = 0; index < paths.Count; index++) {
							timeOriginal = index * 1000d / fps;

							if (canSkipFrames && index != paths.Count - 1 && timeOriginal < timeOutput) {
								continue;
							}

							var path = paths[index];
							GrfImage grfImage = new GrfImage(path);
							grfImage.Convert(GrfImageType.Indexed8);

							Image image;

							using (MemoryStream m = new MemoryStream()) {
								grfImage.Save(m);
								image = Image.FromStream(m);
								muxer.AddFrame(image);
							}

							Progress = ((float)(index + 1) / paths.Count) * 50f + 50f;

							AProgress.IsCancelling(this);

							timeOutput += outputInterval;
						}
					}
				}
			}
			catch (OperationCanceledException) {
				IsCancelled = true;

				if (Progress < 50f) {
					_controller.FrameViewer.RelativeCenter = _gifEditViewModel.ViewportCenter;

					_controller.FrameViewer.Dispatch(p => {
						primary.Width = (int)_controller.FrameViewer.ActualWidth;
						primary.Height = (int)_controller.FrameViewer.ActualHeight;
						_controller.FrameViewer.QuickUpdate();
					});
				}
			}
			catch (Exception err) {
				_controller.FrameViewer.RelativeCenter = _gifEditViewModel.ViewportCenter;
				_gifEditViewModel.IsSaving = false;
				ErrorHandler.HandleException(err);
			}
			finally {
				_gifEditViewModel.IsSaving = false;
				_gifEditViewModel.IsCancelEnabled = true;
				Progress = 100;

				this.Dispatch(delegate {
					StrEditorConfiguration.GifBackgroundQuick.Set(old);
				});

				TemporaryFilesManager.ClearTemporaryFiles();
			}
		}

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		private void _gifEditViewModel_RequestClosing(object sender) {
			if (_asyncOperation.IsRunning) {
				_asyncOperation.Cancel();
				_gifEditViewModel.IsCancelEnabled = false;
			}
			else {
				Hide();
			}
		}

		private void _tbOutput_PreviewDragOver(object sender, DragEventArgs e) {
			e.Handled = true;
		}

		private void _tbOutput_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _tbOutput_Drop(object sender, DragEventArgs e) {
			try {
				if (!e.Data.GetDataPresent(DataFormats.FileDrop, true))
					return;
				string[] strArray = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				if (strArray == null || strArray.Length < 1)
					return;

				_gifEditViewModel.OutputPath = strArray[0];
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _tbSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string file = TkPathRequest.SaveFile(new Setting(null, typeof(StrEditorConfiguration).GetProperty("GifSavePath")),
					"filter", FileFormat.MergeFilters(FileFormat.Gif),
					"fileName", Path.GetFileName(_gifEditViewModel.OutputPath));

				if (file != null) {
					_gifEditViewModel.OutputPath = file;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err.Message, ErrorLevel.Warning);
			}
		}

		private void _tbSaveAs_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _tbSaveAs_Drop(object sender, DragEventArgs e) {
			try {
				if (!e.Data.GetDataPresent(DataFormats.FileDrop, true))
					return;
				string[] strArray = e.Data.GetData(DataFormats.FileDrop, true) as string[];
				if (strArray == null || strArray.Length < 1)
					return;

				_gifEditViewModel.OutputPath = strArray[0];
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonSelectOutput_Click(object sender, RoutedEventArgs e) {
			try {
				Utilities.Services.OpeningService.FileOrFolder(_gifEditViewModel.OutputPath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
