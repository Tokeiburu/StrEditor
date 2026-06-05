using ErrorManager;
using GRF.Graphics;
using GRF.Threading;
using GrfToWpfBridge.Application;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Utilities;

namespace StrEditor.Core.GifExporter {
	public class GifEditViewModel : INotifyPropertyChanged {
		private readonly GifData _gifData;
		private readonly StrController _controller;
		public Point ViewportCenter;

		public RelayCommand ResetCommand { get; }
		public RelayCommand CancelCommand { get; }
		public RelayCommand SaveCommand { get; }

		public Visibility GifControlsVisibility => _gifData.IsGifMode ? Visibility.Visible : Visibility.Collapsed;

		public delegate void RequestEventHandler(object sender);
		public event RequestEventHandler RequestClosing;
		public void OnRequestClosing() => RequestClosing?.Invoke(this);

		public delegate void RequestSaveEventHandler(object sender, int width, int height);
		public event RequestSaveEventHandler RequestSave;
		public void OnRequestSave(int width, int height) => RequestSave?.Invoke(this, width, height);

		public GifEditViewModel(GifData gifData, StrController controller) {
			_gifData = gifData;
			_controller = controller;

			ResetCommand = new RelayCommand(ResetData, () => !_gifData.IsSaving);
			CancelCommand = new RelayCommand(Cancel);
			SaveCommand = new RelayCommand(Save, () => !_gifData.IsSaving);

			gifData.PointsChanged += _gifData_PointsChanged;
		}

		public string BoundsP1X {
			get => _gifData.Points[0].X.ToString("0.##", CultureInfo.InvariantCulture);
			set {
				if (!FormatConverters.SingleConverterTryParse(value, out float val))
					return;

				if (_gifData.Points[0].X == val)
					return;

				_gifData.Points[6] = new TkVector2(val, _gifData.Points[6].Y);
				_gifData.Points[0] = new TkVector2(val, _gifData.Points[0].Y);

				OnPropertyChanged(nameof(BoundsP1X));
				RecalculateMidpoints();
			}
		}

		public string BoundsP1Y {
			get => _gifData.Points[0].Y.ToString("0.##", CultureInfo.InvariantCulture);
			set {
				if (!FormatConverters.SingleConverterTryParse(value, out float val))
					return;

				if (_gifData.Points[0].Y == val)
					return;

				_gifData.Points[0] = new TkVector2(_gifData.Points[0].X, val);
				_gifData.Points[2] = new TkVector2(_gifData.Points[2].X, val);

				OnPropertyChanged(nameof(BoundsP1Y));
				RecalculateMidpoints();
			}
		}

		public string BoundsP2X {
			get => _gifData.Points[4].X.ToString("0.##", CultureInfo.InvariantCulture);
			set {
				if (!FormatConverters.SingleConverterTryParse(value, out float val))
					return;

				if (_gifData.Points[4].X == val)
					return;

				_gifData.Points[2] = new TkVector2(val, _gifData.Points[2].Y);
				_gifData.Points[4] = new TkVector2(val, _gifData.Points[4].Y);

				OnPropertyChanged(nameof(BoundsP2X));
				RecalculateMidpoints();
			}
		}

		public string BoundsP2Y {
			get => _gifData.Points[4].Y.ToString("0.##", CultureInfo.InvariantCulture);
			set {
				if (!FormatConverters.SingleConverterTryParse(value, out float val))
					return;

				if (_gifData.Points[4].Y == val)
					return;

				_gifData.Points[2] = new TkVector2(_gifData.Points[2].X, val);
				_gifData.Points[4] = new TkVector2(_gifData.Points[4].X, val);

				OnPropertyChanged(nameof(BoundsP2Y));
				RecalculateMidpoints();
			}
		}

		public string FrameIndexStart {
			get => _gifData.FrameIndexStart.ToString();
			set {
				if (!int.TryParse(value, out int val))
					return;

				if (val == _gifData.FrameIndexStart)
					return;

				_gifData.FrameIndexStart = val;

				OnPropertyChanged(nameof(FrameIndexStart));
			}
		}

		public string FrameIndexEnd {
			get => _gifData.FrameIndexEnd.ToString();
			set {
				if (!int.TryParse(value, out int val))
					return;

				if (val == _gifData.FrameIndexEnd)
					return;

				_gifData.FrameIndexEnd = val;

				OnPropertyChanged(nameof(FrameIndexEnd));
			}
		}

		public string Fps {
			get => _gifData.Fps.ToString();
			set {
				if (!int.TryParse(value, out int val))
					return;

				if (val == _gifData.Fps)
					return;

				_gifData.Fps = val;

				OnPropertyChanged(nameof(Fps));
			}
		}

		public string OutputPath {
			get => StrEditorConfiguration.GifSavePath;
			set {
				if (StrEditorConfiguration.GifSavePath == value)
					return;

				StrEditorConfiguration.GifSavePath = value;
				OnPropertyChanged(nameof(OutputPath));
			}
		}

		public bool AllowSkipFrames {
			get => StrEditorConfiguration.AllowSkipGifFrames;
			set {
				if (StrEditorConfiguration.AllowSkipGifFrames == value)
					return;

				StrEditorConfiguration.AllowSkipGifFrames = value;
				OnPropertyChanged(nameof(AllowSkipFrames));
			}
		}

		private bool _canCancel = true;
		public bool IsCancelEnabled {
			get {
				if (_gifData.IsSaving) {
					return _canCancel;
				}

				return true;
			}
			set {
				if (_canCancel == value)
					return;

				_canCancel = value;
				OnPropertyChanged(nameof(IsCancelEnabled));
			}
		}

		public bool IsSaving {
			get => _gifData.IsSaving;
			set {
				if (_gifData.IsSaving == value)
					return;

				_gifData.IsSaving = value;

				Application.Current.Dispatcher.Invoke(() => {
					SaveCommand.RaiseCanExecuteChanged();
					ResetCommand.RaiseCanExecuteChanged();
					OnPropertyChanged(nameof(IsSaving));
					OnPropertyChanged(nameof(IsCancelEnabled));
				});
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		public void RecalculateMidpoints() {
			_gifData.Points[1] = new TkVector2((_gifData.Points[0].X + _gifData.Points[2].X) / 2, _gifData.Points[0].Y);
			_gifData.Points[3] = new TkVector2(_gifData.Points[2].X, (_gifData.Points[2].Y + _gifData.Points[4].Y) / 2);
			_gifData.Points[5] = new TkVector2((_gifData.Points[0].X + _gifData.Points[2].X) / 2, _gifData.Points[4].Y);
			_gifData.Points[7] = new TkVector2(_gifData.Points[0].X, (_gifData.Points[0].Y + _gifData.Points[6].Y) / 2);

			_controller.FrameViewer.Update();
		}

		public void ResetData() {
			_gifData.CalculatePoints();

			_gifData.FrameIndexStart = 0;
			_gifData.FrameIndexEnd = _controller.Str.MaxKeyFrame;

			OnPropertyChanged(nameof(FrameIndexStart));
			OnPropertyChanged(nameof(FrameIndexEnd));
			OnPropertyChanged(nameof(BoundsP1X));
			OnPropertyChanged(nameof(BoundsP1Y));
			OnPropertyChanged(nameof(BoundsP2X));
			OnPropertyChanged(nameof(BoundsP2Y));

			_controller.FrameViewer.Update();
		}

		public void Setup() {
			_gifData.Fps = _controller.Str.Fps;
			OnPropertyChanged(nameof(Fps));

			ResetData();

			OnPropertyChanged(nameof(GifControlsVisibility));
		}

		private void _gifData_PointsChanged(object sender) {
			OnPropertyChanged(nameof(BoundsP1X));
			OnPropertyChanged(nameof(BoundsP1Y));
			OnPropertyChanged(nameof(BoundsP2X));
			OnPropertyChanged(nameof(BoundsP2Y));
		}

		public void ValidateFrameIndexes() {
			var str = _controller.Str;

			if (_gifData.FrameIndexStart < 0)
				_gifData.FrameIndexStart = 0;

			if (_gifData.FrameIndexEnd > str.KeyFrameCount)
				_gifData.FrameIndexEnd = str.KeyFrameCount - 1;

			if (_gifData.FrameIndexStart > _gifData.FrameIndexEnd) {
				_gifData.FrameIndexStart = 0;
				_gifData.FrameIndexEnd = str.KeyFrameCount - 1;
			}

			OnPropertyChanged(nameof(FrameIndexStart));
			OnPropertyChanged(nameof(FrameIndexEnd));
		}

		public void Save() {
			try {
				if (_gifData.IsPngMode) {
					_gifData.PngPath = PathRequest.FolderExtract();

					if (_gifData.PngPath == null)
						return;
				}

				ViewportCenter = _controller.FrameViewer.RelativeCenter;

				var centerOffset = _gifData.CenterOffset;
				var width = _gifData.BoundsWidth;
				var height = _gifData.BoundsHeight;

				// Convert to relative position
				centerOffset.X = -centerOffset.X / width + 0.5f;
				centerOffset.Y = centerOffset.Y / height + 0.5f;

				_controller.FrameViewer.RelativeCenter = new Point(centerOffset.X, centerOffset.Y);

				width = (int)(_controller.FrameViewer.ZoomEngine.Scale * width) + 1;
				height = (int)(_controller.FrameViewer.ZoomEngine.Scale * height) + 1;

				if (width < 0 || height < 0) {
					ErrorHandler.HandleException("The width or height cannot be below 0 for the recording gif.");
					return;
				}

				if (OutputPath == "")
					throw new Exception("Invalid destination path.");

				ValidateFrameIndexes();

				OnRequestSave(width, height);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Cancel() {
			OnRequestClosing();
		}
	}
}
