using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ColorPicker.Sliders;
using ErrorManager;
using GRF.FileFormats.StrFormat;
using GRF.Graphics;
using StrEditor.ApplicationConfiguration;
using StrEditor.Core;
using StrEditor.Core.Viewport.Renderers;
using StrEditor.Core.Viewport;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using StrEditor.WPF;
using System.Security.RightsManagement;

namespace StrEditor.Core.KeyframeEditor {
	/// <summary>
	/// Interaction logic for KeyFrameEditor.xaml
	/// </summary>
	public partial class KeyFrameEditor : UserControl {
		private InterpolatedKeyFrame _currentFrame;
		private InterpolatedKeyFrame _copyFrame;
		private bool _isLoading;
		private bool _fieldEditing;
		private Str _str;
		private TextBox[] _tbVertices;
		private TextBox[] _tbTextCoords;
		private TextBox[] _tbBezier;
		private StrController _controller;
		private UpdateDispatcher _upKeyFrameLoad = new UpdateDispatcher(100);
		private UpdateDispatcher _upKeyRefreshField = new UpdateDispatcher(50);

		const int _sliderRange = 40;
		const int _sliderMid = _sliderRange / 2;

		public bool CanEdit => _currentFrame != null;
		public bool HasCopyFrame => _copyFrame != null;
		public InterpolatedKeyFrame CopyFrame => _copyFrame;
		public InterpolatedKeyFrame CurrentFrame => _currentFrame;

		public KeyFrameEditor() {
			InitializeComponent();

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			_initializeTextBoxReferences();

			_qcsKeyFrameColor.PreviewColorChanged += _qcsKeyFrameColor_PreviewColorChanged;
			SetUvVisible();
		}

		private void _initializeTextBoxReferences() {
			_tbVertices = new TextBox[8];
			_tbVertices[0] = _tbCoordsTLX;
			_tbVertices[4] = _tbCoordsTLY;
			_tbVertices[1] = _tbCoordsTRX;
			_tbVertices[5] = _tbCoordsTRY;
			_tbVertices[2] = _tbCoordsBRX;
			_tbVertices[6] = _tbCoordsBRY;
			_tbVertices[3] = _tbCoordsBLX;
			_tbVertices[7] = _tbCoordsBLY;

			_tbTextCoords = new TextBox[4];
			_tbTextCoords[0] = _tbUVTLX;
			_tbTextCoords[1] = _tbUVTLY;
			_tbTextCoords[2] = _tbUVTRX;
			_tbTextCoords[3] = _tbUVTRY;

			_tbBezier = new TextBox[4];
			_tbBezier[0] = _tbBezierP1X;
			_tbBezier[1] = _tbBezierP1Y;
			_tbBezier[2] = _tbBezierP2X;
			_tbBezier[3] = _tbBezierP2Y;
		}

		private LayerRenderer _currentEditLayer;
		private InteractionManager _interactionManager;
		private List<UIElement> _layerUIElements;

		private void _buttonEditMaxFrames_Click(object sender, RoutedEventArgs e) {
			try {
				InputDialog dialog = new InputDialog("Change the max frame count", "Edit Max Frame", _tbMaxFrames.Text);
				dialog.Owner = WpfUtilities.TopWindow;

				if (dialog.ShowDialog() == true) {
					int maxFrames = FormatConverters.IntOrHexConverter(dialog.Input);

					if (maxFrames <= 0) {
						ErrorHandler.HandleException("You need at least 1 key frame.");
						return;
					}

					_controller.TimelineEditor.Commands.SetMaxKeyFrameCount(maxFrames);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _tbFps_TextChanged(object sender, TextChangedEventArgs e) {
			if (_isLoading || _fieldEditing)
				return;

			var fps = FormatConverters.IntOrHexConverter(_tbFps.Text);

			if (fps <= 0)
				return;

			try {
				_str.Commands.ChangeFps(fps);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void DisableEvents() => _isLoading = true;
		public void EnableEvents() => _isLoading = false;

		private void _qcsKeyFrameColor_PreviewColorChanged(object sender, Color value) {
			if (_isLoading || _fieldEditing)
				return;

			if (_currentFrame.Interpolated)
				return;

			var layer = _str[_currentFrame.LayerIdx, _currentFrame.KeyIndex];
			layer.Color[0] = value.R;
			layer.Color[1] = value.G;
			layer.Color[2] = value.B;
			layer.Color[3] = value.A;
			_str.InvalidateVisualRedraw();
		}

		public void Init(Str str) {
			_str = str;

			_tbMaxFrames.Text = _str.MaxKeyFrame.ToString(CultureInfo.InvariantCulture);

			bool old = _isLoading;
			_isLoading = true;
			_tbFps.Text = _str.Fps.ToString(CultureInfo.InvariantCulture);
			_isLoading = old;
		}

		public void AsyncUpdate(InterpolatedKeyFrame inter) {
			_controller.PlayAnimation.Stop();
			
			if (_fieldEditing)
				return;

			var layer = inter == null ? null : _str.Layers[inter.LayerIdx];
			_upKeyFrameLoad.Execute(() => Dispatcher.BeginInvoke(new Action(() => Update(layer, inter)), DispatcherPriority.Render));
		}

		public void Update(StrLayer layer, InterpolatedKeyFrame inter) {
			try {
				if (_controller == null)
					return;

				_controller.PlayAnimation.Stop();

				if (_isLoading)
					return;

				if (inter == null) {
					_disableUI();
					return;
				}

				_loadKeyFrame(layer, inter);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _loadKeyFrame(StrLayer layer, InterpolatedKeyFrame inter) {
			_currentFrame = inter;

			try {
				_isLoading = true;

				_layerUIElements?.ForEach(p => p.IsEnabled = true);

				SetAngle(inter.Angle);
				SetColor(inter.Color);
				SetOffsetX(inter.Offset.X);
				SetOffsetY(inter.Offset.Y);

				_cbSrc.SelectedIndex = inter.SourceAlpha - 1;
				_cbDst.SelectedIndex = inter.DestinationAlpha - 1;

				UpdateTextures(layer, true);

				SetVertices(inter.Vertices);
				SetTextCoords(inter.TextCoords);
				SetBezier(inter.Bezier);

				var keyFrame = layer[inter.KeyIndex];
				SetScaleBias(keyFrame.ScaleBias);
				SetOffsetBias(keyFrame.OffsetBias);
				SetAngleBias(keyFrame.AngleBias);
				UpdateScale(_currentFrame.Vertices);

				_isInterpolated.IsChecked = keyFrame.IsInterpolated;
				SetFrameIndex(inter.FrameIndex);

				_cbAnimations.SelectedIndex = inter.AnimationType;

				if (inter.Delay == 0) {
					_tbDelay.Text = "";
				}
				else {
					_tbDelay.Text = (1f / inter.Delay).ToString("0.##");
				}
			}
			finally {
				_isLoading = false;
			}
		}

		private void _disableUI() {
			try {
				_isLoading = true;

				_layerUIElements?.ForEach(p => p.IsEnabled = false);

				SetAngle(0);
				SetColor(new float[4]);
				SetOffsetX(Str.OffsetX);
				SetOffsetY(Str.OffsetY);

				_cbSrc.SelectedIndex = 0;
				_cbDst.SelectedIndex = 0;

				_selectedTexture.SelectedIndex = -1;

				var vertices = new float[8];

				SetVertices(vertices);
				SetTextCoords(vertices);
				SetBezier(vertices);
				SetFrameIndex(_controller.TimelineEditor.TimelineFrameIndex);
				SetScale(0, 0);

				_cbAnimations.SelectedIndex = 0;
				_tbDelay.Text = "";

				SetScaleBias(0);
				SetOffsetBias(0);
				SetAngleBias(0);
			}
			finally {
				_isLoading = false;
			}
		}

		private void _setSlider(SliderColor slider, float bias) {
			bias = bias + _sliderMid;
			slider.SetPosition(bias / _sliderRange, true);
		}

		public void _execute(Action<KeyFrameEditor> action) {
			bool old = _isLoading;

			try {
				DisableEvents();
				action(this);
			}
			finally {
				EnableEvents();
			}

			_isLoading = old;
		}

		public void Execute(Action<KeyFrameEditor> action) {
			_controller.PlayAnimation.Stop();
			//_upKeyRefreshField.Execute(() => this.Dispatch(() => Execute(action)));
			_upKeyRefreshField.Execute(() => Dispatcher.BeginInvoke(new Action(() => _execute(action)), DispatcherPriority.Render));
		}

		public void InitComponent(StrController controller) {
			_controller = controller;

			_interactionManager = _controller.FrameViewer.InteractionManager;

			_initLabelEvents();

			Loaded += delegate {
				var elements = FindChildren<UIElement>(this);
				elements.Remove(_buttonEditMaxFrames);
				elements.Remove(_buttonEditTextures);
				elements.Remove(_tbFps);
				elements.Remove(_tbMaxFrames);
				_layerUIElements = elements;
			};

			_controller.TimelineEditor.TimelineFrameIndexChanged += _timelineEditor_TimelineFrameIndexChanged;
			_controller.FrameViewer.KeyDown += _frameViewer_KeyDown;
			_controller.FrameViewer.KeyUp += _frameViewer_KeyUp; ;
		}

		private void _timelineEditor_TimelineFrameIndexChanged() {
			this.Dispatch(delegate {
				SetFrameIndex(_controller.TimelineEditor.TimelineFrameIndex);
			});
		}

		public List<T> FindChildren<T>(DependencyObject parent, List<T> children = null) where T : DependencyObject {
			if (children == null)
				children = new List<T>();

			if (parent == null)
				return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
				var child = VisualTreeHelper.GetChild(parent, i);

				if (child is T typed &&
					(child is SliderColor ||
					child is TextBox ||
					child is CheckBox ||
					child is QuickColorSelector ||
					child is ComboBox)) {
					children.Add(typed);
				}

				FindChildren(child, children);
			}

			return children;
		}

		public void SetUvVisible() {
			Visibility visibility = StrEditorConfiguration.ShowUvCoords ? Visibility.Visible : Visibility.Collapsed;

			_labelUvSpacer.Visibility = visibility;
			_labelUvP1.Visibility = visibility;
			_dockPanelUvT.Visibility = visibility;
		}

		#region Manual field setters
		public void SetOffsetX(float x) {
			_tbPosX.Text = (x - Str.OffsetX).ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void SetOffsetY(float y) {
			_tbPosY.Text = (y - Str.OffsetY).ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void SetOffsets(TkVector2 p) {
			SetOffsetX(p.X);
			SetOffsetY(p.Y);
		}

		public void SetAngle(float angle) {
			_tbAngle.Text = angle.ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void SetColor(float[] color) {
			_tbColorR.Text = color[0].ToString("0.##", CultureInfo.InvariantCulture);
			_tbColorG.Text = color[1].ToString("0.##", CultureInfo.InvariantCulture);
			_tbColorB.Text = color[2].ToString("0.##", CultureInfo.InvariantCulture);
			_tbColorA.Text = color[3].ToString("0.##", CultureInfo.InvariantCulture);
			_qcsKeyFrameColor.Color = Color.FromArgb((byte)color[3], (byte)color[0], (byte)color[1], (byte)color[2]);
		}

		public void SetVertices(float[] vertices) {
			for (int i = 0; i < 4; i++) {
				_tbVertices[i].Text = vertices[i].ToString("0.##", CultureInfo.InvariantCulture);
				_tbVertices[i + 4].Text = (-vertices[i + 4]).ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public void SetTextCoords(float[] vertices) {
			if (!StrEditorConfiguration.ShowUvCoords)
				return;

			for (int i = 0; i < 4; i++) {
				_tbTextCoords[i].Text = vertices[i].ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public void SetBezier(float[] vertices) {
			for (int i = 0; i < 4; i++) {
				_tbBezier[i].Text = vertices[i].ToString("0.##", CultureInfo.InvariantCulture);
			}
		}

		public void SetFrameIndex(int index) {
			_tbFrameIndex.Text = index.ToString(CultureInfo.InvariantCulture);
		}

		public void SetScaleBias(float bias) {
			_tbScaleBias.Text = bias.ToString(CultureInfo.InvariantCulture);
			_setSlider(_sliderScale, bias);
		}

		public void SetOffsetBias(float bias) {
			_tbOffsetBias.Text = bias.ToString(CultureInfo.InvariantCulture);
			_setSlider(_sliderOffset, bias);
		}

		public void SetAngleBias(float bias) {
			_tbAngleBias.Text = bias.ToString(CultureInfo.InvariantCulture);
			_setSlider(_sliderAngle, bias);
		}

		public void SetScale(float sx, float sy) {
			_tbScaleX.Text = sx.ToString("0.##", CultureInfo.InvariantCulture);
			_tbScaleY.Text = sy.ToString("0.##", CultureInfo.InvariantCulture);
		}

		public void UpdateScale(float[] vertices) {
			var renderer = _controller.FrameViewer.GetSelectedRenderer();

			if (renderer == null) {
				SetScale(1, 1);
				return;
			}

			for (int i = 0; i < 8; i++)
				_originalVertices[i] = _currentFrame.Vertices[i];

			var scale = _getFrameScale(renderer, true);
			SetScale(scale.X, scale.Y);
		}
		#endregion

		public void InvalidateKeyFrame() {
			var inter = InterpolatedKeyFrame.Interpolate(_controller.Str, _controller.TimelineEditor.SelectedLayerIndex, _controller.TimelineEditor.SelectedFrameIndex);

			AsyncUpdate(inter);
			_controller.FrameViewer.Update();
		}

		public void Copy() {
			if (_currentFrame == null) {
				_copyFrame = null;
				return;
			}

			_copyFrame = InterpolatedKeyFrame.Interpolate(_str, _currentFrame.LayerIdx, _controller.TimelineEditor.SelectedFrameIndex);
		}
	}
}
